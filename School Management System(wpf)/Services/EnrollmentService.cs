using System;
using System.Collections.Generic;
using System.Linq;
using School_Management_System.Data;
using School_Management_System.Interfaces;
using School_Management_System.Models;

namespace School_Management_System.Services
{
    internal class EnrollmentService : IEnrollmentService
    {
        private readonly EnrollmentStateMachineService _stateMachine = new();
        private readonly PermissionBoundaryService _permissionBoundary = new();
        private readonly GovernedOperationLogService _operationLogService = new();

        public IEnumerable<Enrollment> GetAll()
        {
            using var db = new AppDbContext();
            StructuralSchemaService.EnsureApplied(db);
            return db.Enrollments
                .OrderByDescending(x => x.EnrolledAt)
                .ThenByDescending(x => x.Id)
                .ToList();
        }

        public Enrollment? GetById(long id)
        {
            using var db = new AppDbContext();
            return db.Enrollments.Find(id);
        }

        public OperationResult<EnrollmentValidationSummary> BuildValidationSummary(EnrollmentDraft draft, long? existingEnrollmentId = null)
        {
            if (draft == null)
            {
                return OperationResult<EnrollmentValidationSummary>.Fail("Enrollment draft is required.");
            }

            using var db = new AppDbContext();
            var schoolYear = db.SchoolYears.Find(draft.SchoolYearId);
            var student = db.Students.Find(draft.StudentId);
            var section = db.Sections.Find(draft.SectionId);
            var curriculum = db.Curricula.Find(draft.CurriculumId);

            if (schoolYear == null || student == null || section == null || curriculum == null)
            {
                return OperationResult<EnrollmentValidationSummary>.Fail("School year, student, section, and curriculum must exist before enrollment can proceed.");
            }

            if (schoolYear.IsArchived)
            {
                return OperationResult<EnrollmentValidationSummary>.Fail("Selected school year is archived.");
            }

            if (section.IsArchived)
            {
                return OperationResult<EnrollmentValidationSummary>.Fail("Selected section is archived and cannot accept enrollments.");
            }

            if (section.SchoolYearId != draft.SchoolYearId)
            {
                return OperationResult<EnrollmentValidationSummary>.Fail("Selected section does not belong to the selected school year.");
            }

            if (student.Status != UserStatus.ACTIVE)
            {
                return OperationResult<EnrollmentValidationSummary>.Fail("Selected student is inactive and cannot be enrolled.");
            }

            var summary = new EnrollmentValidationSummary
            {
                SchoolYearId = draft.SchoolYearId,
                StudentId = draft.StudentId,
                SectionId = draft.SectionId,
                SchoolYearName = schoolYear.Name,
                StudentName = $"{student.LastName}, {student.FirstName}",
                SectionName = section.Name
            };

            var window = ValidateEnrollmentWindow(db, draft.SchoolYearId);
            summary.SchoolYearOpen = window.Success;
            if (window.Success)
            {
                summary.Messages.Add("Enrollment window: OPEN");
            }
            else
            {
                summary.Messages.Add($"ERROR: {window.Message}");
            }

            summary.DuplicateEnrollmentExists = db.Enrollments.Any(x =>
                x.SchoolYearId == draft.SchoolYearId &&
                x.StudentId == draft.StudentId &&
                (!existingEnrollmentId.HasValue || x.Id != existingEnrollmentId.Value));
            summary.Messages.Add(summary.DuplicateEnrollmentExists
                ? "ERROR: Duplicate enrollment found for the same student and school year."
                : "Duplicate enrollment check: PASSED");

            summary.RequirementsComplete = HasCompleteRequirements(db, draft.StudentId);
            summary.Messages.Add(summary.RequirementsComplete
                ? "Requirement completion check: PASSED"
                : "Requirements are incomplete. Final approval will be blocked until completion.");

            summary.CurrentSectionEnrolled = CountEnrolledInSection(db, draft.SchoolYearId, draft.SectionId, existingEnrollmentId);
            summary.SectionCapacity = section.Capacity;
            summary.SectionHasCapacity = !section.Capacity.HasValue || summary.CurrentSectionEnrolled < section.Capacity.Value;
            if (!summary.SectionHasCapacity)
            {
                summary.WaitlistPosition = ResolveNextWaitlistPosition(db, draft.SchoolYearId, draft.SectionId, existingEnrollmentId);
                summary.Messages.Add($"Section is full. Student will be waitlisted (position #{summary.WaitlistPosition ?? 1}).");
            }
            else
            {
                summary.Messages.Add("Section capacity check: AVAILABLE");
            }

            summary.EnrollmentType = NormalizeEnrollmentType(draft.EnrollmentType, db, draft.StudentId);
            summary.SuggestedStatus = summary.SectionHasCapacity ? EnrollmentStatus.PENDING : EnrollmentStatus.RESERVED;
            summary.Messages.Add($"Suggested initial status: {summary.SuggestedStatus}");

            return OperationResult<EnrollmentValidationSummary>.Ok(summary);
        }

