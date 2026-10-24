#!/usr/bin/env bash
# Wipes the database and fills it with test data (2 admins, 5 supervisors, 20 students, templates, pathways, evaluations, badges).
# Runs the server once in Development with --seed-only: applies migrations, seeds, and exits (the site is not started).
# Test accounts: first.last@gamp.test, password = DbSeeder.TestPassword (GamP-SCPeriop.Server/Data/DbSeeder.cs)
#
# Usage:
#   ./scripts/seed.sh                                  # database from appsettings.Development.json
#   ./scripts/seed.sh -f                               # no confirmation
#   ./scripts/seed.sh -c "Server=localhost,1433;..."   # another database
set -euo pipefail

root="$(cd "$(dirname "$0")/.." && pwd)"
force=false
connection=""

while [ $# -gt 0 ]; do
    case "$1" in
        -f|--force) force=true ;;
        -c|--connection-string) connection="$2"; shift ;;
        *) echo "Unknown option: $1" >&2; exit 2 ;;
    esac
    shift
done

command -v dotnet >/dev/null || { echo "The .NET SDK is not installed (dotnet was not found)." >&2; exit 1; }

if [ "$force" = false ]; then
    target="the database in appsettings.Development.json"
    [ -n "$connection" ] && target="the database in --connection-string"
    read -r -p "This DELETES all data in $target and replaces it with test data. Continue? (y/N) " answer
    case "$answer" in y|Y|yes|s|sim) ;; *) echo "Cancelled."; exit 1 ;; esac
fi

export ASPNETCORE_ENVIRONMENT=Development
[ -n "$connection" ] && export ConnectionStrings__DefaultConnection="$connection"

dotnet run --project "$root/GamP-SCPeriop.Server" --no-launch-profile -- --Logging:LogLevel:Microsoft.EntityFrameworkCore=Warning --seed-only

echo
echo "Done. Log in with any first.last@gamp.test account (password: DbSeeder.TestPassword)"
