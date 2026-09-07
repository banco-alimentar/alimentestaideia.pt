#!/usr/bin/env bash

# Ensure the settings required by the Admin Function "Run now" queue exist
# locally and in the developer, preprod, and production Azure environments.
# Existing values are never overwritten. Secret values are never printed.
#
# Usage:
#   ./scripts/ensure-function-execution-command-settings.sh --dry-run
#   ./scripts/ensure-function-execution-command-settings.sh

set -euo pipefail

resource_group="AlimenteEstaIdeia"
function_app_name="AlimentaEstaIdeia-tools"
web_app_name="AlimentaEstaIdeia"
repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
local_function_settings="$repo_root/BancoAlimentar.AlimentaEstaIdeia.Function/local.settings.json"
local_web_settings="$repo_root/BancoAlimentar.AlimentaEstaIdeia.Web/appsettings.Development.json"
queue_name="function-execution-commands"
queue_connection_string=""
dry_run=0
skip_local=0
skip_azure=0

usage() {
    cat <<'EOF'
Usage:
  ensure-function-execution-command-settings.sh [options]

Options:
  --resource-group <name>             Azure resource group.
  --function-app <name>               Azure Function App name.
  --web-app <name>                    Azure Web App name.
  --queue-name <name>                 Queue name (default: function-execution-commands).
  --queue-connection-string <value>   Queue connection string for missing Azure settings.
  --dry-run                           Report changes without writing files or Azure settings.
  --skip-local                        Do not inspect or update local JSON settings.
  --skip-azure                        Do not inspect or update Azure settings.
  -h, --help                          Show this help.

The Azure settings use the environment-variable form:
  FunctionExecutionCommands__Enabled
  FunctionExecutionCommands__ConnectionString
  FunctionExecutionCommands__QueueName

The local Web JSON file uses the equivalent .NET configuration form with colons.
Existing settings, including disabled or empty settings, are reported but not overwritten.
EOF
}

while [[ $# -gt 0 ]]; do
    case "$1" in
        --resource-group) resource_group="${2:-}"; shift 2 ;;
        --function-app) function_app_name="${2:-}"; shift 2 ;;
        --web-app) web_app_name="${2:-}"; shift 2 ;;
        --queue-name) queue_name="${2:-}"; shift 2 ;;
        --queue-connection-string) queue_connection_string="${2:-}"; shift 2 ;;
        --dry-run) dry_run=1; shift ;;
        --skip-local) skip_local=1; shift ;;
        --skip-azure) skip_azure=1; shift ;;
        -h|--help) usage; exit 0 ;;
        *) echo "Unknown argument: $1" >&2; usage >&2; exit 2 ;;
    esac
done

if [[ -z "$resource_group" || -z "$function_app_name" || -z "$web_app_name" || -z "$queue_name" ]]; then
    echo "Resource group, application names, and queue name cannot be empty." >&2
    exit 2
fi

if [[ ! "$queue_name" =~ ^[a-z0-9]([a-z0-9-]{1,61}[a-z0-9])?$ ]]; then
    echo "Queue name must be a lowercase Azure Queue name: $queue_name" >&2
    exit 2
fi

if [[ "$skip_local" -eq 0 ]]; then
    if ! command -v python3 >/dev/null 2>&1; then
        echo "Python 3 is required to update local JSON settings." >&2
        exit 1
    fi

    echo "Local Function settings: $local_function_settings"
    python3 - "$local_function_settings" "$queue_name" "$dry_run" <<'PY'
import json
import os
import sys

path, queue_name, dry_run = sys.argv[1:]
dry_run = dry_run == "1"
if os.path.exists(path):
    with open(path, encoding="utf-8") as stream:
        document = json.load(stream)
else:
    document = {"IsEncrypted": False, "Values": {}}

values = document.setdefault("Values", {})
required = {
    "FunctionExecutionCommands__Enabled": "true",
    "FunctionExecutionCommands__ConnectionString": "UseDevelopmentStorage=true",
    "FunctionExecutionCommands__QueueName": queue_name,
}
changed = False
for name, value in required.items():
    if name not in values:
        values[name] = value
        changed = True
        print(f"  {name}: {'would create' if dry_run else 'created'}")
    elif str(values[name]).strip() == "":
        print(f"  {name}: present but empty")
    elif name.endswith("Enabled") and str(values[name]).strip().lower() != "true":
        print(f"  {name}: present but disabled")
    else:
        print(f"  {name}: present")

if changed and not dry_run:
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", encoding="utf-8") as stream:
        json.dump(document, stream, indent=2)
        stream.write("\n")
PY

    echo "Local Web settings: $local_web_settings"
    python3 - "$local_web_settings" "$queue_name" "$dry_run" <<'PY'
import json
import os
import sys

path, queue_name, dry_run = sys.argv[1:]
dry_run = dry_run == "1"
if os.path.exists(path):
    with open(path, encoding="utf-8") as stream:
        document = json.load(stream)
else:
    document = {}