        public OperationResult<Enrollment> SubmitEnrollmentRequest(EnrollmentDraft draft, long? existingEnrollmentId = null)
        {
            var policyKey = existingEnrollmentId.HasValue ? PolicyActionKey.ENROLLMENT_TRANSFER : PolicyActionKey.ENROLLMENT_SUBMIT;
            using var correlationScope = CorrelationContext.BeginScopeIfMissing();
            _permissionBoundary.EnsureAllowed(policyKey);
            var correlationId = CorrelationContext.Ensure();
            _operationLogService.Log(
                policyKey,
                existingEnrollmentId.HasValue ? "ENROLLMENT_TRANSFER_START" : "ENROLLMENT_SUBMIT_START",
                "enrollments",
                existingEnrollmentId,
                GovernedOperationStatus.STARTED,
                "Enrollment submit/transfer started.",
                payload: new { existingEnrollmentId, draft?.StudentId, draft?.SchoolYearId },
                correlationId: correlationId);

            if (draft == null)
            {
                _operationLogService.Log(
                    policyKey,
                    "ENROLLMENT_SUBMIT_BLOCKED",
                    "enrollments",
                    existingEnrollmentId,
                    GovernedOperationStatus.BLOCKED,
                    "Enrollment draft is required.",
                    correlationId: correlationId);
                return OperationResult<Enrollment>.Fail("Enrollment draft is required.");
            }

            var summaryResult = BuildValidationSummary(draft, existingEnrollmentId);
            if (!summaryResult.Success || summaryResult.Data == null)
            {
                _operationLogService.Log(
                    policyKey,
                    "ENROLLMENT_SUBMIT_BLOCKED",
                    "enrollments",
                    existingEnrollmentId,
                    GovernedOperationStatus.BLOCKED,
                    summaryResult.Message,
                    payload: summaryResult.Errors,
                    correlationId: correlationId);
                return OperationResult<Enrollment>.Fail(summaryResult.Message, summaryResult.Errors);
            }

            var summary = summaryResult.Data;
            if (!summary.CanSubmit)
            {
                _operationLogService.Log(
                    policyKey,
                    "ENROLLMENT_SUBMIT_BLOCKED",
                    "enrollments",
                    existingEnrollmentId,
                    GovernedOperationStatus.BLOCKED,
                    "Enrollment cannot be submitted.",
                    payload: summary.Messages,
                    correlationId: correlationId);
                return OperationResult<Enrollment>.Fail("Enrollment cannot be submitted.", summary.Messages);
            }

            using var db = new AppDbContext();
            StructuralSchemaService.EnsureApplied(db);
            var section = db.Sections.Find(draft.SectionId);
            if (section == null)
            {
                _operationLogService.Log(
                    policyKey,
                    "ENROLLMENT_SUBMIT_BLOCKED",
                    "enrollments",
                    existingEnrollmentId,
                    GovernedOperationStatus.BLOCKED,
                    "Section was not found.",
                    correlationId: correlationId);
                return OperationResult<Enrollment>.Fail("Section was not found.");
            }

            var now = DateTime.UtcNow;
            Enrollment enrollment;
            if (existingEnrollmentId.HasValue)
            {
                enrollment = db.Enrollments.Find(existingEnrollmentId.Value) ?? new Enrollment();
                if (enrollment.Id == 0)
                {
                    _operationLogService.Log(
                        policyKey,
                        "ENROLLMENT_SUBMIT_BLOCKED",
                        "enrollments",
                        existingEnrollmentId,
                        GovernedOperationStatus.BLOCKED,
                        "Enrollment record was not found.",
                        correlationId: correlationId);
                    return OperationResult<Enrollment>.Fail("Enrollment record was not found.");
                }
            }
            else
            {
                enrollment = new Enrollment
                {
                    CreatedAt = now,
                    EnrolledAt = now
                };
                db.Enrollments.Add(enrollment);
            }

            enrollment.SchoolYearId = draft.SchoolYearId;
            enrollment.StudentId = draft.StudentId;
            enrollment.SectionId = draft.SectionId;
            enrollment.GradeLevelId = section.GradeLevelId;
            enrollment.CurriculumId = draft.CurriculumId;
            enrollment.EnrollmentType = summary.EnrollmentType;
            var previousStatus = existingEnrollmentId.HasValue ? enrollment.Status : (EnrollmentStatus?)null;
            var previousApprovalStatus = existingEnrollmentId.HasValue ? enrollment.ApprovalStatus : (EnrollmentApprovalStatus?)null;
            var nextApprovalStatus = EnrollmentApprovalStatus.PENDING;
            var transitionValidation = _stateMachine.ValidateTransition(previousStatus, summary.SuggestedStatus);
            if (!transitionValidation.Success)
            {
                _operationLogService.Log(
                    policyKey,
                    "ENROLLMENT_SUBMIT_BLOCKED",
                    "enrollments",
                    existingEnrollmentId,
                    GovernedOperationStatus.BLOCKED,
                    transitionValidation.Message,
                    payload: transitionValidation.Errors,
                    correlationId: correlationId);
                return OperationResult<Enrollment>.Fail(transitionValidation.Message, transitionValidation.Errors);
            }

            enrollment.Status = summary.SuggestedStatus;
            enrollment.ApprovalStatus = nextApprovalStatus;
            enrollment.WaitlistPosition = summary.SuggestedStatus == EnrollmentStatus.RESERVED ? summary.WaitlistPosition : null;
            enrollment.ApprovedByUserId = null;
            enrollment.ApprovedAt = null;
            enrollment.Notes = string.IsNullOrWhiteSpace(draft.Notes) ? null : draft.Notes.Trim();
            enrollment.UpdatedAt = now;
            if (enrollment.CreatedAt == default)
            {
                enrollment.CreatedAt = now;
            }
            if (enrollment.EnrolledAt == default)
            {
                enrollment.EnrolledAt = now;
            }

            db.SaveChanges();

            var transitionResult = _stateMachine.RecordTransition(
                db,
                enrollment.Id,
                previousStatus,
                enrollment.Status,
                previousApprovalStatus,
                enrollment.ApprovalStatus,
                existingEnrollmentId.HasValue ? EnrollmentTransitionTrigger.TRANSFER_UPDATE : EnrollmentTransitionTrigger.SUBMIT_REQUEST,
                reasonCode: existingEnrollmentId.HasValue ? "TRANSFER_UPDATE" : "SUBMIT_REQUEST",
                reasonText: existingEnrollmentId.HasValue
                    ? "Enrollment transfer/update request submitted."
                    : "Enrollment request submitted.");
            if (!transitionResult.Success)
            {
                _operationLogService.Log(
                    policyKey,
                    "ENROLLMENT_SUBMIT_FAILED",
                    "enrollments",
                    enrollment.Id,
                    GovernedOperationStatus.FAILED,
                    transitionResult.Message,
                    payload: transitionResult.Errors,
                    correlationId: correlationId);
                return OperationResult<Enrollment>.Fail(transitionResult.Message, transitionResult.Errors);
            }

            SyncClassStudentLinks(db, enrollment);
            if (enrollment.Status == EnrollmentStatus.RESERVED)
            {
                ResequenceWaitlist(db, enrollment.SchoolYearId, enrollment.SectionId);
                db.SaveChanges();
            }

            _operationLogService.Log(
                policyKey,
                existingEnrollmentId.HasValue ? "ENROLLMENT_TRANSFER_SUCCESS" : "ENROLLMENT_SUBMIT_SUCCESS",
                "enrollments",
                enrollment.Id,
                GovernedOperationStatus.SUCCEEDED,
                $"Enrollment submitted with status {enrollment.Status}.",
                payload: new { enrollment.Status, enrollment.ApprovalStatus, enrollment.WaitlistPosition },
                correlationId: correlationId);
            return OperationResult<Enrollment>.Ok(enrollment, $"Enrollment submitted with status {enrollment.Status}.");
        }

