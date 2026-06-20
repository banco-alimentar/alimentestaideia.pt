# alimentestaideia

[Alimentestaideia.pt](http://alimentestaideia.pt/) is the website that enables the food donations for [Federação Portuguesa de Bancos Alimentares Contra a Fome](https://www.bancoalimentar.pt/)

[![Build status](https://dev.azure.com/BancoAlimentar/Alimentestaideia.pt/_apis/build/status/developer-debug)](https://dev.azure.com/BancoAlimentar/Alimentestaideia.pt/_build/latest?definitionId=11)
 
# Contributing to [alimentestaideia.pt](http://alimentestaideia.pt/)
If you know about any of Asp.Net core, CSS, HTML or just want to test the site and submit suggestions or bugs, please check [Contributing](Documentation/CONTRIBUTING.md).
Here is a [video](https://youtu.be/Z9l3VG3iljU) with an overview on how the site is built and how to contribute

## Feature suggestions

If you want to suggest a new feature, please submit a new issue and label it [Enhancement](https://github.com/banco-alimentar/alimentestaideia.pt/issues?q=is%3Aissue+is%3Aopen+label%3Aenhancement).

## Testing the web site

- [Test projects overview](Documentation/TESTS.md) — unit, integration, and Selenium suites
- [Payments information to be used on the test site](Documentation/Payments-How-to-Test-while-Developing.md)
- [Penetration testing of the application](Documentation/Penetration-Test-Setup/)

## Maintaining the site
- [Azure DevOps CI/CD](Documentation/CI-Azure-DevOps.md) — YAML pipelines, developer deploy, hosted agents
- [Azure Functions](Documentation/Azure-Functions.md) — scheduled background jobs (reports, subscription cleanup, Multibanco reminders)

### Backoffice
#### Admin access
- [Production backoffice](https://www.alimentestaideia.pt/Admin/)
- [Development backoffice](https://alimentaestaideia-developer.azurewebsites.net/Admin/)

The admin home (`/Admin`) lists tools for **Admin** and **Manager** users. Users with the **SuperAdmin** role see additional links on the same page, each marked with a **Super admin** badge:

- Food Banks — `/Admin/FoodBanks`
- Clear tenant static cache — `/Admin/ClearTenantStaticCache`
- Reload runtime settings — `/Admin/ReloadSettings`
- User and role management — `/RoleManagement/UserRoles`
- Role management — `/RoleManagement/Roles`

SuperAdmin-only pages are protected by the `RoleArea` authorization policy. The legacy `/Admin/SuperAdmin` URL redirects to `/Admin`.

Other admin utilities:

- [Dump configuration settings](https://www.alimentestaideia.pt/admin/Configuration) (Admin/Manager)
- Managing feature flags — Azure Portal → App Configuration

## User Contributions
Since we "open sourced" the project we've had many people contributing. Take a look at [all contributors here](
https://github.com/banco-alimentar/alimentestaideia.pt/graphs/contributors?from=2021-03-20&to=2029-12-31&type=c).

## Attribution
A lot of content was taken from [Home Assistant contributing page](https://github.com/home-assistant/core/blob/dev/CONTRIBUTING.md) 
