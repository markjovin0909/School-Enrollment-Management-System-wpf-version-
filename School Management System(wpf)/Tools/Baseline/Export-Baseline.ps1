[CmdletBinding()]
param(
    [int]$LookbackDays = 30,
    [string]$OutputDir = "",
    [string]$SupportIssuesCsv = "",
    [string]$TaskTimeStudyCsv = "",
    [string]$EnvironmentOverride = "",
    [switch]$RequireDatabase
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
if (Get-Variable -Name PSNativeCommandUseErrorActionPreference -ErrorAction SilentlyContinue) {
    $PSNativeCommandUseErrorActionPreference = $false
}

function Get-ConnectionParts {
    param(
        [Parameter(Mandatory = $true)][string]$AppConfigPath,
        [string]$EnvironmentOverride = ""
    )

    if (-not (Test-Path -LiteralPath $AppConfigPath)) {
        throw "App.config not found: $AppConfigPath"
    }

    [xml]$config = Get-Content -LiteralPath $AppConfigPath
    $activeEnvironmentNode = @($config.configuration.appSettings.add | Where-Object { $_.key -eq "ActiveEnvironment" })[0]
    if ($null -eq $activeEnvironmentNode) {
        throw "ActiveEnvironment key was not found in App.config appSettings."
    }

    $activeEnvironment = "$($activeEnvironmentNode.value)".Trim()
    if (-not [string]::IsNullOrWhiteSpace($EnvironmentOverride)) {
        $activeEnvironment = $EnvironmentOverride.Trim()
    }
    if ([string]::IsNullOrWhiteSpace($activeEnvironment)) {
        throw "ActiveEnvironment value is empty in App.config."
    }

    $connectionName = "Db$activeEnvironment"
    $connectionNode = @($config.configuration.connectionStrings.add | Where-Object { $_.name -eq $connectionName })[0]
    if ($null -eq $connectionNode) {
        throw "Connection string '$connectionName' not found in App.config."
    }

    $connectionString = "$($connectionNode.connectionString)"
    $parts = @{}
    foreach ($segment in $connectionString.Split(";")) {
        if ([string]::IsNullOrWhiteSpace($segment)) {
            continue
        }

        $kv = $segment.Split("=", 2)
        if ($kv.Count -eq 2) {
            $parts[$kv[0].Trim().ToLowerInvariant()] = $kv[1].Trim()
        }
    }

    foreach ($required in @("server", "uid", "password", "database")) {
        if (-not $parts.ContainsKey($required) -or [string]::IsNullOrWhiteSpace("$($parts[$required])")) {
            throw "Connection string '$connectionName' is missing required key '$required'."
        }
    }

    return @{
        ActiveEnvironment = $activeEnvironment
        Server = $parts["server"]
        User = $parts["uid"]
        Password = $parts["password"]
        Database = $parts["database"]
        Port = if ($parts.ContainsKey("port")) { $parts["port"] } else { "" }
    }
}

function New-MySqlDefaultsFile {
    param(
        [Parameter(Mandatory = $true)][hashtable]$ConnectionParts
    )

    $path = Join-Path $env:TEMP ("sms-baseline-" + [Guid]::NewGuid().ToString("N") + ".cnf")
    $lines = @(
        "[client]",
        "host=$($ConnectionParts.Server)",
        "user=$($ConnectionParts.User)",
        "password=$($ConnectionParts.Password)",
        "database=$($ConnectionParts.Database)",
        "default-character-set=utf8mb4"
    )

    if (-not [string]::IsNullOrWhiteSpace("$($ConnectionParts.Port)")) {
        $lines += "port=$($ConnectionParts.Port)"
    }

    Set-Content -LiteralPath $path -Value $lines -Encoding ascii
    return $path
}

function Invoke-MySqlQuery {
    param(
        [Parameter(Mandatory = $true)][string]$DefaultsFilePath,
        [Parameter(Mandatory = $true)][string]$Query
    )

    $result = & mysql --defaults-extra-file="$DefaultsFilePath" --batch --skip-column-names --raw --execute "$Query" 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "MySQL query failed: $result"
    }

    if ($result -is [System.Array]) {
        return @($result | ForEach-Object { "$_".TrimEnd() } | Where-Object { $_ -ne "" })
    }

    $text = "$result".Trim()
    if ([string]::IsNullOrWhiteSpace($text)) {
        return @()
    }

    return @($text)
}

