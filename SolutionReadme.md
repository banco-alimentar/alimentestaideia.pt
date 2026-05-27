# Solution Architecture Review — Alimenta Esta Ideia

**Solution file:** `BancoAlimentar.AlimentaEstaIdeia.Web.sln`  
**Review date:** 2026-05-26  
**Scope:** Static analysis of the Visual Studio solution (19 `.csproj` projects, CI definitions, and representative code paths). No runtime penetration testing was performed.

---

## Executive summary

This solution implements **alimentestaideia.pt** — a multi-tenant ASP.NET Core web application for food-bank donations (Federação Portuguesa dos Bancos Alimentares), with payment integrations (EasyPay, PayPal, Multibanco), ASP.NET Identity, admin/backoffice areas, and a growing **SAS (multi-tenant “Doar”)** platform extracted into dedicated libraries.

**Architectural style (inferred):** A **modular monolith** with **layered** tendencies (Model → Repository → Web) plus a **parallel vertical slice** for multi-tenancy (`Sas.*`). It is **not** clean architecture: persistence, payment SDKs, telemetry, and ASP.NET concerns leak across layers.

**Strengths**

- Clear separation between **tenant infrastructure** (`Sas.Model`, `InfrastructureDbContext`) and **donation domain** (`Model`, `ApplicationDbContext`).
- Production configuration uses **Azure Key Vault** and **managed identity** patterns for secrets.
- Uniform **.NET 9** target (except vendored EasyPay client on **.NET 8**).
- Meaningful automated tests exist around **donations** and **invoicing**; StyleCop and `TreatWarningsAsErrors` enforce consistency.

**Top concerns**

1. **Production/staging enable `UseDeveloperExceptionPage()`** — stack traces and internal details can leak to users ([`Startup.cs`](BancoAlimentar.AlimentaEstaIdeia.Web/Startup.cs) lines 476–480).
2. **`Startup.ConfigureServices` calls `BuildServiceProvider()`** and exposes a **static `ServiceCollection`** — fragile startup, harder testing, known ASP.NET anti-pattern.
3. **Payment webhooks and reminder APIs** appear **unauthenticated at controller level**; security relies on shared secrets / provider validation ([`PaymentNotification.cs`](BancoAlimentar.AlimentaEstaIdeia.Web/Api/PaymentNotification.cs)).
4. **`DonationRepository` (~857 LOC)** mixes querying, caching, payment types, and telemetry — high change risk.
5. **CI builds without running tests** (GitHub Actions test step commented out; Azure `azure-pipeline-core.yml` referenced in `.sln` but **missing from repo**).
6. **`DefaultAzureCredential` with `AdditionallyAllowedTenants = { "*" }`** in multiple entry points — broad cross-tenant Azure AD acceptance.
7. **Test projects reference the full Web host**, increasing coupling and build time.
8. **Hardcoded credentials in E2E tests** (PayPal sandbox password in source).
9. **Dual DI containers** (Autofac root scope + MS.Extensions DI) with minimal Autofac registration — complexity without clear benefit.
10. **EasyPay client** is a large generated SDK (`PaymentGenericOperationsApi.cs` ~2284 lines) on **net8.0** while the app is **net9.0**.

---

## 1. Solution overview

### 1.1 Project inventory

