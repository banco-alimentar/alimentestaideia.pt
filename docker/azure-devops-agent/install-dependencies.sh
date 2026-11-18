#!/usr/bin/env bash
set -euo pipefail

architecture="$(dpkg --print-architecture)"
if [[ "$architecture" != amd64 ]]; then
    echo "Error: This agent requires amd64, but the base image is $architecture. Rebuild with docker build --platform linux/amd64." >&2
    exit 1
fi

# Adapted from scripts/setup-ubuntu-agent.sh at be09d270ffe8213b15c40c7695f047b103f67a75.
# Containers use the host Docker daemon; do not install/start systemd or dockerd here.
apt-get update
apt-get install -y --no-install-recommends \
    ca-certificates curl git openssh-client bash coreutils zip unzip util-linux tini \
    python3 ruby ruby-minitest ruby-psych \
    libc6 libgcc-s1 libstdc++6 libicu74 libssl3t64 zlib1g libkrb5-3 libunwind8 \
    libcurl4t64 liblttng-ust1t64 libnuma1 tzdata

install -d -m 0755 /etc/apt/keyrings
add_repository() {
    local name="$1" uri="$2" suite="$3" component="$4" key_url="$5"
    curl --fail --silent --show-error --location --retry 3 "$key_url" \
        --output "/etc/apt/keyrings/$name.asc"
    printf 'deb [arch=amd64 signed-by=/etc/apt/keyrings/%s.asc] %s %s %s\n' \
        "$name" "$uri" "$suite" "$component" > "/etc/apt/sources.list.d/$name.list"
}

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
    docker-ce-cli docker-buildx-plugin docker-compose-plugin \
    google-chrome-stable powershell azure-cli

chrome_version="$(google-chrome --product-version)"
curl --fail --silent --show-error --location --retry 3 \
    https://googlechromelabs.github.io/chrome-for-testing/latest-patch-versions-per-build-with-downloads.json \
    --output /tmp/chromedriver.json
driver_url="$(python3 - "$chrome_version" <<'PY'
import json
import re
import sys

version = sys.argv[1]
if not re.fullmatch(r"\d+\.\d+\.\d+\.\d+", version):
    sys.exit(f"Invalid installed Chrome version: {version}")
build = version.rsplit(".", 1)[0]
with open("/tmp/chromedriver.json", encoding="utf-8") as source:
    entry = json.load(source)["builds"].get(build)
if entry is None:
    sys.exit(f"No ChromeDriver published for Chrome build {build}; rebuild after publication.")
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
)"
curl --fail --silent --show-error --location --retry 3 "$driver_url" \
    --output /tmp/chromedriver.zip
unzip -p /tmp/chromedriver.zip chromedriver-linux64/chromedriver > /usr/local/bin/chromedriver
chmod 0755 /usr/local/bin/chromedriver
[[ "$(chromedriver --version)" == "ChromeDriver ${chrome_version%.*}."* ]]

git --version
google-chrome --version
chromedriver --version
python3 --version
ruby -rminitest -rpsych -e 'puts "Ruby Minitest/Psych OK"'
pwsh -NoLogo -NoProfile -Command '$ErrorActionPreference = "Stop"; $PSVersionTable.PSVersion.ToString(); if ($PSVersionTable.PSVersion.Major -lt 7) { exit 1 }'
az version
docker --version
docker compose version
docker buildx version

rm /tmp/chromedriver.json /tmp/chromedriver.zip
apt-get clean
rm -rf /var/lib/apt/lists
