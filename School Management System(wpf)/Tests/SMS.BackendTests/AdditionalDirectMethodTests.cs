using School_Management_System.Data;
using School_Management_System.Models;
using School_Management_System.Services;
using SMS.BackendTests.TestSupport;
using Xunit;

namespace SMS.BackendTests;

public sealed class AdditionalDirectMethodTests : BackendTestBase
{
    [Fact]
    public void Core_Service_Methods_Work_Directly()
    {
        using (var db = CreateDb())
        {
            Factory.SeedCoreData(db);

            db.SchoolYears.Add(new SchoolYear
            {
                Id = 99,
                Name = "Archived SY",
                Status = SchoolYearStatus.CLOSED,
                IsArchived = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            db.Sections.Add(new Section
            {
                Id = 99,
                SchoolYearId = TestDataFactory.SchoolYearId,
                GradeLevelId = TestDataFactory.GradeLevelId,
                Name = "Archived Section",
                Capacity = 20,
                IsArchived = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            db.SaveChanges();
        }

        var accountSecurity = new AccountSecurityService();
        accountSecurity.LogLogout(SessionContext.CurrentUser!, "compat");

        var schoolYearService = new SchoolYearService();
        var active = schoolYearService.GetActiveSchoolYear();
        schoolYearService.Restore(99);

        var sectionService = new SectionService();
        sectionService.Restore(99);

        var settingService = new SchoolSettingService();
        var reserved = settingService.ReserveNextStudentNumber();
        var parsed = SchoolSettingService.ParseGradeLevelIds("5,4,4,3");
        var normalized = SchoolSettingService.NormalizeGradeLevelIds(new long[] { 9, 8, 8 });

        var enrollmentService = new EnrollmentService();
        var summary = enrollmentService.BuildValidationSummary(new EnrollmentDraft
        {
            SchoolYearId = TestDataFactory.SchoolYearId,
            StudentId = TestDataFactory.Student2Id,
            SectionId = TestDataFactory.SectionId,
            CurriculumId = TestDataFactory.CurriculumId,
            EnrollmentType = "NEW"
        });

        Assert.NotNull(active);
        Assert.StartsWith("S-", reserved);
        Assert.Equal(new long[] { 5, 4, 3 }, parsed);
        Assert.Equal("8,9", normalized);
        Assert.True(summary.Success);
    }

    [Fact]
    public void Queue_Sla_And_OperationResult_Methods_Work_Directly()
    {
        var policy = new EnrollmentQueueSlaPolicy(2, 4, new[] { EnrollmentStatus.PENDING });
        var evaluation = new EnrollmentQueueSlaService().Evaluate(new Enrollment
        {
            Status = EnrollmentStatus.PENDING,
            UpdatedAt = DateTime.UtcNow.AddHours(-5)
        }, DateTime.UtcNow, policy);
        var loaded = new EnrollmentQueueSlaService().LoadPolicy();

        var ok = OperationResult.Ok("ok");
        var fail = OperationResult.Fail("fail", new[] { "e1" });
        var genericOk = OperationResult<int>.Ok(5, "great");
        var genericFail = OperationResult<int>.Fail("bad", new[] { "e2" });

        Assert.True(policy.Tracks(EnrollmentStatus.PENDING));
        Assert.Contains("warning", policy.DescribeThresholds(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(EnrollmentQueueSlaSeverity.Critical, evaluation.Severity);
        Assert.NotNull(loaded);
        Assert.True(ok.Success);
        Assert.False(fail.Success);
        Assert.Equal(5, genericOk.Data);
        Assert.False(genericFail.Success);
    }

    [Fact]
    public void Governance_Report_And_Structural_Methods_Work_Directly()
    {
        using (var db = CreateDb())
        {
            Factory.SeedCoreData(db);
            StructuralSchemaService.EnsureApplied(db);
        }

        StructuralSchemaService.EnsureApplied();

        var pass = GovernanceReadinessCheck.Pass("pass");
        var warn = GovernanceReadinessCheck.Warn("warn");
        var fail = GovernanceReadinessCheck.Fail("fail");
        var report = GovernanceReadinessReport.FromChecks(new[] { pass, warn });
        var failedReport = GovernanceReadinessReport.FromChecks(new[] { pass, fail });
        var readiness = new GovernanceReadinessService().Evaluate();

        Assert.True(report.Success);
        Assert.False(failedReport.Success);
        Assert.Contains("PASS", report.ToDisplayText());
        Assert.NotEmpty(readiness.Checks);
    }

    [Fact]
    public void Audit_Validation_And_Enrollment_State_Helpers_Work_Directly()
    {
        using (var db = CreateDb())
        {
            Factory.SeedCoreData(db);
        }

        using (CorrelationContext.BeginScope("REQ-AUDIT"))
        {
            AuditTrailService.Log("APPROVE", "enrollments", TestDataFactory.EnrollmentId, null, new
            {
                ReasonCode = "APPROVE",
                PreviousStatus = "PENDING",
                NewStatus = "ENROLLED"
            });
            AuditTrailService.LogWithActor(TestDataFactory.AdminUserId, "PROMOTE_WAITLIST", "enrollments", null, null, new
            {
                ReasonCode = "PROMOTE",
                SchoolYearId = TestDataFactory.SchoolYearId,
                SectionId = TestDataFactory.SectionId
            });
        }

        using (var db = CreateDb())
        {
            var audits = db.AuditLogs.OrderBy(x => x.Id).ToList();
            Assert.True(audits.Count >= 3);
            Assert.Contains("REQ-AUDIT", audits.Last().Payload);
        }

        var machine = new EnrollmentStateMachineService();
        using (var db = CreateDb())
        {
            var result = machine.RecordTransition(
                db,
                TestDataFactory.EnrollmentId,
                EnrollmentStatus.ENROLLED,
                EnrollmentStatus.RESERVED,
                EnrollmentApprovalStatus.APPROVED,
                EnrollmentApprovalStatus.APPROVED,
                EnrollmentTransitionTrigger.SET_STATUS,
                reasonCode: "QUEUE",
                reasonText: "Moved to queue");

            Assert.True(result.Success);
        }

        var history = machine.GetHistory(TestDataFactory.EnrollmentId);
        var display = new EnrollmentValidationSummary
        {
            SchoolYearName = "2026-2027",
            StudentName = "Student, Jane",
            SectionName = "Aster",
            EnrollmentType = "NEW",
            SuggestedStatus = EnrollmentStatus.ENROLLED,
            SchoolYearOpen = true,
            RequirementsComplete = true,
            SectionHasCapacity = true,
            CurrentSectionEnrolled = 1,
            SectionCapacity = 30
        }.ToDisplayText();

        Assert.NotEmpty(history);
        Assert.Contains("Enrollment Validation Summary", display);
    }

    [Fact]
    public async Task Backup_And_Restore_Async_Entry_Points_Execute_Direct_Failure_Paths()
    {
        var service = new BackupRestoreService();

        var backupNull = await service.BackupAsync(null!);
        var backupMissingFolder = await service.BackupAsync(new BackupRequest { BackupFolder = string.Empty });
        var restoreNull = await service.RestoreAsync(null!);
        var restoreMissingFile = await service.RestoreAsync(new RestoreRequest { RestoreFilePath = "C:\\missing-file.sql" });
        var preflightNull = service.EvaluateRestorePreflight(null!);

        var ok = RestorePreflightResult.Ok("ok", new[] { "warn" }, new[] { PreflightCheckResult.Pass("A", "a") });
        var fail = RestorePreflightResult.Fail("fail", new[] { "block" }, new[] { "warn2" }, new[] { PreflightCheckResult.Block("B", "b") });

        Assert.False(backupNull.Success);
        Assert.False(backupMissingFolder.Success);
        Assert.False(restoreNull.Success);
        Assert.False(restoreMissingFile.Success);
        Assert.False(preflightNull.Success);
        Assert.True(ok.Success);
        Assert.False(fail.Success);
    }
}
