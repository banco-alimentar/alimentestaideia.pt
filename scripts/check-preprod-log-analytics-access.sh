#!/usr/bin/env bash

# Read-only diagnostic for the Site Health report's Azure identity and
# Log Analytics permissions. This script never prints secret values.

set -euo pipefail

usage() {
    cat <<'EOF'
Usage:
  check-preprod-log-analytics-access.sh \
    --resource-group <resource-group> \
    --app-name <app-service-name> \
    [--slot <slot-name>] \
    [--workspace-id <workspace-customer-id>]

Example:
  ./scripts/check-preprod-log-analytics-access.sh \
    --resource-group my-preprod-rg \
    --app-name alimentaestaideia-preprod \
    --workspace-id 41951796-bdfb-4289-9456-69f2e3d991b7

The script requires Azure CLI and an authenticated account with permission to
read the App Service, its settings, role assignments, and Log Analytics workspaces.
EOF
}

resource_group=""
app_name=""
slot_name=""
workspace_id="41951796-bdfb-4289-9456-69f2e3d991b7"

while [[ $# -gt 0 ]]; do
    case "$1" in
        --resource-group)
            resource_group="${2:-}"
            shift 2
            ;;
        --app-name)
            app_name="${2:-}"
            shift 2
            ;;
        --slot)
            slot_name="${2:-}"
            shift 2
            ;;
        --workspace-id)
            workspace_id="${2:-}"
            shift 2
            ;;
        -h|--help)
            usage
            exit 0
            ;;
        *)
            echo "Unknown argument: $1" >&2
            usage >&2
            exit 2
            ;;
    esac
done

if [[ -z "$resource_group" || -z "$app_name" ]]; then
    usage >&2
    exit 2
fi

if ! command -v az >/dev/null 2>&1; then
    echo "Azure CLI ('az') is required." >&2
    exit 1
fi

if ! az account show --only-show-errors >/dev/null 2>&1; then
    echo "Run 'az login' first." >&2
    exit 1
fi

webapp_args=(--resource-group "$resource_group" --name "$app_name")
if [[ -n "$slot_name" ]]; then
    webapp_args+=(--slot "$slot_name")
fi

echo "App Service: $app_name${slot_name:+ (slot: $slot_name)}"
echo "Resource group: $resource_group"
echo "Workspace customer ID: $workspace_id"
echo

identity_json="$(az webapp identity show "${webapp_args[@]}" --only-show-errors -o json)"

system_principal_id="$(az webapp identity show "${webapp_args[@]}" \
    --query principalId -o tsv --only-show-errors)"
system_client_id="$(az webapp identity show "${webapp_args[@]}" \
    --query clientId -o tsv --only-show-errors)"

if [[ -n "$system_principal_id" && "$system_principal_id" != "null" ]]; then
    echo "System-assigned managed identity principal ID: $system_principal_id"
    echo "System-assigned managed identity client ID:    $system_client_id"
else
    echo "System-assigned managed identity: not enabled"
fi

user_assigned_client_ids="$(python3 - "$identity_json" <<'PY'
import json
import sys

identity = json.loads(sys.argv[1])
for value in (identity.get("userAssignedIdentities") or {}).values():
    client_id = value.get("clientId")
    if client_id:
        print(client_id)
PY
)"

if [[ -n "$user_assigned_client_ids" ]]; then
    echo "User-assigned managed identity client ID(s):"
    while IFS= read -r client_id; do
        [[ -n "$client_id" ]] && echo "  $client_id"
    done <<< "$user_assigned_client_ids"
else
    echo "User-assigned managed identities: none"
fi

setting_names="$(az webapp config appsettings list "${webapp_args[@]}" \
    --query "[?name=='AZURE_CLIENT_ID' || name=='AZURE_TENANT_ID' || name=='AZURE_CLIENT_SECRET'].name" \
    -o tsv --only-show-errors)"

