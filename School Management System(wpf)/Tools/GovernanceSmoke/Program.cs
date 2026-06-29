using Microsoft.EntityFrameworkCore;
using School_Management_System.Data;
using School_Management_System.Models;
using School_Management_System.Services;

var checks = new List<(string Name, bool Passed, string Detail)>();
var smokeDbName = $"sms_governance_smoke_{Guid.NewGuid():N}";

AppDbContext.TestOptionsConfigurator = options =>
{
    options.UseInMemoryDatabase(smokeDbName);
};

try
{
    Seed();

    var permission = new PermissionBoundaryService();
    var superAdmin = new User
    {
        Id = 900,
        Username = "superadmin",
        Role = UserRole.SUPERADMIN,
        Status = UserStatus.ACTIVE,
        CanLogin = true
    };
    SessionContext.CurrentUser = superAdmin;
    checks.Add(("Permission.SuperAdminAllow", permission.IsAllowed(PolicyActionKey.ENROLLMENT_APPROVE, superAdmin), "SUPERADMIN should be allowed."));

    var teacher = new User
    {
        Id = 901,
        Username = "teacher",
        Role = UserRole.TEACHER,
        Status = UserStatus.ACTIVE,
        CanLogin = true
    };
    checks.Add(("Permission.TeacherDenied", !permission.IsAllowed(PolicyActionKey.ENROLLMENT_APPROVE, teacher), "TEACHER should be denied."));

    var enrollmentService = new EnrollmentService();
    var submitResult = enrollmentService.SubmitEnrollmentRequest(new EnrollmentDraft
    {
        SchoolYearId = 1,
        StudentId = 2,
        SectionId = 1,
        CurriculumId = 1,
        EnrollmentType = "NEW"
    });

    checks.Add(("Enrollment.Submit", submitResult.Success && submitResult.Data != null, submitResult.Message));

    if (submitResult.Success && submitResult.Data != null)
    {
        using (var capacityDb = new AppDbContext())
        {
            var section = capacityDb.Sections.Single(x => x.Id == 1);
            section.Capacity = 2;
            capacityDb.SaveChanges();
        }

        var approveResult = enrollmentService.ApproveEnrollment(submitResult.Data.Id);
        checks.Add(("Enrollment.Approve", approveResult.Success && approveResult.Data?.Status == EnrollmentStatus.ENROLLED, approveResult.Message));

        var forbidden = enrollmentService.SetStatus(submitResult.Data.Id, EnrollmentStatus.CANCELLED);
        checks.Add(("Enrollment.ForbiddenTransition", !forbidden.Success, forbidden.Message));

        using var db = new AppDbContext();
        var transitionCount = db.EnrollmentStateTransitions.Count(x => x.EnrollmentId == submitResult.Data.Id);
        checks.Add(("Enrollment.TransitionHistory", transitionCount >= 2, $"Transition count: {transitionCount}"));
    }
    else
    {
        checks.Add(("Enrollment.Approve", false, "Submit failed; approval skipped."));
        checks.Add(("Enrollment.ForbiddenTransition", false, "Submit failed; forbidden transition check skipped."));
        checks.Add(("Enrollment.TransitionHistory", false, "Submit failed; transition history check skipped."));
    }

    var exceptionQueue = new ExceptionQueueService();
    var exceptionRequest = new ExceptionQueueCreateRequest
    {
        Category = "SMOKE",
        SourceModule = "GovernanceSmoke",
        Entity = "enrollments",
        EntityId = 1,
        Severity = ExceptionQueueSeverity.WARNING,
        Summary = "Smoke dedupe case.",
        Details = "First raise."
    };
    var first = exceptionQueue.Raise(exceptionRequest);
    var second = exceptionQueue.Raise(exceptionRequest);
    checks.Add(("ExceptionQueue.Dedupe", first.Id == second.Id && second.OccurrenceCount >= 2, $"ItemId={second.Id}, OccurrenceCount={second.OccurrenceCount}"));

    var restoreService = new BackupRestoreService();
    var restorePreflight = restoreService.EvaluateRestorePreflight(new RestoreRequest
    {
        RestoreFilePath = "C:\\_missing_file.sql"
    });
    checks.Add(("Restore.PreflightBlock", !restorePreflight.Success, restorePreflight.Summary));
    checks.Add(("Restore.PreflightChecks", restorePreflight.Checks.Count > 0, $"Check count: {restorePreflight.Checks.Count}"));

    var metrics = new OperationalMetricsDashboardService().BuildSnapshot(DateTime.UtcNow);
    var metricsOkay =
        metrics.All.Count == 4 &&
        metrics.All.All(x => int.TryParse(x.Value, out _));
    checks.Add(("Metrics.Snapshot", metricsOkay, string.Join(" | ", metrics.All.Select(x => $"{x.Title}={x.Value}"))));
}
catch (Exception ex)
{
    checks.Add(("Smoke.UnhandledException", false, ex.ToString()));
}
finally
{
    SessionContext.Clear();
    AppDbContext.TestOptionsConfigurator = null;
}

