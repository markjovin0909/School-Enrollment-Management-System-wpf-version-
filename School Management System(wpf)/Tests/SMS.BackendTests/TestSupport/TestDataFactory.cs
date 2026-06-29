using System.Threading;
using School_Management_System.Data;
using School_Management_System.Models;
using School_Management_System.Services;

namespace SMS.BackendTests.TestSupport;

public sealed class TestDataFactory
{
    public const long AdminUserId = 1;
    public const long StudentUserId = 2;
    public const long StudentUser2Id = 3;
    public const long TeacherUserId = 4;
    public const long TeacherUser2Id = 5;
    public const long FreeStudentUserId = 6;
    public const long FreeTeacherUserId = 7;
    public const long GradeLevelId = 1;
    public const long GradeLevel2Id = 2;
    public const long CurriculumId = 1;
    public const long SubjectId = 1;
    public const long Subject2Id = 2;
    public const long Subject3Id = 3;
    public const long SchoolYearId = 1;
    public const long SectionId = 1;
    public const long TeacherId = 1;
    public const long Teacher2Id = 2;
    public const long RoomId = 1;
    public const long TimeSlotId = 1;
    public const long GradingPeriodId = 1;
    public const long GradeComponentId = 1;
    public const long StudentId = 1;
    public const long Student2Id = 2;
    public const long EnrollmentId = 1;
    public const long ClassOfferingId = 1;
    public const long AttendanceSessionId = 1;
    public const long AssessmentId = 1;

    private int _counter;