        public OperationResult<Enrollment> ApproveEnrollment(long enrollmentId)
        {
            using var correlationScope = CorrelationContext.BeginScopeIfMissing();
            _permissionBoundary.EnsureAllowed(PolicyActionKey.ENROLLMENT_APPROVE);
            var correlationId = CorrelationContext.Ensure();
            _operationLogService.Log(
                PolicyActionKey.ENROLLMENT_APPROVE,
                "ENROLLMENT_APPROVE_START",
                "enrollments",
                enrollmentId,
                GovernedOperationStatus.STARTED,
                "Enrollment approval started.",
                correlationId: correlationId);

            using var db = new AppDbContext();
            StructuralSchemaService.EnsureApplied(db);
            var enrollment = db.Enrollments.Find(enrollmentId);
            if (enrollment == null)
            {
                _operationLogService.Log(
                    PolicyActionKey.ENROLLMENT_APPROVE,
                    "ENROLLMENT_APPROVE_BLOCKED",
                    "enrollments",
                    enrollmentId,
                    GovernedOperationStatus.BLOCKED,
                    "Enrollment record was not found.",
                    correlationId: correlationId);
                return OperationResult<Enrollment>.Fail("Enrollment record was not found.");
            }

            if (enrollment.Status is EnrollmentStatus.CANCELLED or EnrollmentStatus.DROPPED or EnrollmentStatus.COMPLETED or EnrollmentStatus.TRANSFERRED_OUT)
            {
                _operationLogService.Log(
                    PolicyActionKey.ENROLLMENT_APPROVE,
                    "ENROLLMENT_APPROVE_BLOCKED",
                    "enrollments",
                    enrollmentId,
                    GovernedOperationStatus.BLOCKED,
                    $"Enrollment cannot be approved because it is {enrollment.Status}.",
                    correlationId: correlationId);
                return OperationResult<Enrollment>.Fail($"Enrollment cannot be approved because it is {enrollment.Status}.");
            }

            var windowResult = ValidateEnrollmentWindow(db, enrollment.SchoolYearId);
            if (!windowResult.Success)
            {
                _operationLogService.Log(
                    PolicyActionKey.ENROLLMENT_APPROVE,
                    "ENROLLMENT_APPROVE_BLOCKED",
                    "enrollments",
                    enrollmentId,
                    GovernedOperationStatus.BLOCKED,
                    windowResult.Message,
                    correlationId: correlationId);
                return OperationResult<Enrollment>.Fail(windowResult.Message);
            }

            if (!HasCompleteRequirements(db, enrollment.StudentId))
            {
                _operationLogService.Log(
                    PolicyActionKey.ENROLLMENT_APPROVE,
                    "ENROLLMENT_APPROVE_BLOCKED",
                    "enrollments",
                    enrollmentId,
                    GovernedOperationStatus.BLOCKED,
                    "Enrollment cannot be approved until all required documents are submitted.",
                    correlationId: correlationId);
                return OperationResult<Enrollment>.Fail("Enrollment cannot be approved until all required documents are submitted.");
            }

            var student = db.Students.Find(enrollment.StudentId);
            if (student == null || student.Status != UserStatus.ACTIVE)
            {
                _operationLogService.Log(
                    PolicyActionKey.ENROLLMENT_APPROVE,
                    "ENROLLMENT_APPROVE_BLOCKED",
                    "enrollments",
                    enrollmentId,
                    GovernedOperationStatus.BLOCKED,
                    "Enrollment cannot be approved because the student is inactive.",
                    correlationId: correlationId);
                return OperationResult<Enrollment>.Fail("Enrollment cannot be approved because the student is inactive.");
            }

            var previousStatus = enrollment.Status;
            var previousApprovalStatus = enrollment.ApprovalStatus;
            var capacityAvailable = HasSectionCapacity(db, enrollment.SchoolYearId, enrollment.SectionId, enrollment.Id);
            var nextStatus = capacityAvailable ? EnrollmentStatus.ENROLLED : EnrollmentStatus.RESERVED;
            var transitionValidation = _stateMachine.ValidateTransition(previousStatus, nextStatus);
            if (!transitionValidation.Success)
            {
                _operationLogService.Log(
                    PolicyActionKey.ENROLLMENT_APPROVE,
                    "ENROLLMENT_APPROVE_BLOCKED",
                    "enrollments",
                    enrollmentId,
                    GovernedOperationStatus.BLOCKED,
                    transitionValidation.Message,
                    correlationId: correlationId);
                return OperationResult<Enrollment>.Fail(transitionValidation.Message, transitionValidation.Errors);
            }

            enrollment.Status = nextStatus;
            enrollment.WaitlistPosition = enrollment.Status == EnrollmentStatus.RESERVED
                ? ResolveNextWaitlistPosition(db, enrollment.SchoolYearId, enrollment.SectionId, enrollment.Id)
                : null;
            enrollment.ApprovalStatus = EnrollmentApprovalStatus.APPROVED;
            enrollment.ApprovedByUserId = SessionContext.CurrentUser?.Id;
            enrollment.ApprovedAt = DateTime.UtcNow;
            enrollment.UpdatedAt = DateTime.UtcNow;

            db.SaveChanges();
            var transitionResult = _stateMachine.RecordTransition(
                db,
                enrollment.Id,
                previousStatus,
                enrollment.Status,
                previousApprovalStatus,
                enrollment.ApprovalStatus,
                EnrollmentTransitionTrigger.APPROVE,
                reasonCode: "APPROVE",
                reasonText: "Enrollment approval decision.");
            if (!transitionResult.Success)
            {
                _operationLogService.Log(
                    PolicyActionKey.ENROLLMENT_APPROVE,
                    "ENROLLMENT_APPROVE_FAILED",
                    "enrollments",
                    enrollmentId,
                    GovernedOperationStatus.FAILED,
                    transitionResult.Message,
                    payload: transitionResult.Errors,
                    correlationId: correlationId);
                return OperationResult<Enrollment>.Fail(transitionResult.Message, transitionResult.Errors);
            }

            SyncClassStudentLinks(db, enrollment);
            if (enrollment.Status == EnrollmentStatus.RESERVED)
            {
                ResequenceWaitlist(db, enrollment.SchoolYearId, enrollment.SectionId);
                db.SaveChanges();
            }

            _operationLogService.Log(
                PolicyActionKey.ENROLLMENT_APPROVE,
                "ENROLLMENT_APPROVE_SUCCESS",
                "enrollments",
                enrollmentId,
                GovernedOperationStatus.SUCCEEDED,
                enrollment.Status == EnrollmentStatus.ENROLLED
                    ? "Enrollment approved and finalized."
                    : "Enrollment approved but placed on waitlist due to full section.",
                payload: new { enrollment.Status, enrollment.ApprovalStatus, enrollment.WaitlistPosition },
                correlationId: correlationId);
            return OperationResult<Enrollment>.Ok(
                enrollment,
                enrollment.Status == EnrollmentStatus.ENROLLED
                    ? "Enrollment approved and finalized."
                    : "Enrollment approved but placed on waitlist due to full section.");
        }