var failures = checks.Where(x => !x.Passed).ToList();
foreach (var check in checks)
{
    Console.WriteLine($"{(check.Passed ? "PASS" : "FAIL")} {check.Name} :: {check.Detail}");
}

Console.WriteLine();
Console.WriteLine($"Summary: {checks.Count - failures.Count}/{checks.Count} checks passed.");
Environment.ExitCode = failures.Count == 0 ? 0 : 1;

static void Seed()
{
    using var db = new AppDbContext();
    var now = DateTime.UtcNow;

    db.Users.Add(new User
    {
        Id = 1,
        Username = "admin",
        PasswordHash = "hash",
        Role = UserRole.SUPERADMIN,
        CanLogin = true,
        Status = UserStatus.ACTIVE,
        CreatedAt = now,
        UpdatedAt = now
    });

    db.Users.Add(new User
    {
        Id = 2,
        Username = "student_user",
        PasswordHash = "hash",
        Role = UserRole.STUDENT,
        CanLogin = true,
        Status = UserStatus.ACTIVE,
        CreatedAt = now,
        UpdatedAt = now
    });

    db.GradeLevels.Add(new GradeLevel
    {
        Id = 1,
        Code = "G1",
        Name = "Grade 1",
        CreatedAt = now,
        UpdatedAt = now
    });

    db.Curricula.Add(new Curriculum
    {
        Id = 1,
        Name = "Core",
        IsActive = true,
        CreatedAt = now,
        UpdatedAt = now
    });

    db.SchoolYears.Add(new SchoolYear
    {
        Id = 1,
        Name = "2026-2027",
        Status = SchoolYearStatus.ACTIVE,
        IsArchived = false,
        EnrollmentOpenDate = now.Date.AddDays(-30),
        EnrollmentCloseDate = now.Date.AddDays(30),
        CreatedAt = now,
        UpdatedAt = now
    });

    db.Sections.Add(new Section
    {
        Id = 1,
        SchoolYearId = 1,
        GradeLevelId = 1,
        Name = "Aster",
        Capacity = 1,
        IsArchived = false,
        CreatedAt = now,
        UpdatedAt = now
    });

    db.Students.Add(new Student
    {
        Id = 2,
        UserId = 2,
        Lrn = "123456789012",
        StudentNumber = "S-000001",
        FirstName = "Test",
        LastName = "Student",
        Status = UserStatus.ACTIVE,
        CreatedAt = now,
        UpdatedAt = now
    });

    db.Users.Add(new User
    {
        Id = 3,
        Username = "existing_student_user",
        PasswordHash = "hash",
        Role = UserRole.STUDENT,
        CanLogin = true,
        Status = UserStatus.ACTIVE,
        CreatedAt = now,
        UpdatedAt = now
    });

    db.Students.Add(new Student
    {
        Id = 3,
        UserId = 3,
        Lrn = "123456789013",
        StudentNumber = "S-000002",
        FirstName = "Existing",
        LastName = "Student",
        Status = UserStatus.ACTIVE,
        CreatedAt = now,
        UpdatedAt = now
    });

    db.StudentRequirements.Add(new StudentRequirement
    {
        Id = 1,
        StudentId = 2,
        RequirementName = "Birth Certificate",
        IsSubmitted = true,
        SubmittedAt = now,
        CreatedAt = now,
        UpdatedAt = now
    });

    db.StudentRequirements.Add(new StudentRequirement
    {
        Id = 2,
        StudentId = 3,
        RequirementName = "Birth Certificate",
        IsSubmitted = true,
        SubmittedAt = now,
        CreatedAt = now,
        UpdatedAt = now
    });

    db.Enrollments.Add(new Enrollment
    {
        Id = 1,
        SchoolYearId = 1,
        StudentId = 3,
        GradeLevelId = 1,
        SectionId = 1,
        CurriculumId = 1,
        Status = EnrollmentStatus.ENROLLED,
        ApprovalStatus = EnrollmentApprovalStatus.APPROVED,
        EnrollmentType = "NEW",
        ApprovedByUserId = 1,
        ApprovedAt = now,
        EnrolledAt = now.AddMinutes(-5),
        CreatedAt = now.AddMinutes(-5),
        UpdatedAt = now.AddMinutes(-5)
    });

    db.SaveChanges();
}