| Project | Classification | Purpose (evidence-based) |
|--------|----------------|---------------------------|
| **BancoAlimentar.AlimentaEstaIdeia.Web** | Web app (ASP.NET Core, Razor Pages + API) | Primary public site: donations, identity, admin areas, payment notification endpoints, EF migrations for `ApplicationDbContext`. |
| **BancoAlimentar.AlimentaEstaIdeia.Sas.Web.TenantManagement** | Web app (secondary host) | Tenant/admin tooling for SAS platform; own `Program.cs` / `Startup.cs`. |
| **BancoAlimentar.AlimentaEstaIdeia.Function** | Azure Functions worker | Background jobs: subscription cleanup, multi-tenant payment notifications (`DeleteOldSubscriptionFunction`, `MultiBancoPaymentNotificationFunction`, etc.). |
| **BancoAlimentar.AlimentaEstaIdeia.Tools** | Console / maintenance utility | One-off DB and Key Vault migration scripts (mostly commented in `Program.cs`). |
| **BancoAlimentar.AlimentaEstaIdeia.Model** | Domain / data model library | EF entities, identity types, donation catalog models; references EF Core + Identity. |
| **BancoAlimentar.AlimentaEstaIdeia.Repository** | Data access / application services | Repositories, `UnitOfWork`, validators, mail abstractions; orchestrates DB + external payment models. |
| **BancoAlimentar.AlimentaEstaIdeia.Common** | Shared utilities | Cross-cutting helpers and repository base types; references `Sas.Model` and EasyPay. |
| **BancoAlimentar.AlimentaEstaIdeia.Sas.Model** | Infrastructure domain | Tenants, domains, strategies; `InfrastructureDbContext` and migrations. |
| **BancoAlimentar.AlimentaEstaIdeia.Sas.Repository** | Infrastructure persistence | `InfrastructureUnitOfWork` and tenant DB access. |
| **BancoAlimentar.AlimentaEstaIdeia.Sas.Core** | Application / infrastructure (web plumbing) | Multi-tenant middleware, static files per tenant, hosted sync services, layout. |
| **BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider** | Configuration / cross-cutting | Key Vault integration, per-tenant `IConfiguration` (`TenantConfigurationRoot`), auth option post-configure. |
| **Easypay.Rest.Client** (`EasyPay/`) | Third-party API client (vendored) | OpenAPI-generated REST client for EasyPay. |
| **Paypal** | Third-party integration library | PayPal order/client helpers used by Web. |
| **BancoAlimentar.AlimentaEstaIdeia.Repository.Tests** | Unit / integration tests | xUnit tests for repositories (references **Web**). |
| **BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tests** | Unit / integration tests | Tenant naming/middleware tests (references **Web**). |
| **BancoAlimentar.AlimentaEstaldeia.Web.Integration.Tests** | Integration tests | WebApplicationFactory-style tests (folder name typo: *Estaldeia*). |
| **BancoAlimentar.AlimentaEstaIdeia.Testing.Common** | Test shared library | Fixtures/helpers for integration tests. |
| **BancoAlimentar.AlimentaEstaIdeia.Web.Selenium.UITest** | UI tests (Selenium) | Browser automation against deployed environments. |
| **BancoAlimentar.AlimentaEstaIdeia.Web.EndToEndTests** | E2E tests (Playwright) | Full donation flows including PayPal sandbox. |

**Solution folders (non-build):** `Pipeline` (YAML), `Github` (workflow), `Tests`, `Tools`, `Sas`.

### 1.2 How projects relate

```
                    ┌─────────────────────────────────────────┐
                    │  Web  │  Sas.Web.TenantManagement       │
                    │  Function (Azure)                       │
                    └───────────┬─────────────────────────────┘
                                │
         ┌──────────────────────┼──────────────────────┐
         ▼                      ▼                      ▼
    Sas.Core              Repository              Paypal
         │                      │
         ├─ Sas.ConfigurationProvider
         ├─ Sas.Repository ── Common ── Sas.Model
         │                      │
         │                      ├── Model
         │                      └── EasyPay
         └─ (middleware, hosted services)

Tests ──► Web + Repository + Testing.Common (+ Selenium/E2E standalone)
Tools ──► Model, Repository, Sas.*
```

**Runtime entry points**

| Host | Entry | Role |
|------|--------|------|
| **Web** | `BancoAlimentar.AlimentaEstaIdeia.Web/Program.cs` → `Startup` | Main production site |
| **TenantManagement** | `Sas.Web.TenantManagement/Program.cs` | SAS tenant administration |
| **Function** | `Function/Program.cs` | Scheduled/webhook background processing |
| **Tools** | `Tools/Program.cs` | Manual ops (local/dev) |

---

## 2. Dependency analysis