function Get-ScalarInt {
    param(
        [Parameter(Mandatory = $true)][string]$DefaultsFilePath,
        [Parameter(Mandatory = $true)][string]$Query
    )

    $rows = @(Invoke-MySqlQuery -DefaultsFilePath $DefaultsFilePath -Query $Query)
    if ($rows.Length -eq 0) {
        return 0
    }

    return [int]$rows[0]
}

function Parse-TabRow {
    param(
        [Parameter(Mandatory = $true)][string]$Row,
        [Parameter(Mandatory = $true)][string[]]$Columns
    )

    $parts = $Row.Split("`t")
    $obj = [ordered]@{}
    for ($i = 0; $i -lt $Columns.Count; $i++) {
        $value = if ($i -lt $parts.Length) { $parts[$i] } else { "" }
        $obj[$Columns[$i]] = $value
    }

    return [PSCustomObject]$obj
}

function To-DoubleOrZero {
    param([string]$Value)
    if ([string]::IsNullOrWhiteSpace($Value)) {
        return 0.0
    }

    return [double]::Parse($Value, [System.Globalization.CultureInfo]::InvariantCulture)
}

function To-IntOrZero {
    param([string]$Value)
    if ([string]::IsNullOrWhiteSpace($Value)) {
        return 0
    }

    return [int]$Value
}

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = (Resolve-Path (Join-Path $scriptDir "..\..\")).Path
$appConfigPath = Join-Path $projectRoot "App.config"
$governanceDir = Join-Path $projectRoot "Governance\Baseline"

if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $OutputDir = Join-Path $governanceDir "BaselineReports"
}

if ([string]::IsNullOrWhiteSpace($SupportIssuesCsv)) {
    $SupportIssuesCsv = Join-Path $governanceDir "SupportIssues.csv"
}

if ([string]::IsNullOrWhiteSpace($TaskTimeStudyCsv)) {
    $TaskTimeStudyCsv = Join-Path $governanceDir "TaskTimeStudy.csv"
}

New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

$capturedAtUtc = [DateTime]::UtcNow
$capturedAtLocal = Get-Date
$stamp = $capturedAtLocal.ToString("yyyyMMdd_HHmmss")
$windowStartUtc = $capturedAtUtc.AddDays(-1 * $LookbackDays)

$connectionParts = Get-ConnectionParts -AppConfigPath $appConfigPath -EnvironmentOverride $EnvironmentOverride
$defaultsFile = New-MySqlDefaultsFile -ConnectionParts $connectionParts

try {
    $windowClause = "UTC_TIMESTAMP() - INTERVAL $LookbackDays DAY"
    $databaseReachable = $true
    $databaseError = ""
    $totalAuditActions = 0
    $errorAuditActions = 0
    $rollbackAuditActions = 0
    $archiveRestoreEvents = 0
    $sessionMetrics = [PSCustomObject]@{
        SessionCount = "0"
        AvgSeconds = "0"
        MinSeconds = "0"
        MaxSeconds = "0"
    }
    $enrollmentCycle = [PSCustomObject]@{
        EnrollmentCount = "0"
        AvgMinutes = "0"
    }
    $auditDistribution = @()
    $enrollmentStatusDistribution = @()

    try {
        $totalAuditActions = Get-ScalarInt -DefaultsFilePath $defaultsFile -Query "SELECT COUNT(*) FROM audit_logs WHERE created_at >= $windowClause;"
        $errorAuditActions = Get-ScalarInt -DefaultsFilePath $defaultsFile -Query @"
SELECT COUNT(*)
FROM audit_logs
WHERE created_at >= $windowClause
  AND (
      UPPER(action) LIKE '%FAILED%'
      OR UPPER(action) LIKE '%ERROR%'
      OR UPPER(action) = 'LOGIN_FAILED'
  );
"@
        $rollbackAuditActions = Get-ScalarInt -DefaultsFilePath $defaultsFile -Query @"
SELECT COUNT(*)
FROM audit_logs
WHERE created_at >= $windowClause
  AND (
      UPPER(action) LIKE 'RESTORE%'
      OR UPPER(action) LIKE '%ROLLBACK%'
      OR UPPER(action) IN ('UNDO', 'REOPEN')
  );
"@
        $archiveRestoreEvents = Get-ScalarInt -DefaultsFilePath $defaultsFile -Query @"
SELECT COUNT(*)
FROM archive_records
WHERE is_restored = 1
  AND restored_at IS NOT NULL
  AND restored_at >= $windowClause;
"@

        $sessionRows = @(Invoke-MySqlQuery -DefaultsFilePath $defaultsFile -Query @"
SELECT
    COUNT(*) AS session_count,
    COALESCE(ROUND(AVG(TIMESTAMPDIFF(SECOND, login_at, logout_at)), 2), 0) AS avg_seconds,
    COALESCE(MIN(TIMESTAMPDIFF(SECOND, login_at, logout_at)), 0) AS min_seconds,
    COALESCE(MAX(TIMESTAMPDIFF(SECOND, login_at, logout_at)), 0) AS max_seconds
FROM (
    SELECT
        l.user_id,
        l.created_at AS login_at,
        (
            SELECT MIN(o.created_at)
            FROM audit_logs o
            WHERE o.user_id = l.user_id
              AND UPPER(o.action) = 'LOGOUT'
              AND o.created_at > l.created_at
        ) AS logout_at
    FROM audit_logs l
    WHERE UPPER(l.action) = 'LOGIN_SUCCESS'
      AND l.created_at >= $windowClause
) sessions
WHERE logout_at IS NOT NULL
  AND TIMESTAMPDIFF(SECOND, login_at, logout_at) BETWEEN 30 AND 28800;
"@)
        if ($sessionRows.Length -gt 0) {
            $sessionMetrics = Parse-TabRow -Row $sessionRows[0] -Columns @("SessionCount", "AvgSeconds", "MinSeconds", "MaxSeconds")
        }

        $enrollmentCycleRows = @(Invoke-MySqlQuery -DefaultsFilePath $defaultsFile -Query @"
SELECT
    COUNT(*) AS enrollment_count,
    COALESCE(ROUND(AVG(TIMESTAMPDIFF(MINUTE, created_at, COALESCE(approved_at, updated_at))), 2), 0) AS avg_minutes
FROM enrollments
WHERE created_at >= $windowClause;
"@)
        if ($enrollmentCycleRows.Length -gt 0) {
            $enrollmentCycle = Parse-TabRow -Row $enrollmentCycleRows[0] -Columns @("EnrollmentCount", "AvgMinutes")
        }

        $auditDistributionRows = @(Invoke-MySqlQuery -DefaultsFilePath $defaultsFile -Query @"
SELECT action, COUNT(*) AS action_count
FROM audit_logs
WHERE created_at >= $windowClause
GROUP BY action
ORDER BY action_count DESC, action ASC;
"@)
        foreach ($row in $auditDistributionRows) {
            $auditDistribution += Parse-TabRow -Row $row -Columns @("Action", "Count")
        }

        $enrollmentStatusRows = @(Invoke-MySqlQuery -DefaultsFilePath $defaultsFile -Query @"
SELECT status, COUNT(*) AS status_count
FROM enrollments
GROUP BY status
ORDER BY status_count DESC, status ASC;
"@)
        foreach ($row in $enrollmentStatusRows) {
            $enrollmentStatusDistribution += Parse-TabRow -Row $row -Columns @("Status", "Count")
        }
    }
    catch {
        $databaseReachable = $false
        $databaseError = $_.Exception.Message
        if ($RequireDatabase) {
            throw
        }
        Write-Warning "Database baseline extraction failed. Continuing with non-database signals. Details: $databaseError"
    }

    $backupHistoryPath = Join-Path $env:APPDATA "School Management System\BackupRestore\history.json"
    $backupHistoryWindow = @()
    if (Test-Path -LiteralPath $backupHistoryPath) {
        $backupHistoryJson = Get-Content -LiteralPath $backupHistoryPath -Raw
        if (-not [string]::IsNullOrWhiteSpace($backupHistoryJson)) {
            $backupHistory = $backupHistoryJson | ConvertFrom-Json
            $backupHistoryWindow = @($backupHistory | Where-Object {
                $_.TimestampUtc -and ([DateTime]$_.TimestampUtc -ge $windowStartUtc)
            })
        }
    }

    $backupFailedCount = @($backupHistoryWindow | Where-Object { "$($_.Status)".ToUpperInvariant() -eq "FAILED" }).Count
    $restoreFailedCount = @($backupHistoryWindow | Where-Object {
        "$($_.Action)".ToUpperInvariant().Contains("RESTORE") -and "$($_.Status)".ToUpperInvariant() -eq "FAILED"
    }).Count

    $notificationPath = Join-Path $env:APPDATA "School Management System\Notifications\notifications.json"
    $notificationSignals = @()
    if (Test-Path -LiteralPath $notificationPath) {
        $notificationJson = Get-Content -LiteralPath $notificationPath -Raw
        if (-not [string]::IsNullOrWhiteSpace($notificationJson)) {
            $allNotifications = $notificationJson | ConvertFrom-Json
            $notificationSignals = @($allNotifications | Where-Object {
                $_.CreatedAtUtc -and ([DateTime]$_.CreatedAtUtc -ge $windowStartUtc) -and (
                    "$($_.Category)".ToUpperInvariant() -eq "DATABASE" -or
                    "$($_.Title)".ToUpperInvariant().Contains("FAILED") -or
                    "$($_.Message)".ToUpperInvariant().Contains("FAILED") -or
                    "$($_.Message)".ToUpperInvariant().Contains("ERROR")
                )
            })
        }
    }

    $supportRows = @()
    if (Test-Path -LiteralPath $SupportIssuesCsv) {
        $supportRows = @(Import-Csv -LiteralPath $SupportIssuesCsv | Where-Object {
            $_.opened_utc -and ([DateTime]$_.opened_utc -ge $windowStartUtc)
        })
    }

    $supportIssuesOpened = $supportRows.Count
    $supportIssuesUnresolved = @($supportRows | Where-Object {
        [string]::IsNullOrWhiteSpace($_.closed_utc) -or "$($_.status)".ToUpperInvariant() -eq "OPEN"
    }).Count

    $taskStudyRows = @()
    if (Test-Path -LiteralPath $TaskTimeStudyCsv) {
        $taskStudyRows = @(Import-Csv -LiteralPath $TaskTimeStudyCsv | Where-Object {
            $_.captured_utc -and ([DateTime]$_.captured_utc -ge $windowStartUtc)
        })
    }

    $taskStudyByWorkflow = @()
    if ($taskStudyRows.Count -gt 0) {
        $taskStudyByWorkflow = @(
            $taskStudyRows |
            Group-Object workflow |
            ForEach-Object {
                $durations = @($_.Group | ForEach-Object { [double]$_.duration_seconds })
                [PSCustomObject]@{
                    Workflow = $_.Name
                    Samples = $durations.Count
                    AvgSeconds = [math]::Round(($durations | Measure-Object -Average).Average, 2)
                    MinSeconds = [math]::Round(($durations | Measure-Object -Minimum).Minimum, 2)
                    MaxSeconds = [math]::Round(($durations | Measure-Object -Maximum).Maximum, 2)
                }
            }
        )
    }

    $errorRatePercent = if ($totalAuditActions -gt 0) {
        [math]::Round(($errorAuditActions / $totalAuditActions) * 100.0, 2)
    }
    else {
        0.0
    }

    $rollbackIncidentCount = $rollbackAuditActions + $archiveRestoreEvents

    $baseline = [ordered]@{
        CapturedAtUtc = $capturedAtUtc.ToString("O")
        LookbackDays = $LookbackDays
        WindowStartUtc = $windowStartUtc.ToString("O")
        Environment = $connectionParts.ActiveEnvironment
        Database = $connectionParts.Database
        DatabaseReachable = $databaseReachable
        DatabaseError = $databaseError
        Metrics = [ordered]@{
            TaskTime = [ordered]@{
                SessionDurationProxy = [ordered]@{
                    SessionCount = To-IntOrZero $sessionMetrics.SessionCount
                    AvgSeconds = To-DoubleOrZero $sessionMetrics.AvgSeconds
                    MinSeconds = To-DoubleOrZero $sessionMetrics.MinSeconds
                    MaxSeconds = To-DoubleOrZero $sessionMetrics.MaxSeconds
                }
                EnrollmentCycleProxy = [ordered]@{
                    EnrollmentCount = To-IntOrZero $enrollmentCycle.EnrollmentCount
                    AvgMinutes = To-DoubleOrZero $enrollmentCycle.AvgMinutes
                }
                TaskStudy = [ordered]@{
                    SourceFile = $TaskTimeStudyCsv
                    SampleCount = $taskStudyRows.Count
                    ByWorkflow = $taskStudyByWorkflow
                }
            }
            ErrorRate = [ordered]@{
                TotalAuditActions = $totalAuditActions
                ErrorAuditActions = $errorAuditActions
                ErrorRatePercent = $errorRatePercent
                BackupFailedSignals = $backupFailedCount
                RestoreFailedSignals = $restoreFailedCount
            }
            RollbackIncidents = [ordered]@{
                RollbackLikeAuditActions = $rollbackAuditActions
                ArchiveRestoreEvents = $archiveRestoreEvents
                CombinedIncidentCount = $rollbackIncidentCount
            }
            SupportIssues = [ordered]@{
                SourceFile = $SupportIssuesCsv
                OpenedCount = $supportIssuesOpened
                UnresolvedCount = $supportIssuesUnresolved
                NotificationFallbackSignals = $notificationSignals.Count
            }
        }
    }

    $jsonPath = Join-Path $OutputDir ("baseline_{0}.json" -f $stamp)
    $mdPath = Join-Path $OutputDir ("baseline_{0}.md" -f $stamp)
    $auditCsvPath = Join-Path $OutputDir ("audit_action_distribution_{0}.csv" -f $stamp)
    $enrollmentCsvPath = Join-Path $OutputDir ("enrollment_status_distribution_{0}.csv" -f $stamp)

    $baseline | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $jsonPath -Encoding UTF8
    $auditDistribution | Export-Csv -NoTypeInformation -LiteralPath $auditCsvPath -Encoding UTF8
    $enrollmentStatusDistribution | Export-Csv -NoTypeInformation -LiteralPath $enrollmentCsvPath -Encoding UTF8

    $summary = @()
    $summary += "# Baseline Snapshot ($stamp)"
    $summary += ""
    $summary += "- Captured UTC: $($baseline.CapturedAtUtc)"
    $summary += "- Lookback days: $LookbackDays"
    $summary += "- Environment: $($connectionParts.ActiveEnvironment)"
    $summary += "- Database: $($connectionParts.Database)"
    $summary += "- Database reachable: $($baseline.DatabaseReachable)"
    if (-not $baseline.DatabaseReachable -and -not [string]::IsNullOrWhiteSpace($baseline.DatabaseError)) {
        $summary += "- Database error: $($baseline.DatabaseError)"
    }
    $summary += ""
    $summary += "## Core Metrics"
    $summary += ""
    $summary += "| Metric | Value |"
    $summary += "|---|---:|"
    $summary += "| Task Time - Session Avg (sec) | $([math]::Round($baseline.Metrics.TaskTime.SessionDurationProxy.AvgSeconds,2)) |"
    $summary += "| Task Time - Enrollment Cycle Avg (min) | $([math]::Round($baseline.Metrics.TaskTime.EnrollmentCycleProxy.AvgMinutes,2)) |"
    $summary += "| Error Rate (%) | $([math]::Round($baseline.Metrics.ErrorRate.ErrorRatePercent,2)) |"
    $summary += "| Rollback Incidents (combined) | $($baseline.Metrics.RollbackIncidents.CombinedIncidentCount) |"
    $summary += "| Support Issues Opened | $($baseline.Metrics.SupportIssues.OpenedCount) |"
    $summary += "| Support Issues Unresolved | $($baseline.Metrics.SupportIssues.UnresolvedCount) |"
    $summary += "| Notification Fallback Signals | $($baseline.Metrics.SupportIssues.NotificationFallbackSignals) |"
    $summary += ""
    $summary += "## Source Notes"
    $summary += ""
    $summary += "- Task study source: $TaskTimeStudyCsv"
    $summary += "- Support issue source: $SupportIssuesCsv"
    $summary += "- If support CSV is missing, support counts may remain zero and fallback notifications are used only as signal."
    $summary += "- Use -EnvironmentOverride Local/Remote/Online to target a different configured environment."
    $summary += ""
    $summary += "## Output Files"
    $summary += ""
    $summary += "- $jsonPath"
    $summary += "- $mdPath"
    $summary += "- $auditCsvPath"
    $summary += "- $enrollmentCsvPath"

    $summary -join [Environment]::NewLine | Set-Content -LiteralPath $mdPath -Encoding UTF8

    Write-Host "Baseline export complete." -ForegroundColor Green
    Write-Host "Summary: $mdPath"
    Write-Host "JSON:    $jsonPath"
}
finally {
    if (Test-Path -LiteralPath $defaultsFile) {
        Remove-Item -LiteralPath $defaultsFile -Force -ErrorAction SilentlyContinue
    }
}
