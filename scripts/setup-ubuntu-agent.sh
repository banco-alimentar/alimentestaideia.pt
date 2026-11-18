#!/usr/bin/env bash
set -euo pipefail

usage() {
    cat <<'EOF'
Usage: sudo bash scripts/setup-ubuntu-agent.sh --agent-user USER --allow-docker-group

Prepare an existing Azure Pipelines agent host running Ubuntu 24.04 LTS (amd64).
Install Git, curl, zip/unzip, Chrome, matching ChromeDriver, Docker Engine/Compose,
Ruby/Minitest/Psych, Python 3, PowerShell 7, Azure CLI, and native .NET dependencies.

USER must be the existing, non-root account running the agent service.
--allow-docker-group acknowledges that Docker group membership grants root-equivalent access.

Run only while the agent is stopped and the host has no active container workloads.
Existing vendor APT repositories are reused. Conflicting distribution Docker packages
are not removed automatically. Rerunning can upgrade installed software.

The existing agent registration is not changed. .NET SDK 10 is installed by UseDotNet@2
in the pipelines. No Azure credentials or PATs are requested or stored.
EOF
}

die() {
    echo "Error: $*" >&2
    exit 1
}

add_repository() {
    local name="$1" uri="$2" suite="$3" component="$4" key_url="$5"
    local key="/etc/apt/keyrings/alimentestaideia-${name}.asc"
    if grep -RqsF -- "$uri" /etc/apt/sources.list.d; then
        echo "Reusing existing APT repository: $uri"
        return
    fi

    curl --fail --silent --show-error --location --retry 3 "$key_url" \
        --output "$work_dir/$name.asc"
    install -m 0644 "$work_dir/$name.asc" "$key"
    printf 'deb [arch=amd64 signed-by=%s] %s %s %s\n' \
        "$key" "$uri" "$suite" "$component" > "/etc/apt/sources.list.d/${name}.list"
}

resolve_chromedriver() {
    python3 - "$1" "$2" <<'PY'
import json
import re
import sys

version, manifest = sys.argv[1:]
if not re.fullmatch(r"\d+\.\d+\.\d+\.\d+", version):
    sys.exit(f"Invalid installed Chrome version: {version}")
build = version.rsplit(".", 1)[0]
with open(manifest, encoding="utf-8") as source:
    entry = json.load(source)["builds"].get(build)
if entry is None:
    sys.exit(f"No ChromeDriver published for Chrome build {build}; retry after publication.")
for download in entry.get("downloads", {}).get("chromedriver", []):
    if download["platform"] == "linux64":
        url = download["url"]
        match = re.fullmatch(
            r"https://storage\.googleapis\.com/chrome-for-testing-public/"
            r"(\d+\.\d+\.\d+\.\d+)/linux64/chromedriver-linux64\.zip", url
        )
        if match is None or match[1].rsplit(".", 1)[0] != build:
            sys.exit("ChromeDriver manifest contains an unexpected URL or mismatched build.")
        print(url)
        break
else:
    sys.exit(f"No linux64 ChromeDriver published for Chrome build {build}.")
PY
}

verify_tools() {
    git --version
    curl --version
    command -v zip unzip timeout >/dev/null
    google-chrome --version
    chromedriver --version
    python3 --version
    ruby -rminitest -rpsych -e 'puts "Ruby Minitest/Psych OK"'
    pwsh -NoLogo -NoProfile -Command '$ErrorActionPreference = "Stop"; $PSVersionTable.PSVersion.ToString(); if ($PSVersionTable.PSVersion.Major -lt 7) { exit 1 }'
    az version
    docker compose version
    docker info >/dev/null
}

