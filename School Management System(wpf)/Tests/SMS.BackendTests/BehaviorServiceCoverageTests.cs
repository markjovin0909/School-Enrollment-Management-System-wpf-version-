using System;
using System.IO;
using School_Management_System.Models;
using School_Management_System.Services;
using SMS.BackendTests.TestSupport;
using Xunit;

namespace SMS.BackendTests;

public sealed class BehaviorServiceCoverageTests : BackendTestBase
{
    [Fact]
    public void SchoolSettingService_Custom_Functions_Work()
    {
        using var db = CreateDb();
        Factory.SeedCoreData(db);
        var service = new SchoolSettingService();

        var defaultSetting = service.GetOrCreateDefault();
        var defaultGradeLevels = service.GetDefaultGradeLevelIds();
        var peek = service.PeekNextStudentNumber();
        var reserved = service.ReserveNextStudentNumber();

        Assert.NotNull(defaultSetting);
        Assert.Equal(new long[] { 1, 2 }, defaultGradeLevels);
        Assert.Equal("S-000010", peek);
        Assert.Equal("S-000010", reserved);
        Assert.Equal("1,3,5", SchoolSettingService.NormalizeGradeLevelIds(new long[] { 5, 3, 1, 3 }));
        Assert.Equal(new long[] { 1, 2 }, SchoolSettingService.ParseGradeLevelIds("1, 2, invalid, 2"));
    }