### 2.1 Project-to-project reference map

| Project | References |
|---------|------------|
| **Model** | *(none)* |
| **Sas.Model** | *(none)* |
| **Common** | Sas.Model, EasyPay |
| **Sas.ConfigurationProvider** | Common, Model, Sas.Model |
| **Repository** | Common, Model, Sas.ConfigurationProvider, Sas.Model, EasyPay |
| **Sas.Repository** | Common, Sas.Model, EasyPay |
| **Sas.Core** | Common, Sas.ConfigurationProvider, Sas.Repository, EasyPay |
| **Web** | Model, Repository, Sas.ConfigurationProvider, Sas.Core, Sas.Model, Sas.Repository, EasyPay, Paypal |
| **Function** | Common, Model, Repository, Sas.ConfigurationProvider, Sas.Model |
| **Sas.Web.TenantManagement** | Repository, Sas.Core, Sas.Model, Sas.Repository |
| **Tools** | Model, Repository, Sas.Model, Sas.Repository |
| **Paypal** | *(not listed in grep — likely package-only)* |
| **Repository.Tests** | Model, Repository, **Web** |
| **Sas.Core.Tests** | Repository, Sas.Core, Testing.Common, **Web** |
| **Integration.Tests** | Model, Repository, Testing.Common, **Web** |
| **Testing.Common** | Model, Sas.ConfigurationProvider, Sas.Model |
| **Selenium / E2E** | Common, Model, Repository, Sas.Model (Selenium); E2E may differ |

### 2.2 Circular dependencies

**No project-reference cycles** were found (DAG is acyclic).

**Logical / layering cycles (smells):**

- **Sas.ConfigurationProvider → Model**: Configuration layer depends on donation domain entities — tight coupling when tenant config should depend on abstractions or `Sas.Model` only.
- **Repository → Sas.ConfigurationProvider**: Data layer pulls in Key Vault and HTTP-aware tenant configuration.
- **Common → EasyPay**: “Shared utilities” depend on payment vendor SDK.

### 2.3 Tight coupling and suspicious references

| Issue | Impact |
|-------|--------|
| **Tests → Web** | Repository and SAS tests compile against the full web application; changes to `Startup` or Razor break unit test builds. |
| **Repository uses `Easypay.Rest.Client.Model`** | Persistence layer is payment-vendor-specific; swapping providers requires editing repositories. |
| **Web holds EF migrations** for `ApplicationDbContext` while **Sas.Model holds migrations** for `InfrastructureDbContext` | Acceptable split, but cross-project migration ownership must be documented for deploy pipelines. |
| **Autofac package on Web** (`Autofac.AspNetCore.Multitenant`) vs **minimal Autofac usage** | `Program.cs` creates an empty root container; most registration is MS DI in `Startup` — dead complexity. |
| **Typo project/folder** `AlimentaEstaldeia` | Confusing onboarding, brittle scripts searching by name. |

### 2.4 Possibly redundant or underused artifacts

| Item | Notes |
|------|--------|
| **Autofac root scope** in `Program.cs` | No `ConfigureContainer` registrations found in Web; likely legacy. |
| **azure-pipeline-core.yml** | Listed in `.sln` under `Pipeline` but **file not present** in repository (only `azure-pipeline-selenium-ui-tests.yml` found). |
| **Commented Azure App Configuration** in `Startup` | Feature flag infra partially wired (`AddFeatureManagement`) but App Config integration disabled. |
| **Distributed SQL cache** | Commented out in `Startup` — dead path. |
| **Paypal folder vs PayPal SDK packages** | Web also references PayPal HTTP types in csproj exclusions — verify single integration path. |

---

## 3. Architecture review

### 3.1 Inferred style

**Modular monolith + multi-tenant plugin model:**