main() {
    local agent_user="" allow_docker_group=false
    while [[ $# -gt 0 ]]; do
        case "$1" in
            --agent-user)
                [[ $# -ge 2 && "$2" != --* ]] || die "--agent-user requires a username."
                agent_user="$2"
                shift 2
                ;;
            --allow-docker-group) allow_docker_group=true; shift ;;
            -h|--help) usage; return ;;
            *) die "Unknown option: $1. Use --help." ;;
        esac
    done
    [[ -n "$agent_user" ]] || die "Specify --agent-user with the existing agent service account."
    [[ "$allow_docker_group" == true ]] || die "Read --help, then pass --allow-docker-group to acknowledge root-equivalent Docker access."
    [[ "$EUID" -eq 0 ]] || die "Run this script with sudo."
    local agent_uid
    agent_uid="$(id -u -- "$agent_user")" || die "Agent account '$agent_user' does not exist."
    [[ "$agent_uid" -ne 0 ]] || die "The agent must run as a non-root account."

    # shellcheck source=/dev/null
    source /etc/os-release
    [[ "$ID" == ubuntu && "$VERSION_ID" == 24.04 ]] || die "Only Ubuntu 24.04 LTS is supported."
    [[ "$(dpkg --print-architecture)" == amd64 ]] || die "Google Chrome requires an amd64 host for this setup."
    [[ -d /run/systemd/system ]] || die "A host running systemd is required; do not run inside a build container."
    systemctl show-environment >/dev/null

    local package status query_result
    for package in docker.io docker-compose docker-compose-v2 docker-doc docker-buildx podman-docker containerd runc; do
        if status="$(dpkg-query -W -f='${db:Status-Status}' "$package" 2>/dev/null)"; then
            :
        else
            query_result=$?
            [[ "$query_result" -eq 1 ]] || die "Could not query installed package '$package' (exit $query_result)."
            status="not-installed"
        fi
        [[ "$status" != installed ]] || die "Conflicting package '$package' is installed. Plan its migration to Docker CE manually before rerunning; no packages or containers were removed."
    done

    work_dir="$(mktemp -d -t alimentestaideia-agent.XXXXXXXX)"
    trap 'rm -f -- "$work_dir/docker.asc" "$work_dir/microsoft-prod.asc" "$work_dir/azure-cli.asc" "$work_dir/google-chrome.asc" "$work_dir/chromedriver.json" "$work_dir/chromedriver.zip" "$work_dir/chromedriver"; rmdir -- "$work_dir"' EXIT
    trap 'exit 130' INT
    trap 'exit 143' TERM
    export DEBIAN_FRONTEND=noninteractive

    apt-get update
    apt-get install -y --no-install-recommends \
        ca-certificates curl git bash coreutils zip unzip util-linux \
        python3 ruby ruby-minitest ruby-psych \
        libc6 libgcc-s1 libstdc++6 libicu74 libssl3t64 zlib1g libkrb5-3 libunwind8 tzdata
    install -d -m 0755 /etc/apt/keyrings
    add_repository docker https://download.docker.com/linux/ubuntu noble stable \
        https://download.docker.com/linux/ubuntu/gpg
    add_repository microsoft-prod https://packages.microsoft.com/ubuntu/24.04/prod noble main \
        https://packages.microsoft.com/keys/microsoft.asc
    add_repository azure-cli https://packages.microsoft.com/repos/azure-cli/ noble main \
        https://packages.microsoft.com/keys/microsoft.asc
    add_repository google-chrome https://dl.google.com/linux/chrome/deb/ stable main \
        https://dl.google.com/linux/linux_signing_key.pub
    apt-get update
    apt-get install -y --no-install-recommends \
        docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin \
        google-chrome-stable powershell azure-cli

    local chrome_version driver_url driver_version
    chrome_version="$(google-chrome --product-version)"
    curl --fail --silent --show-error --location --retry 3 \
        https://googlechromelabs.github.io/chrome-for-testing/latest-patch-versions-per-build-with-downloads.json \
        --output "$work_dir/chromedriver.json"
    driver_url="$(resolve_chromedriver "$chrome_version" "$work_dir/chromedriver.json")"
    curl --fail --silent --show-error --location --retry 3 "$driver_url" \
        --output "$work_dir/chromedriver.zip"
    unzip -p "$work_dir/chromedriver.zip" chromedriver-linux64/chromedriver > "$work_dir/chromedriver"
    chmod 0755 "$work_dir/chromedriver"
    driver_version="$("$work_dir/chromedriver" --version)"
    [[ "$driver_version" == "ChromeDriver ${chrome_version%.*}."* ]] || die "Downloaded ChromeDriver does not match Chrome $chrome_version."
    install -m 0755 "$work_dir/chromedriver" /usr/local/bin/chromedriver

    echo "Granting root-equivalent Docker group access to $agent_user (explicitly acknowledged)."
    usermod -aG docker "$agent_user"
    systemctl enable --now docker

    export PATH="/usr/local/bin:/usr/bin:/bin:$PATH"
    echo "Verifying tools and Docker access as $agent_user..."
    runuser -u "$agent_user" -- env PATH="$PATH" bash -euo pipefail -c "$(declare -f verify_tools); verify_tools"
    cat <<'EOF'

Host dependencies installed and verified.
Restart the existing Azure Pipelines agent service so it picks up PATH and Docker group changes.
Keep /usr/local/bin on the service PATH. UseDotNet@2 installs .NET SDK 10 during pipeline runs.
Keep loopback ports 1025 and 8025 free for Mailpit; no public inbound ports are needed.
After a Chrome upgrade, rerun this script while the agent is stopped to refresh ChromeDriver.
EOF
}

if [[ "${BASH_SOURCE[0]}" == "$0" ]]; then
    main "$@"
fi
