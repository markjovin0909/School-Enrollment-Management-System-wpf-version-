using Microsoft.EntityFrameworkCore;
using School_Management_System.Data;
using School_Management_System.Models;
using School_Management_System.Services;
using Xunit;

namespace SMS.BackendTests;

public sealed class EnrollmentWorkflowTests : IDisposable
{
    private readonly string _dbName = $"sms_backend_tests_{Guid.NewGuid():N}";

    public EnrollmentWorkflowTests()
    {
        AppDbContext.TestOptionsConfigurator = options => options.UseInMemoryDatabase(_dbName);
        SessionContext.CurrentUser = new User
        {
            Id = 900,
            Username = "superadmin",
            Role = UserRole.SUPERADMIN,
            Status = UserStatus.ACTIVE,
            CanLogin = true
        };
    }

    [Fact]
    public void SubmitEnrollmentRequest_AvailableSeat_AutoApprovesAndRecordsInitialTransition()
    {
        Seed(capacity: 2, includeExistingEnrollment: true);

        var service = new EnrollmentService();
        var result = service.SubmitEnrollmentRequest(CreateDraft(studentId: 2));

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(EnrollmentStatus.ENROLLED, result.Data!.Status);
        Assert.Equal(EnrollmentApprovalStatus.APPROVED, result.Data.ApprovalStatus);

        using var db = new AppDbContext();
        var transitions = db.EnrollmentStateTransitions
            .Where(x => x.EnrollmentId == result.Data.Id)
            .OrderBy(x => x.Id)
            .ToList();

        Assert.Single(transitions);
        Assert.Null(transitions[0].PreviousStatus);
        Assert.Equal(EnrollmentStatus.ENROLLED, transitions[0].NewStatus);
        Assert.Equal(EnrollmentTransitionTrigger.SUBMIT_REQUEST, transitions[0].TriggerAction);
    }

    [Fact]
    public void ApproveEnrollment_AlreadyApprovedEnrollment_ReturnsFailure()
    {
        Seed(capacity: 2, includeExistingEnrollment: true);
        var service = new EnrollmentService();
        var submitResult = service.SubmitEnrollmentRequest(CreateDraft(studentId: 2));

        Assert.True(submitResult.Success);
        var approveResult = service.ApproveEnrollment(submitResult.Data!.Id);

        Assert.False(approveResult.Success);
        Assert.Equal("Enrollment is already approved and enrolled.", approveResult.Message);

        using var db = new AppDbContext();
        Assert.Single(db.EnrollmentStateTransitions.Where(x => x.EnrollmentId == submitResult.Data.Id));
    }

    [Fact]
    public void ApproveEnrollment_ReservedEnrollmentWithReleasedCapacity_EnrollsAndRecordsHistory()
    {
        Seed(capacity: 1, includeExistingEnrollment: true);
        var service = new EnrollmentService();
        var submitResult = service.SubmitEnrollmentRequest(CreateDraft(studentId: 2));

        Assert.True(submitResult.Success);
        Assert.NotNull(submitResult.Data);
        Assert.Equal(EnrollmentStatus.RESERVED, submitResult.Data!.Status);
        Assert.Equal(EnrollmentApprovalStatus.PENDING, submitResult.Data.ApprovalStatus);

        using (var db = new AppDbContext())
        {
            var section = db.Sections.Single(x => x.Id == 1);
            section.Capacity = 2;
            db.SaveChanges();
        }

        var approveResult = service.ApproveEnrollment(submitResult.Data.Id);

        Assert.True(approveResult.Success);
        Assert.NotNull(approveResult.Data);
        Assert.Equal(EnrollmentStatus.ENROLLED, approveResult.Data!.Status);
        Assert.Equal(EnrollmentApprovalStatus.APPROVED, approveResult.Data.ApprovalStatus);

        using var verificationDb = new AppDbContext();
        var transitions = verificationDb.EnrollmentStateTransitions
            .Where(x => x.EnrollmentId == submitResult.Data.Id)
            .OrderBy(x => x.Id)
            .ToList();

        Assert.Equal(2, transitions.Count);
        Assert.Equal(EnrollmentStatus.RESERVED, transitions[0].NewStatus);
        Assert.Equal(EnrollmentTransitionTrigger.SUBMIT_REQUEST, transitions[0].TriggerAction);
        Assert.Equal(EnrollmentStatus.RESERVED, transitions[1].PreviousStatus);
        Assert.Equal(EnrollmentStatus.ENROLLED, transitions[1].NewStatus);
        Assert.Equal(EnrollmentTransitionTrigger.APPROVE, transitions[1].TriggerAction);
    }

    public void Dispose()
    {
        SessionContext.Clear();
        AppDbContext.TestOptionsConfigurator = null;
    }

    private static EnrollmentDraft CreateDraft(long studentId)
    {
        return new EnrollmentDraft
        {
            SchoolYearId = 1,
            StudentId = studentId,
            SectionId = 1,
            CurriculumId = 1,
            EnrollmentType = "NEW"
        };
    }

    private static void Seed(int capacity, bool includeExistingEnrollment)
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
            Capacity = capacity,
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

        if (includeExistingEnrollment)
        {
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
        }

        db.SaveChanges();
    }
}