- **Horizontal layers:** Model → Repository → Web.
- **Vertical SAS module:** Sas.Model / Sas.Repository / Sas.Core / Sas.ConfigurationProvider consumed by Web.
- **Not microservices:** Single deployable web app; Functions are auxiliary workers sharing DB/configuration patterns.
- **Tenant resolution:** `UseDoarMultitenancy()` middleware (`Sas.Core`) + `ITenantProvider` + per-request `TenantConfigurationRoot` replacing `IConfiguration`.

### 3.2 Separation of concerns violations

| Layer bleed | Example |
|-------------|---------|
| **Domain in web** | ~96 Razor Page code-behind files; donation and payment flow logic likely split between Pages and `DonationRepository`. |
| **Infrastructure in domain model** | `Model` references EF Core, Identity, Azure Storage — anemic “domain” is persistence-oriented. |
| **Configuration in repository stack** | `Repository` → `Sas.ConfigurationProvider` (Key Vault, auth post-configure). |
| **Presentation in repository** | `DonationRepository` uses Application Insights telemetry directly. |
| **Payment DTOs in repository** | EasyPay `TransactionNotificationRequest` types flow through Web API and repositories. |

### 3.3 Domain / application / infrastructure / presentation

| Concern | Where it lives | Assessment |
|---------|----------------|------------|
| **Domain rules** | Model entities + Repository methods | No explicit domain services; rules embedded in repositories and page models. |
| **Application use cases** | Web Pages, API controllers, Repository | No dedicated Application project. |
| **Infrastructure** | EF contexts, Azure, EasyPay/PayPal, Key Vault | Spread across Repository, Sas.*, Web `Startup`. |
| **Presentation** | Razor, static files, API | Web + tenant-specific view paths under `Pages/Tenants/`. |

**Impact:** Hard to unit-test business rules without DB and web host; refactors to payment or tenancy ripple across many projects.

---

## 4. Code quality and maintainability

### 4.1 Large classes and hotspots

| Artifact | ~LOC | Concern |
|----------|------|---------|
| `DonationRepository.cs` | 857 | God repository: totals, payments, caching (`ConcurrentBag` static), queries. |
| `Startup.cs` | 533 | God startup: auth, DB, telemetry, health, localization, MiniProfiler, data protection. |
| EasyPay `*Api.cs` generated files | 876–2284 | Vendor SDK noise; avoid manual edits. |
| EF migration designers | 770–810 each | Normal for EF; keep out of architectural metrics. |
| `KeyVaultConfigurationManager.cs` | not fully measured | Central secret orchestration — high risk change surface. |

### 4.2 Duplication and naming

- Parallel **Startup** implementations in Web and `Sas.Web.TenantManagement` (auth, DB, data protection patterns duplicated).
- Inconsistent spelling: **AlimentaEstaIdeia** vs **AlimentaEstaldeia** (integration test project).
- `PaymentStatus.Payed` (domain spelling) — consistent internally but worth documenting for APIs.

### 4.3 Testing and extensibility pain

- **Static `Startup.ServiceCollection`** and **service locator smell** via early `BuildServiceProvider()`.
- **Static cache** on `DonationRepository` (`ConcurrentBag<TotalDonationsResult>`) — shared mutable state across requests/instances.
- **MiniProfiler `EnableDebugMode = true`** registered unconditionally in `ConfigureServices` (may expose profiling in non-dev if pipeline enabled).

### 4.4 Technical debt hotspots

1. Payment notification + donation status update paths (money movement).
2. Multi-tenant configuration merge (`TenantConfigurationRoot`).
3. Subscription and invoice repositories (transaction usage in `InvoiceRepository` — partial).
4. Identity + external login post-configure per tenant.
5. Migrations split across **Web** and **Sas.Model**.

---

## 5. Configuration and startup flow

### 5.1 Primary entry point — Web

1. **`Program.CreateHostBuilder`**
   - Default host + **Autofac** child scope factory (mostly empty root container).
   - **Key Vault** loaded when `IsProduction()` or `IsStaging()` for `VaultUri` and `SasVaultUri`.
   - `DefaultAzureCredential` with **`AdditionallyAllowedTenants = { "*" }`**.
