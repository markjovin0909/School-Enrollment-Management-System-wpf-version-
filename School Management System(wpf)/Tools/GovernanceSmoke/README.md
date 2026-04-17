# Governance Smoke Checks

This tool validates the structural governance backend (`SR-01` to `SR-06`) using an in-memory database.

## Run

From project root:

```powershell
dotnet run --project Tools/GovernanceSmoke/SMS.GovernanceSmoke.csproj
```

Or with helper script:

```powershell
powershell -ExecutionPolicy Bypass -File Tools/GovernanceSmoke/Run-SmokeChecks.ps1
```

## What it checks

- Permission boundary decisions
- Enrollment submit/approve/forbidden transition enforcement
- Enrollment transition history persistence
- Exception queue dedupe behavior
- Restore preflight blocking/check outputs
- Operational metrics snapshot generation
