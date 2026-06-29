using School_Management_System.Data;
using School_Management_System.Interfaces;
using School_Management_System.Models;
using School_Management_System.Repositories;
using SMS.BackendTests.TestSupport;
using Xunit;

namespace SMS.BackendTests;

public sealed class RepositoryCoverageTests : BackendTestBase
{
    public static IEnumerable<object[]> RepositoryCases()
    {
        yield return Case("AnnouncementRepository", db => new AnnouncementRepository(db), f => f.CreateAnnouncement(), e => ((Announcement)e).Title = "Updated", AssertHardDelete<Announcement>, AssertUpdated<Announcement>(x => x.Title == "Updated"));
        yield return Case("ArchiveRecordRepository", db => new ArchiveRecordRepository(db), f => f.CreateArchiveRecord(), e => ((ArchiveRecord)e).Notes = "Updated", AssertHardDelete<ArchiveRecord>, AssertUpdated<ArchiveRecord>(x => x.Notes == "Updated"));
        yield return Case("AssessmentRepository", db => new AssessmentRepository(db), f => f.CreateAssessment(), e => ((Assessment)e).Title = "Updated", AssertHardDelete<Assessment>, AssertUpdated<Assessment>(x => x.Title == "Updated"));
        yield return Case("AssessmentScoreRepository", db => new AssessmentScoreRepository(db), f => f.CreateAssessmentScore(), e => ((AssessmentScore)e).Score = 22, AssertHardDelete<AssessmentScore>, AssertUpdated<AssessmentScore>(x => x.Score == 22));
        yield return Case("AttendanceRecordRepository", db => new AttendanceRecordRepository(db), f => f.CreateAttendanceRecord(), e => ((AttendanceRecord)e).Reason = "Updated", AssertHardDelete<AttendanceRecord>, AssertUpdated<AttendanceRecord>(x => x.Reason == "Updated"));
        yield return Case("AttendanceSessionRepository", db => new AttendanceSessionRepository(db), f => f.CreateAttendanceSession(), e => ((AttendanceSession)e).SessionDate = DateTime.UtcNow.Date.AddDays(2), AssertHardDelete<AttendanceSession>, AssertUpdated<AttendanceSession>(x => x.SessionDate.Date == DateTime.UtcNow.Date.AddDays(2)));
        yield return Case("AuditLogRepository", db => new AuditLogRepository(db), f => f.CreateAuditLog(), e => ((AuditLog)e).Action = "UPDATED", AssertHardDelete<AuditLog>, AssertUpdated<AuditLog>(x => x.Action == "UPDATED"));
        yield return Case("ClassOfferingRepository", db => new ClassOfferingRepository(db), f => f.CreateClassOffering(), e => ((ClassOffering)e).Room = "Updated", AssertHardDelete<ClassOffering>, AssertUpdated<ClassOffering>(x => x.Room == "Updated"));
        yield return Case("ClassScheduleRepository", db => new ClassScheduleRepository(db), f => f.CreateClassSchedule(), e => ((ClassSchedule)e).DayOfWeek = 3, AssertHardDelete<ClassSchedule>, AssertUpdated<ClassSchedule>(x => x.DayOfWeek == 3));
        yield return Case("ClassStudentRepository", db => new ClassStudentRepository(db), f => f.CreateClassStudent(), e => ((ClassStudent)e).Status = ClassStudentStatus.DROPPED, AssertHardDelete<ClassStudent>, AssertUpdated<ClassStudent>(x => x.Status == ClassStudentStatus.DROPPED));
        yield return Case("CurriculumRepository", db => new CurriculumRepository(db), f => f.CreateCurriculum(), e => ((Curriculum)e).Name = "Updated", AssertSoftBool<Curriculum>(x => x.IsActive == false), AssertUpdated<Curriculum>(x => x.Name == "Updated"));
        yield return Case("CurriculumSubjectRepository", db => new CurriculumSubjectRepository(db), f => f.CreateCurriculumSubject(), e => ((CurriculumSubject)e).SortOrder = 99, AssertHardDelete<CurriculumSubject>, AssertUpdated<CurriculumSubject>(x => x.SortOrder == 99));
        yield return Case("EnrollmentRepository", db => new EnrollmentRepository(db), f => f.CreateEnrollment(), e => ((Enrollment)e).Notes = "Updated", AssertHardDelete<Enrollment>, AssertUpdated<Enrollment>(x => x.Notes == "Updated"));
        yield return Case("GradeComponentRepository", db => new GradeComponentRepository(db), f => f.CreateGradeComponent(), e => ((GradeComponent)e).Weight = 0.5m, AssertSoftBool<GradeComponent>(x => x.IsActive == false), AssertUpdated<GradeComponent>(x => x.Weight == 0.5m));
        yield return Case("GradeLevelRepository", db => new GradeLevelRepository(db), f => f.CreateGradeLevel(), e => ((GradeLevel)e).Name = "Updated", AssertHardDelete<GradeLevel>, AssertUpdated<GradeLevel>(x => x.Name == "Updated"));
        yield return Case("GradingPeriodRepository", db => new GradingPeriodRepository(db), f => f.CreateGradingPeriod(), e => ((GradingPeriod)e).Name = "Updated", AssertHardDelete<GradingPeriod>, AssertUpdated<GradingPeriod>(x => x.Name == "Updated"));
        yield return Case("RoomRepository", db => new RoomRepository(db), f => f.CreateRoom(), e => ((Room)e).Name = "Updated", AssertSoftBool<Room>(x => x.IsActive == false), AssertUpdated<Room>(x => x.Name == "Updated"));
        yield return Case("SchoolSettingRepository", db => new SchoolSettingRepository(db), f => f.CreateSchoolSetting(), e => ((SchoolSetting)e).SchoolName = "Updated", AssertHardDelete<SchoolSetting>, AssertUpdated<SchoolSetting>(x => x.SchoolName == "Updated"));
        yield return Case("SchoolYearRepository", db => new SchoolYearRepository(db), f => f.CreateSchoolYear(), e => ((SchoolYear)e).Name = "Updated", AssertSoftBool<SchoolYear>(x => x.IsArchived), AssertUpdated<SchoolYear>(x => x.Name == "Updated"));
        yield return Case("SectionRepository", db => new SectionRepository(db), f => f.CreateSection(), e => ((Section)e).Name = "Updated", AssertSoftBool<Section>(x => x.IsArchived), AssertUpdated<Section>(x => x.Name == "Updated"));
        yield return Case("StudentGradeRepository", db => new StudentGradeRepository(db), f => f.CreateStudentGrade(), e => ((StudentGrade)e).QuarterGrade = 91, AssertHardDelete<StudentGrade>, AssertUpdated<StudentGrade>(x => x.QuarterGrade == 91));
        yield return Case("StudentRepository", db => new StudentRepository(db), f => f.CreateStudent(), e => ((Student)e).LastName = "Updated", AssertSoftStatus<Student>(UserStatus.INACTIVE), AssertUpdated<Student>(x => x.LastName == "Updated"));
        yield return Case("StudentRequirementRepository", db => new StudentRequirementRepository(db), f => f.CreateStudentRequirement(), e => ((StudentRequirement)e).IsSubmitted = true, AssertHardDelete<StudentRequirement>, AssertUpdated<StudentRequirement>(x => x.IsSubmitted));
        yield return Case("SubjectRepository", db => new SubjectRepository(db), f => f.CreateSubject(), e => ((Subject)e).Title = "Updated", AssertSoftBool<Subject>(x => x.IsActive == false), AssertUpdated<Subject>(x => x.Title == "Updated"));
        yield return Case("TeacherRepository", db => new TeacherRepository(db), f => f.CreateTeacher(), e => ((Teacher)e).LastName = "Updated", AssertSoftStatus<Teacher>(UserStatus.INACTIVE), AssertUpdated<Teacher>(x => x.LastName == "Updated"));
        yield return Case("TimeSlotRepository", db => new TimeSlotRepository(db), f => f.CreateTimeSlot(), e => ((TimeSlot)e).Name = "Updated", AssertHardDelete<TimeSlot>, AssertUpdated<TimeSlot>(x => x.Name == "Updated"));
        yield return Case("UserRepository", db => new UserRepository(db), f => f.CreateUser(), e => ((User)e).Username = "updated-user", AssertSoftStatus<User>(UserStatus.INACTIVE), AssertUpdated<User>(x => x.Username == "updated-user"));
    }

