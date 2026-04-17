param(
    [switch]$NoRestore
)

$ErrorActionPreference = "Stop"

$project = Join-Path $PSScriptRoot "SMS.GovernanceSmoke.csproj"
$args = @("run", "--project", $project)
if ($NoRestore)
{
    $args += "--no-restore"
}

Write-Host "Running governance smoke checks..." -ForegroundColor Cyan
dotnet @args

if ($LASTEXITCODE -ne 0)
{
    throw "Governance smoke checks failed."
}

Write-Host "Governance smoke checks passed." -ForegroundColor Green