2. **`Startup.ConfigureServices`**
   - Tenant SAS services (`ITenantProvider`, naming strategies, `TenantConfigurationRoot` scoped `IConfiguration`).
   - EF: `ApplicationDbContext` (SQL Server, retries, migrations assembly **Web**).
   - EF: `InfrastructureDbContext` (SQL Server or in-memory SQLite seed for dev).
   - Identity, feature flags, EasyPay/PayPal builders, CORS, App Insights, MiniProfiler, health checks.
3. **`Startup.Configure`**
   - Exception handling (see security: developer page in production).
   - `UseDoarMultitenancy()`, tenant static files, routing, auth.

### 5.2 Configuration sources

| Source | Used by |
|--------|---------|
| `appsettings*.json`, environment variables | All hosts |
| User secrets | Web, Function, Tools |
| Azure Key Vault | Web, Function, TenantManagement (staging/production) |
| Per-tenant Key Vault secrets | `KeyVaultConfigurationManager` / `TenantConfigurationRoot` |

### 5.3 DI and logging

- **Primary DI:** `Microsoft.Extensions.DependencyInjection` in `Startup`.
- **Autofac:** Host integration present; **no evidence of module-based registration** in Web.
- **Logging:** Application Insights + console (Function); telemetry initializers and filters in Web.

### 5.4 Risky configuration patterns

| Pattern | Risk |
|---------|------|
| `AdditionallyAllowedTenants = { "*" }` | Cross-tenant token acceptance for Azure AD credentials. |
| `PaymentNotification.Get` validates `key == configuration["ApiCertificateV3"]` | Shared secret in query string; logging/Referer leakage. |
| **E2E test** hardcoded PayPal password | Secret in git history ([`DevelopmentDonations.cs`](BancoAlimentar.AlimentaEstaIdeia.Web.EndToEndTests/DevelopmentDonations.cs) line 171). |
| `Sas.ConfigurationProvider` sets `<WarningLevel>0</WarningLevel>` | Disables warnings despite StyleCop package — inconsistent quality gate. |
| Web `.csproj` `DocumentationFile` path still references **net5.0** | Stale path; may break doc generation or analyzers. |
| CORS policy allows trailing slash origins | `https://alimentestaideia.pt/` — may not match requests without trailing slash. |

---

## 6. Testing review

### 6.1 Strategy (as implemented)

| Layer | Project | Approx. tests (`[Fact]`/`[Theory]`/etc.) |
|-------|---------|------------------------------------------|
| Unit / repo | Repository.Tests | ~48 (35 in DonationRepositoryTests alone) |
| Unit / SAS | Sas.Core.Tests | ~5 |
| Integration | AlimentaEstaldeia.Web.Integration.Tests | ~7 |
| UI | Web.Selenium.UITest | ~7 |
| E2E | Web.EndToEndTests | Playwright scenarios (includes PayPal login) |

**Shared:** `Testing.Common` for fixtures.

### 6.2 Gaps

- **No dedicated tests** for `Sas.ConfigurationProvider`, `Function` triggers, or **Paypal** project.
- **Payment webhooks** (EasyPay POST, generic notifications) — not enough evidence of automated security/contract tests.
- **TenantManagement** web app — no test project reference found.
- **GitHub Actions** — `dotnet test` is **commented out** ([`.github/workflows/alimentestaideia.yaml`](.github/workflows/alimentestaideia.yaml)).
- Tests depend on **full Web** project → slow CI and environment-sensitive failures.

### 6.3 Highest-value tests to add first

1. **Payment notification handlers** — idempotency, invalid signature/secret, duplicate callbacks, status transitions to `Payed`.
2. **`TenantConfigurationRoot`** — tenant isolation: tenant A config must not leak to tenant B.
3. **`DonationRepository` totals/cache** — correctness and thread-safety without static shared cache.
4. **Authorization policies** — `AdminArea` / `RoleArea` for external login schemes.
5. **Function `MultiBancoPaymentNotificationFunction`** — integration with repository under retry (Polly referenced in Function csproj).

---

