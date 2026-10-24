<#
.SYNOPSIS
    Wipes the database and fills it with test data (2 admins, 5 supervisors, 20 students, templates, pathways, evaluations, badges).

.DESCRIPTION
    Runs the server once in Development with --seed-only: applies migrations, seeds, and exits (the site is not started).
    By default it uses the connection string from GamP-SCPeriop.Server/appsettings.Development.json (LocalDB).
    Test accounts: first.last@gamp.test, password = DbSeeder.TestPassword (GamP-SCPeriop.Server/Data/DbSeeder.cs)

.EXAMPLE
    .\scripts\seed.ps1
.EXAMPLE
    .\scripts\seed.ps1 -Force
.EXAMPLE
    .\scripts\seed.ps1 -ConnectionString "Server=localhost,1433;Database=GamP_Test;User Id=sa;Password=...;TrustServerCertificate=True"
#>
param(
    # Use another database instead of the one in appsettings.Development.json
    [string]$ConnectionString,
    # Don't ask for confirmation
    [switch]$Force
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "GamP-SCPeriop.Server"

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Error "The .NET SDK is not installed (dotnet was not found)."
}

$target = if ($ConnectionString) { "the database in -ConnectionString" } else { "the database in appsettings.Development.json" }
if (-not $Force) {
    $answer = Read-Host "This DELETES all data in $target and replaces it with test data. Continue? (y/N)"
    if ($answer -notmatch '^(y|yes|s|sim)$') { Write-Host "Cancelled."; exit 1 }
}

$env:ASPNETCORE_ENVIRONMENT = "Development"
if ($ConnectionString) { $env:ConnectionStrings__DefaultConnection = $ConnectionString }

try {
    dotnet run --project $project --no-launch-profile -- --Logging:LogLevel:Microsoft.EntityFrameworkCore=Warning --seed-only
    if ($LASTEXITCODE -ne 0) { Write-Error "Seeding failed (exit code $LASTEXITCODE)." }
    Write-Host ""
    Write-Host "Done. Log in with any first.last@gamp.test account (password: DbSeeder.TestPassword)" -ForegroundColor Green
}
finally {
    Remove-Item Env:ConnectionStrings__DefaultConnection -ErrorAction SilentlyContinue
}