    [Theory]
    [MemberData(nameof(RepositoryCases))]
    public void Repository_Crud_Works_For_Backend_Surface(object testCaseObject)
    {
        var testCase = (RepositoryCase)testCaseObject;
        using var db = CreateDb();
        Factory.SeedCoreData(db);

        var repository = testCase.CreateRepository(db);
        var entity = testCase.CreateEntity(Factory);
        repository.GetType().GetMethod("Add")!.Invoke(repository, [entity]);

        Assert.True(entity.Id > 0);
        Assert.NotNull(repository.GetType().GetMethod("GetById")!.Invoke(repository, [entity.Id]));
        Assert.NotEmpty((System.Collections.IEnumerable)repository.GetType().GetMethod("GetAll")!.Invoke(repository, null)!);

        testCase.Mutate(entity);
        repository.GetType().GetMethod("Update")!.Invoke(repository, [entity]);
        testCase.AssertUpdated(db, entity.Id);

        repository.GetType().GetMethod("Delete")!.Invoke(repository, [entity.Id]);
        testCase.AssertDeleted(db, entity.Id);
    }

    [Fact]
    public void UserRepository_Supports_Username_Lookups()
    {
        using var db = CreateDb();
        Factory.SeedCoreData(db);
        var repo = new UserRepository(db);

        var user = repo.GetByUsername("SUPERADMIN");

        Assert.NotNull(user);
        Assert.True(repo.ExistsUsername("superadmin"));
        Assert.False(repo.ExistsUsername("missing-user"));
    }