## 7. Build and deployment concerns

| Topic | Finding |
|-------|---------|
| **Target frameworks** | Almost all **net9.0**; **Easypay.Rest.Client** is **net8.0** — generally compatible but pins transitive packages differently. |
| **Warnings as errors** | Enabled on most projects — good; undermined on `Sas.ConfigurationProvider` (`WarningLevel` 0). |
| **Missing pipeline file** | `azure-pipeline-core.yml` in solution, absent on disk — Azure DevOps badge in README may point to external definition. |
| **GitHub Actions** | `actions/checkout@v2`, `setup-dotnet@v1.8.1` — dated; build only, no test gate. |
| **Package drift** | `Microsoft.AspNetCore.Http.Abstractions` **2.3.0** on net9 projects (Common, Sas.Core) alongside FrameworkReference — redundant/old surface. |
| **Migrations** | Two contexts, two migration assemblies — deploy must run both in order. |
| **Publish size** | Large `wwwroot`, tenant themes, ImageSharp, PDF generation — expect long publish and CDN/static sync (`TenantStaticSyncHostedService`). |

**CI/CD complication:** Selenium pipeline is separate (`azure-pipeline-selenium-ui-tests.yml`); core build/test/release path is fragmented.

---

## 8. Security and reliability

### 8.1 Security

| Area | Observation |
|------|-------------|
| **Secrets** | Key Vault for prod/staging — good. User secrets for dev. **Do not commit** real tokens; E2E PayPal password in repo is a finding. |
| **Exception handling** | `UseDeveloperExceptionPage()` in **Production** and **Staging** branches — critical information disclosure. |
| **Auth on APIs** | Payment notification controllers use `[ApiController]` routes without `[Authorize]` in sampled files — rely on HMAC/shared key/provider validation; verify EasyPay signature validation in `EasyPayControllerBase` subclasses (not fully audited here). |
| **Multitenancy** | Strong middleware + per-tenant config; failure modes depend on correct `Infrastructure` DB data. |
| **Data protection** | Azure Blob + Key Vault in production — good. |
| **Identity** | Confirmed email required; role policies for Admin/SuperAdmin. |
| **Azure credential** | `AdditionallyAllowedTenants = "*"` — widen blast radius if credential is mis-issued. |
| **Serialization** | Newtonsoft.Json on MVC; EasyPay client custom settings — review for `TypeNameHandling` if enabled in custom codecs (sample shows standard settings in `ApiClient.cs`; full audit not done). |
| **PayPal client** | `DataContractJsonSerializer` in `PayPalClient.cs` — legacy serializer; ensure no untrusted type graphs. |

### 8.2 Reliability

| Area | Observation |
|------|-------------|
| **SQL retries** | `EnableRetryOnFailure()` on SQL Server contexts — good. |
| **Transactions** | Used in `InvoiceRepository`; payment flows — **not enough evidence** of consistent transactional boundaries across donation + payment + email. |
| **Idempotency** | `PaymentNotificationRepository.EmailNotificationExits` suggests duplicate email protection — extend pattern to payment state updates. |
| **Hosted services** | `TenantStaticSyncHostedService` — failure impact on static assets per tenant (investigate retry/dead-letter). |
| **Function app** | Polly package referenced; verify policies on external calls. |
| **Caching** | Static `ConcurrentBag` in `DonationRepository` — stale totals under concurrency. |

---

## 9. Refactoring recommendations

### Quick wins (days)

| # | Action | Why | Benefit |
|---|--------|-----|---------|
| 1 | Remove `UseDeveloperExceptionPage()` from non-Development environments | Stops stack trace leakage | Security compliance, safer production |
| 2 | Enable `dotnet test` in GitHub Actions (and Azure core pipeline) | Tests currently not gating merges | Catch regressions early |
| 3 | Remove hardcoded PayPal password from E2E; use secret/env | Credential in git | Rotatable secrets, no sandbox compromise |
| 4 | Delete or complete Autofac setup (prefer single DI container) | Empty Autofac root adds confusion | Simpler startup, faster onboarding |
| 5 | Fix `DocumentationFile` path (`net5.0` → `net9.0`) in Web csproj | Stale tooling config | Reliable builds/analyzers |
| 6 | Restore or remove broken `azure-pipeline-core.yml` solution link | Broken solution items confuse VS/CI | Cleaner repo, working pipelines |

