# Beta web deployment pipeline specification

## Goal

Deploy every successful change pushed to `734-2026-atcud` to the `beta` slot of the
`alimentaestaideia` Azure App Service.

## User outcome

An engineer can push a change to `734-2026-atcud`, wait for the Azure DevOps pipeline,
and test that exact web build at `https://alimentaestaideia-beta.azurewebsites.net`.

## Scope

- Build, test, package, and deploy the Web App using the Developer pipeline's current
  build behaviour.
- Trigger only for pushes to `734-2026-atcud`.
- Deploy only the Web App package to the existing `beta` slot.
- Use the existing Azure service connection and Key Vault integration.
- Register a YAML pipeline in Azure DevOps after the YAML is available on the remote
  branch.

## Database behaviour

The Beta slot must use the same runtime database configuration source as Developer.
Both slots run in `Staging` and use `https://alimentaestaideia-key.vault.azure.net/`.
The application resolves `ConnectionStrings:DefaultConnection` from that Key Vault.
The deployment must not change database configuration or apply migrations.

## Explicit exclusions

- No Azure Function App deployment: there is no `beta` Function App slot.
- No database migration, slot swap, production deployment, or change to Key Vault
  secrets or App Service connection strings.
- No changes to the Developer, Preprod, or Production pipeline triggers.

## Acceptance criteria

1. A push to `734-2026-atcud` starts the Beta pipeline, and no other branch does.
2. The pipeline restores, builds, and runs the same automated test projects as the
   Developer pipeline.
3. A successful build deploys the web package to App Service `alimentaestaideia`, slot
   `beta`.
4. The pipeline has no Function App, migration, slot-swap, or production deployment
   stage.
5. The Azure DevOps pipeline definition points to the new YAML file on
   `734-2026-atcud`.
