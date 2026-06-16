# Azure DevOps CI/CD

Deploy pipelines for [alimentestaideia.pt](https://dev.azure.com/BancoAlimentar/Alimentestaideia.pt/) run in Azure DevOps. GitHub Actions (`.github/workflows/alimentestaideia.yaml`) only builds and tests; it does **not** deploy to Azure slots.

## Pipeline files in this repo

| File | Purpose |
|------|---------|
| [`azure-pipelines/developer-debug.yml`](../azure-pipelines/developer-debug.yml) | Build, test, deploy **developer** web + function slots |
| [`azure-pipeline-selenium-ui-tests.yml`](../azure-pipeline-selenium-ui-tests.yml) | Legacy Selenium UI test publish (branch `developer`) |

Both appear under the **Pipeline** folder in the Visual Studio solution.

## Developer deploy flow (YAML)

The `developer-debug.yml` pipeline replaces the classic pair:

- Build **developer-debug** (definition id=11)
- Release **[DEV] AlimentaEstaIdeia** (definition id=3)

**Stages:** Build → Deploy web (developer slot) → Deploy function (developer slot).

**Trigger:** pushes to `developer`.

**Agents:** Microsoft-hosted (`vmImage: windows-latest`).

Browser / Playwright E2E tests are **not** run in Azure pipelines. Use unit, integration, and (optionally) Selenium against dev from your machine — see [TESTS.md](TESTS.md).

## First-time setup in Azure DevOps

1. **Create the YAML pipeline**  
   Pipelines → New pipeline → Azure Repos Git → **Existing Azure Pipelines YAML file** → `/azure-pipelines/developer-debug.yml`.

2. **Service connection**  
   Set `AzureServiceConnection` in the YAML (or as a pipeline variable) to your ARM service connection **name** (Project settings → Service connections). The classic pipeline used a connection GUID; the name is what YAML needs.

3. **Pipeline extensions** (org marketplace)  
   - [Replace Tokens](https://marketplace.visualstudio.com/items?itemName=qetza.replacetokens) (`replacetokens@5`)  
   - [Version .NET Core Assemblies](https://marketplace.visualstudio.com/items?itemName=IvanSkrylev.IvanSklylevVersioningTask) — verify the task name matches your org install  

4. **Environments**  
   Create (or rename in YAML): `Developer`, `Function developer`.

5. **Key Vault**  
   Pipeline reads secrets from `alimentaestaideia-key` via `AzureKeyVault@2` before token replacement in `appsettings*.json`.

6. **Parallel jobs**  
   Confirm at least one [Microsoft-hosted parallel job](https://dev.azure.com/BancoAlimentar/_admin/_buildQueue?_a=resourceLimits) is available.

7. **Validate, then retire classic**  
   After a green run, disable classic build id=11 and release id=3.

## Variables (edit in YAML or pipeline UI)

| Variable | Typical value |
|----------|----------------|
| `AzureServiceConnection` | ARM connection name |
| `WebAppName` | `alimentaestaideia` |
| `FunctionAppName` | `AlimentaEstaIdeia-tools` |
| `DeveloperSlot` | `developer` |
| `ResourceGroupName` | `AlimenteEstaIdeia` |
| `KeyVaultName` | `alimentaestaideia-key` |

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
| `scripts/remove-dev-release-e2e.ps1` | Remove Playwright E2E environment from classic **[DEV] AlimentaEstaIdeia** release |

Requires `$env:AZDO_PAT` with Build (Read) and Release (Read); write scope for the switch/remove scripts.

## Browser tests (local / dev only)

The removed `BancoAlimentar.AlimentaEstaIdeia.Web.EndToEndTests` Playwright project is no longer in the repo. Azure **[DEV] AlimentaEstaIdeia** must not run Playwright — disable the **E2E tests** release environment (see script above) or migrate to `azure-pipelines/developer-debug.yml`, which has no E2E stage.

## Related documentation

- [TESTS.md](TESTS.md) — test layers and CI expectations  
- [Contributing-Windows-setup.md](Contributing-Windows-setup.md) — local dev and deployment slots  
- [SolutionReadme.md](SolutionReadme.md) — solution architecture overview  