        public OperationResult<int> PromoteWaitlist(long schoolYearId, long sectionId)
        {
            using var correlationScope = CorrelationContext.BeginScopeIfMissing();
            _permissionBoundary.EnsureAllowed(PolicyActionKey.ENROLLMENT_PROMOTE_WAITLIST);
            var correlationId = CorrelationContext.Ensure();
            _operationLogService.Log(
                PolicyActionKey.ENROLLMENT_PROMOTE_WAITLIST,
                "ENROLLMENT_PROMOTE_WAITLIST_START",
                "enrollments",
                null,
                GovernedOperationStatus.STARTED,
                "Waitlist promotion started.",
                payload: new { schoolYearId, sectionId },
                correlationId: correlationId);

            using var db = new AppDbContext();
            StructuralSchemaService.EnsureApplied(db);
            var promoted = PromoteWaitlistInternal(db, schoolYearId, sectionId);
            db.SaveChanges();
            _operationLogService.Log(
                PolicyActionKey.ENROLLMENT_PROMOTE_WAITLIST,
                "ENROLLMENT_PROMOTE_WAITLIST_SUCCESS",
                "enrollments",
                null,
                GovernedOperationStatus.SUCCEEDED,
                promoted == 0
                    ? "No eligible waitlisted enrollment was promoted."
                    : $"{promoted} waitlisted enrollment(s) promoted.",
                payload: new { schoolYearId, sectionId, promoted },
                correlationId: correlationId);
            return OperationResult<int>.Ok(promoted, promoted == 0
                ? "No eligible waitlisted enrollment was promoted."
                : $"{promoted} waitlisted enrollment(s) promoted.");
        }