echo
echo "Credential-related app settings present (values intentionally hidden):"
if [[ -n "$setting_names" ]]; then
    while IFS= read -r setting_name; do
        [[ -n "$setting_name" ]] && echo "  $setting_name"
    done <<< "$setting_names"
else
    echo "  none"
fi

has_client_id=0
has_tenant_id=0
has_client_secret=0
grep -qx 'AZURE_CLIENT_ID' <<< "$setting_names" && has_client_id=1 || true
grep -qx 'AZURE_TENANT_ID' <<< "$setting_names" && has_tenant_id=1 || true
grep -qx 'AZURE_CLIENT_SECRET' <<< "$setting_names" && has_client_secret=1 || true

echo
if [[ "$has_client_id" -eq 1 && "$has_tenant_id" -eq 1 && "$has_client_secret" -eq 1 ]]; then
    echo "Likely credential selected by DefaultAzureCredential: EnvironmentCredential"
    configured_client_id="$(az webapp config appsettings list "${webapp_args[@]}" \
        --query "[?name=='AZURE_CLIENT_ID'].value | [0]" -o tsv --only-show-errors)"
    echo "Configured AZURE_CLIENT_ID: $configured_client_id"
    echo "Check Log Analytics permissions for this service principal, not only the App Service identity."
elif [[ "$has_client_id" -eq 1 ]]; then
    echo "Likely credential selected by DefaultAzureCredential: user-assigned managed identity"
    configured_client_id="$(az webapp config appsettings list "${webapp_args[@]}" \
        --query "[?name=='AZURE_CLIENT_ID'].value | [0]" -o tsv --only-show-errors)"
    echo "Configured managed identity client ID: $configured_client_id"
else
    echo "Likely credential selected by DefaultAzureCredential: App Service managed identity"
fi

workspace_resource_id="$(az monitor log-analytics workspace list \
    --query "[?customerId=='$workspace_id'].id | [0]" \
    -o tsv --only-show-errors)"

echo
if [[ -z "$workspace_resource_id" ]]; then
    echo "Workspace not found in the current Azure subscription/context."
    echo "Check the workspace ID and run 'az account set --subscription <subscription>'."
    exit 1
fi

echo "Workspace resource ID: $workspace_resource_id"

show_assignments() {
    local label="$1"
    local principal_id="$2"

    [[ -z "$principal_id" || "$principal_id" == "null" ]] && return 0

    echo
    echo "$label ($principal_id) role assignments affecting the workspace:"
    az role assignment list \
        --assignee-object-id "$principal_id" \
        --scope "$workspace_resource_id" \
        --include-inherited \
        --query "[?principalId=='$principal_id'].{Role:roleDefinitionName,Scope:scope,PrincipalId:principalId}" \
        -o table --only-show-errors
}

show_assignments "System identity" "$system_principal_id"

if [[ -n "$user_assigned_client_ids" ]]; then
    while IFS= read -r client_id; do
        [[ -z "$client_id" ]] && continue
        user_assigned_principal_id="$(az identity list \
            --query "[?clientId=='$client_id'].principalId | [0]" \
            -o tsv --only-show-errors)"
        show_assignments "User-assigned identity $client_id" "$user_assigned_principal_id"
    done <<< "$user_assigned_client_ids"
fi

if [[ "$has_client_id" -eq 1 && "$has_tenant_id" -eq 1 && "$has_client_secret" -eq 1 ]]; then
    service_principal_object_id="$(az ad sp show --id "$configured_client_id" \
        --query id -o tsv --only-show-errors 2>/dev/null || true)"
    show_assignments "Environment service principal $configured_client_id" "$service_principal_object_id"
fi

echo
echo "Expected role: Log Analytics Reader (or a custom role containing"
echo "Microsoft.OperationalInsights/workspaces/query/*/read)."
echo "If the expected identity has no matching assignment above, that explains the 403."
echo "Role changes may require a restart and time to propagate."
