#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

die() {
    echo "Error: $*" >&2
    exit 1
}

require_command() {
    command -v "$1" >/dev/null 2>&1 || die "Required command '$1' was not found."
}

require_command dotnet
require_command curl
require_command python3
require_command docker

sdk_version="$(dotnet --version)"
case "$sdk_version" in
    10.*) ;;
    *) die "This repository requires .NET SDK 10; found $sdk_version." ;;
esac

chrome_available=false
for browser in google-chrome google-chrome-stable chromium chromium-browser; do
    if command -v "$browser" >/dev/null 2>&1; then
        chrome_available=true
        break
    fi
done
if [[ "$chrome_available" == false && "${OSTYPE:-}" == darwin* ]]; then
    if [[ -d "/Applications/Google Chrome.app" || -d "$HOME/Applications/Google Chrome.app" ]]; then
        chrome_available=true
    fi
fi
[[ "$chrome_available" == true ]] || die "Install Google Chrome or Chromium before running the Selenium suite."
docker compose version >/dev/null 2>&1 || die "Docker Compose is unavailable; install Docker Desktop."
docker info >/dev/null 2>&1 || die "Docker is not running; start Docker Desktop."

failures=()
mailpit_started=false
mailpit_project="alimentestaideia-local-tests-$$"

cleanup() {
    local exit_code=$?
    if [[ "$mailpit_started" == true ]]; then
        echo "Stopping the Mailpit container started by this script..."
        docker compose --project-name "$mailpit_project" --file compose.mailpit.yml down --volumes || \
            echo "Warning: could not stop this script's Mailpit container." >&2
    fi
    exit "$exit_code"
}
trap cleanup EXIT
trap 'exit 130' INT
trap 'exit 143' TERM

run_suite() {
    local name="$1"
    shift

    printf '\n== %s ==\n' "$name"
    if "$@"; then
        echo "PASS: $name"
    else
        echo "FAIL: $name" >&2
        failures+=("$name")
    fi
}

mailpit_ready() {
    curl --fail --silent --show-error --max-time 2 \
        http://127.0.0.1:8025/api/v1/messages >/dev/null 2>&1 || return 1

    python3 -c 'import socket; s=socket.create_connection(("127.0.0.1", 1025), timeout=2); s.settimeout(2); banner=s.recv(64); s.close(); raise SystemExit(not banner.startswith(b"220"))' \
        >/dev/null 2>&1
}

echo "Using .NET SDK $sdk_version from $repo_root"

dotnet restore BancoAlimentar.AlimentaEstaIdeia.Web.sln
dotnet build BancoAlimentar.AlimentaEstaIdeia.Web.sln --configuration Release --no-restore

run_suite "Repository tests" dotnet test \
    BancoAlimentar.AlimentaEstaIdeia.Repository.Tests/BancoAlimentar.AlimentaEstaIdeia.Repository.Tests.csproj \
    --configuration Release --no-build --no-restore
run_suite "Sas.Core tests" dotnet test \
    BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tests/BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tests.csproj \
    --configuration Release --no-build --no-restore
run_suite "Web integration tests" dotnet test \
    BancoAlimentar.AlimentaEstaldeia.Web.IntegrationTests/BancoAlimentar.AlimentaEstaldeia.Web.Integration.Tests.csproj \
    --configuration Release --no-build --no-restore
run_suite "Azure Functions tests" dotnet test \
    BancoAlimentar.AlimentaEstaIdeia.Function.Tests/BancoAlimentar.AlimentaEstaIdeia.Function.Tests.csproj \
    --configuration Release --no-build --no-restore
run_suite "Selenium offline tests" bash scripts/test-selenium.sh offline --no-build --no-restore -- \
    RunConfiguration.TreatNoTestsAsError=true

echo "Starting the repository's pinned Mailpit container..."
mailpit_started=true
if ! docker compose --project-name "$mailpit_project" --file compose.mailpit.yml up --detach; then
    echo "Could not start Mailpit. Make sure loopback ports 8025 (HTTP) and 1025 (SMTP) are free." >&2
    exit 1
fi

mailpit_is_ready=false
for attempt in {1..45}; do
    if mailpit_ready; then
        mailpit_is_ready=true
        break
    fi
    sleep 1
done

if [[ "$mailpit_is_ready" == true ]]; then
    run_suite "Selenium local browser tests" env \
        SeleniumTest__Target=Local \
        SeleniumTest__Headless="${SeleniumTest__Headless:-true}" \
        SeleniumTest__Local__SelfContained=true \
        SeleniumTest__Local__MailpitBaseUrl=http://127.0.0.1:8025 \
        SeleniumTest__Local__MailpitSmtpPort=1025 \
        bash scripts/test-selenium.sh local --no-build --no-restore -- \
        RunConfiguration.TreatNoTestsAsError=true
else
    echo "Mailpit did not become ready on loopback ports 8025 (HTTP) and 1025 (SMTP)." >&2
    failures+=("Mailpit startup")
    failures+=("Selenium local browser tests (Mailpit unavailable)")
fi

if ((${#failures[@]} > 0)); then
    echo >&2
    echo "Failed or skipped suites:" >&2
    printf ' - %s\n' "${failures[@]}" >&2
    exit 1
fi

echo
echo "All local test suites passed."
