# Azure DevOps CI/CD

CI/CD for [alimentestaideia.pt](https://dev.azure.com/BancoAlimentar/Alimentestaideia.pt/) runs in Azure DevOps. GitHub Actions no longer runs builds, tests, or dependency audits; GitGuardian remains an independent security check.

## Pipeline files in this repo

| File | Purpose |
|------|---------|
| [`azure-pipelines/core-build.yml`](../azure-pipelines/core-build.yml) | **Release** build artifact for preprod/prod (replaces classic id=8) |
| [`azure-pipelines/developer-debug.yml`](../azure-pipelines/developer-debug.yml) | Build, test, deploy **developer** web + function slots |
| [`azure-pipelines/preprod-release.yml`](../azure-pipelines/preprod-release.yml) | Apply EF migrations, deploy **preprod** slot (replaces classic release id=5) |
| [`azure-pipeline-selenium-ui-tests.yml`](../azure-pipeline-selenium-ui-tests.yml) | PR build/tests, Selenium, and scheduled NuGet dependency audit |

**Agent pool:** Every YAML job uses the self-hosted **Default** pool. The Linux agent needs .NET 10 (installed by `UseDotNet@2`), PowerShell 7+, Azure CLI, Chrome/ChromeDriver, Docker Compose, Ruby, and Python 3. Docker must be running, and the agent account must be able to use it.

**SDK:** The solution targets **.NET 10** (`global.json` pins SDK `10.0.300`). Every build pipeline runs **UseDotNet@2** with **`10.0.x`** before restore.

The shared browser job is [`azure-pipelines/templates/selenium-job.yml`](../azure-pipelines/templates/selenium-job.yml).

## PR validation and trusted branch builds

Azure **PR validation** runs for every PR targeting `developer`, including
documentation-only PRs, and can be queued manually. It does not run on branch pushes. One job
restores/builds the solution and runs all five test projects sequentially: Repository, Sas.Core,
Integration, Function, and CleanProdHarden. It publishes TRX results and merged Cobertura/HTML
coverage. A second job runs the secret-free self-contained Selenium checks and depends on the
.NET job while still running after test failures so both result sets are available.

The same YAML runs the NuGet vulnerability/outdated-package audit weekly on `master` and during a
manual queue. Scheduled runs skip build, test, and browser jobs. GitGuardian remains independent
of these Azure pipelines. Core and developer pipelines retain their existing branch-push triggers.

The Core and developer pipelines set `pr: none`. They always begin with **BrowserRegression**.
Credentialed stages are included only for the following trusted contexts:

| Pipeline | Source refs | Allowed run reasons | After successful browser validation |
|----------|-------------|---------------------|-------------------------------------|
| `developer-debug` | `refs/heads/developer` | `IndividualCI`, `BatchedCI`, `Manual` | Build/test, publish, then deploy developer web and Functions slots |
| Core | `refs/heads/developer`, `refs/heads/master` | `IndividualCI`, `BatchedCI`, `Manual` | Build deployable projects and publish release packages |

For PR refs, tags, feature branches, missing context, or other run reasons, compile-time guards
omit the stages containing Key Vault, token replacement, artifact publication, and deployments.
Runtime guards repeat the branch/reason checks and require each named dependency to be exactly
`Succeeded`. Failed, skipped, canceled, or `SucceededWithIssues` browser validation cannot release
artifacts or deploy. A manual feature-branch run is test-only; it cannot deploy that feature branch.

These are repository execution guards, not a security boundary against someone who can change
the YAML. Environment/service-connection branch control, approvals, trusted-template policies,
and GitHub protected branches remain separate administrative controls. After migration, update
GitHub branch protection to require the Azure PR validation status instead of the retired GitHub
Actions build/test status; preserve GitGuardian and review requirements.

## Core release packaging

After its isolated browser stage succeeds, `azure-pipelines/core-build.yml` uses the
self-hosted **Default** Linux agent
and restores and builds only the public
website, tenant-management website, and Functions app, including their referenced libraries.
The packaging job does not restore or build the test projects. Browser validation runs in its own
job; PR unit/integration validation runs in the standalone PR pipeline and the developer pipeline.

Each publish task sets `publishWebProjects: false` so its explicit project path is used.
The three project-named ZIP files and the `drop` artifact remain unchanged for release consumers.

The shared `azure-pipelines/templates/report-disk-usage.yml` step reports filesystem capacity,
inode usage, and the largest source, NuGet cache, artifact, temporary, and tool-cache directories
before restore, after restore/before build, after build/before publishing, and after each publish.
These read-only, best-effort reports also run after failures; they do not delete agent files.
Compare consecutive reports when diagnosing `MSB3026: No space left on device` or agent loss.
The self-hosted agent's disk and installed tool versions are managed by the agent owner.

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

**Agent:** The self-hosted **Default** Linux agent is used for Selenium, build, and deployment.

The web deployment uses **Zip Deploy** because Web Deploy (`msdeploy`) is Windows-only. Zip Deploy
overwrites the deployed `wwwroot` contents and keeps the app's files writable; do not switch this
web app to Run From Package. Function deployment remains Run From Package as before.

The shared browser job gates the build/publish stage. Web and function deployment explicitly depend
on successful browser/build stages, with the function stage also requiring successful web deployment.
All credentialed stages require a trusted developer-branch context. The browser job tests its
temporary application, not the deployed slot. See [TESTS.md](TESTS.md).

## PR validation, Selenium, and dependency audit

[`azure-pipeline-selenium-ui-tests.yml`](../azure-pipeline-selenium-ui-tests.yml) is the PR validation and scheduled audit pipeline. It builds and tests the five .NET test projects, publishes their TRX and coverage, then runs the offline checks and self-contained Selenium selection with headless Chrome. The current CI selections contain 109 offline cases and 60 browser cases; seven additional live browser cases remain developer-only.

The pipeline uses the Linux agent in **Default**. Docker runs only the pinned Mailpit image from `compose.mailpit.yml`; the .NET application and Chrome run directly on the agent. Mailpit's SMTP and API ports bind only to loopback, and browser fixtures create temporary SQLite in-memory application and infrastructure databases. Payment-provider responses are simulated locally. No SQL Server, Key Vault access, Azure service connection, SMTP credentials, or real recipient accounts are needed.

Both selections fail if no tests match and publish TRX results to the Azure run's **Tests** tab, including on failure. Cleanup runs on success, failure, or cancellation within the job's cancellation timeout and removes only that run's Mailpit Compose project and temporary inbox. The inbox is not published as an artifact because messages contain test-account security links. If the self-hosted agent is lost before cleanup, remove only the interrupted run's `selenium-<BuildId>-<JobAttempt>` Compose project before reusing its loopback ports.

The shared job first runs Ruby standard-library policy tests for triggers, template expansion,
branch/reason guards, dependency failures, and reporting contracts. After cleanup, a read-only
Python standard-library verifier checks both explicit TRX files for complete passing results,
consistent counters/identities, and unique display names. Missing, malformed, failed, skipped,
incomplete, or duplicate-name reports fail validation, but the publication step still runs.

Run 2412 proved the original hosted job executed 109 offline checks and 60 browser cases successfully.
Its Tests tab displayed 162 because eight long-URL theory cases had the same truncated display name.
Short scenario labels now distinguish these cases; the verifier detects this regression without
rewriting reports. The refactored job and its reporting fix still require a validation run on the Default agent.

To configure it in Azure DevOps:

1. Point the existing PR validation YAML pipeline at `/azure-pipeline-selenium-ui-tests.yml`. A classic pipeline is not updated by changing this file.
2. Use the GitHub repository connection and check that pipeline UI settings do not override YAML triggers. The file validates PRs targeting `developer`; manual runs can select any branch containing the changes.
3. Configure the Linux agent with the required tools and run the PR pipeline on a feature branch. Confirm all five test projects, Selenium, TRX publication, coverage, and pipeline/report policy checks succeed. Do not queue a deployment-capable pipeline to validate unmerged changes.
4. After a PR demonstrates the expected status, update required checks for `developer`. The PR pipeline gates merges only when configured as required; deployment/package gates remain explicit dependencies inside their respective YAML pipelines. Do not require this automatic PR status on `master`, which is outside the PR trigger.

The pipeline explicitly selects `Local` and `SelfContained=true`, even though it runs in Azure. It never selects the deployed developer slot, sends email through its SMTP server, applies cloud migrations, or changes staging financial records. Developer-slot tests remain a separate, explicitly configured workflow described in [Local-Email-Tests.md](Local-Email-Tests.md#run-against-the-developer-slot).

## Administrative follow-up and deferred smoke tests

Repository changes do not modify these external settings:

- Review Azure pipeline UI trigger overrides and any classic releases that consume `drop`.
  A classic release does not automatically inherit YAML branch/reason guards.
- Restrict environment/service-connection access to trusted branches with appropriate approvals.
  Do not grant PR validation access to Key Vault or deployment credentials.
- After successful PR validation, require the Azure PR validation status on `developer`, preserving
  existing GitGuardian and review checks on both protected branches. Remove the retired GitHub Actions
  **Required build and tests** requirement. Review any legacy required Core or developer
  deployment status before changing it; these pipelines do not run on PRs.
- Read-only post-deployment smoke tests are not implemented by this change. The 19 existing
  developer-slot browser cases remain manual and may create staging records/send slot emails.
  Before production promotion, retain the requirement for a successful preproduction smoke test.

Local configuration/report-policy validation requires Ruby with Minitest/Psych and Python 3. Run:

```bash
ruby scripts/ci/tests/pipeline_safety_test.rb
ruby scripts/ci/tests/test_result_reporting_test.rb
```

The policy tests exercise a deliberately limited YAML/expression model and the actual aggregate
shell/verifier CLI. They do not replace Azure service-side compilation or prove external permissions.

## First-time setup in Azure DevOps

1. **Configure YAML pipelines**
   Pipelines → New pipeline → Azure Repos Git → **Existing Azure Pipelines YAML file**:
   - `/azure-pipelines/core-build.yml` — **Alimentestaideia.pt Core** (Release artifact, id=8)
   - `/azure-pipelines/developer-debug.yml` — developer build + deploy (id=11 / release id=3)
   - `/azure-pipelines/preprod-release.yml` — preprod migrations + deploy (release id=5)
   - `/azure-pipeline-selenium-ui-tests.yml` — PR validation and scheduled dependency audit (existing validation definition)

   Select the **Default** pool for every YAML pipeline. Confirm its Linux agent is online and has
   the tools listed under [Default self-hosted agent setup](#default-self-hosted-agent-setup).

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
   Pipeline reads secrets from `alimentaestaideia-key` via `AzureKeyVault@2` before token replacement. Secret names use `--` instead of `:` (e.g. `ConnectionStrings--Infrastructure`); tokens in `appsettings*.json` must match the secret name inside `#{…}#`.

   Optional food-bank seed settings are not in Key Vault; `appsettings*.json` uses empty strings for `AzureStorage:FoodBankSourceBlobName` and `AzureStorage:FoodBankSourceContainerName` (food-bank DB seeding is skipped when unset).

6. **Validate, then retire classic**
   After a green run, disable classic build id=8 (Core), release id=3 (developer), and release id=5 (preprod). Build definition id=11 already uses `developer-debug.yml`.

### Troubleshooting: `Requested SDK version: 10.0.300` / exit code 145

`UseDotNet@2` installs **`10.0.x`** before restore. If the task cannot download the SDK, check the self-hosted agent's network/proxy access and write permissions for its tool cache.

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
| `ResourceGroupName` | `AlimenteEstaIdeia` |
| `KeyVaultName` | `alimentaestaideia-key` |
| `PreprodSlot` | `preprod` |

## Environments and URLs

| Slot | URL |
|------|-----|
| Developer | https://alimentaestaideia-developer.azurewebsites.net |
| Pre-production | https://alimentaestaideia-preprod.azurewebsites.net |
| Production | https://alimentestaideia.pt |

## Default self-hosted agent setup

The Linux agent in the **Default** pool is shared by PR validation, scheduled dependency auditing,
builds, and deployments. With one agent, jobs queue and run sequentially. Install and maintain
Chrome, a compatible ChromeDriver, Docker with Compose v2, Ruby with Minitest/Psych, Python 3,
PowerShell 7+, Azure CLI, and the Azure Pipelines agent. Keep .NET 10 available through `UseDotNet@2`.
Ensure the agent service account can access Docker and the network endpoints required by NuGet,
GitHub, Azure DevOps, Key Vault, and Azure deployment tasks.

Temporary helper scripts:

| Script | Purpose |
|--------|---------|
| `scripts/debug-ado-agent-pool.ps1` | Diagnose queued/offline agents |
| `scripts/verify-ado-build-pools.ps1` | Show queue/pool id per definition |
| `scripts/switch-ado-pools-to-hosted.ps1` | Legacy helper from the hosted-agent migration |
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