        public OperationResult<Enrollment> ReturnForCorrection(long enrollmentId)
        {
            using var correlationScope = CorrelationContext.BeginScopeIfMissing();
            _permissionBoundary.EnsureAllowed(PolicyActionKey.ENROLLMENT_RETURN_FOR_CORRECTION);
            var correlationId = CorrelationContext.Ensure();
            _operationLogService.Log(
                PolicyActionKey.ENROLLMENT_RETURN_FOR_CORRECTION,
                "ENROLLMENT_RETURN_FOR_CORRECTION_START",
                "enrollments",
                enrollmentId,
                GovernedOperationStatus.STARTED,
                "Return-for-correction started.",
                correlationId: correlationId);

            using var db = new AppDbContext();
            StructuralSchemaService.EnsureApplied(db);
            var enrollment = db.Enrollments.Find(enrollmentId);
            if (enrollment == null)
            {
                _operationLogService.Log(
                    PolicyActionKey.ENROLLMENT_RETURN_FOR_CORRECTION,
                    "ENROLLMENT_RETURN_FOR_CORRECTION_BLOCKED",
                    "enrollments",
                    enrollmentId,
                    GovernedOperationStatus.BLOCKED,
                    "Enrollment record was not found.",
                    correlationId: correlationId);
                return OperationResult<Enrollment>.Fail("Enrollment record was not found.");
            }

            if (enrollment.Status is EnrollmentStatus.CANCELLED or EnrollmentStatus.DROPPED or EnrollmentStatus.COMPLETED or EnrollmentStatus.TRANSFERRED_OUT)
            {
                _operationLogService.Log(
                    PolicyActionKey.ENROLLMENT_RETURN_FOR_CORRECTION,
                    "ENROLLMENT_RETURN_FOR_CORRECTION_BLOCKED",
                    "enrollments",
                    enrollmentId,
                    GovernedOperationStatus.BLOCKED,
                    $"Enrollment cannot be returned for correction because it is {enrollment.Status}.",
                    correlationId: correlationId);
                return OperationResult<Enrollment>.Fail($"Enrollment cannot be returned for correction because it is {enrollment.Status}.");
            }

            var previousStatus = enrollment.Status;
            var previousApprovalStatus = enrollment.ApprovalStatus;
            var transitionValidation = _stateMachine.ValidateTransition(previousStatus, EnrollmentStatus.PENDING);
            if (!transitionValidation.Success)
            {
                _operationLogService.Log(
                    PolicyActionKey.ENROLLMENT_RETURN_FOR_CORRECTION,
                    "ENROLLMENT_RETURN_FOR_CORRECTION_BLOCKED",
                    "enrollments",
                    enrollmentId,
                    GovernedOperationStatus.BLOCKED,
                    transitionValidation.Message,
                    correlationId: correlationId);
                return OperationResult<Enrollment>.Fail(transitionValidation.Message, transitionValidation.Errors);
            }

            enrollment.Status = EnrollmentStatus.PENDING;
            enrollment.ApprovalStatus = EnrollmentApprovalStatus.PENDING;
            enrollment.WaitlistPosition = null;
            enrollment.ApprovedByUserId = null;
            enrollment.ApprovedAt = null;
            enrollment.UpdatedAt = DateTime.UtcNow;

            db.SaveChanges();
            var transitionResult = _stateMachine.RecordTransition(
                db,
                enrollment.Id,
                previousStatus,
                enrollment.Status,
                previousApprovalStatus,
                enrollment.ApprovalStatus,
                EnrollmentTransitionTrigger.RETURN_FOR_CORRECTION,
                reasonCode: "RETURN_FOR_CORRECTION",
                reasonText: "Enrollment returned for correction.");
            if (!transitionResult.Success)
            {
                _operationLogService.Log(
                    PolicyActionKey.ENROLLMENT_RETURN_FOR_CORRECTION,
                    "ENROLLMENT_RETURN_FOR_CORRECTION_FAILED",
                    "enrollments",
                    enrollmentId,
                    GovernedOperationStatus.FAILED,
                    transitionResult.Message,
                    payload: transitionResult.Errors,
                    correlationId: correlationId);
                return OperationResult<Enrollment>.Fail(transitionResult.Message, transitionResult.Errors);
            }

            SyncClassStudentLinks(db, enrollment);

            if (previousStatus == EnrollmentStatus.ENROLLED)
            {
                PromoteWaitlistInternal(db, enrollment.SchoolYearId, enrollment.SectionId);
            }

            ResequenceWaitlist(db, enrollment.SchoolYearId, enrollment.SectionId);
            db.SaveChanges();
            _operationLogService.Log(
                PolicyActionKey.ENROLLMENT_RETURN_FOR_CORRECTION,
                "ENROLLMENT_RETURN_FOR_CORRECTION_SUCCESS",
                "enrollments",
                enrollmentId,
                GovernedOperationStatus.SUCCEEDED,
                "Enrollment returned for correction.",
                correlationId: correlationId);
            return OperationResult<Enrollment>.Ok(enrollment, "Enrollment returned for correction.");
        }

        public OperationResult<Enrollment> SetStatus(long enrollmentId, EnrollmentStatus nextStatus)
        {
            using var correlationScope = CorrelationContext.BeginScopeIfMissing();
            _permissionBoundary.EnsureAllowed(PolicyActionKey.ENROLLMENT_SET_STATUS);
            var correlationId = CorrelationContext.Ensure();
            _operationLogService.Log(
                PolicyActionKey.ENROLLMENT_SET_STATUS,
                "ENROLLMENT_SET_STATUS_START",
                "enrollments",
                enrollmentId,
                GovernedOperationStatus.STARTED,
                $"Enrollment status update requested: {nextStatus}.",
                correlationId: correlationId);

            using var db = new AppDbContext();
            StructuralSchemaService.EnsureApplied(db);
            var enrollment = db.Enrollments.Find(enrollmentId);
            if (enrollment == null)
            {
                _operationLogService.Log(
                    PolicyActionKey.ENROLLMENT_SET_STATUS,
                    "ENROLLMENT_SET_STATUS_BLOCKED",
                    "enrollments",
                    enrollmentId,
                    GovernedOperationStatus.BLOCKED,
                    "Enrollment record was not found.",
                    correlationId: correlationId);
                return OperationResult<Enrollment>.Fail("Enrollment record was not found.");
            }

            if (StatusRequiresEnrollmentWindow(nextStatus))
            {
                var window = ValidateEnrollmentWindow(db, enrollment.SchoolYearId);
                if (!window.Success)
                {
                    _operationLogService.Log(
                        PolicyActionKey.ENROLLMENT_SET_STATUS,
                        "ENROLLMENT_SET_STATUS_BLOCKED",
                        "enrollments",
                        enrollmentId,
                        GovernedOperationStatus.BLOCKED,
                        window.Message,
                        correlationId: correlationId);
                    return OperationResult<Enrollment>.Fail(window.Message);
                }
            }

            var previousStatus = enrollment.Status;
            var previousApprovalStatus = enrollment.ApprovalStatus;
            var transitionValidation = _stateMachine.ValidateTransition(previousStatus, nextStatus);
            if (!transitionValidation.Success)
            {
                _operationLogService.Log(
                    PolicyActionKey.ENROLLMENT_SET_STATUS,
                    "ENROLLMENT_SET_STATUS_BLOCKED",
                    "enrollments",
                    enrollmentId,
                    GovernedOperationStatus.BLOCKED,
                    transitionValidation.Message,
                    correlationId: correlationId);
                return OperationResult<Enrollment>.Fail(transitionValidation.Message, transitionValidation.Errors);
            }

            enrollment.Status = nextStatus;
            enrollment.UpdatedAt = DateTime.UtcNow;

            if (nextStatus is EnrollmentStatus.CANCELLED or EnrollmentStatus.DROPPED)
            {
                enrollment.ApprovalStatus = EnrollmentApprovalStatus.REJECTED;
                enrollment.WaitlistPosition = null;
            }
            else if (nextStatus == EnrollmentStatus.ENROLLED)
            {
                enrollment.ApprovalStatus = EnrollmentApprovalStatus.APPROVED;
                if (!enrollment.ApprovedAt.HasValue)
                {
                    enrollment.ApprovedAt = DateTime.UtcNow;
                }
                if (!enrollment.ApprovedByUserId.HasValue)
                {
                    enrollment.ApprovedByUserId = SessionContext.CurrentUser?.Id;
                }
                enrollment.WaitlistPosition = null;
            }
            else if (nextStatus == EnrollmentStatus.RESERVED)
            {
                enrollment.WaitlistPosition = ResolveNextWaitlistPosition(db, enrollment.SchoolYearId, enrollment.SectionId, enrollment.Id);
            }

            db.SaveChanges();
            var transitionResult = _stateMachine.RecordTransition(
                db,
                enrollment.Id,
                previousStatus,
                enrollment.Status,
                previousApprovalStatus,
                enrollment.ApprovalStatus,
                EnrollmentTransitionTrigger.SET_STATUS,
                reasonCode: "SET_STATUS",
                reasonText: "Enrollment status updated.");
            if (!transitionResult.Success)
            {
                _operationLogService.Log(
                    PolicyActionKey.ENROLLMENT_SET_STATUS,
                    "ENROLLMENT_SET_STATUS_FAILED",
                    "enrollments",
                    enrollmentId,
                    GovernedOperationStatus.FAILED,
                    transitionResult.Message,
                    payload: transitionResult.Errors,
                    correlationId: correlationId);
                return OperationResult<Enrollment>.Fail(transitionResult.Message, transitionResult.Errors);
            }

            SyncClassStudentLinks(db, enrollment);

            if (previousStatus == EnrollmentStatus.ENROLLED && nextStatus != EnrollmentStatus.ENROLLED)
            {
                PromoteWaitlistInternal(db, enrollment.SchoolYearId, enrollment.SectionId);
            }
            ResequenceWaitlist(db, enrollment.SchoolYearId, enrollment.SectionId);
            db.SaveChanges();

            _operationLogService.Log(
                PolicyActionKey.ENROLLMENT_SET_STATUS,
                "ENROLLMENT_SET_STATUS_SUCCESS",
                "enrollments",
                enrollmentId,
                GovernedOperationStatus.SUCCEEDED,
                $"Enrollment status updated to {enrollment.Status}.",
                payload: new { enrollment.Status, enrollment.ApprovalStatus, enrollment.WaitlistPosition },
                correlationId: correlationId);
            return OperationResult<Enrollment>.Ok(enrollment, $"Enrollment status updated to {enrollment.Status}.");
        }