### Medium effort (weeks)

| # | Action | Why | Benefit |
|---|--------|-----|---------|
| 7 | Extract **application services** for donation/payment use cases; slim `DonationRepository` | 857-line repository | Testable rules, smaller change blast radius |
| 8 | Stop **test projects referencing Web**; use `WebApplicationFactory` + shared `Program` partial or test host project | Tight coupling | Faster tests, isolated failures |
| 9 | Move payment provider types behind interfaces in **Application** layer | Repository imports EasyPay models | Swappable payments, cleaner layers |
| 10 | Consolidate duplicate **Startup** between Web and TenantManagement | DRY for auth/DB/KeyVault | One place to fix security config |
| 11 | Replace `BuildServiceProvider()` in `ConfigureServices` with `IConfigureOptions` / factory pattern | ASP.NET anti-pattern | Stable startup, fewer singleton bugs |
| 12 | Align **Easypay.Rest.Client** to `net9.0` or consume as NuGet package | TF mismatch | Consistent security patches |
| 13 | Narrow `AdditionallyAllowedTenants` to known tenant IDs | Security hardening | Reduced cross-tenant auth risk |
| 14 | Rename `AlimentaEstaldeia` → `AlimentaEstaIdeia` test project/folder | Typo | Less operational confusion |

### Strategic architecture changes (months)

| # | Action | Why | Benefit |
|---|--------|-----|---------|
| 15 | Introduce **Application** + **Contracts** projects; keep `Model` persistence-only or split Domain vs Persistence | Clean boundaries | Sustainable features, clearer testing |
| 16 | Formalize **bounded context** between “Donation core” and “SAS platform” with integration events or shared kernel only for tenant ID | Two products in one solution | Independent release cycles |
| 17 | Webhook **dedicated worker** (Functions already started) with outbox pattern for payment events | Web handles HTTP + DB + email today | Resilience, replay, idempotency |
| 18 | Centralize **observability** (structured logging, correlation IDs on donation flow — partial middleware exists) | Operations at scale | Faster incident response |
| 19 | Package EasyPay/PayPal as versioned internal NuGet rather than solution folders | Large generated code in repo | Smaller solution, clearer ownership |

---

## 10. Project-by-project breakdown

### BancoAlimentar.AlimentaEstaIdeia.Web

- **Type:** ASP.NET Core 9 web application (Razor Pages, areas Admin/Identity/RoleManagement, minimal API controllers under `Api/`).
- **References:** Full stack including Sas.*, payments, Repository.
- **Notes:** Owns `ApplicationDbContext` migrations; `Startup` is the system composition root; ~96 page models; feature management and multi-language support.

### BancoAlimentar.AlimentaEstaIdeia.Model

- **Type:** Class library (EF + Identity entities).
- **References:** NuGet only.
- **Notes:** “Domain” is database-centric; no dependency inversion for repositories.

### BancoAlimentar.AlimentaEstaIdeia.Repository

- **Type:** Data access + orchestration.
- **References:** Common, Model, Sas.ConfigurationProvider, Sas.Model, EasyPay.
- **Notes:** `UnitOfWork` aggregates repositories; `DonationRepository` is the main complexity hotspot.

### BancoAlimentar.AlimentaEstaIdeia.Common

- **Type:** Shared library.
- **References:** Sas.Model, EasyPay.
- **Notes:** Should remain small; watch for becoming a second “god” library.

### BancoAlimentar.AlimentaEstaIdeia.Sas.*