    private static object[] Case(
        string name,
        Func<AppDbContext, object> createRepository,
        Func<TestDataFactory, IBaseModel> createEntity,
        Action<IBaseModel> mutate,
        Action<AppDbContext, long> assertDeleted,
        Action<AppDbContext, long> assertUpdated)
    {
        return
        [
            new RepositoryCase(name, createRepository, createEntity, mutate, assertDeleted, assertUpdated)
        ];
    }

    private static Action<AppDbContext, long> AssertUpdated<T>(Func<T, bool> predicate) where T : class, IBaseModel
    {
        return (db, id) =>
        {
            var entity = db.Set<T>().Find(id);
            Assert.NotNull(entity);
            Assert.True(predicate(entity!));
        };
    }

    private static void AssertHardDelete<T>(AppDbContext db, long id) where T : class, IBaseModel
    {
        Assert.Null(db.Set<T>().Find(id));
    }

    private static Action<AppDbContext, long> AssertSoftStatus<T>(object expected) where T : class, IBaseModel
    {
        return (db, id) =>
        {
            var entity = db.Set<T>().Find(id);
            Assert.NotNull(entity);
            var status = entity!.GetType().GetProperty("Status")!.GetValue(entity);
            Assert.Equal(expected, status);
        };
    }

    private static Action<AppDbContext, long> AssertSoftBool<T>(Func<T, bool> predicate) where T : class, IBaseModel
    {
        return (db, id) =>
        {
            var entity = db.Set<T>().Find(id);
            Assert.NotNull(entity);
            Assert.True(predicate(entity!));
        };
    }

    private sealed class RepositoryCase
    {
        public RepositoryCase(
            string name,
            Func<AppDbContext, object> createRepository,
            Func<TestDataFactory, IBaseModel> createEntity,
            Action<IBaseModel> mutate,
            Action<AppDbContext, long> assertDeleted,
            Action<AppDbContext, long> assertUpdated)
        {
            Name = name;
            CreateRepository = createRepository;
            CreateEntity = createEntity;
            Mutate = mutate;
            AssertDeleted = assertDeleted;
            AssertUpdated = assertUpdated;
        }

        public string Name { get; }
        public Func<AppDbContext, object> CreateRepository { get; }
        public Func<TestDataFactory, IBaseModel> CreateEntity { get; }
        public Action<IBaseModel> Mutate { get; }
        public Action<AppDbContext, long> AssertDeleted { get; }
        public Action<AppDbContext, long> AssertUpdated { get; }

        public override string ToString() => Name;
    }
}
