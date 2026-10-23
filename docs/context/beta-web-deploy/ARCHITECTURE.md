# Beta web deployment pipeline architecture

## Pattern

Add a dedicated Azure DevOps YAML pipeline that follows the Build and Deploy Web
stages of `azure-pipelines/developer-debug.yml`. Do not reuse the Developer pipeline
with runtime parameters because its trigger and deployment stages are intentionally
specific to the `developer` branch and slot.

## Pipeline file

Create `azure-pipelines/beta-debug.yml` with:

- a single branch trigger: `734-2026-atcud`;
- the existing Microsoft-hosted Windows agent, .NET 10, Key Vault, token replacement,
  versioning, restore, build, test, web publish, and `drop` artifact steps;
- a `DeployBeta` deployment stage that downloads the `drop` artifact and uses
  `AzureRmWebAppDeployment@4` to deploy only
  `BancoAlimentar.AlimentaEstaIdeia.Web.zip` to `alimentaestaideia/beta`;
- a dedicated Azure DevOps environment named `Beta` for deployment history and
  approvals, if configured later.

## Safety boundaries

- Keep `DeploymentType: webDeploy`, `TakeAppOfflineFlag`, `RemoveAdditionalFilesFlag`,
  and `RenameFilesFlag` aligned with the Developer Web deployment.
- Do not add the Function publish/deploy stage, timer-setting script, migration script,
  or a slot swap.
- Do not modify App Service or Key Vault configuration. The existing Beta `Staging`
  environment and Key Vault source are the database-configuration contract.

## Azure DevOps registration

After the YAML is committed and pushed, create a YAML pipeline in the existing Azure
DevOps project, connected to `banco-alimentar/alimentestaideia.pt`, selecting
`/azure-pipelines/beta-debug.yml` from `734-2026-atcud`. The first run may require
authorization for the existing ARM service connection and Key Vault access.
