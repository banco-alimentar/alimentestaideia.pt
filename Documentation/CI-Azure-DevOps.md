# Azure DevOps CI/CD

Deploy pipelines for [alimentestaideia.pt](https://dev.azure.com/BancoAlimentar/Alimentestaideia.pt/) run in Azure DevOps. GitHub Actions (`.github/workflows/alimentestaideia.yaml`) only builds and tests; it does **not** deploy to Azure slots.

## Pipeline files in this repo

| File | Purpose |
|------|---------|
| [`azure-pipelines/developer-debug.yml`](../azure-pipelines/developer-debug.yml) | Build, test, deploy **developer** web + function slots |
| [`azure-pipelines/preprod-release.yml`](../azure-pipelines/preprod-release.yml) | Apply EF migrations, deploy **preprod** slot (replaces classic release id=5) |
| [`azure-pipeline-selenium-ui-tests.yml`](../azure-pipeline-selenium-ui-tests.yml) | Legacy Selenium UI test publish (branch `developer`) |

**SDK:** The solution targets **.NET 10** (`global.json` pins SDK `10.0.300`). Every build pipeline must run **UseDotNet@2** / **setup-dotnet** with **`10.0.x`** before restore. Hosted agents do not include .NET 10 by default.

Both appear under the **Pipeline** folder in the Visual Studio solution.

## Pre-production deploy flow (YAML)

The `preprod-release.yml` pipeline replaces the classic release **[PRE-PROD] core version** (definition id=5).

**Stages:** Apply EF migrations → Deploy web (preprod slot) → Disable function timers on function app preprod slot.

**Trigger:** manual (optionally wire `resources.pipelines.coreBuild.trigger` to your release branch).

**Artifact:** Web zip from the **Alimentestaideia.pt Core** build pipeline (adjust `resources.pipelines.coreBuild.source` if your build name differs).

### Why migrations run in the pipeline

Runtime migration on first tenant request (`TenantInitializationService`) can leave a window where new code is live but the schema is stale (HTTP 500 on pages such as CampaignsHistory). The pipeline applies migrations **before** deploying the new package.

Two contexts must be updated:

| Context | Project | Typical Key Vault secret |
|---------|---------|--------------------------|
| `ApplicationDbContext` (tenant DB) | `BancoAlimentar.AlimentaEstaIdeia.Web` | `ConnectionStrings--DefaultConnection` |
| `InfrastructureDbContext` | `BancoAlimentar.AlimentaEstaIdeia.Sas.Model` | `ConnectionStrings:Infrastructure` |

Local or one-off apply (after `az login` and SQL firewall):

```powershell
$tenantCs = az keyvault secret show --vault-name alimentaestaideia-key --name ConnectionStrings--DefaultConnection --query value -o tsv
.\scripts\apply-database-migrations.ps1 -DefaultConnection $tenantCs
```

Script: [`scripts/apply-database-migrations.ps1`](../scripts/apply-database-migrations.ps1).

## Developer deploy flow (YAML)

The `developer-debug.yml` pipeline replaces the classic pair:

- Build **developer-debug** (definition id=11)
- Release **[DEV] AlimentaEstaIdeia** (definition id=3)

**Stages:** Build → Deploy web (developer slot) → Deploy function (developer slot) → Disable function timers on developer slot.

**Trigger:** pushes to `developer`.

**Agents:** Microsoft-hosted (`vmImage: windows-latest`).

Browser / Playwright E2E tests are **not** run in Azure pipelines. Use unit, integration, and (optionally) Selenium against dev from your machine — see [TESTS.md](TESTS.md).

## First-time setup in Azure DevOps

1. **Create the YAML pipeline**  
   Pipelines → New pipeline → Azure Repos Git → **Existing Azure Pipelines YAML file** → `/azure-pipelines/developer-debug.yml` (developer) or `/azure-pipelines/preprod-release.yml` (preprod).

2. **Service connection**  
   Set `AzureServiceConnection` to the ARM connection **name** (Project settings → Service connections). For this project the classic GUID `7ada5308-2372-46ef-af95-bdfff138b059` maps to:

   `Microsoft Azure Sponsorship BA(f1b937fb-ca82-4eb6-a452-77af7a531344)`

   On first run, Azure DevOps may prompt to **authorize** the pipeline to use this connection — approve when asked.

3. **Pipeline extensions** (org marketplace)  
   - [Replace Tokens](https://marketplace.visualstudio.com/items?itemName=qetza.replacetokens) (`replacetokens@5`)  

   Assembly versioning uses built-in `PowerShell@2` and `scripts/ci/version-dotnet-assemblies.ps1` (no marketplace extension required).

4. **Environments**  
   Create (or rename in YAML): `Developer`, `Function developer`, `PRE-PROD`.

5. **Key Vault**  
   Pipeline reads secrets from `alimentaestaideia-key` via `AzureKeyVault@2` before token replacement in `appsettings*.json`.

6. **Parallel jobs**  
   Confirm at least one [Microsoft-hosted parallel job](https://dev.azure.com/BancoAlimentar/_admin/_buildQueue?_a=resourceLimits) is available.

7. **Validate, then retire classic**  
   After a green run, disable classic release id=3 (developer) and release id=5 (preprod). Build definition id=11 already uses this YAML file.

### Troubleshooting: `Requested SDK version: 10.0.300` / exit code 145

Hosted agents ship .NET 9 by default. This YAML runs **UseDotNet@2** with **`10.0.x`** before restore. If you still see SDK 9 only, confirm the run used **revision** of the definition that points at `azure-pipelines/developer-debug.yml` (not an old classic designer revision).

Classic **Alimentestaideia.pt Core** (id=8), if still active, must also use **UseDotNet → 10.0.x**.

### Troubleshooting: missing versioning marketplace task

The YAML pipeline uses `scripts/ci/version-dotnet-assemblies.ps1` via `PowerShell@2` instead of **Manifest Versioning Build Tasks**. If an older revision still references `VersionDotNetCoreAssemblies` or `IvanSklylevVersioningTask`, update to the current `azure-pipelines/developer-debug.yml`.

### Troubleshooting: service connection not found / not authorized

YAML tasks need the connection **name**, not the GUID. If validation fails with `AlimenteEstaIdeia-Azure` (or similar placeholder), set `AzureServiceConnection` to the name shown in Project settings → Service connections.

If the name is correct but the run still fails, open the pipeline run → **View** the authorization prompt for the ARM connection, or go to the service connection → **⋯** → **Security** → allow access to the pipeline.

## Variables (edit in YAML or pipeline UI)

| Variable | Typical value |
|----------|----------------|
| `AzureServiceConnection` | `Microsoft Azure Sponsorship BA(f1b937fb-ca82-4eb6-a452-77af7a531344)` |
| `WebAppName` | `alimentaestaideia` |
| `FunctionAppName` | `AlimentaEstaIdeia-tools` |
| `DeveloperSlot` | `developer` |
| `ResourceGroupName` | `AlimenteEstaIdeia` |
| `KeyVaultName` | `alimentaestaideia-key` |
| `PreprodSlot` | `preprod` |

## Environments and URLs

| Slot | URL |
|------|-----|
| Developer | https://alimentaestaideia-developer.azurewebsites.net |
| Pre-production | https://alimentaestaideia-preprod.azurewebsites.net |
| Production | https://alimentestaideia.pt |

## Migrating from self-hosted agents

Classic pipelines used the **Default** self-hosted pool (queue id=10, pool id=1). Microsoft-hosted uses **Azure Pipelines** (queue id=18, pool id=9).

Temporary helper scripts (delete after migration):

| Script | Purpose |
|--------|---------|
| `scripts/debug-ado-agent-pool.ps1` | Diagnose queued/offline agents |
| `scripts/verify-ado-build-pools.ps1` | Show queue/pool id per definition |
| `scripts/switch-ado-pools-to-hosted.ps1` | Move classic definitions to hosted queue 18 |
| `scripts/export-ado-pipelines-to-yaml.ps1` | Export classic build/release drafts to YAML |
| `scripts/apply-database-migrations.ps1` | Apply EF migrations for tenant + infrastructure databases |
| `scripts/configure-function-slot-timer-settings.ps1` | Disable timer functions on non-production function app slots |
| `scripts/remove-dev-release-e2e.ps1` | Remove Playwright E2E environment from classic **[DEV] AlimentaEstaIdeia** release |

Requires `$env:AZDO_PAT` with Build (Read) and Release (Read); write scope for the switch/remove scripts.

## Browser tests (local / dev only)

The removed `BancoAlimentar.AlimentaEstaIdeia.Web.EndToEndTests` Playwright project is no longer in the repo. Azure **[DEV] AlimentaEstaIdeia** must not run Playwright — disable the **E2E tests** release environment (see script above) or migrate to `azure-pipelines/developer-debug.yml`, which has no E2E stage.

## Related documentation

- [TESTS.md](TESTS.md) — test layers and CI expectations  
- [Contributing-Windows-setup.md](Contributing-Windows-setup.md) — local dev and deployment slots  
- [SolutionReadme.md](SolutionReadme.md) — solution architecture overview  