        public void Create(Enrollment entity)
        {
            using var db = new AppDbContext();
            StructuralSchemaService.EnsureApplied(db);
            ValidateLegacyEnrollment(db, entity, null);
            var transitionValidation = _stateMachine.ValidateTransition(null, entity.Status);
            if (!transitionValidation.Success)
            {
                throw new DomainValidationException(transitionValidation.Message, transitionValidation.Errors);
            }

            db.Enrollments.Add(entity);
            db.SaveChanges();
            var transitionResult = _stateMachine.RecordTransition(
                db,
                entity.Id,
                null,
                entity.Status,
                null,
                entity.ApprovalStatus,
                EnrollmentTransitionTrigger.SYSTEM_SYNC,
                reasonCode: "CREATE",
                reasonText: "Enrollment created through service endpoint.");
            if (!transitionResult.Success)
            {
                throw new DomainValidationException(transitionResult.Message, transitionResult.Errors);
            }

            SyncClassStudentLinks(db, entity);
            db.SaveChanges();
        }

        public void Update(Enrollment entity)
        {
            using var db = new AppDbContext();
            StructuralSchemaService.EnsureApplied(db);
            var existing = db.Enrollments.Find(entity.Id);
            if (existing == null)
            {
                throw new DomainValidationException("Enrollment record was not found.");
            }

            var previousStatus = existing.Status;
            var previousApprovalStatus = existing.ApprovalStatus;

            existing.SchoolYearId = entity.SchoolYearId;
            existing.StudentId = entity.StudentId;
            existing.GradeLevelId = entity.GradeLevelId;
            existing.SectionId = entity.SectionId;
            existing.CurriculumId = entity.CurriculumId;
            existing.EnrollmentType = entity.EnrollmentType;
            existing.WaitlistPosition = entity.WaitlistPosition;
            existing.ApprovedByUserId = entity.ApprovedByUserId;
            existing.ApprovedAt = entity.ApprovedAt;
            existing.Notes = entity.Notes;
            existing.EnrolledAt = entity.EnrolledAt;

            existing.Status = entity.Status;
            existing.ApprovalStatus = entity.ApprovalStatus;
            ValidateLegacyEnrollment(db, existing, existing.Id);

            var transitionResult = _stateMachine.RecordTransition(
                db,
                existing.Id,
                previousStatus,
                entity.Status,
                previousApprovalStatus,
                entity.ApprovalStatus,
                EnrollmentTransitionTrigger.SYSTEM_SYNC,
                reasonCode: "UPDATE",
                reasonText: "Enrollment updated through service endpoint.");
            if (!transitionResult.Success)
            {
                throw new DomainValidationException(transitionResult.Message, transitionResult.Errors);
            }
            db.SaveChanges();
            SyncClassStudentLinks(db, existing);
            db.SaveChanges();
        }

        public void Delete(long id)
        {
            using var db = new AppDbContext();
            var existing = db.Enrollments.Find(id);
            if (existing == null)
            {
                return;
            }

            db.ClassStudents.RemoveRange(db.ClassStudents.Where(x => x.EnrollmentId == id));
            db.Enrollments.Remove(existing);
            db.SaveChanges();
        }

