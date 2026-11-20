# Azure DevOps CI/CD

CI/CD for [alimentestaideia.pt](https://dev.azure.com/BancoAlimentar/Alimentestaideia.pt/) runs in Azure DevOps. GitHub Actions no longer runs builds, tests, or dependency audits; GitGuardian remains an independent security check.

## Pipeline files in this repo

| File | Purpose |
|------|---------|
| [`azure-pipelines/core-build.yml`](../azure-pipelines/core-build.yml) | **Release** build artifact for preprod/prod (replaces classic id=8) |
| [`azure-pipelines/developer-debug.yml`](../azure-pipelines/developer-debug.yml) | PR/push validation for **developer/master**, weekly NuGet audit, and guarded **developer** web + function deployment |
| [`azure-pipelines/preprod-release.yml`](../azure-pipelines/preprod-release.yml) | Apply EF migrations, deploy **preprod** slot (replaces classic release id=5) |

**Agent pool:** Every YAML job uses the self-hosted **Default** pool. The Linux agent needs .NET 10 (installed by `UseDotNet@2`), PowerShell 7+, Azure CLI, Chrome/ChromeDriver, Docker Compose, Ruby, and Python 3. Docker must be running, and the agent account must be able to use it.

**SDK:** The solution targets **.NET 10** (`global.json` pins SDK `10.0.404`). Every build pipeline runs **UseDotNet@2** with **`10.0.x`** before restore.

The shared browser job is [`azure-pipelines/templates/selenium-job.yml`](../azure-pipelines/templates/selenium-job.yml).

For an optional self-hosted Linux container image, see
[Containerized Azure DevOps agent](Azure-DevOps-Container-Agent.md), including
Docker socket permissions, Mailpit networking, and Windows-only deployment limitations.
Existing pipeline pool selections are unchanged.

## Self-hosted workspace cleanup

Every build, test, audit, migration, and deployment job sets
`workspace: { clean: all }`. Azure cleans that job's `Pipeline.Workspace` before
the job starts, removing old sources, build outputs, and downloaded artifacts
even after a previous run was interrupted. Deployment jobs download their
published artifacts again instead of relying on files left by an earlier job.
Publication and test-result processing still run before the current job ends.

This is pre-job cleanup, not a post-build deletion script. It preserves the
agent tool/SDK cache and the default NuGet cache outside `Pipeline.Workspace`.
It does not delete other agents' workspaces, prune shared Docker resources, or
limit the peak storage required by concurrent jobs. Keep the existing
`always()` cleanup that removes only the current Selenium run's Mailpit project.
Review cache growth separately during scheduled, agent-scoped maintenance.

No agent image rebuild or restart is required. The policy takes effect when a
job runs with the updated YAML; classic pipeline definitions need equivalent
configuration separately. YAML-only changes do not match the automatic C# path
filters, so validate changes with a manual `developer-debug` run on a feature branch.
Feature-branch runs contain validation and auditing only, not deployment stages.

## PR validation and trusted branch builds

The consolidated **developer-debug** pipeline validates PRs targeting `developer` or `master`
and pushes to either branch when at least one changed file matches `**/*.cs`. It also supports
manual runs. Its secret-free **BrowserRegression** stage first restores/builds the solution in
Release and runs all five test projects sequentially: Repository, Sas.Core, Integration,
Function, and CleanProdHarden. It publishes TRX results and merged Cobertura/HTML coverage.
A second job runs the self-contained Selenium checks and depends on the .NET job while still
running after test failures so both result sets are available. Either job failing fails the
stage and prevents developer packaging/deployment.

The same YAML runs the NuGet vulnerability/outdated-package audit weekly on `master` and during a
manual queue. Its **DependencyAudit** stage has no dependencies, so it runs independently of
validation/deployment outcomes and is not a deployment gate. Scheduled runs skip validation,
packaging, and deployment. GitGuardian remains independent of these Azure pipelines. Core
continues building release packages on `developer` and `master` pushes with a `.cs` change;
its packaging flow and preproduction deployment are unchanged.

The path filter includes C# files in any project or test directory, at any depth, and is
case-sensitive. Mixed C# and non-C# changes still trigger validation/builds. Changes only to
documentation, YAML, `.csproj`, package versions, configuration, Razor markup, or static assets
do not trigger these pipelines; queue a manual run when those changes need validation or deployment.
Manual queues bypass the path filters; the weekly dependency audit and manual preproduction
release remain available.

Core still sets `pr: none`; `developer-debug` now owns PR validation for both protected branches.
Both begin with **BrowserRegression**, but only Core runs browser validation on every run reason;
the consolidated pipeline skips the entire validation stage for the weekly audit.
Credentialed stages are included only for the following trusted contexts:

| Pipeline | Source refs | Allowed run reasons | After successful browser validation |
|----------|-------------|---------------------|-------------------------------------|
| `developer-debug` | `refs/heads/developer` | `IndividualCI`, `BatchedCI`, `Manual` | Build/publish Debug packages, then deploy developer web and Functions slots |
| Core | `refs/heads/developer`, `refs/heads/master` | `IndividualCI`, `BatchedCI`, `Manual` | Build deployable projects and publish release packages |

For PR refs, tags, feature branches, missing context, or other run reasons, compile-time guards
omit the stages containing Key Vault, token replacement, release artifact publication, and deployments.
In `developer-debug`, this also applies to every `master` run: PRs/pushes validate only, while
scheduled runs audit and manual runs validate/audit. None can deploy a developer slot from `master`.
Runtime guards repeat the branch/reason checks and require each named dependency to be exactly
`Succeeded`. Failed, skipped, canceled, or `SucceededWithIssues` browser validation cannot release
artifacts or deploy. A manual feature-branch run validates/audits only; it cannot deploy that feature branch.

These are repository execution guards, not a security boundary against someone who can change
the YAML. Environment/service-connection branch control, approvals, trusted-template policies,
and GitHub protected branches remain separate administrative controls. Consolidation means PR
validation shares a pipeline definition with deployment-resource authorization; do not rely on
separate pipeline permissions to isolate it. Enforce trusted-branch restrictions and appropriate
approvals on the service connection and environments, and restrict untrusted PR execution on
the shared self-hosted pool. After migration, require the consolidated `developer-debug` PR
status on both protected branches; preserve GitGuardian and review requirements.

## Core release packaging

After its isolated browser stage succeeds, `azure-pipelines/core-build.yml` uses the
self-hosted **Default** Linux agent
and restores and builds only the public
website, tenant-management website, and Functions app, including their referenced libraries.
The packaging job does not restore or build the test projects. Browser validation runs in its own
job; PR/push unit/integration validation runs in the consolidated developer pipeline.

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

**Stages on trusted developer runs:** BrowserRegression (Release .NET tests/coverage, then Selenium) → Build/publish Debug packages → Deploy web (developer slot) → Deploy function (developer slot) → Disable function timers on developer slot.

**Triggers:** PRs targeting and pushes to `developer`/`master` with at least one `.cs` change,
plus manual queueing. PRs and `master` pushes run validation only. The weekly `master` schedule
runs only the independent dependency audit; manual runs also include that audit. Only trusted
`developer` CI/manual contexts can include packaging or deployment stages.

**Agent:** The self-hosted **Default** Linux agent is used for Selenium, build, and deployment.

The web deployment uses **Zip Deploy** because Web Deploy (`msdeploy`) is Windows-only. Zip Deploy
overwrites the deployed `wwwroot` contents and keeps the app's files writable; do not switch this
web app to Run From Package. Function deployment remains Run From Package as before.

The combined .NET/browser stage gates the build/publish stage. The packaging job no longer repeats
the four former Debug test runs: all five projects are validated once in Release before credentials
are accessed. Debug packaging, project-named ZIPs, and the `drop` artifact remain unchanged.
Web and function deployment explicitly depend on successful validation/build stages, with the
function stage also requiring successful web deployment. All credentialed stages require a
trusted developer-branch context. The browser job tests its temporary application, not the
deployed slot. See [TESTS.md](TESTS.md).

## PR validation, Selenium, and dependency audit

[`azure-pipelines/developer-debug.yml`](../azure-pipelines/developer-debug.yml) owns PR/push validation and the scheduled audit. It builds and tests the five .NET test projects, publishes their TRX and coverage, then runs the offline checks and self-contained Selenium selection with headless Chrome. The current CI selections contain 109 offline cases and 60 browser cases; seven additional live browser cases remain developer-only. The former root-level `azure-pipeline-selenium-ui-tests.yml` has been removed.

The pipeline uses the Linux agent in **Default**. Docker runs only the pinned Mailpit image from `compose.mailpit.yml`; the .NET application and Chrome run directly on the agent. Mailpit's SMTP and API ports bind only to loopback. Each job sets `MAILPIT_SMTP_PORT=0` and `MAILPIT_HTTP_PORT=0`, letting Docker allocate free host ports atomically, then discovers them with `docker compose port`. HTTP/SMTP readiness checks and browser tests use those exact endpoints through job-scoped pipeline variables. Concurrent agent containers sharing the host daemon do not need fixed per-agent ports. Browser fixtures create temporary SQLite in-memory application and infrastructure databases. Payment-provider responses are simulated locally. No SQL Server, Key Vault access, Azure service connection, SMTP credentials, or real recipient accounts are needed.

Both selections fail if no tests match and publish TRX results to the Azure run's **Tests** tab, including on failure. Cleanup runs on success, failure, or cancellation within the job's cancellation timeout and removes only that run's Mailpit Compose project and temporary inbox. The inbox is not published as an artifact because messages contain test-account security links. If the self-hosted agent is lost before cleanup, remove only the interrupted run's `selenium-<BuildId>-<JobAttempt>` Compose project after confirming its job is no longer active; do not prune shared Docker resources.

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

1. Keep the existing **developer-debug** definition (id=11) pointing at `/azure-pipelines/developer-debug.yml`. Do not repoint the old Selenium definition to this path: two active definitions would duplicate validation, schedules, and potentially developer deployments.
2. Use the GitHub repository connection and check that pipeline UI settings do not override YAML triggers. Merge the YAML into both `developer` and `master` so their push and scheduled runs use it; Azure registers YAML schedules separately per branch. Remove any UI-defined schedule that would override the weekly YAML schedule.
3. Configure the Linux agent with the required tools and manually run `developer-debug` on a feature branch containing these changes. Confirm all five test projects, Selenium, TRX publication, coverage, policy checks, and dependency auditing succeed. Confirm packaging, Key Vault, and deployment stages are absent. A manual run on `developer` is deployment-capable; do not use it for this test.
4. Verify the new PR status with a C#-changing PR targeting each branch. Update required checks on **both** `developer` and `master` to the observed consolidated status, preserving GitGuardian and review checks. Check skip handling for non-C#-only PRs before making this path-filtered status required.
5. Disable the old **Selenium regression tests** pipeline definition and its CI/PR/UI schedules, then remove its obsolete required status. Repository changes do not disable an Azure definition, and the old definition can fail when it tries to load the deleted YAML. Preserve deployment environment/service-connection restrictions and disable the classic developer release if it still consumes `drop`, to avoid duplicate deployments.

The browser job explicitly selects `Local` and `SelfContained=true`, even though it runs in Azure. It never selects the deployed developer slot, sends email through its SMTP server, applies cloud migrations, or changes staging financial records. Developer-slot tests remain a separate, explicitly configured workflow described in [Local-Email-Tests.md](Local-Email-Tests.md#run-against-the-developer-slot).

## Administrative follow-up and deferred smoke tests

Repository changes do not modify these external settings:

- Disable the retired standalone Selenium definition and review Azure pipeline UI trigger
  overrides, UI schedules, and any classic releases that consume `drop`.
  A classic release does not automatically inherit YAML branch/reason guards.
- Restrict environment/service-connection access to trusted branches with appropriate approvals.
  PR validation must not retrieve Key Vault or deployment credentials, even though its pipeline
  definition now also owns trusted developer deployment.
- After successful PR validation, require the consolidated `developer-debug` PR status on
  `developer` and `master`, preserving existing GitGuardian and review checks. Replace the old
  Selenium and retired GitHub Actions **Required build and tests** requirements. Review any
  legacy required Core deployment status before changing it; Core does not run on PRs.
- Read-only post-deployment smoke tests are not implemented by this change. The 19 existing
  developer-slot browser cases remain manual and may create staging records/send slot emails.
  Before production promotion, retain the requirement for a successful preproduction smoke test.

Local configuration/report-policy validation requires Ruby with Minitest/Psych and Python 3. Run:

```bash
ruby scripts/ci/tests/pipeline_safety_test.rb
ruby scripts/ci/tests/test_result_reporting_test.rb
ruby scripts/ci/tests/agent_setup_test.rb
```

The policy tests exercise a deliberately limited YAML/expression model and the actual aggregate
shell/verifier CLI. They do not replace Azure service-side compilation or prove external permissions.

## First-time setup in Azure DevOps

1. **Configure YAML pipelines**
   Pipelines → New pipeline → Azure Repos Git → **Existing Azure Pipelines YAML file**:
   - `/azure-pipelines/core-build.yml` — **Alimentestaideia.pt Core** (Release artifact, id=8)
   - `/azure-pipelines/developer-debug.yml` — developer/master validation, dependency audit, developer build + deploy (id=11 / release id=3)
   - `/azure-pipelines/preprod-release.yml` — preprod migrations + deploy (release id=5)

   Retire the separate Selenium validation definition; its root-level YAML has been removed.

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
   After a green run, disable classic build id=8 (Core), release id=3 (developer), and release id=5 (preprod). Keep build definition id=11, which already uses `developer-debug.yml`. Follow the consolidated PR-status migration above before retiring the old Selenium validation definition.

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

For an **Ubuntu 24.04 LTS amd64** host with an existing agent registration, use
[`scripts/setup-ubuntu-agent.sh`](../scripts/setup-ubuntu-agent.sh). Stop the agent service and
ensure no container workloads are active before installation. Replace `azagent` with the actual
service account:

```bash
sudo bash scripts/setup-ubuntu-agent.sh --agent-user azagent --allow-docker-group
```

The script installs the host tools and native .NET dependencies from Ubuntu/vendor APT
repositories, downloads the ChromeDriver matching the installed Chrome build, enables Docker,
and verifies the tools as the agent account. `.NET SDK 10` is installed by `UseDotNet@2`, not this
script. Existing Docker distribution packages cause an explicit stop rather than automatic
removal; migrate them separately if necessary. Rerunning can upgrade installed software.

`--allow-docker-group` explicitly acknowledges that Docker access is root-equivalent. Use a
dedicated agent host and restrict which untrusted PRs may run on it, especially when it also
handles deployments. The script does not register agents, change cloud settings, or request
credentials. Restart the existing agent service afterward so it picks up Docker membership and
the tool PATH (including `/usr/local/bin`). Keep loopback ports 1025 and 8025 free for Mailpit.
Rerun the setup while the agent is stopped after a Chrome upgrade to refresh ChromeDriver.

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