required = {
    "FunctionExecutionCommands:Enabled": False,
    "FunctionExecutionCommands:ConnectionString": "UseDevelopmentStorage=true",
    "FunctionExecutionCommands:QueueName": queue_name,
}
changed = False
for name, value in required.items():
    if name not in document:
        document[name] = value
        changed = True
        print(f"  {name}: {'would create' if dry_run else 'created'}")
    elif str(document[name]).strip() == "":
        print(f"  {name}: present but empty")
    elif name.endswith(":Enabled") and str(document[name]).strip().lower() != "true":
        print(f"  {name}: present but disabled")
    else:
        print(f"  {name}: present")

if changed and not dry_run:
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", encoding="utf-8") as stream:
        json.dump(document, stream, indent=2)
        stream.write("\n")
PY
fi

if [[ "$skip_azure" -eq 1 ]]; then
    exit 0
fi

if ! command -v az >/dev/null 2>&1; then
    echo "Azure CLI ('az') is required for Azure checks." >&2
    exit 1
fi
if ! az account show --only-show-errors >/dev/null 2>&1; then
    echo "Run 'az login' first, or use --skip-azure." >&2
    exit 1
fi

azure_setting_name() {
    local app_kind="$1" app_name="$2" slot_name="$3" canonical_name="$4"
    local query="[?name=='$canonical_name'] | [0].name"
    local args=("$app_kind" config appsettings list --resource-group "$resource_group" --name "$app_name" --query "$query" --output tsv --only-show-errors)
    if [[ -n "$slot_name" ]]; then
        args+=(--slot "$slot_name")
    fi
    az "${args[@]}"
}

azure_setting_value() {
    local app_kind="$1" app_name="$2" slot_name="$3" canonical_name="$4"
    local query="[?name=='$canonical_name'] | [0].value"
    local args=("$app_kind" config appsettings list --resource-group "$resource_group" --name "$app_name" --query "$query" --output tsv --only-show-errors)
    if [[ -n "$slot_name" ]]; then
        args+=(--slot "$slot_name")
    fi
    az "${args[@]}"
}

apply_azure_app() {
    local app_kind="$1" app_name="$2" environment_name="$3" slot_name="$4"
    local -a names=(
        "FunctionExecutionCommands__Enabled"
        "FunctionExecutionCommands__ConnectionString"
        "FunctionExecutionCommands__QueueName"
    )
    local -a desired=("true" "" "$queue_name")
    local -a missing=() values=()
    local existing_name existing_value fallback_connection=""
    local index

    echo
    echo "$environment_name — $app_kind $app_name${slot_name:+ (slot: $slot_name)}"

    if [[ "$app_kind" == "functionapp" ]]; then
        fallback_connection="$(azure_setting_value functionapp "$app_name" "$slot_name" "AzureWebJobsStorage")"
    else
        fallback_connection="$(azure_setting_value functionapp "$function_app_name" "$slot_name" "AzureWebJobsStorage")"
    fi
    if [[ -n "$queue_connection_string" ]]; then
        desired[1]="$queue_connection_string"
    else
        desired[1]="$fallback_connection"
    fi

    for index in "${!names[@]}"; do
        existing_name="$(azure_setting_name "$app_kind" "$app_name" "$slot_name" "${names[$index]}")"
        existing_value="$(azure_setting_value "$app_kind" "$app_name" "$slot_name" "${names[$index]}")"

        if [[ -n "$existing_name" ]]; then
            if [[ -z "$existing_value" ]]; then
                echo "  ${names[$index]}: present but empty"
            elif [[ "${names[$index]}" == "FunctionExecutionCommands__Enabled" && "${existing_value,,}" != "true" ]]; then
                echo "  ${names[$index]}: present but disabled"
            else
                echo "  ${names[$index]}: present"
            fi
            continue
        fi

        if [[ -z "${desired[$index]}" ]]; then
            echo "  ${names[$index]}: missing (not created; provide --queue-connection-string)" >&2
            continue
        fi

        echo "  ${names[$index]}: $([[ "$dry_run" -eq 1 ]] && echo 'would create' || echo 'missing')"
        missing+=("${names[$index]}")
        values+=("${desired[$index]}")
    done

    if [[ "$dry_run" -eq 1 || "${#missing[@]}" -eq 0 ]]; then
        return 0
    fi

    local -a args=("$app_kind" config appsettings set --resource-group "$resource_group" --name "$app_name" --only-show-errors --output none)
    if [[ -n "$slot_name" ]]; then
        args+=(--slot "$slot_name" --slot-settings)
    else
        args+=(--settings)
    fi
    for index in "${!missing[@]}"; do
        args+=("${missing[$index]}=${values[$index]}")
    done
    az "${args[@]}"
    echo "  Missing settings created."
}

echo
echo "Azure settings"
echo "Subscription and tenant are taken from the current Azure CLI login."

apply_azure_app functionapp "$function_app_name" developer developer
apply_azure_app functionapp "$function_app_name" preprod preprod
apply_azure_app functionapp "$function_app_name" prod ""
apply_azure_app webapp "$web_app_name" developer developer
apply_azure_app webapp "$web_app_name" preprod preprod
apply_azure_app webapp "$web_app_name" prod ""

if [[ "$dry_run" -eq 1 ]]; then
    echo
    echo "Dry run complete. No local or Azure settings were changed."
else
    echo
    echo "Settings check complete. Existing values were preserved."
fi