        private static void ValidateLegacyEnrollment(AppDbContext db, Enrollment entity, long? excludeId)
        {
            if (entity == null)
            {
                throw new DomainValidationException("Enrollment data is required.");
            }

            if (entity.SchoolYearId <= 0 || entity.StudentId <= 0 || entity.SectionId <= 0 || entity.CurriculumId <= 0)
            {
                throw new DomainValidationException("School year, student, section, and curriculum are required.");
            }

            var section = db.Sections.Find(entity.SectionId);
            if (section == null)
            {
                throw new DomainValidationException("Selected section does not exist.");
            }

            if (section.IsArchived)
            {
                throw new DomainValidationException("Selected section is archived.");
            }

            if (section.SchoolYearId != entity.SchoolYearId)
            {
                throw new DomainValidationException("Section and school year mismatch.");
            }

            var schoolYear = db.SchoolYears.Find(entity.SchoolYearId);
            if (schoolYear == null)
            {
                throw new DomainValidationException("Selected school year does not exist.");
            }

            if (schoolYear.IsArchived)
            {
                throw new DomainValidationException("Selected school year is archived.");
            }

            if (!db.Students.Any(x => x.Id == entity.StudentId))
            {
                throw new DomainValidationException("Selected student does not exist.");
            }

            var student = db.Students.Find(entity.StudentId);
            if (student == null || student.Status != UserStatus.ACTIVE)
            {
                throw new DomainValidationException("Selected student must be active before enrollment can proceed.");
            }

            if (!db.Curricula.Any(x => x.Id == entity.CurriculumId))
            {
                throw new DomainValidationException("Selected curriculum does not exist.");
            }

            if (db.Enrollments.Any(x =>
                    x.SchoolYearId == entity.SchoolYearId &&
                    x.StudentId == entity.StudentId &&
                    (!excludeId.HasValue || x.Id != excludeId.Value)))
            {
                throw new DomainValidationException("Duplicate enrollment is not allowed for the same student and school year.");
            }

            if (StatusRequiresEnrollmentWindow(entity.Status))
            {
                var window = ValidateEnrollmentWindow(db, entity.SchoolYearId);
                if (!window.Success)
                {
                    throw new DomainValidationException(window.Message);
                }
            }

            entity.GradeLevelId = section.GradeLevelId;
            entity.EnrollmentType = NormalizeEnrollmentType(entity.EnrollmentType, db, entity.StudentId);
            entity.WaitlistPosition = entity.Status == EnrollmentStatus.RESERVED
                ? ResolveNextWaitlistPosition(db, entity.SchoolYearId, entity.SectionId, excludeId)
                : null;

            if (entity.Status == EnrollmentStatus.ENROLLED && !HasSectionCapacity(db, entity.SchoolYearId, entity.SectionId, excludeId))
            {
                throw new DomainValidationException("Section is already at full capacity.");
            }

            if (entity.Status == EnrollmentStatus.ENROLLED && entity.ApprovalStatus == EnrollmentApprovalStatus.PENDING)
            {
                entity.ApprovalStatus = EnrollmentApprovalStatus.APPROVED;
                entity.ApprovedByUserId ??= SessionContext.CurrentUser?.Id;
                entity.ApprovedAt ??= DateTime.UtcNow;
            }

            var now = DateTime.UtcNow;
            if (entity.CreatedAt == default)
            {
                entity.CreatedAt = now;
            }
            if (entity.EnrolledAt == default)
            {
                entity.EnrolledAt = now;
            }
            entity.UpdatedAt = now;
        }

        private static OperationResult ValidateEnrollmentWindow(AppDbContext db, long schoolYearId)
        {
            var schoolYear = db.SchoolYears.Find(schoolYearId);
            if (schoolYear == null)
            {
                return OperationResult.Fail("Selected school year was not found.");
            }

            if (schoolYear.IsArchived)
            {
                return OperationResult.Fail($"Enrollment is closed for {schoolYear.Name} because the school year is archived.");
            }

            if (schoolYear.Status != SchoolYearStatus.ACTIVE)
            {
                return OperationResult.Fail($"Enrollment is closed for {schoolYear.Name} because its status is {schoolYear.Status}.");
            }

            var settings = db.SchoolSettings.OrderByDescending(x => x.Id).FirstOrDefault();
            if (settings == null && !schoolYear.EnrollmentOpenDate.HasValue && !schoolYear.EnrollmentCloseDate.HasValue)
            {
                return OperationResult.Ok();
            }

            var today = DateTime.Today;
            var openDate = schoolYear.EnrollmentOpenDate?.Date ?? settings?.EnrollmentOpenDate?.Date;
            var closeDate = schoolYear.EnrollmentCloseDate?.Date ?? settings?.EnrollmentCloseDate?.Date;

            if (openDate.HasValue && closeDate.HasValue && openDate.Value > closeDate.Value)
            {
                return OperationResult.Fail("Enrollment period configuration is invalid: open date is later than close date.");
            }

            if (openDate.HasValue && today < openDate.Value)
            {
                return OperationResult.Fail($"Enrollment opens on {openDate.Value:yyyy-MM-dd}.");
            }
            if (closeDate.HasValue && today > closeDate.Value)
            {
                return OperationResult.Fail($"Enrollment closed on {closeDate.Value:yyyy-MM-dd}.");
            }

            return OperationResult.Ok();
        }

        private static string NormalizeEnrollmentType(string? enrollmentType, AppDbContext db, long studentId)
        {
            var normalized = (enrollmentType ?? string.Empty).Trim().ToUpperInvariant();
            if (normalized is "NEW" or "RETURNING" or "TRANSFEREE")
            {
                return normalized;
            }

            return ResolveEnrollmentType(db, studentId);
        }

        private static string ResolveEnrollmentType(AppDbContext db, long studentId)
        {
            var hasPastEnrollment = db.Enrollments.Any(x => x.StudentId == studentId);
            if (hasPastEnrollment)
            {
                return "RETURNING";
            }

            var student = db.Students.Find(studentId);
            if (student != null && !string.IsNullOrWhiteSpace(student.PreviousSchool))
            {
                return "TRANSFEREE";
            }

            return "NEW";
        }

        private static bool HasCompleteRequirements(AppDbContext db, long studentId)
        {
            var requirements = db.StudentRequirements
                .Where(x => x.StudentId == studentId)
                .ToList();
            return requirements.Count > 0 && requirements.All(x => x.IsSubmitted);
        }