    [Fact]
    public void SchoolBrandingService_Falls_Back_To_Default_Logo()
    {
        using var db = CreateDb();
        Factory.SeedCoreData(db);

        var branding = new SchoolBrandingService().GetCurrentBranding();

        Assert.Equal("SMS Academy", branding.SchoolName);
        Assert.EndsWith("Logo.jpg", branding.LogoAbsolutePath, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(branding.LogoImage);
    }

    [Fact]
    public void FileStorageService_Saves_And_Deletes_School_Logo()
    {
        var sourceDir = Path.Combine(Path.GetTempPath(), "sms-branding-tests");
        Directory.CreateDirectory(sourceDir);
        var sourcePath = Path.Combine(sourceDir, $"{Guid.NewGuid():N}.jpg");
        File.WriteAllBytes(sourcePath, new byte[] { 1, 2, 3, 4, 5 });

        var storedPath = FileStorageService.SaveSchoolLogo(sourcePath);
        var absolutePath = FileStorageService.GetSchoolLogoAbsolutePath(storedPath);

        Assert.False(string.IsNullOrWhiteSpace(storedPath));
        Assert.Contains("storage", storedPath!, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("branding", storedPath!, StringComparison.OrdinalIgnoreCase);
        Assert.True(File.Exists(absolutePath));

        FileStorageService.DeleteSchoolLogo(storedPath);

        Assert.False(File.Exists(absolutePath));
    }

    [Fact]
    public void SchoolYearService_Additional_Functions_Work()
    {
        using var db = CreateDb();
        Factory.SeedCoreData(db);
        var service = new SchoolYearService();

        var active = service.GetActiveSchoolYear();
        var openResult = service.ValidateEnrollmentWindow(TestDataFactory.SchoolYearId, DateTime.UtcNow.Date);

        Assert.NotNull(active);
        Assert.True(openResult.Success);
    }

    [Fact]
    public void Auth_And_Account_Security_Flows_Work()
    {
        using (var db = CreateDb())
        {
            Factory.SeedCoreData(db);
        }

        var auth = new AuthService();
        var registerResult = auth.Register(new User
        {
            Username = "fresh-admin",
            Role = UserRole.SUPERADMIN,
            CanLogin = true,
            Status = UserStatus.ACTIVE
        }, "FreshPass123!");

        var loginResult = auth.Authenticate("fresh-admin", "FreshPass123!");
        var security = new AccountSecurityService();
        var changeResult = security.ChangePassword(registerResult.Data!.Id, "FreshPass123!", "ChangedPass123!");
        var resetResult = security.ResetPassword(registerResult.Data.Id, "ResetPass123!");

        using (var db = CreateDb())
        {
            var setting = db.SchoolSettings.OrderByDescending(x => x.Id).First();
            setting.SchoolCode = "SMS-RECOVER";
            db.SaveChanges();
        }

        var recoverResult = security.RecoverPassword("superadmin", UserRole.SUPERADMIN, "SMS-RECOVER", "SUPERADMIN", "RecoverPass123!");
        security.LogLogout(registerResult.Data);

        Assert.True(registerResult.Success);
        Assert.True(loginResult.Success);
        Assert.True(changeResult.Success);
        Assert.True(resetResult.Success);
        Assert.True(recoverResult.Success);
    }

    [Fact]
    public void StudentAccountService_Methods_Work()
    {
        using (var db = CreateDb())
        {
            Factory.SeedCoreData(db);
        }

        var service = new StudentAccountService();
        var all = service.GetAll().ToList();
        var byStudent = service.GetByStudentId(TestDataFactory.StudentId);
        var byUser = service.GetByUserId(TestDataFactory.StudentUserId);
        var created = service.CreateManagedAccount("managed-student", UserStatus.ACTIVE);
        var synced = service.SyncStudentAccount(TestDataFactory.StudentId);
        var status = service.SetStudentAccountStatus(TestDataFactory.StudentId, UserStatus.INACTIVE);
        var reset = service.ResetStudentAccount(TestDataFactory.StudentId);

        Assert.NotEmpty(all);
        Assert.NotNull(byStudent);
        Assert.NotNull(byUser);
        Assert.True(created.Success);
        Assert.True(synced.Success);
        Assert.True(status.Success);
        Assert.True(reset.Success);
    }

    [Fact]
    public void Requirement_Checklist_Functions_Work()
    {
        var service = new RequirementChecklistService();
        var requirements = new[]
        {
            new StudentRequirement { Id = 1, StudentId = 50, RequirementName = "Birth Certificate", IsSubmitted = true, Notes = "[STATUS:VERIFIED] verified", UpdatedAt = DateTime.UtcNow },
            new StudentRequirement { Id = 2, StudentId = 50, RequirementName = "Custom Paper", IsSubmitted = false, Notes = "pending", UpdatedAt = DateTime.UtcNow }
        };

        var snapshot = service.BuildForStudent(50, requirements);
        var notes = service.BuildPersistedNotes("user note", RequirementChecklistStatus.REJECTED);
        var stripped = service.StripStatusTag(notes);

        Assert.Equal(6, service.GetRequiredRequirements().Count);
        Assert.Equal(RequirementChecklistStatus.VERIFIED, service.ResolveStatus(requirements[0]));
        Assert.Contains(snapshot.Items, x => x.RequirementName == "Custom Paper");
        Assert.Contains("[STATUS:REJECTED]", notes);
        Assert.Equal("user note", stripped);
    }

    [Fact]
    public void ExceptionQueue_And_Governed_Log_Functions_Work()
    {
        using (var db = CreateDb())
        {
            Factory.SeedCoreData(db);
        }

        var queue = new ExceptionQueueService();
        var item = queue.Raise(new ExceptionQueueCreateRequest
        {
            Category = "TEST",
            SourceModule = "UnitTests",
            Entity = "students",
            EntityId = 1,
            Severity = ExceptionQueueSeverity.WARNING,
            Summary = "Issue",
            Details = "Details"
        });
        var active = queue.GetActive();
        var assigned = queue.Assign(item.Id, TestDataFactory.AdminUserId);
        var resolved = queue.Resolve(item.Id, "Fixed");

        var logService = new GovernedOperationLogService();
        logService.Log(PolicyActionKey.ENROLLMENT_APPROVE, "TEST_ACTION", "students", 1, GovernedOperationStatus.SUCCEEDED, "ok");
        var recent = logService.GetRecent();

        Assert.NotEmpty(active);
        Assert.True(assigned.Success);
        Assert.True(resolved.Success);
        Assert.NotEmpty(recent);
    }

    [Fact]
    public void Permission_And_StateMachine_Functions_Work()
    {
        var permission = new PermissionBoundaryService();
        var allowed = permission.IsAllowed(PolicyActionKey.ENROLLMENT_APPROVE, SessionContext.CurrentUser);
        var denied = permission.IsAllowed(PolicyActionKey.ENROLLMENT_APPROVE, new User { Role = UserRole.TEACHER });
        var machine = new EnrollmentStateMachineService();

        Assert.True(allowed);
        Assert.False(denied);
        Assert.True(machine.ValidateTransition(null, EnrollmentStatus.PENDING).Success);
        Assert.False(machine.ValidateTransition(EnrollmentStatus.ENROLLED, EnrollmentStatus.CANCELLED).Success);
    }

    [Fact]
    public void Notification_And_Report_History_Functions_Work()
    {
        var notification = new NotificationCenterService();
        var user = new User { Id = 90, Role = UserRole.SUPERADMIN };
        notification.PublishToRole(UserRole.SUPERADMIN, "Role Title", "Role Message", "General", 1);
        notification.PublishToUser(90, "User Title", "User Message", "General", 2);
        notification.PublishFromAudit("BACKUP_FAILED", "database", 3, "failure payload");
        var forUser = notification.GetForUser(user, includeRead: false, take: 10);
        var unreadBefore = notification.GetUnreadCount(user);
        notification.MarkAllAsRead(user);
        var unreadAfter = notification.GetUnreadCount(user);

        var report = new ReportPresetHistoryService();
        var saved = report.SavePreset(new ReportFilterPreset { Name = "Preset A", ReportType = "Students", Status = "All" });
        report.AppendHistory(new ReportRunHistoryEntry { ReportType = "Students", PresetName = "Preset A", Success = true });
        var presets = report.LoadPresets();
        var history = report.LoadHistory();
        var deleted = report.DeletePreset(saved.Data!.Id);

        Assert.NotEmpty(forUser);
        Assert.True(unreadBefore >= 1);
        Assert.Equal(0, unreadAfter);
        Assert.True(saved.Success);
        Assert.NotEmpty(presets);
        Assert.NotEmpty(history);
        Assert.True(deleted.Success);
    }

    [Fact]
    public void Utility_Static_Functions_Work()
    {
        var correlationId = CorrelationContext.Ensure();
        using var scope = CorrelationContext.BeginScope("REQ-TEST");
        var scopedId = CorrelationContext.CurrentId;
        using var generatedScope = CorrelationContext.BeginScopeIfMissing();

        Assert.True(PasswordHasher.VerifyPassword("ValidPass123!", PasswordHasher.HashPassword("ValidPass123!")));
        Assert.True(PasswordPolicyService.Validate("AnotherPass123!", "user").Success);
        Assert.Equal("*******6789", DataPrivacyService.MaskPhone("09123456789"));
        Assert.Equal("*****6789", DataPrivacyService.MaskIdentifier("123456789", 4));
        Assert.Equal("J*** D**", DataPrivacyService.MaskName("John Doe"));
        Assert.False(string.IsNullOrWhiteSpace(correlationId));
        Assert.Equal("REQ-TEST", scopedId);
    }

    [Fact]
    public void Session_LoginAttempt_Preflight_And_FileStorage_Functions_Work()
    {
        var username = $"lock-{Guid.NewGuid():N}";
        for (var i = 0; i < 5; i++)
        {
            LoginAttemptService.RecordFailure(username);
        }

        var locked = LoginAttemptService.IsLocked(username, out var remaining);
        LoginAttemptService.RecordSuccess(username);
        var unlocked = LoginAttemptService.IsLocked(username, out _);

        SessionContext.CurrentUser = new User { Id = 1, Username = "u", Role = UserRole.SUPERADMIN, Status = UserStatus.ACTIVE, CanLogin = true };
        SessionContext.IdleTimeout = TimeSpan.Zero;
        SessionContext.Touch();

        var preflight = new PreflightPipelineService().Evaluate("demo", new Func<PreflightCheckResult>[]
        {
            () => PreflightCheckResult.Pass("A", "ok"),
            () => PreflightCheckResult.Warning("B", "warn"),
            () => PreflightCheckResult.Block("C", "block")
        });

        var sourceFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.txt");
        File.WriteAllText(sourceFile, "profile");
        var stored = FileStorageService.SaveProfileImage(sourceFile);
        var absolute = FileStorageService.GetAbsolutePath(stored);

        Assert.True(locked);
        Assert.True(remaining > TimeSpan.Zero);
        Assert.False(unlocked);
        Assert.True(SessionContext.IsExpired());
        Assert.False(preflight.Success);
        Assert.NotNull(stored);
        Assert.True(File.Exists(absolute));
    }

    [Fact]
    public void Metrics_Archive_And_Backup_Focused_Functions_Work()
    {
        using (var db = CreateDb())
        {
            Factory.SeedCoreData(db);
            db.ArchiveRecords.Add(new ArchiveRecord
            {
                EntityType = "User",
                OriginalEntityId = 999,
                Payload = "{\"Id\":999,\"Username\":\"restored-user\",\"PasswordHash\":\"hash\",\"Role\":\"SUPERADMIN\",\"CanLogin\":true,\"Status\":\"ACTIVE\",\"CreatedAt\":\"2026-01-01T00:00:00Z\",\"UpdatedAt\":\"2026-01-01T00:00:00Z\"}",
                DeletedAt = DateTime.UtcNow,
                Notes = "HARD_DELETE"
            });
            db.SaveChanges();
        }

        var metrics = new OperationalMetricsDashboardService().BuildSnapshot(DateTime.UtcNow);
        var archive = new ArchiveRecordService();
        var preview = archive.BuildRestoreImpactPreview(1);
        var backup = new BackupRestoreService();
        var settings = backup.LoadSettings();
        backup.SaveSettings(settings);
        var history = backup.LoadHistory();
        var preflight = backup.EvaluateRestorePreflight(new RestoreRequest { RestoreFilePath = "C:\\missing.sql" });
        var latestLog = backup.GetLatestLogFilePath();

        Assert.Equal(4, metrics.All.Count);
        Assert.True(preview.Success);
        Assert.NotNull(settings);
        Assert.NotNull(history);
        Assert.False(preflight.Success);
        Assert.True(string.IsNullOrEmpty(latestLog) || File.Exists(latestLog));
    }
}
