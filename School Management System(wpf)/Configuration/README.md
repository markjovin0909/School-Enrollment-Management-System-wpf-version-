# Database Configuration Guide

## Overview

This School Management System uses a centralized database configuration system that allows switching between **Local**, **Remote**, and **Online** database environments without modifying code.

## File Structure

```
Configuration/
└── DatabaseConfig.cs          # Configuration manager class
App.config                      # Connection strings and environment settings
```

## Quick Start

### 1. Choose Your Environment

Edit `App.config` and change the `ActiveEnvironment` value:

```xml
<!-- Use "Local" for development -->
<add key="ActiveEnvironment" value="Local" />

<!-- Use "Remote" for staging -->
<add key="ActiveEnvironment" value="Remote" />

<!-- Use "Online" for production -->
<add key="ActiveEnvironment" value="Online" />
```

### 2. Update Connection Strings (if needed)

Edit the connection strings in `App.config` under `<connectionStrings>`:

```xml
<add name="DbLocal"
     connectionString="server=localhost; uid=root; password=root; database=School_sms"
     providerName="MySql.Data.MySqlClient" />
```

## Usage Examples

### Get Current Environment

```csharp
var environment = DatabaseConfig.ActiveEnvironment;
// Output: Local, Remote, or Online
```

### Get Connection String

```csharp
var connectionString = DatabaseConfig.GetConnectionString();
// Returns: "server=localhost; uid=root; password=root; database=School_sms"
```

### Validate Configuration

```csharp
var (isValid, message) = DatabaseConfig.ValidateConfiguration();
if (isValid)
{
    Console.WriteLine($"✓ {message}");  // Configuration valid. Environment: Local
}
else
{
    Console.WriteLine($"✗ {message}");  // Shows error details
}
```

### In Your Forms

```csharp
// Open database configuration dialog
var configForm = new DatabaseConfigurationForm();
configForm.ShowDialog();
```

## Environment-Specific Configuration

### Local Development

- **Server**: localhost (SQLEXPRESS or MySQL)
- **User**: root (local dev user)
- **Password**: root
- **Database**: School_sms

```xml
<add name="DbLocal"
     connectionString="server=localhost; uid=root; password=root; database=School_sms"
     providerName="MySql.Data.MySqlClient" />
```

### Remote (Staging/Private Network)

- **Server**: 192.168.1.100 (on your network)
- **User**: db_user
- **Password**: [secure password]
- **Database**: School_sms

```xml
<add name="DbRemote"
     connectionString="server=192.168.1.100; uid=db_user; password=db_password; database=School_sms"
     providerName="MySql.Data.MySqlClient" />
```

### Online (Production/Cloud)

- **Server**: myserver.mysql.database.azure.com (Azure MySQL)
- **User**: db_user@myserver
- **Password**: [secure password]
- **Database**: School_sms
- **SSL**: Required

```xml
<add name="DbOnline"
     connectionString="server=myserver.mysql.database.azure.com; uid=db_user@myserver; password=db_password; database=School_sms; SslMode=Required"
     providerName="MySql.Data.MySqlClient" />
```

## Security Best Practices

### ⚠️ Do NOT:

- ❌ Check in App.config with real passwords to version control
- ❌ Store passwords in plain text in production
- ❌ Use the same credentials across environments

### ✓ Do:

- ✅ Use a `.gitignore` to exclude App.config from git:
  ```
  App.config
  App.Release.config
  ```
- ✅ Store sensitive configs in:
  - Azure Key Vault (cloud)
  - Environment variables (local)
  - Secure vaults (corporate)
- ✅ Use strong passwords for production databases
- ✅ Use SSL/TLS connections (SslMode=Required)

### Example: Using Environment Variables

```csharp
// Override connection string from environment variable
var envConnectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
if (!string.IsNullOrEmpty(envConnectionString))
{
    // Use environment variable instead
}
```

## Deployment

### For Development

1. Keep `ActiveEnvironment` = "Local"
2. Use local MySQL instance
3. No special configuration needed

### For Staging (Remote)

1. Change `ActiveEnvironment` = "Remote"
2. Update DbRemote connection string with staging server details
3. Test thoroughly before production

### For Production (Online)

1. Change `ActiveEnvironment` = "Online"
2. Update DbOnline connection with production credentials
3. Use Azure MySQL or equivalent managed service
4. Ensure SSL/TLS is enabled
5. Test disaster recovery and backups

## Troubleshooting

### "No database selected" Error

- **Cause**: Connection string is missing or invalid
- **Fix**: Verify the ActiveEnvironment in App.config matches an existing connection string

### "Invalid ActiveEnvironment value" Error

- **Cause**: Typo in ActiveEnvironment setting
- **Fix**: Ensure value is exactly "Local", "Remote", or "Online" (case-insensitive)

### Connection Timeout

- **Cause**: Database server is unreachable
- **Fix**: Verify server address, port, and firewall rules

### Authentication Failed

- **Cause**: Wrong username or password
- **Fix**: Test credentials directly with MySQL client

### SSL Certificate Error (Azure MySQL)

- **Cause**: SslMode not set correctly
- **Fix**: Add `SslMode=Required` to connection string

## Testing Configuration

Use the `DatabaseConfigurationForm` to test your configuration:

```csharp
// In your admin panel or settings
var configForm = new DatabaseConfigurationForm();
configForm.ShowDialog(this);
```

This will display:

- ✓ Current environment
- ✓ (Masked) connection string
- ✓ Validation status

## Advanced: Adding New Environments

To add a new environment (e.g., "Staging"):

1. **Update DatabaseConfig.cs enum:**

   ```csharp
   public enum Environment
   {
       Local,
       Remote,
       Online,
       Staging  // Add new environment
   }
   ```

2. **Update mapping method:**

   ```csharp
   private static string GetConnectionStringName(Environment environment)
   {
       return environment switch
       {
           Environment.Local => "DbLocal",
           Environment.Remote => "DbRemote",
           Environment.Online => "DbOnline",
           Environment.Staging => "DbStaging",  // Add mapping
           // ...
       };
   }
   ```

3. **Add connection string to App.config:**
   ```xml
   <add name="DbStaging"
        connectionString="server=staging.example.com; uid=user; password=pwd; database=School_sms"
        providerName="MySql.Data.MySqlClient" />
   ```

## Support

For issues or questions, check:

- DatabaseConfig.cs documentation
- App.config comments
- DatabaseConfigurationForm example
