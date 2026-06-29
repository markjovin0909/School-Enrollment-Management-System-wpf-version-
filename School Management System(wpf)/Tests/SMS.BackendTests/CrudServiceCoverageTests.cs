using School_Management_System.Data;
using School_Management_System.Interfaces;
using School_Management_System.Models;
using School_Management_System.Services;
using SMS.BackendTests.TestSupport;
using Xunit;

namespace SMS.BackendTests;

public sealed class CrudServiceCoverageTests : BackendTestBase
{
    public static IEnumerable<object[]> CrudCases()
    {
        yield return Case("AnnouncementService", () => new AnnouncementService(), f => f.CreateAnnouncement(), e => ((Announcement)e).Title = "Updated", AssertMissing<Announcement>);
        yield return Case("AssessmentService", () => new AssessmentService(), f => f.CreateAssessment(), e => ((Assessment)e).Title = "Updated", AssertMissing<Assessment>);
        yield return Case("AssessmentScoreService", () => new AssessmentScoreService(), f => f.CreateAssessmentScore(), e => ((AssessmentScore)e).Score = 19, AssertMissing<AssessmentScore>);
        yield return Case("AttendanceRecordService", () => new AttendanceRecordService(), f => f.CreateAttendanceRecord(), e => ((AttendanceRecord)e).Reason = "Updated", AssertMissing<AttendanceRecord>);
        yield return Case("AttendanceSessionService", () => new AttendanceSessionService(), f => f.CreateAttendanceSession(), e => ((AttendanceSession)e).SessionDate = DateTime.UtcNow.Date.AddDays(3), AssertMissing<AttendanceSession>);
        yield return Case("AuditLogService", () => new AuditLogService(), f => f.CreateAuditLog(), e => ((AuditLog)e).Action = "UPDATED", AssertMissing<AuditLog>);
        yield return Case("ClassScheduleService", () => new ClassScheduleService(), f => f.CreateClassSchedule(), e => ((ClassSchedule)e).DayOfWeek = 5, AssertMissing<ClassSchedule>);
        yield return Case("ClassStudentService", () => new ClassStudentService(), f => f.CreateClassStudent(), e => ((ClassStudent)e).Status = ClassStudentStatus.DROPPED, AssertMissing<ClassStudent>);
        yield return Case("CurriculumSubjectService", () => new CurriculumSubjectService(), f => f.CreateCurriculumSubject(), e => ((CurriculumSubject)e).SortOrder = 99, AssertMissing<CurriculumSubject>);
        yield return Case("GradingPeriodService", () => new GradingPeriodService(), f => f.CreateGradingPeriod(), e => ((GradingPeriod)e).Name = "Updated", AssertMissing<GradingPeriod>);
        yield return Case("RoomService", () => new RoomService(), f => f.CreateRoom(), e => ((Room)e).Name = "Updated", (db, id) => Assert.False(db.Rooms.Find(id)!.IsActive));
        yield return Case("SchoolSettingService", () => new SchoolSettingService(), f => f.CreateSchoolSetting(), e => ((SchoolSetting)e).SchoolName = "Updated", AssertMissing<SchoolSetting>);
        yield return Case("StudentGradeService", () => new StudentGradeService(), f => f.CreateStudentGrade(), e => ((StudentGrade)e).QuarterGrade = 95, AssertMissing<StudentGrade>);
        yield return Case("StudentRequirementService", () => new StudentRequirementService(), f => f.CreateStudentRequirement(), e => ((StudentRequirement)e).IsSubmitted = true, AssertMissing<StudentRequirement>);
        yield return Case("TimeSlotService", () => new TimeSlotService(), f => f.CreateTimeSlot(), e => ((TimeSlot)e).Name = "Updated", AssertMissing<TimeSlot>);
        yield return Case("UserService", () => new UserService(), f => f.CreateUser(), e => ((User)e).Username = "updated-user", (db, id) => Assert.Equal(UserStatus.INACTIVE, db.Users.Find(id)!.Status));
        yield return Case("TeacherService", () => new TeacherService(), f => f.CreateTeacher(), e => ((Teacher)e).LastName = "Updated", (db, id) => Assert.Equal(UserStatus.INACTIVE, db.Teachers.Find(id)!.Status));
        yield return Case("StudentService", () => new StudentService(), f => f.CreateStudent(), e => ((Student)e).LastName = "Updated", (db, id) => Assert.Equal(UserStatus.INACTIVE, db.Students.Find(id)!.Status));
        yield return Case("SchoolYearService", () => new SchoolYearService(), f => f.CreateSchoolYear(), e => ((SchoolYear)e).Name = "Updated", (db, id) => Assert.True(db.SchoolYears.Find(id)!.IsArchived));
        yield return Case("SectionService", () => new SectionService(), f => f.CreateSection(), e => ((Section)e).Name = "Updated", (db, id) => Assert.True(db.Sections.Find(id)!.IsArchived));
        yield return Case("CurriculumService", () => new CurriculumService(), f => f.CreateCurriculum(), e => ((Curriculum)e).Name = "Updated", (db, id) => Assert.False(db.Curricula.Find(id)!.IsActive));
        yield return Case("GradeLevelService", () => new GradeLevelService(), f => f.CreateGradeLevel(), e => ((GradeLevel)e).Name = "Updated", AssertMissing<GradeLevel>);
        yield return Case("GradeComponentService", () => new GradeComponentService(), f => f.CreateGradeComponent(), e => ((GradeComponent)e).Weight = 0.6m, (db, id) => Assert.False(db.Set<GradeComponent>().Find(id)!.IsActive));
        yield return Case("SubjectService", () => new SubjectService(), f => f.CreateSubject(), e => ((Subject)e).Title = "Updated", (db, id) => Assert.False(db.Subjects.Find(id)!.IsActive));
        yield return Case("ClassOfferingService", () => new ClassOfferingService(), f => f.CreateClassOffering(), e => ((ClassOffering)e).Room = "Updated", AssertMissing<ClassOffering>);
        yield return Case("ArchiveRecordService", () => new ArchiveRecordService(), f => f.CreateArchiveRecord(), e => ((ArchiveRecord)e).Notes = "Updated", AssertMissing<ArchiveRecord>);
    }

