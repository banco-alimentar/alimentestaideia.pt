#!/usr/bin/env bash
set -euo pipefail

die() {
    echo "Error: $*" >&2
    exit 1
}

[[ "$(id -u)" -ne 0 ]] || die "Run the agent as the image's non-root user."
[[ -n "${AZP_URL:-}" ]] || die "Set AZP_URL to your Azure DevOps organization URL."
[[ -n "${AZP_POOL:-}" ]] || die "Set AZP_POOL to an existing self-hosted agent pool."
[[ -n "${AZP_TOKEN_FILE:-}" && -s "$AZP_TOKEN_FILE" && -r "$AZP_TOKEN_FILE" ]] \
    || die "Mount a non-empty, readable PAT file and set AZP_TOKEN_FILE."
[[ -z "${AZP_TOKEN:-}" ]] || die "Use AZP_TOKEN_FILE instead of exposing a PAT in the environment."

export VSO_AGENT_IGNORE="AZP_TOKEN,AZP_TOKEN_FILE${VSO_AGENT_IGNORE:+,$VSO_AGENT_IGNORE}"
export AZP_AGENT_NAME="${AZP_AGENT_NAME:-$(hostname)}"
export AZP_WORK="${AZP_WORK:-/azp/_work}"
mkdir -p "$AZP_WORK"
docker info >/dev/null \
    || die "Cannot access Docker. Mount the host socket and add its numeric group with --group-add."

# Keep the PAT out of command arguments and logs, including the package discovery request.
package_url="$(python3 - <<'PY'
import base64
import json
import os
from pathlib import Path
import sys
import urllib.parse
import urllib.request

url = os.environ["AZP_URL"].rstrip("/")
parsed = urllib.parse.urlsplit(url)
if parsed.scheme != "https" or not parsed.hostname or parsed.username or parsed.password or parsed.query or parsed.fragment:
    sys.exit("AZP_URL must be an HTTPS organization URL without credentials, query, or fragment.")
query = {"platform": "linux-x64", "top": "1"}
if os.environ.get("AZP_AGENT_VERSION"):
    query["packageVersion"] = os.environ["AZP_AGENT_VERSION"]
token = Path(os.environ["AZP_TOKEN_FILE"]).read_text(encoding="utf-8").strip()
if not token:
    sys.exit("AZP_TOKEN_FILE contains an empty PAT.")
auth = base64.b64encode(f":{token}".encode()).decode()
request = urllib.request.Request(
    f"{url}/_apis/distributedtask/packages/agent?{urllib.parse.urlencode(query)}",
    headers={"Authorization": f"Basic {auth}"},
)
with urllib.request.urlopen(request, timeout=60) as response:
    packages = json.load(response)["value"]
if not packages:
    sys.exit("No matching linux-x64 Azure Pipelines agent package was found.")
download_url = packages[0]["downloadUrl"]
if urllib.parse.urlsplit(download_url).scheme != "https":
    sys.exit("The agent package download URL must use HTTPS.")
print(download_url)
PY
)"
curl --fail --silent --show-error --location --retry 3 "$package_url" --output agent.tar.gz
tar --extract --gzip --file agent.tar.gz
rm agent.tar.gz
# The agent's environment script reads optional variables without nounset guards.
set +u
source ./env.sh
set -u

child=""
configuration_started=false
cleanup() {
    local status=$?
    trap - EXIT INT TERM
    if [[ -n "$child" ]] && kill -0 "$child" 2>/dev/null; then
        kill -TERM "$child"
        if wait "$child"; then
            :
        else
            echo "Agent stopped with exit code $?." >&2
        fi
    fi
    if [[ "$configuration_started" == true ]]; then
        echo "Removing Azure Pipelines agent registration..."
        local attempt removed=false
        for attempt in 1 2 3; do
            if VSTS_AGENT_INPUT_TOKEN="$(< "$AZP_TOKEN_FILE")" \
                ./config.sh remove --unattended --auth pat; then
                removed=true
                break
            fi
            echo "Agent removal attempt $attempt failed." >&2
            sleep 5
        done
        if [[ "$removed" != true ]]; then
            echo "Agent removal failed; remove '$AZP_AGENT_NAME' from '$AZP_POOL' manually." >&2
            status=1
        fi
    fi
    exit "$status"
}
trap cleanup EXIT
trap 'exit 130' INT
trap 'exit 143' TERM

configuration_started=true
VSTS_AGENT_INPUT_TOKEN="$(< "$AZP_TOKEN_FILE")" ./config.sh \
    --unattended --acceptTeeEula --auth pat \
    --url "$AZP_URL" --pool "$AZP_POOL" --agent "$AZP_AGENT_NAME" --work "$AZP_WORK"

./run.sh "$@" &
child=$!
wait "$child"