        private static int CountEnrolledInSection(AppDbContext db, long schoolYearId, long sectionId, long? excludeEnrollmentId = null)
        {
            return db.Enrollments.Count(x =>
                x.SchoolYearId == schoolYearId &&
                x.SectionId == sectionId &&
                x.Status == EnrollmentStatus.ENROLLED &&
                (!excludeEnrollmentId.HasValue || x.Id != excludeEnrollmentId.Value));
        }

        private static bool HasSectionCapacity(AppDbContext db, long schoolYearId, long sectionId, long? excludeEnrollmentId = null)
        {
            var section = db.Sections.Find(sectionId);
            if (section == null || !section.Capacity.HasValue)
            {
                return true;
            }

            return CountEnrolledInSection(db, schoolYearId, sectionId, excludeEnrollmentId) < section.Capacity.Value;
        }

        private static int ResolveNextWaitlistPosition(AppDbContext db, long schoolYearId, long sectionId, long? excludeEnrollmentId = null)
        {
            var maxPosition = db.Enrollments
                .Where(x =>
                    x.SchoolYearId == schoolYearId &&
                    x.SectionId == sectionId &&
                    x.Status == EnrollmentStatus.RESERVED &&
                    (!excludeEnrollmentId.HasValue || x.Id != excludeEnrollmentId.Value))
                .Max(x => (int?)x.WaitlistPosition) ?? 0;
            return maxPosition + 1;
        }

        private static void ResequenceWaitlist(AppDbContext db, long schoolYearId, long sectionId)
        {
            var queue = db.Enrollments
                .Where(x =>
                    x.SchoolYearId == schoolYearId &&
                    x.SectionId == sectionId &&
                    x.Status == EnrollmentStatus.RESERVED)
                .OrderBy(x => x.WaitlistPosition ?? int.MaxValue)
                .ThenBy(x => x.EnrolledAt)
                .ThenBy(x => x.Id)
                .ToList();

            var position = 1;
            foreach (var enrollment in queue)
            {
                enrollment.WaitlistPosition = position++;
                enrollment.UpdatedAt = DateTime.UtcNow;
            }
        }

        private static bool StatusRequiresEnrollmentWindow(EnrollmentStatus status)
        {
            return status is EnrollmentStatus.PENDING or EnrollmentStatus.ENROLLED or EnrollmentStatus.RESERVED;
        }

        private static void SyncClassStudentLinks(AppDbContext db, Enrollment enrollment)
        {
            var links = db.ClassStudents
                .Where(x => x.EnrollmentId == enrollment.Id)
                .ToList();

            if (enrollment.Status != EnrollmentStatus.ENROLLED)
            {
                if (links.Count > 0)
                {
                    db.ClassStudents.RemoveRange(links);
                }
                return;
            }

            var now = DateTime.UtcNow;
            var offeringIds = db.ClassOfferings
                .Where(x => x.SchoolYearId == enrollment.SchoolYearId && x.SectionId == enrollment.SectionId)
                .Select(x => x.Id)
                .ToList();
            var linkedOfferingIds = links.Select(x => x.ClassOfferingId).ToHashSet();

            foreach (var link in links.Where(x => !offeringIds.Contains(x.ClassOfferingId)))
            {
                db.ClassStudents.Remove(link);
            }

            foreach (var offeringId in offeringIds)
            {
                if (linkedOfferingIds.Contains(offeringId))
                {
                    continue;
                }

                db.ClassStudents.Add(new ClassStudent
                {
                    ClassOfferingId = offeringId,
                    StudentId = enrollment.StudentId,
                    EnrollmentId = enrollment.Id,
                    Status = ClassStudentStatus.ACTIVE,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
        }

        private int PromoteWaitlistInternal(AppDbContext db, long schoolYearId, long sectionId)
        {
            var section = db.Sections.Find(sectionId);
            if (section == null)
            {
                return 0;
            }

            var currentEnrolled = CountEnrolledInSection(db, schoolYearId, sectionId, null);
            var availableSeats = section.Capacity.HasValue
                ? Math.Max(0, section.Capacity.Value - currentEnrolled)
                : int.MaxValue;
            if (availableSeats == 0)
            {
                return 0;
            }

            var candidates = db.Enrollments
                .Where(x =>
                    x.SchoolYearId == schoolYearId &&
                    x.SectionId == sectionId &&
                    x.Status == EnrollmentStatus.RESERVED &&
                    x.ApprovalStatus == EnrollmentApprovalStatus.APPROVED)
                .OrderBy(x => x.WaitlistPosition ?? int.MaxValue)
                .ThenBy(x => x.EnrolledAt)
                .ThenBy(x => x.Id)
                .ToList();

            var promoted = 0;
            foreach (var candidate in candidates)
            {
                if (promoted >= availableSeats)
                {
                    break;
                }

                if (!HasCompleteRequirements(db, candidate.StudentId))
                {
                    continue;
                }

                var student = db.Students.Find(candidate.StudentId);
                if (student == null || student.Status != UserStatus.ACTIVE)
                {
                    continue;
                }

                var transitionValidation = _stateMachine.ValidateTransition(candidate.Status, EnrollmentStatus.ENROLLED);
                if (!transitionValidation.Success)
                {
                    continue;
                }

                var previousStatus = candidate.Status;
                var previousApprovalStatus = candidate.ApprovalStatus;
                candidate.Status = EnrollmentStatus.ENROLLED;
                candidate.WaitlistPosition = null;
                candidate.UpdatedAt = DateTime.UtcNow;
                SyncClassStudentLinks(db, candidate);
                _ = _stateMachine.RecordTransition(
                    db,
                    candidate.Id,
                    previousStatus,
                    candidate.Status,
                    previousApprovalStatus,
                    candidate.ApprovalStatus,
                    EnrollmentTransitionTrigger.PROMOTE_WAITLIST,
                    reasonCode: "PROMOTE_WAITLIST",
                    reasonText: "Reserved enrollment promoted from waitlist.");
                promoted++;
            }

            ResequenceWaitlist(db, schoolYearId, sectionId);
            return promoted;
        }
    }
}