    [Theory]
    [MemberData(nameof(CrudCases))]
    public void Crud_Service_Methods_Work_For_Backend_Surface(object testCaseObject)
    {
        var testCase = (ServiceCrudCase)testCaseObject;
        using (var db = CreateDb())
        {
            Factory.SeedCoreData(db);
        }

        var service = testCase.CreateService();
        var entity = testCase.CreateEntity(Factory);

        service.GetType().GetMethod("Create")!.Invoke(service, [entity]);
        long id = entity.Id;
        Assert.True(id > 0);
        Assert.NotNull(service.GetType().GetMethod("GetById")!.Invoke(service, [id]));
        Assert.NotEmpty((System.Collections.IEnumerable)service.GetType().GetMethod("GetAll")!.Invoke(service, null)!);

        testCase.Mutate(entity);
        service.GetType().GetMethod("Update")!.Invoke(service, [entity]);
        Assert.NotNull(service.GetType().GetMethod("GetById")!.Invoke(service, [id]));

        service.GetType().GetMethod("Delete")!.Invoke(service, [id]);
        using var verificationDb = CreateDb();
        testCase.AssertDeleted(verificationDb, id);
    }

    private static object[] Case(
        string name,
        Func<object> createService,
        Func<TestDataFactory, IBaseModel> createEntity,
        Action<IBaseModel> mutate,
        Action<AppDbContext, long> assertDeleted)
    {
        return [new ServiceCrudCase(name, createService, createEntity, mutate, assertDeleted)];
    }

    private static void AssertMissing<T>(AppDbContext db, long id) where T : class, IBaseModel
    {
        Assert.Null(db.Set<T>().Find(id));
    }

    private sealed class ServiceCrudCase
    {
        public ServiceCrudCase(
            string name,
            Func<object> createService,
            Func<TestDataFactory, IBaseModel> createEntity,
            Action<IBaseModel> mutate,
            Action<AppDbContext, long> assertDeleted)
        {
            Name = name;
            CreateService = createService;
            CreateEntity = createEntity;
            Mutate = mutate;
            AssertDeleted = assertDeleted;
        }

        public string Name { get; }
        public Func<object> CreateService { get; }
        public Func<TestDataFactory, IBaseModel> CreateEntity { get; }
        public Action<IBaseModel> Mutate { get; }
        public Action<AppDbContext, long> AssertDeleted { get; }

        public override string ToString() => Name;
    }
}
