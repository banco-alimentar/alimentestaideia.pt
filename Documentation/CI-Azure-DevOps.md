# Azure DevOps CI/CD

Deploy pipelines for [alimentestaideia.pt](https://dev.azure.com/BancoAlimentar/Alimentestaideia.pt/) run in Azure DevOps. GitHub Actions (`.github/workflows/alimentestaideia.yaml`) only builds and tests; it does **not** deploy to Azure slots.

## Pipeline files in this repo

| File | Purpose |
|------|---------|
| [`azure-pipelines/core-build.yml`](../azure-pipelines/core-build.yml) | **Release** build artifact for preprod/prod (replaces classic id=8) |
| [`azure-pipelines/developer-debug.yml`](../azure-pipelines/developer-debug.yml) | Build, test, deploy **developer** web + function slots |
| [`azure-pipelines/atcud-tax.yml`](../azure-pipelines/atcud-tax.yml) | Build, test, deploy pushes to `734-2026-atcud` to the **tax** web slot |
| [`azure-pipelines/preprod-release.yml`](../azure-pipelines/preprod-release.yml) | Apply EF migrations, deploy **preprod** slot (replaces classic release id=5) |
| [`azure-pipeline-selenium-ui-tests.yml`](../azure-pipeline-selenium-ui-tests.yml) | Secret-free Selenium validation for PRs targeting `developer`/`master`, plus manual runs |

**SDK:** The solution targets **.NET 10** (`global.json` pins SDK `10.0.300`). Every build pipeline must run **UseDotNet@2** / **setup-dotnet** with **`10.0.x`** before restore. Hosted agents do not include .NET 10 by default.

The shared browser job is [`azure-pipelines/templates/selenium-job.yml`](../azure-pipelines/templates/selenium-job.yml).

## PR validation and trusted branch builds

GitHub **Build and Test** runs on PRs targeting `developer` or `master`, pushes to those branches,
and manual dispatch. It uses a read-only repository token and has no workflow-wide path exclusions:
documentation-only PRs also receive a result. Its **Required build and tests** aggregate runs even
when upstream jobs fail or are skipped, and succeeds only when the build and all test jobs succeed.

Azure **Selenium regression tests** runs the same isolated browser job for PRs targeting either
branch and for manual runs. It has `trigger: none`, avoiding an extra standalone browser run on
each branch push. Browser validation on pushes is owned by the Core and developer pipelines.

The Core and developer pipelines set `pr: none`. They always begin with **BrowserRegression**.
Credentialed stages are included only for the following trusted contexts:

| Pipeline | Source refs | Allowed run reasons | After successful browser validation |
|----------|-------------|---------------------|-------------------------------------|
| `developer-debug` | `refs/heads/developer` | `IndividualCI`, `BatchedCI`, `Manual` | Build/test, publish, then deploy developer web and Functions slots |
| `atcud-tax` | `refs/heads/734-2026-atcud` | `IndividualCI`, `BatchedCI`, `Manual` | Build/test, publish, then deploy the web app to tax |
| Core | `refs/heads/developer`, `refs/heads/master` | `IndividualCI`, `BatchedCI`, `Manual` | Build deployable projects and publish release packages |

For PR refs, tags, feature branches, missing context, or other run reasons, compile-time guards
omit the stages containing Key Vault, token replacement, artifact publication, and deployments.
Runtime guards repeat the branch/reason checks and require each named dependency to be exactly
`Succeeded`. Failed, skipped, canceled, or `SucceededWithIssues` browser validation cannot release
artifacts or deploy. A manual feature-branch run is test-only; it cannot deploy that feature branch.

The ATCUD tax pipeline deploys only the web app to the `tax` slot. It does not deploy Functions
or apply database migrations. If a branch change requires a schema update, apply the tax database
migrations as an ordered deployment step before the code relies on the new schema. It is separate
from the existing classic beta release, which this pipeline does not modify.

These are repository execution guards, not a security boundary against someone who can change
the YAML. Environment/service-connection branch control, approvals, trusted-template policies,
and GitHub protected branches remain separate administrative controls.

## Core release packaging

After its isolated browser stage succeeds, `azure-pipelines/core-build.yml` uses `ubuntu-24.04`
and restores and builds only the public
website, tenant-management website, and Functions app, including their referenced libraries.
The packaging job does not restore or build the test projects. Browser validation runs in its own
job with its own build; unit/integration validation remains in GitHub Actions and the developer pipeline.

Each publish task sets `publishWebProjects: false` so its explicit project path is used.
The three project-named ZIP files and the `drop` artifact remain unchanged for release consumers.

The shared `azure-pipelines/templates/report-disk-usage.yml` step reports filesystem capacity,
inode usage, and the largest source, NuGet cache, artifact, temporary, and tool-cache directories
before restore, after restore/before build, after build/before publishing, and after each publish.
These read-only, best-effort reports also run after failures; they do not delete agent files.
Compare consecutive reports when diagnosing `MSB3026: No space left on device` or agent loss.
Pinning the agent image prevents automatic major Ubuntu upgrades; it does not increase disk capacity.

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

**Stages:** BrowserRegression → Build/test/publish → Deploy web (developer slot) → Deploy function (developer slot) → Disable function timers on developer slot.

**Trigger:** pushes to `developer`, plus manual queueing. PR validation belongs to the dedicated
secret-free pipelines; PR-trigger UI overrides still cannot include credentialed stages in this YAML.

**Agents:** Microsoft-hosted `ubuntu-24.04` for Selenium; `windows-latest` for build/deployment.

The shared browser job gates the build/publish stage. Web and function deployment explicitly depend
on successful browser/build stages, with the function stage also requiring successful web deployment.
All credentialed stages require a trusted developer-branch context. The browser job tests its
temporary application, not the deployed slot. See [TESTS.md](TESTS.md).

## Self-contained Selenium pipeline

[`azure-pipeline-selenium-ui-tests.yml`](../azure-pipeline-selenium-ui-tests.yml) replaces the legacy .NET 2.2 artifact-only pipeline. It restores and builds the Selenium project with .NET 10, runs the offline checks, starts Mailpit, and executes the self-contained browser selection with headless Chrome. The current selections contain 109 offline cases and 60 browser cases.

The pipeline uses a Microsoft-hosted `ubuntu-24.04` agent with Chrome, ChromeDriver, and Docker Compose. Docker runs only the pinned Mailpit image from `compose.mailpit.yml`; the .NET application and Chrome run directly on the agent. You do not need Docker on your Mac. Mailpit's SMTP and API ports bind only to loopback, and browser fixtures create temporary SQLite in-memory application and infrastructure databases. Payment-provider responses are simulated locally. No SQL Server, Key Vault access, Azure service connection, SMTP credentials, or real recipient accounts are needed.

Both selections fail if no tests match and publish TRX results to the Azure run's **Tests** tab, including on failure. Cleanup runs on success, failure, or cancellation within the job's cancellation timeout and removes only that run's Mailpit Compose project and temporary inbox. The inbox is not published as an artifact because messages contain test-account security links. An agent lost before cleanup relies on hosted-agent disposal.

The shared job first runs Ruby standard-library policy tests for triggers, template expansion,
branch/reason guards, dependency failures, and reporting contracts. After cleanup, a read-only
Python standard-library verifier checks both explicit TRX files for complete passing results,
consistent counters/identities, and unique display names. Missing, malformed, failed, skipped,
incomplete, or duplicate-name reports fail validation, but the publication step still runs.

Run 2412 proved the original hosted job executed 109 offline checks and 60 browser cases successfully.
Its Tests tab displayed 162 because eight long-URL theory cases had the same truncated display name.
Short scenario labels now distinguish these cases; the verifier detects this regression without
rewriting reports. The refactored job and its reporting fix still require a new hosted validation run.

To configure it in Azure DevOps:

1. Edit the existing Selenium YAML pipeline to use `/azure-pipeline-selenium-ui-tests.yml`, or create a pipeline using that existing YAML file. A classic pipeline is not updated by changing this file.
2. Use the GitHub repository connection and check that pipeline UI settings do not override YAML triggers. The file validates PRs targeting `developer` and `master`; manual runs can select any branch containing the changes.
3. Validate this refactor by manually running only the standalone Selenium pipeline on the feature branch. Confirm all 169 .NET test results appear, with no failures/skips, and the policy/report checks pass. Do not queue a deployment-capable pipeline to validate unmerged changes.
4. After a PR demonstrates the expected statuses, review required checks for both protected branches. The standalone pipeline gates merges only when configured as required; deployment/package gates are explicit dependencies inside their respective YAML pipelines.

The pipeline explicitly selects `Local` and `SelfContained=true`, even though it runs in Azure. It never selects the deployed developer slot, sends email through its SMTP server, applies cloud migrations, or changes staging financial records. Developer-slot tests remain a separate, explicitly configured workflow described in [Local-Email-Tests.md](Local-Email-Tests.md#run-against-the-developer-slot).

## Administrative follow-up and deferred smoke tests

Repository changes do not modify these external settings:

- Review Azure pipeline UI trigger overrides and any classic releases that consume `drop`.
  A classic release does not automatically inherit YAML branch/reason guards.
- Restrict environment/service-connection access to trusted branches with appropriate approvals.
  Do not grant PR validation access to Key Vault or deployment credentials.
- After successful PR validation, require the actual GitHub **Required build and tests** status,
  the Azure **Selenium regression tests** check, and existing security checks for both `developer`
  and `master`. Preserve existing review/security requirements. Review any legacy required Core or
  developer deployment status before changing its requirement, since those pipelines no longer
  run automatically on PRs.
- Read-only post-deployment smoke tests are not implemented by this change. The 19 existing
  developer-slot browser cases remain manual and may create staging records/send slot emails.
  Before production promotion, retain the requirement for a successful preproduction smoke test.

Local configuration/report-policy validation requires Ruby with Minitest/Psych and Python 3;
these are available on the hosted Ubuntu 24.04 image. Run:

```bash
ruby scripts/ci/tests/pipeline_safety_test.rb
ruby scripts/ci/tests/test_result_reporting_test.rb
```

The policy tests exercise a deliberately limited YAML/expression model and the actual aggregate
shell/verifier CLI. They do not replace Azure service-side compilation or prove external permissions.

## First-time setup in Azure DevOps

1. **Create the YAML pipeline**  
   Pipelines → New pipeline → select the configured repository connection → **Existing Azure Pipelines YAML file**:
   - `/azure-pipelines/core-build.yml` — **Alimentestaideia.pt Core** (Release artifact, id=8)
   - `/azure-pipelines/developer-debug.yml` — developer build + deploy (id=11 / release id=3)
   - `/azure-pipelines/atcud-tax.yml` — branch-specific build + deploy to the tax web slot
   - `/azure-pipelines/preprod-release.yml` — preprod migrations + deploy (release id=5)

2. **Service connection**  
   Set `AzureServiceConnection` to the ARM connection **name** (Project settings → Service connections). For this project the classic GUID `7ada5308-2372-46ef-af95-bdfff138b059` maps to:

   `Microsoft Azure Sponsorship BA(f1b937fb-ca82-4eb6-a452-77af7a531344)`

   On first run, Azure DevOps may prompt to **authorize** the pipeline to use this connection — approve when asked.

3. **Pipeline extensions** (org marketplace)  
   - [Replace Tokens](https://marketplace.visualstudio.com/items?itemName=qetza.replacetokens) (`replacetokens@5`)  

   Assembly versioning uses built-in `PowerShell@2` and `scripts/ci/version-dotnet-assemblies.ps1` (no marketplace extension required).

4. **Environments**
   Create (or rename in YAML): `Developer`, `Function developer`, `TAX`, `PRE-PROD`.

5. **ATCUD tax App Service slot**
   In Azure Portal, open `alimentaestaideia` → **Deployment slots** → **Add**, and create a slot
   named `tax`. App Service deployment slots require a Standard, Premium, or Isolated plan tier.
   If you clone configuration from another slot, review it before starting the pipeline. Configure
   tax-specific database and external-service settings, and mark settings that must stay with this
   slot as **Deployment slot settings**. In particular, do not let the tax slot use a production
   database or send production email/payments during validation. The new YAML pipeline deploys code
   only; it does not migrate databases, deploy Functions, or swap slots.

6. **Key Vault**
   Pipeline reads secrets from `alimentaestaideia-key` via `AzureKeyVault@2` before token replacement. Secret names use `--` instead of `:` (e.g. `ConnectionStrings--Infrastructure`); tokens in `appsettings*.json` must match the secret name inside `#{…}#`.

   Optional food-bank seed settings are not in Key Vault; `appsettings*.json` uses empty strings for `AzureStorage:FoodBankSourceBlobName` and `AzureStorage:FoodBankSourceContainerName` (food-bank DB seeding is skipped when unset).

7. **Parallel jobs**
   Confirm at least one [Microsoft-hosted parallel job](https://dev.azure.com/BancoAlimentar/_admin/_buildQueue?_a=resourceLimits) is available.

8. **Validate, then retire classic**
   After a green run, disable classic build id=8 (Core), release id=3 (developer), and release id=5 (preprod). Build definition id=11 already uses `developer-debug.yml`.

### Troubleshooting: `Requested SDK version: 10.0.300` / exit code 145

Hosted agents ship .NET 9 by default. This YAML runs **UseDotNet@2** with **`10.0.x`** before restore. If you still see SDK 9 only, confirm the run used **revision** of the definition that points at `azure-pipelines/developer-debug.yml` (not an old classic designer revision).

Classic **Alimentestaideia.pt Core** (id=8) must use **UseDotNet → 10.0.x** (not 9.0.x). Prefer pointing definition id=8 at [`azure-pipelines/core-build.yml`](../azure-pipelines/core-build.yml), which matches `global.json` SDK **10.0.300**.

### Troubleshooting: missing versioning marketplace task

The YAML pipeline uses `scripts/ci/version-dotnet-assemblies.ps1` via `PowerShell@2` instead of **Manifest Versioning Build Tasks**. If an older revision still references `VersionDotNetCoreAssemblies` or `IvanSklylevVersioningTask`, update to the current `azure-pipelines/developer-debug.yml`.

Azure DevOps build numbers like `20260622.5` are kept as the package/informational version. The script maps them to a valid four-part assembly/file version (`2026.6.22.5`) because each assembly version component must be ≤ 65535.

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
| `TaxSlot` | `tax` |
| `ResourceGroupName` | `AlimenteEstaIdeia` |
| `KeyVaultName` | `alimentaestaideia-key` |
| `PreprodSlot` | `preprod` |

## Environments and URLs

| Slot | URL |
|------|-----|
| Developer | https://alimentaestaideia-developer.azurewebsites.net |
| Beta | https://alimentaestaideia-beta.azurewebsites.net |
| Tax | https://alimentaestaideia-tax.azurewebsites.net |
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

## Legacy Playwright release tests

The removed `BancoAlimentar.AlimentaEstaIdeia.Web.EndToEndTests` Playwright project is no longer in the repo. Azure **[DEV] AlimentaEstaIdeia** must not run Playwright — disable the **E2E tests** release environment (see script above) or migrate to `azure-pipelines/developer-debug.yml`, which has no E2E stage.

## Related documentation

- [TESTS.md](TESTS.md) — test layers and CI expectations  
- [Contributing-Windows-setup.md](Contributing-Windows-setup.md) — local dev and deployment slots  
- [SolutionReadme.md](SolutionReadme.md) — solution architecture overview  
