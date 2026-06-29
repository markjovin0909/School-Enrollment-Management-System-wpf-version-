using Microsoft.EntityFrameworkCore;
using School_Management_System.Data;
using School_Management_System.Models;
using School_Management_System.Services;

namespace SMS.BackendTests.TestSupport;

public abstract class BackendTestBase : IDisposable
{
    private readonly string? _originalAppData;
    private readonly string _tempAppDataRoot;
    private readonly string _dbName;

    protected BackendTestBase()
    {
        _originalAppData = Environment.GetEnvironmentVariable("APPDATA");
        _tempAppDataRoot = Path.Combine(Path.GetTempPath(), "sms-backend-tests", Guid.NewGuid().ToString("N"));
        _dbName = $"sms_backend_{Guid.NewGuid():N}";
        Directory.CreateDirectory(_tempAppDataRoot);
        Environment.SetEnvironmentVariable("APPDATA", _tempAppDataRoot);

        AppDbContext.TestOptionsConfigurator = options => options.UseInMemoryDatabase(_dbName);
        SessionContext.CurrentUser = new User
        {
            Id = TestDataFactory.AdminUserId,
            Username = "superadmin",
            Role = UserRole.SUPERADMIN,
            Status = UserStatus.ACTIVE,
            CanLogin = true
        };

        Factory = new TestDataFactory();
    }

    protected TestDataFactory Factory { get; }

    private protected AppDbContext CreateDb()
    {
        return new AppDbContext();
    }

    public void Dispose()
    {
        SessionContext.Clear();
        AppDbContext.TestOptionsConfigurator = null;
        Environment.SetEnvironmentVariable("APPDATA", _originalAppData);

        try
        {
            if (Directory.Exists(_tempAppDataRoot))
            {
                Directory.Delete(_tempAppDataRoot, recursive: true);
            }
        }
        catch
        {
            // Ignore temp cleanup failures in tests.
        }
    }
}