- **Sas.Model:** Tenant infrastructure EF model.
- **Sas.Repository:** Infrastructure unit of work.
- **Sas.ConfigurationProvider:** Key Vault + tenant-aware configuration (warning level disabled).
- **Sas.Core:** HTTP pipeline, tenant middleware, static file provider, hosted sync.
- **Sas.Web.TenantManagement:** Second web host for operators.

### BancoAlimentar.AlimentaEstaIdeia.Function

- **Type:** Azure Functions (.NET 9 isolated worker).
- **References:** Repository stack without Web UI.
- **Notes:** Shares Key Vault pattern; registers `InfrastructureDbContext` only in shown `Program.cs` snippet.

### Easypay.Rest.Client & Paypal

- **Type:** Integration libraries (vendored / thin wrapper).
- **Notes:** EasyPay is auto-generated bulk; treat as external dependency boundary.

### BancoAlimentar.AlimentaEstaIdeia.Tools

- **Type:** Console maintenance.
- **Notes:** Contains Key Vault copy utilities with broad tenant credential settings; keep out of routine CI publish.

### Test projects

- **Repository.Tests:** Strongest unit coverage (donations, invoices, NIF validator).
- **Sas.Core.Tests:** Light coverage of naming and middleware.
- **Integration / Selenium / E2E:** Environment-dependent; require secrets and running sites.

---

## Top 10 risks / issues (priority order)

| P | Issue | Impact |
|---|--------|--------|
| 1 | Developer exception page in Production/Staging | Confidentiality breach, compliance |
| 2 | Payment endpoints security model unclear / shared query secrets | Fraudulent payment confirmation |
| 3 | `DonationRepository` size + static cache | Wrong totals, race bugs, hard maintenance |
| 4 | CI does not run tests; missing core pipeline file | Regressions ship to production |
| 5 | Hardcoded sandbox credentials in E2E tests | Secret exposure, account abuse |
| 6 | `AdditionallyAllowedTenants = "*"` on Azure credentials | Cross-tenant Azure access |
| 7 | Tests compile against entire Web project | Slow, brittle test suite |
| 8 | Layering violations (Repository → Configuration, EasyPay in data layer) | Expensive changes, weak domain model |
| 9 | Dual DI (Autofac + MS DI) without clear modules | Startup bugs, memory scopes |
| 10 | EasyPay net8.0 vs app net9.0 + massive vendored SDK | Supply-chain and upgrade friction |

---

## Practical refactoring roadmap

### Phase 0 — Stabilize (1–2 sprints)

- Fix production exception handling and CORS origins.
- Turn on automated tests in CI; add webhook smoke tests.
- Remove secrets from E2E source; rotate exposed sandbox password.
- Fix solution/pipeline broken references.

### Phase 1 — Contain complexity (1–2 months)

- Split donation/payment **application services** from `DonationRepository`.
- Remove `BuildServiceProvider` / static `ServiceCollection` from `Startup`.
- Decouple test projects from Web csproj reference where possible.
- Document two-database migration deploy procedure.

### Phase 2 — Platform hardening (quarter)

- Extract payment abstractions; move webhooks toward Function + outbox.
- Unify SAS and Web startup configuration.
- Upgrade/consolidate EasyPay client packaging.
- Expand tenant isolation tests and security review on notification APIs.

### Phase 3 — Structural evolution (optional)

- Application layer projects and bounded context documentation.
- Evaluate whether TenantManagement should share host with policy separation or remain split.

---

## Evidence limitations

The following were **not** fully verified in this review:

- Runtime EasyPay **signature/HMAC** validation on all notification types.
- Complete **authorization** attributes on every API/controller action.
- Production **App Service** configuration and network restrictions.
- Database **row-level security** per tenant on `ApplicationDbContext` (tenant filter may be application-level only).
- Exact **Azure DevOps** pipeline steps (external to repo).

Where marked **“not enough evidence”**, validate with targeted code review, integration tests, or penetration test follow-up per [`Documentation/Penetration-Test-Setup/`](Documentation/Penetration-Test-Setup/).

---

*Generated as an architecture review artifact for the Alimenta Esta Ideia solution. Update this document when major structural or hosting changes land.*