    internal void SeedCoreData(AppDbContext db)
    {
        if (db.Users.Any())
        {
            return;
        }

        var now = DateTime.UtcNow;
        var passwordHash = PasswordHasher.HashPassword("ValidPass123!");

        db.Users.AddRange(
            new User { Id = AdminUserId, Username = "superadmin", PasswordHash = passwordHash, Role = UserRole.SUPERADMIN, CanLogin = true, Status = UserStatus.ACTIVE, CreatedAt = now, UpdatedAt = now },
            new User { Id = StudentUserId, Username = "student-001", PasswordHash = passwordHash, Role = UserRole.STUDENT, CanLogin = false, Status = UserStatus.ACTIVE, CreatedAt = now, UpdatedAt = now },
            new User { Id = StudentUser2Id, Username = "student-002", PasswordHash = passwordHash, Role = UserRole.STUDENT, CanLogin = false, Status = UserStatus.ACTIVE, CreatedAt = now, UpdatedAt = now },
            new User { Id = TeacherUserId, Username = "teacher-001", PasswordHash = passwordHash, Role = UserRole.TEACHER, CanLogin = false, Status = UserStatus.ACTIVE, CreatedAt = now, UpdatedAt = now },
            new User { Id = TeacherUser2Id, Username = "teacher-002", PasswordHash = passwordHash, Role = UserRole.TEACHER, CanLogin = false, Status = UserStatus.ACTIVE, CreatedAt = now, UpdatedAt = now },
            new User { Id = FreeStudentUserId, Username = "student-free", PasswordHash = passwordHash, Role = UserRole.STUDENT, CanLogin = false, Status = UserStatus.ACTIVE, CreatedAt = now, UpdatedAt = now },
            new User { Id = FreeTeacherUserId, Username = "teacher-free", PasswordHash = passwordHash, Role = UserRole.TEACHER, CanLogin = false, Status = UserStatus.ACTIVE, CreatedAt = now, UpdatedAt = now });

        db.GradeLevels.AddRange(
            new GradeLevel { Id = GradeLevelId, Code = "G1", Name = "Grade 1", CreatedAt = now, UpdatedAt = now },
            new GradeLevel { Id = GradeLevel2Id, Code = "G2", Name = "Grade 2", CreatedAt = now, UpdatedAt = now });

        db.Curricula.Add(new Curriculum
        {
            Id = CurriculumId,
            Name = "Core Curriculum",
            Description = "Core",
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        });

        db.SchoolSettings.Add(new SchoolSetting
        {
            Id = 1,
            SchoolName = "SMS Academy",
            SchoolCode = "SMS",
            SchoolAddress = "Test Address",
            PrincipalName = "Test Principal",
            GradingSetup = "Quarterly",
            EnrollmentConfiguration = "Open",
            EnrollmentOpenDate = now.Date.AddDays(-30),
            EnrollmentCloseDate = now.Date.AddDays(30),
            StudentNumberPrefix = "S",
            NextStudentNumber = 10,
            DefaultSectionCapacity = 30,
            DefaultGradeLevelIds = "1,2",
            CreatedAt = now,
            UpdatedAt = now
        });

        db.SchoolYears.Add(new SchoolYear
        {
            Id = SchoolYearId,
            Name = "2026-2027",
            StartDate = new DateTime(2026, 6, 1),
            EndDate = new DateTime(2027, 3, 31),
            EnrollmentOpenDate = now.Date.AddDays(-30),
            EnrollmentCloseDate = now.Date.AddDays(30),
            Status = SchoolYearStatus.ACTIVE,
            IsArchived = false,
            CreatedAt = now,
            UpdatedAt = now
        });

        db.Subjects.AddRange(
            new Subject { Id = SubjectId, Code = "ENG-1", Title = "English 1", GradeLevelId = GradeLevelId, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new Subject { Id = Subject2Id, Code = "MTH-1", Title = "Math 1", GradeLevelId = GradeLevelId, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new Subject { Id = Subject3Id, Code = "SCI-1", Title = "Science 1", GradeLevelId = GradeLevelId, IsActive = true, CreatedAt = now, UpdatedAt = now });

        db.CurriculumSubjects.AddRange(
            new CurriculumSubject { Id = 1, CurriculumId = CurriculumId, GradeLevelId = GradeLevelId, SubjectId = SubjectId, IsRequired = true, SortOrder = 1, CreatedAt = now, UpdatedAt = now },
            new CurriculumSubject { Id = 2, CurriculumId = CurriculumId, GradeLevelId = GradeLevelId, SubjectId = Subject2Id, IsRequired = true, SortOrder = 2, CreatedAt = now, UpdatedAt = now });

        db.Teachers.AddRange(
            new Teacher { Id = TeacherId, UserId = TeacherUserId, EmployeeNo = "EMP-001", FirstName = "Alice", LastName = "Teacher", Status = UserStatus.ACTIVE, CreatedAt = now, UpdatedAt = now },
            new Teacher { Id = Teacher2Id, UserId = TeacherUser2Id, EmployeeNo = "EMP-002", FirstName = "Bob", LastName = "Teacher", Status = UserStatus.ACTIVE, CreatedAt = now, UpdatedAt = now });

        db.Sections.Add(new Section
        {
            Id = SectionId,
            SchoolYearId = SchoolYearId,
            GradeLevelId = GradeLevelId,
            Name = "Aster",
            Capacity = 30,
            AdviserTeacherId = TeacherId,
            IsArchived = false,
            CreatedAt = now,
            UpdatedAt = now
        });

        db.Rooms.Add(new Room
        {
            Id = RoomId,
            Code = "R1",
            Name = "Room 1",
            Capacity = 30,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        });

        db.TimeSlots.Add(new TimeSlot
        {
            Id = TimeSlotId,
            Code = "AM1",
            Name = "Morning 1",
            StartTime = new TimeSpan(8, 0, 0),
            EndTime = new TimeSpan(9, 0, 0),
            IsBellPeriod = true,
            SortOrder = 1,
            CreatedAt = now,
            UpdatedAt = now
        });

        db.Set<GradingPeriod>().Add(new GradingPeriod
        {
            Id = GradingPeriodId,
            SchoolYearId = SchoolYearId,
            Name = "First Quarter",
            StartDate = now.Date.AddDays(-10),
            EndDate = now.Date.AddDays(20),
            Status = GradingPeriodStatus.OPEN,
            CreatedAt = now,
            UpdatedAt = now
        });

        db.Set<GradeComponent>().Add(new GradeComponent
        {
            Id = GradeComponentId,
            Name = GradeComponentName.WRITTEN_WORKS,
            Weight = 0.3m,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        });

        db.Students.AddRange(
            new Student { Id = StudentId, UserId = StudentUserId, Lrn = "100000000001", StudentNumber = "S-000001", FirstName = "Jane", LastName = "Student", Birthdate = new DateTime(2012, 1, 1), Status = UserStatus.ACTIVE, CreatedAt = now, UpdatedAt = now },
            new Student { Id = Student2Id, UserId = StudentUser2Id, Lrn = "100000000002", StudentNumber = "S-000002", FirstName = "John", LastName = "Student", Birthdate = new DateTime(2012, 2, 2), Status = UserStatus.ACTIVE, CreatedAt = now, UpdatedAt = now });

        db.StudentRequirements.Add(new StudentRequirement
        {
            Id = 1,
            StudentId = StudentId,
            RequirementName = "Birth Certificate",
            IsSubmitted = true,
            SubmittedAt = now,
            Notes = "[STATUS:VERIFIED] Verified",
            CreatedAt = now,
            UpdatedAt = now
        });

        db.ClassOfferings.Add(new ClassOffering
        {
            Id = ClassOfferingId,
            SchoolYearId = SchoolYearId,
            SectionId = SectionId,
            SubjectId = SubjectId,
            TeacherId = TeacherId,
            CurriculumId = CurriculumId,
            Status = ClassOfferingStatus.ACTIVE,
            Room = "Room 1",
            CreatedAt = now,
            UpdatedAt = now
        });

        db.Enrollments.Add(new Enrollment
        {
            Id = EnrollmentId,
            SchoolYearId = SchoolYearId,
            StudentId = StudentId,
            GradeLevelId = GradeLevelId,
            SectionId = SectionId,
            CurriculumId = CurriculumId,
            Status = EnrollmentStatus.ENROLLED,
            ApprovalStatus = EnrollmentApprovalStatus.APPROVED,
            EnrollmentType = "NEW",
            ApprovedByUserId = AdminUserId,
            ApprovedAt = now,
            EnrolledAt = now,
            CreatedAt = now,
            UpdatedAt = now
        });

        db.ClassStudents.Add(new ClassStudent
        {
            Id = 1,
            ClassOfferingId = ClassOfferingId,
            StudentId = StudentId,
            EnrollmentId = EnrollmentId,
            Status = ClassStudentStatus.ACTIVE,
            CreatedAt = now,
            UpdatedAt = now
        });

        db.ClassSchedules.Add(new ClassSchedule
        {
            Id = 1,
            ClassOfferingId = ClassOfferingId,
            RoomId = RoomId,
            TimeSlotId = TimeSlotId,
            DayOfWeek = 1,
            StartTime = new TimeSpan(8, 0, 0),
            EndTime = new TimeSpan(9, 0, 0),
            CreatedAt = now,
            UpdatedAt = now
        });

        db.Set<AttendanceSession>().Add(new AttendanceSession
        {
            Id = AttendanceSessionId,
            ClassOfferingId = ClassOfferingId,
            SessionDate = now.Date,
            CreatedAt = now,
            UpdatedAt = now
        });

        db.Set<AttendanceRecord>().Add(new AttendanceRecord
        {
            Id = 1,
            AttendanceSessionId = AttendanceSessionId,
            StudentId = StudentId,
            MarkedByUserId = AdminUserId,
            Status = AttendanceStatus.PRESENT,
            CreatedAt = now,
            UpdatedAt = now
        });

        db.Set<Assessment>().Add(new Assessment
        {
            Id = AssessmentId,
            ClassOfferingId = ClassOfferingId,
            GradingPeriodId = GradingPeriodId,
            ComponentId = GradeComponentId,
            Title = "Quiz 1",
            MaxScore = 50,
            DateGiven = now.Date,
            CreatedAt = now,
            UpdatedAt = now
        });

        db.Set<AssessmentScore>().Add(new AssessmentScore
        {
            Id = 1,
            AssessmentId = AssessmentId,
            StudentId = StudentId,
            Score = 45,
            CreatedAt = now,
            UpdatedAt = now
        });

        db.Set<StudentGrade>().Add(new StudentGrade
        {
            Id = 1,
            ClassOfferingId = ClassOfferingId,
            GradingPeriodId = GradingPeriodId,
            StudentId = StudentId,
            QuarterGrade = 90,
            CreatedAt = now,
            UpdatedAt = now
        });

        db.Set<Announcement>().Add(new Announcement
        {
            Id = 1,
            PostedByUserId = AdminUserId,
            Title = "Welcome",
            Body = "Hello",
            AudienceType = AnnouncementAudienceType.ALL,
            PostedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        });

        db.AuditLogs.Add(new AuditLog
        {
            Id = 1,
            UserId = AdminUserId,
            Action = "SEED",
            Entity = "system",
            EntityId = 1,
            Payload = "{}",
            CreatedAt = now
        });

        db.SaveChanges();
    }

    public User CreateUser() => new()
    {
        Username = $"user-{Next()}",
        PasswordHash = PasswordHasher.HashPassword("ValidPass123!"),
        Role = UserRole.STUDENT,
        CanLogin = true,
        Status = UserStatus.ACTIVE
    };

    public Teacher CreateTeacher(long userId = FreeTeacherUserId) => new()
    {
        UserId = userId,
        EmployeeNo = $"EMP-{Next()}",
        FirstName = "New",
        LastName = $"Teacher{Next()}",
        Status = UserStatus.ACTIVE
    };

    public Student CreateStudent(long userId = FreeStudentUserId) => new()
    {
        UserId = userId,
        Lrn = $"9000000000{Next():D2}",
        StudentNumber = $"SX-{Next():D6}",
        FirstName = "New",
        LastName = $"Student{Next()}",
        Birthdate = new DateTime(2013, 1, 1),
        Status = UserStatus.ACTIVE
    };

    public SchoolYear CreateSchoolYear() => new()
    {
        Name = $"2027-2028-{Next()}",
        StartDate = new DateTime(2027, 6, 1),
        EndDate = new DateTime(2028, 3, 31),
        EnrollmentOpenDate = new DateTime(2027, 6, 1),
        EnrollmentCloseDate = new DateTime(2027, 7, 31),
        Status = SchoolYearStatus.CLOSED,
        IsArchived = false
    };

    public Section CreateSection() => new()
    {
        SchoolYearId = SchoolYearId,
        GradeLevelId = GradeLevelId,
        Name = $"Section-{Next()}",
        Capacity = 25,
        AdviserTeacherId = Teacher2Id,
        IsArchived = false
    };

    public Curriculum CreateCurriculum() => new()
    {
        Name = $"Curriculum-{Next()}",
        Description = "Test curriculum",
        IsActive = true
    };

    public CurriculumSubject CreateCurriculumSubject() => new()
    {
        CurriculumId = CurriculumId,
        GradeLevelId = GradeLevelId,
        SubjectId = Subject3Id,
        Semester = 1,
        IsRequired = true,
        SortOrder = Next()
    };

    public GradeLevel CreateGradeLevel() => new()
    {
        Code = $"GX{Next()}",
        Name = $"Grade X {Next()}"
    };

    public GradeComponent CreateGradeComponent() => new()
    {
        Name = GradeComponentName.PERFORMANCE_TASKS,
        Weight = 0.4m,
        IsActive = true
    };

    public GradingPeriod CreateGradingPeriod() => new()
    {
        SchoolYearId = SchoolYearId,
        Name = $"Quarter {Next()}",
        StartDate = DateTime.UtcNow.Date,
        EndDate = DateTime.UtcNow.Date.AddDays(30),
        Status = GradingPeriodStatus.OPEN
    };

    public Subject CreateSubject() => new()
    {
        Code = $"SUB-{Next()}",
        Title = $"Subject {Next()}",
        GradeLevelId = GradeLevelId,
        IsActive = true
    };

    public Room CreateRoom() => new()
    {
        Code = $"RM-{Next()}",
        Name = $"Room {Next()}",
        Capacity = 35,
        IsActive = true
    };

    public TimeSlot CreateTimeSlot() => new()
    {
        Code = $"TS-{Next()}",
        Name = $"Slot {Next()}",
        StartTime = new TimeSpan(9, 0, 0),
        EndTime = new TimeSpan(10, 0, 0),
        IsBellPeriod = true,
        SortOrder = Next()
    };

    public ClassOffering CreateClassOffering() => new()
    {
        SchoolYearId = SchoolYearId,
        SectionId = SectionId,
        SubjectId = Subject2Id,
        TeacherId = TeacherId,
        CurriculumId = CurriculumId,
        Status = ClassOfferingStatus.ACTIVE,
        Room = "Room 2"
    };

    public ClassSchedule CreateClassSchedule() => new()
    {
        ClassOfferingId = ClassOfferingId,
        RoomId = RoomId,
        TimeSlotId = TimeSlotId,
        DayOfWeek = 2,
        StartTime = new TimeSpan(10, 0, 0),
        EndTime = new TimeSpan(11, 0, 0)
    };

    public ClassStudent CreateClassStudent() => new()
    {
        ClassOfferingId = ClassOfferingId,
        StudentId = StudentId,
        EnrollmentId = EnrollmentId,
        Status = ClassStudentStatus.ACTIVE
    };

    public AttendanceSession CreateAttendanceSession() => new()
    {
        ClassOfferingId = ClassOfferingId,
        SessionDate = DateTime.UtcNow.Date.AddDays(1)
    };

    public AttendanceRecord CreateAttendanceRecord() => new()
    {
        AttendanceSessionId = AttendanceSessionId,
        StudentId = StudentId,
        MarkedByUserId = AdminUserId,
        Status = AttendanceStatus.LATE,
        Reason = "Traffic"
    };

    public Assessment CreateAssessment() => new()
    {
        ClassOfferingId = ClassOfferingId,
        GradingPeriodId = GradingPeriodId,
        ComponentId = GradeComponentId,
        Title = $"Assessment {Next()}",
        MaxScore = 25,
        DateGiven = DateTime.UtcNow.Date
    };

    public AssessmentScore CreateAssessmentScore() => new()
    {
        AssessmentId = AssessmentId,
        StudentId = StudentId,
        Score = 20
    };

    public StudentGrade CreateStudentGrade() => new()
    {
        ClassOfferingId = ClassOfferingId,
        GradingPeriodId = GradingPeriodId,
        StudentId = StudentId,
        QuarterGrade = 88
    };

    public StudentRequirement CreateStudentRequirement() => new()
    {
        StudentId = StudentId,
        RequirementName = $"Requirement {Next()}",
        IsSubmitted = false
    };

    public SchoolSetting CreateSchoolSetting() => new()
    {
        SchoolName = $"School {Next()}",
        SchoolCode = $"SC{Next()}",
        SchoolAddress = "Address",
        PrincipalName = "Principal",
        GradingSetup = "Quarterly",
        EnrollmentConfiguration = "Open",
        StudentNumberPrefix = "S",
        NextStudentNumber = 1,
        DefaultSectionCapacity = 30
    };

    public AuditLog CreateAuditLog() => new()
    {
        UserId = AdminUserId,
        Action = $"ACTION_{Next()}",
        Entity = "entity",
        EntityId = Next(),
        Payload = "{}",
        CreatedAt = DateTime.UtcNow
    };

    public ArchiveRecord CreateArchiveRecord() => new()
    {
        EntityType = "Student",
        OriginalEntityId = 999,
        Payload = "{}",
        DeletedAt = DateTime.UtcNow,
        Notes = "HARD_DELETE"
    };

    public Announcement CreateAnnouncement() => new()
    {
        PostedByUserId = AdminUserId,
        Title = $"Announcement {Next()}",
        Body = "Body",
        AudienceType = AnnouncementAudienceType.ALL,
        PostedAt = DateTime.UtcNow
    };

    public Enrollment CreateEnrollment(long studentId = Student2Id) => new()
    {
        SchoolYearId = SchoolYearId,
        StudentId = studentId,
        GradeLevelId = GradeLevelId,
        SectionId = SectionId,
        CurriculumId = CurriculumId,
        Status = EnrollmentStatus.PENDING,
        ApprovalStatus = EnrollmentApprovalStatus.PENDING,
        EnrollmentType = "NEW",
        EnrolledAt = DateTime.UtcNow
    };

    private int Next()
    {
        return Interlocked.Increment(ref _counter);
    }
}
