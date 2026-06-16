# Azure DevOps pipeline export

Organization: **BancoAlimentar**
Project: **Alimentestaideia.pt**
Exported (UTC): 2026-06-12T20:26:14.6203076Z

## Build pipelines

- **Alimentestaideia.pt Core** (id=8, classic) → `build-alimentestaideia-pt-core.yml` — [open](https://dev.azure.com/BancoAlimentar/Alimentestaideia.pt/_build?definitionId=8)
- **banco-alimentar.alimentestaideia.pt** (id=1, yaml) → `build-banco-alimentar-alimentestaideia-pt.yml` — [open](https://dev.azure.com/BancoAlimentar/Alimentestaideia.pt/_build?definitionId=1)
- **banco-alimentar.alimentestaideia.pt DebugBuild** (id=3, yaml) → `build-banco-alimentar-alimentestaideia-pt-debugbuild.yml` — [open](https://dev.azure.com/BancoAlimentar/Alimentestaideia.pt/_build?definitionId=3)
- **developer-debug** (id=11, classic) → `build-developer-debug.yml` — [open](https://dev.azure.com/BancoAlimentar/Alimentestaideia.pt/_build?definitionId=11)
- **developer-debug-tax-law** (id=12, classic) → `build-developer-debug-tax-law.yml` — [open](https://dev.azure.com/BancoAlimentar/Alimentestaideia.pt/_build?definitionId=12)

## Release pipelines

- **[Beta] AlimentaEstaIdeia** (id=2) → `release-beta-alimentaestaideia.yml` — [open](https://dev.azure.com/BancoAlimentar/Alimentestaideia.pt/_release?definitionId=2)
  - Environments: AzurePreProd
- **[Debug] luguerre-debug AlimentaEstaIdeia** (id=4) → `release-debug-luguerre-debug-alimentaestaideia.yml` — [open](https://dev.azure.com/BancoAlimentar/Alimentestaideia.pt/_release?definitionId=4)
  - Environments: Debug Build to luguerre-debug
- **[DEV] AlimentaEstaIdeia** (id=3) → `release-dev-alimentaestaideia.yml` — [open](https://dev.azure.com/BancoAlimentar/Alimentestaideia.pt/_release?definitionId=3)
  - Environments: Developer, Function developer, E2E tests
- **[PRE-PROD] core version** (id=5) → `release-pre-prod-core-version.yml` — [open](https://dev.azure.com/BancoAlimentar/Alimentestaideia.pt/_release?definitionId=5)
  - Environments: PRE-PROD
- **[PROD] AlimentaEstaIdeia** (id=1) → `release-prod-alimentaestaideia.yml` — [open](https://dev.azure.com/BancoAlimentar/Alimentestaideia.pt/_release?definitionId=1)
  - Environments: AzurePreProd
- **[PROD][Core] AlimentaEstaIdeia** (id=6) → `release-prod-core-alimentaestaideia.yml` — [open](https://dev.azure.com/BancoAlimentar/Alimentestaideia.pt/_release?definitionId=6)
  - Environments: PrePROD, PROD, PrePROD Function, PROD Function

## Next steps

1. Review generated YAML under `C:\Users\tiaandra\source\repos\alimentestaideia.pt\scripts\ado-yaml-export`.
2. Merge build + release into one multi-stage pipeline where appropriate.
3. Replace secret placeholders with variable groups / Key Vault.
4. Create new YAML pipeline in Azure DevOps pointing at committed files.
5. Disable classic definitions after validation.

Delete `scripts/export-ado-pipelines-to-yaml.ps1` and this export folder when migration is complete.