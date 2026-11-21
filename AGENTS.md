# Repository guidelines

## Project overview

`alimentestaideia.pt` is an ASP.NET Core application for the Federação Portuguesa de Bancos
Alimentares Contra a Fome. The repository contains the public website, tenant-management
components, shared libraries, Azure Functions, and automated tests.

The main solution is `BancoAlimentar.AlimentaEstaIdeia.Web.sln`. The repository targets .NET 10;
`global.json` pins the SDK to `10.0.300` with feature-band roll-forward.

## Development workflow

- Read the relevant documentation in `Documentation/` before changing deployment, database,
  payment, authentication, or Azure Functions behavior.
- Preserve existing project structure, naming, localization resources, and StyleCop conventions.
- Reuse existing services and test helpers before adding new abstractions.
- Do not commit secrets, connection strings, local settings, generated build output, or deployment
  credentials.

## Build and test

Install .NET SDK 10 before running repository commands. Use these commands from the repository root:

```bash
dotnet restore BancoAlimentar.AlimentaEstaIdeia.Web.sln
dotnet build BancoAlimentar.AlimentaEstaIdeia.Web.sln --configuration Release --no-restore
dotnet test BancoAlimentar.AlimentaEstaIdeia.Repository.Tests/BancoAlimentar.AlimentaEstaIdeia.Repository.Tests.csproj --configuration Release
dotnet test BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tests/BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tests.csproj --configuration Release
dotnet test BancoAlimentar.AlimentaEstaldeia.Web.IntegrationTests/BancoAlimentar.AlimentaEstaldeia.Web.Integration.Tests.csproj --configuration Release
dotnet test BancoAlimentar.AlimentaEstaIdeia.Function.Tests/BancoAlimentar.AlimentaEstaIdeia.Function.Tests.csproj --configuration Release
dotnet test BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Tests/BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Tests.csproj --configuration Release
```

The CleanProdHarden SQL Server tests are skipped unless `CLEANPRODHARDEN_TEST_SQL` contains a
connection string to a restored copy; they only dry-run and never commit. See
[Documentation/CleanProdHarden-Tool.md](Documentation/CleanProdHarden-Tool.md).

The GitHub Actions workflow runs build and test jobs with .NET 10. Azure Pipelines use
`UseDotNet@2` with `10.0.x`.

## Azure deployment

- GitHub Actions builds and tests; Azure DevOps performs slot deployments.
- `azure-pipelines/core-build.yml` builds the Release artifacts used by preproduction and
  production releases.
- `azure-pipelines/preprod-release.yml` applies database migrations and deploys the web package
  to the `preprod` slot.
- Preproduction web deployment uses Zip Deploy. Keep the package prebuilt and do not use
  `runFromZip`, because the application expects writable App Service storage.
- The classic Azure DevOps release may still require equivalent settings in the portal. A change
  to repository YAML does not update a classic release definition automatically.
- Never swap the preproduction slot into production until the preproduction smoke test succeeds.
- Treat database migrations as an ordered deployment step. Do not bypass the migration stage when
  the application includes schema changes.

See [Documentation/CI-Azure-DevOps.md](Documentation/CI-Azure-DevOps.md) and
[Documentation/Azure-Functions.md](Documentation/Azure-Functions.md) for operational details.

## Changelog and decision records

When you make a change that involves an update decision, record it in [CHANGELOG.md](CHANGELOG.md)
under `## [Unreleased]`. Update decisions include:
- database migrations, data fixes, or reconciliation scripts
- changing a package, framework, or runtime version
- breaking changes, or changes to shared contracts or schemas
- choosing between several possible fixes for an issue

Each entry should include:
- **Context:** what happened (error message, migration name, affected component).
- **Options considered:** the alternatives that were evaluated.
- **Decision:** the option chosen and why.
- **Rollout:** the manual steps needed (scripts to run, order, per-tenant actions), if any.

Use the [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) sections (`Added`, `Changed`,
`Fixed`, `Removed`, ...) and link to the files that were added or changed.

Do not weaken or remove safety checks in existing migrations to get past data problems. Fix the
data with a separate, reviewable script under
`BancoAlimentar.AlimentaEstaIdeia.Web/Migrations/Scripts/`. These scripts should be safe to run
per tenant and run in dry-run mode by default (transaction + rollback).

## Git and commits

- Use conventional commits with a meaningful scope, for example:
  `ci(ado): use Zip Deploy for preprod App Service`.
- Keep commits focused and reviewable. Run the relevant tests before committing.
- Prefer pull requests for protected branches. Do not bypass branch protection or required checks.
- Check `git status` before editing and preserve unrelated user changes.
