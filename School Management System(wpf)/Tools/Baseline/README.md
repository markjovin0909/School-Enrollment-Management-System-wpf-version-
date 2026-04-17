# Baseline Export Tool

## Purpose
Generate Step 1 baseline artifacts for the frozen workflow scope.

## Command
```powershell
powershell -ExecutionPolicy Bypass -File .\Tools\Baseline\Export-Baseline.ps1 -LookbackDays 30
```

## Optional Inputs
```powershell
-OutputDir ".\Governance\Baseline\BaselineReports"
-SupportIssuesCsv ".\Governance\Baseline\SupportIssues.csv"
-TaskTimeStudyCsv ".\Governance\Baseline\TaskTimeStudy.csv"
-EnvironmentOverride "Local"
-RequireDatabase
```

## Generated Outputs
1. `baseline_*.json`
2. `baseline_*.md`
3. `audit_action_distribution_*.csv`
4. `enrollment_status_distribution_*.csv`

## Notes
1. Reads connection settings from `App.config` using `ActiveEnvironment`.
2. `-EnvironmentOverride` lets you force `Local`, `Remote`, or `Online`.
2. Uses `mysql` CLI installed on machine.
3. Avoid committing secrets or machine-specific generated data unless explicitly required.
