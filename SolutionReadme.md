# Solution Architecture Review — Alimenta Esta Ideia

**Solution file:** `BancoAlimentar.AlimentaEstaIdeia.Web.sln`  
**Review date:** 2026-05-27  
**Scope:** Static analysis of the Visual Studio solution (19 buildable `.csproj` projects, CI definitions, and representative code paths). No runtime penetration testing was performed.

---

## Executive summary

This solution implements **alimentestaideia.pt** — a multi-tenant ASP.NET Core web application for food-bank donations (Federação Portuguesa dos Bancos Alimentares), with payment integrations (EasyPay for Multibanco/MBWay/credit card, PayPal), ASP.NET Identity, admin/backoffice areas, and a **SAS (“Doar”)** multi-tenant platform in dedicated libraries.

**Architectural style (inferred):** A **modular monolith** with **layered** tendencies (Model → Repository → Web) plus a **parallel vertical slice** for multi-tenancy (`Sas.*`). It is **not** clean architecture: persistence, payment SDKs, telemetry, and ASP.NET concerns leak across layers.

**Strengths**

- Clear separation between **tenant infrastructure** (`Sas.Model`, `InfrastructureDbContext`) and **donation domain** (`Model`, `ApplicationDbContext`).
- Production configuration uses **Azure Key Vault** and **managed identity** patterns for secrets.
- Uniform **.NET 9** target (except vendored EasyPay client on **.NET 8**).
- Meaningful automated tests around **donations** and **invoicing**; StyleCop and `TreatWarningsAsErrors` on most projects.
- Recent hardening: **Autofac removed**, **developer exception page limited to Development** on main Web host, **Functions `local.settings.json` untracked**, GitHub Actions on **Node 24–compatible action versions**.

**Top concerns (current)**

1. **Payment webhooks** rely on shared secrets / provider validation; sampled `PaymentNotification` uses query-string `key` vs `ApiCertificateV3` without `[Authorize]` ([`PaymentNotification.cs`](BancoAlimentar.AlimentaEstaIdeia.Web/Api/PaymentNotification.cs)).
2. **`DonationRepository` (~875 LOC)** mixes querying, caching, payment types, and telemetry — high change risk.
3. **CI builds without running tests** (`dotnet test` commented out in [`.github/workflows/alimentestaideia.yaml`](.github/workflows/alimentestaideia.yaml)); `azure-pipeline-core.yml` referenced in `.sln` but **missing from repo**.
4. **`DefaultAzureCredential` with `AdditionallyAllowedTenants = { "*" }`** in Web, Function, Key Vault manager, and Tools — broad cross-tenant Azure AD acceptance.
5. **Secrets in git history** (e.g. legacy `Unicre.AccessKey` in pre-2021 `Web.config`; GitHub secret scanning alert #6) — rotation + history purge still required.
6. **Test projects reference the full Web host** — tight coupling and slow CI.
7. **`Startup.ConfigureServices` calls `BuildServiceProvider()`** and exposes **static `ServiceCollection`** — fragile startup anti-pattern.
8. **EasyPay client** is a large generated SDK on **net8.0** while the app is **net9.0**.
9. **`HttpsPort = 5001`** in non-Development HTTPS redirection ([`Startup.cs`](BancoAlimentar.AlimentaEstaIdeia.Web/Startup.cs)) — likely wrong for Azure App Service.

### Recent improvements (since prior review)

| Change | Status |
|--------|--------|
| Remove unused **Autofac** host integration | Done — standard `Host.CreateDefaultBuilder` in [`Program.cs`](BancoAlimentar.AlimentaEstaIdeia.Web/Program.cs) |
| `UseDeveloperExceptionPage()` only in **Development** (Web) | Done — Staging/Production use `/Error` + HSTS |
| Upgrade GitHub Actions to `checkout@v5`, `setup-dotnet@v5` | Done |
| Ignore **`local.settings.json`**; add example template | Done (`.gitignore`) |
| Fix `DocumentationFile` path **net5.0 → net9.0** | Done |
| Fix duplicate invoice **es/fr** `.resx` keys | Done |
| Selenium UI test nullable / obsolete API warnings | Done |
| Purge **Unicre.AccessKey** from git history | **Not done** — still in old commits; app no longer uses it (credit card via **EasyPay**) |

---

## 1. Solution overview

### 1.1 Project inventory

| Project | Classification | Purpose (evidence-based) |
|--------|----------------|---------------------------|
| **BancoAlimentar.AlimentaEstaIdeia.Web** | Web app (ASP.NET Core, Razor Pages + API) | Primary public site: donations, identity, admin areas, payment notification endpoints, EF migrations for `ApplicationDbContext`. |
| **BancoAlimentar.AlimentaEstaIdeia.Sas.Web.TenantManagement** | Web app (secondary host) | Tenant/admin tooling for SAS platform; own `Program.cs` / `Startup.cs`. |
| **BancoAlimentar.AlimentaEstaIdeia.Function** | Azure Functions worker | Background jobs: subscription cleanup, multi-tenant payment notifications. |
| **BancoAlimentar.AlimentaEstaIdeia.Tools** | Console / maintenance utility | One-off DB and Key Vault migration scripts (mostly commented in `Program.cs`). |
| **BancoAlimentar.AlimentaEstaIdeia.Model** | Domain / data model library | EF entities, identity types, donation catalog models; references EF Core + Identity. |
| **BancoAlimentar.AlimentaEstaIdeia.Repository** | Data access / application services | Repositories, `UnitOfWork`, validators, mail abstractions. |
| **BancoAlimentar.AlimentaEstaIdeia.Common** | Shared utilities | Cross-cutting helpers; references `Sas.Model` and EasyPay. |
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
| **BancoAlimentar.AlimentaEstaIdeia.Web.Selenium.UITest** | UI tests (Selenium) | Browser automation against deployed environments (single live browser suite). |
| **BancoAlimentar.AlimentaEstaIdeia.Function.Tests** | Unit tests | Azure Functions (subscriptions, multibanco reminders). |

**Solution folders (non-build):** `Pipeline`, `Github`, `Tests`, `Tools`, `Sas`.

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

Tests ──► Web + Repository + Testing.Common (+ Selenium standalone)
Tools ──► Model, Repository, Sas.*
```

**Runtime entry points**

| Host | Entry | Role |
|------|--------|------|
| **Web** | `BancoAlimentar.AlimentaEstaIdeia.Web/Program.cs` → `Startup` | Main production site |
| **TenantManagement** | `Sas.Web.TenantManagement/Program.cs` | SAS tenant administration |
| **Function** | `Function/Program.cs` | Scheduled/webhook background processing |
| **Tools** | `Tools/Program.cs` | Manual ops (local/dev) |

**Payment note:** UI labels reference “Unicre” for credit card (`#pagamentounicre`, `PagarRedUnicre`), but implementation uses **EasyPay** (`OnPostCreditCardAsync` → `SinglePaymentMethods.Cc`). Legacy **`Unicre.AccessKey`** exists only in **git history** (removed with old `Link.BA.Donate.WebSite`); current code does not read it.

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
| **Paypal** | Package references only |
| **Repository.Tests** | Model, Repository, **Web** |
| **Sas.Core.Tests** | Repository, Sas.Core, Testing.Common, **Web** |
| **Integration.Tests** | Model, Repository, Testing.Common, **Web** |
| **Testing.Common** | Model, Sas.ConfigurationProvider, Sas.Model |
| **Selenium.UITest** | Common, Model, Repository, Sas.Model |
| **Function.Tests** | Function, Repository.Tests, Web.TestHost |

### 2.2 Circular dependencies

**No project-reference cycles** were found (DAG is acyclic).

**Logical / layering cycles (smells):**

- **Sas.ConfigurationProvider → Model**: Configuration layer depends on donation domain entities.
- **Repository → Sas.ConfigurationProvider**: Data layer pulls in Key Vault and HTTP-aware tenant configuration.
- **Common → EasyPay**: “Shared utilities” depend on payment vendor SDK.

### 2.3 Tight coupling and suspicious references

| Issue | Impact |
|-------|--------|
| **Tests → Web** | Repository and SAS tests compile against the full web application; Razor/Startup changes break test builds. |
| **Repository uses `Easypay.Rest.Client.Model`** | Persistence layer is payment-vendor-specific. |
| **Web holds EF migrations** for `ApplicationDbContext`; **Sas.Model** holds `InfrastructureDbContext` migrations | Deploy pipelines must run both. |
| **Typo project/folder** `AlimentaEstaldeia` | Confusing onboarding and brittle scripts. |

### 2.4 Possibly redundant or underused artifacts

| Item | Notes |
|------|--------|
| **azure-pipeline-core.yml** | Listed in `.sln` under `Pipeline` but **file not present** (only `azure-pipeline-selenium-ui-tests.yml` at repo root). |
| **Commented Azure App Configuration** in `Startup` | `AddFeatureManagement` active; App Config integration disabled. |
| **Distributed SQL cache** | Commented out in `Startup`. |
| **Tools `Program.cs`** | Most maintenance entry points commented out. |

---

## 3. Architecture review

### 3.1 Inferred style

**Modular monolith + multi-tenant plugin model:**

- **Horizontal layers:** Model → Repository → Web.
- **Vertical SAS module:** Sas.Model / Sas.Repository / Sas.Core / Sas.ConfigurationProvider consumed by Web.
- **Not microservices:** Single deployable web app; Functions are auxiliary workers sharing DB/configuration patterns.
- **Tenant resolution:** `UseDoarMultitenancy()` middleware (`Sas.Core`) + `ITenantProvider` + per-request `TenantConfigurationRoot` replacing scoped `IConfiguration`.

### 3.2 Separation of concerns violations

| Layer bleed | Example |
|-------------|---------|
| **Domain in web** | Large number of Razor Page code-behind files; donation/payment flow split between Pages and `DonationRepository`. |
| **Infrastructure in domain model** | `Model` references EF Core, Identity, Azure Storage. |
| **Configuration in repository stack** | `Repository` → `Sas.ConfigurationProvider`. |
| **Presentation in repository** | `DonationRepository` uses Application Insights directly. |
| **Payment DTOs in repository** | EasyPay notification types flow through Web API and repositories. |

### 3.3 Domain / application / infrastructure / presentation

| Concern | Where it lives | Assessment |
|---------|----------------|------------|
| **Domain rules** | Model entities + Repository methods | No explicit domain services. |
| **Application use cases** | Web Pages, API controllers, Repository | No dedicated Application project. |
| **Infrastructure** | EF contexts, Azure, EasyPay/PayPal, Key Vault | Spread across Repository, Sas.*, Web `Startup`. |
| **Presentation** | Razor, static files, API | Web + `Pages/Tenants/`. |

**Impact:** Hard to unit-test business rules without DB and web host; payment or tenancy changes ripple widely.

---

## 4. Code quality and maintainability

### 4.1 Large classes and hotspots

| Artifact | ~LOC | Concern |
|----------|------|---------|
| `DonationRepository.cs` | 875 | God repository: totals, payments, static cache (`ConcurrentBag`), queries. |
| `Startup.cs` | 530 | God startup: auth, DB, telemetry, health, localization, MiniProfiler. |
| EasyPay `*Api.cs` generated files | 876–2284 | Vendor SDK; do not hand-edit. |
| `KeyVaultConfigurationManager.cs` | Large | Central secret orchestration — high-risk change surface. |

### 4.2 Duplication and naming

- Parallel **Startup** in Web and `Sas.Web.TenantManagement`.
- **AlimentaEstaIdeia** vs **AlimentaEstaldeia** (integration test project).
- `PaymentStatus.Payed` — consistent internally; document for external APIs.

### 4.3 Testing and extensibility pain

- **Static `Startup.ServiceCollection`** and **`services.AddSingleton(services)`** — service locator smell.
- Early **`BuildServiceProvider()`** in `ConfigureServices` for `EndpointSelector` replacement.
- **Static cache** on `DonationRepository` — shared mutable state across requests.
- **MiniProfiler** registered in `ConfigureServices` for all environments (enabled in pipeline when middleware runs; Staging still calls `UseMiniProfiler()`).

### 4.4 Technical debt hotspots

1. Payment notification + donation status update paths.
2. Multi-tenant configuration merge (`TenantConfigurationRoot`).
3. Subscription and invoice repositories.
4. Identity + per-tenant external login post-configure.
5. Migrations split across **Web** and **Sas.Model**.

---

## 5. Configuration and startup flow

### 5.1 Primary entry point — Web

1. **`Program.CreateHostBuilder`**
   - Standard **`Host.CreateDefaultBuilder`** (Autofac removed).
   - **Key Vault** when `IsProduction()` or `IsStaging()` for `VaultUri` and `SasVaultUri`.
   - `DefaultAzureCredential` with **`AdditionallyAllowedTenants = { "*" }`**.
2. **`Startup.ConfigureServices`**
   - SAS: `ITenantProvider`, naming strategies, scoped `TenantConfigurationRoot` as `IConfiguration`.
   - EF: `ApplicationDbContext` (SQL Server, retries, migrations assembly **Web**).
   - EF: `InfrastructureDbContext` (SQL Server or in-memory SQLite seed for dev).
   - Identity, feature flags, EasyPay/PayPal builders, CORS, App Insights, MiniProfiler, health checks.
3. **`Startup.Configure`**
   - **Development:** `UseDeveloperExceptionPage()`, MiniProfiler, migrations endpoint.
   - **Staging / Production:** `UseExceptionHandler("/Error")`, `UseHsts()` (no developer exception page).

### 5.2 Configuration sources

| Source | Used by |
|--------|---------|
| `appsettings*.json`, environment variables | All hosts |
| User secrets | Web, Function, Tools |
| Azure Key Vault | Web, Function, TenantManagement (staging/production) |
| Per-tenant Key Vault secrets | `KeyVaultConfigurationManager` / `TenantConfigurationRoot` |
| `local.settings.json` | Function (local only; **gitignored**) |

### 5.3 DI and logging

- **DI:** Microsoft.Extensions.DependencyInjection only in `Startup` (no Autofac).
- **Logging:** Application Insights + console (Function); telemetry initializers and filters in Web.

### 5.4 Risky configuration patterns

| Pattern | Risk |
|---------|------|
| `AdditionallyAllowedTenants = { "*" }` | Cross-tenant token acceptance for Azure AD credentials. |
| `PaymentNotification.Get` validates `key == configuration["ApiCertificateV3"]` | Shared secret in query string; Referer/log leakage. |
| **Selenium PayPal credentials** | Must stay in user secrets / CI secrets, not source. |
| **Legacy `Unicre.AccessKey` in git history** | Secret scanning alert; rotate at provider even though app unused. |
| `Sas.ConfigurationProvider` `<WarningLevel>0</WarningLevel>` | Disables warnings despite StyleCop. |
| `options.HttpsPort = 5001` in non-Development | May break HTTPS redirect behind Azure front door. |
| CORS trailing-slash origins | `https://alimentestaideia.pt/` may not match requests without slash. |

---

## 6. Testing review

### 6.1 Strategy (as implemented)

| Layer | Project | Approx. test methods |
|-------|---------|----------------------|
| Unit / repo | Repository.Tests | ~150+ |
| Unit / SAS | Sas.Core.Tests | ~13 |
| Unit / Functions | Function.Tests | 4 |
| Integration | AlimentaEstaldeia.Web.Integration.Tests | ~28 |
| UI (live) | Web.Selenium.UITest | 7 |

**Shared:** `Testing.Common` and `Web.TestHost` for `CustomWebApplicationFactory`.

**Selenium prerequisites:** Chrome, user secrets, dev site up. Run via `azure-pipeline-selenium-ui-tests.yml` or locally before release.

### 6.2 Gaps

- No dedicated tests for `Sas.ConfigurationProvider`, **Function** triggers, or **Paypal** project.
- **Payment webhooks** — limited automated contract/security tests (not enough evidence of full EasyPay signature coverage).
- **TenantManagement** — no test project.
- **GitHub Actions** — `dotnet test` **commented out**.
- Repository/SAS tests depend on **full Web** project.

### 6.3 Highest-value tests to add first

1. **Payment notification handlers** — idempotency, invalid secret, duplicate callbacks, transition to `Payed`.
2. **`TenantConfigurationRoot`** — tenant A config must not leak to tenant B.
3. **`DonationRepository` totals/cache** — correctness without static shared cache.
4. **Authorization policies** — Admin/SuperAdmin and external login schemes.
5. **Function `MultiBancoPaymentNotificationFunction`** — integration with repository under failure/retry.

---

## 7. Build and deployment concerns

| Topic | Finding |
|-------|---------|
| **Target frameworks** | Almost all **net9.0**; **Easypay.Rest.Client** is **net8.0**. |
| **Warnings as errors** | Most projects; undermined on `Sas.ConfigurationProvider` (`WarningLevel` 0). |
| **Missing pipeline file** | `azure-pipeline-core.yml` in solution, absent on disk. |
| **GitHub Actions** | `checkout@v5`, `setup-dotnet@v5` (Node 24); **build only**, no test gate. |
| **Package drift** | `Microsoft.AspNetCore.Http.Abstractions` **2.3.0** on some net9 projects alongside `FrameworkReference`. |
| **Migrations** | Two contexts, two migration assemblies — deploy must run both. |
| **Publish size** | Large `wwwroot`, tenant themes, PDF generation — `TenantStaticSyncHostedService` for static sync. |
| **Secrets in repo** | `**/local.settings.json` gitignored; historical secrets still in git objects. |

**CI/CD:** Selenium pipeline separate (`azure-pipeline-selenium-ui-tests.yml`); core build/test/release path fragmented.

---

## 8. Security and reliability

### 8.1 Security

| Area | Observation |
|------|-------------|
| **Secrets** | Key Vault for prod/staging — good. User secrets for dev. **Rotate** keys exposed in git history (Unicre, past `local.settings.json` if any). |
| **Exception handling** | Web: developer page **only in Development** (improved). TenantManagement: same pattern (Development only). |
| **Auth on APIs** | Payment notifications without `[Authorize]` in sampled code — rely on shared key/provider validation; full EasyPay HMAC audit **not enough evidence**. |
| **Multitenancy** | Middleware + per-tenant config; correctness depends on `Infrastructure` DB data. |
| **Data protection** | Azure Blob + Key Vault in production. |
| **Identity** | Confirmed email required; role policies for Admin/SuperAdmin. |
| **Azure credential** | `AdditionallyAllowedTenants = "*"` — widen blast radius. |
| **Serialization** | Newtonsoft.Json on MVC; EasyPay client — verify no unsafe `TypeNameHandling` on untrusted input (not fully audited). |

### 8.2 Reliability

| Area | Observation |
|------|-------------|
| **SQL retries** | `EnableRetryOnFailure()` on SQL Server contexts. |
| **Transactions** | Used in `InvoiceRepository`; payment flows — **not enough evidence** of consistent boundaries across donation + payment + email. |
| **Idempotency** | `PaymentNotificationRepository` patterns for duplicate email — extend to payment state updates. |
| **Hosted services** | `TenantStaticSyncHostedService` — failure impact on tenant static assets (retry policy not fully reviewed). |
| **Function app** | Polly referenced; verify policies on external calls. |
| **Caching** | Static `ConcurrentBag` in `DonationRepository` — stale totals under concurrency. |

---

## 9. Refactoring recommendations

### Quick wins (days)

| # | Action | Why | Benefit | Status |
|---|--------|-----|---------|--------|
| 1 | `UseDeveloperExceptionPage()` only in Development (Web) | Stack trace leakage | Security | **Done** |
| 2 | Remove unused **Autofac** | Dead complexity | Simpler startup | **Done** |
| 3 | Upgrade GitHub Actions to v5 / Node 24 | Deprecation warnings | CI longevity | **Done** |
| 4 | Gitignore **Function `local.settings.json`** | Secret commits | Safer dev | **Done** |
| 5 | Fix **DocumentationFile** net9.0 path | Stale tooling | Clean builds | **Done** |
| 6 | Enable **`dotnet test`** in GitHub Actions | No test gate | Catch regressions | Open |
| 7 | Keep **PayPal sandbox credentials** in user secrets / CI only (removed Playwright E2E project that had hardcoded password) | Credential in git | Rotatable secrets | Done (E2E removed) |
| 8 | **Rotate + purge** historical secrets (Unicre, storage keys, etc.) | Git history exposure | Compliance | Open |
| 9 | Restore or remove broken **`azure-pipeline-core.yml`** solution link | Broken solution items | Cleaner repo | Open |
| 10 | Fix **`HttpsPort = 5001`** for Azure or remove in Production | Wrong redirect port | Correct HTTPS | Open |

### Medium effort (weeks)

| # | Action | Why | Benefit |
|---|--------|-----|---------|
| 11 | Extract **application services** for donation/payment; slim `DonationRepository` | 875-line repository | Testable rules |
| 12 | Stop **test projects referencing Web** where possible | Tight coupling | Faster, isolated tests |
| 13 | Payment provider **interfaces**; keep EasyPay types at boundary | Repository imports EasyPay | Swappable payments |
| 14 | Consolidate duplicate **Startup** (Web + TenantManagement) | DRY for auth/Key Vault | One security surface |
| 15 | Replace `BuildServiceProvider()` in `ConfigureServices` | ASP.NET anti-pattern | Stable startup |
| 16 | Align **Easypay.Rest.Client** to `net9.0` or internal NuGet | TF mismatch | Consistent patches |
| 17 | Narrow **`AdditionallyAllowedTenants`** | Security hardening | Reduced cross-tenant risk |
| 18 | Rename **AlimentaEstaldeia** test project/folder | Typo | Less confusion |
| 19 | Translate remaining **pt-only** invoice `.resx` keys in `en` | Duplicate fixes in es/fr only | Correct locales |

### Strategic architecture changes (months)

| # | Action | Why | Benefit |
|---|--------|-----|---------|
| 20 | **Application** + **Contracts** projects; persistence-only Model | Clean boundaries | Sustainable features |
| 21 | **Bounded context** between Donation core and SAS platform | Two products in one solution | Independent releases |
| 22 | Webhook **worker** + outbox for payment events | Web does HTTP + DB + email | Resilience, replay |
| 23 | Centralize **observability** (correlation IDs on donation flow) | Operations at scale | Faster incidents |
| 24 | Package EasyPay/PayPal as versioned internal NuGet | Huge generated code in repo | Smaller solution |

---

## 10. Project-by-project breakdown

### BancoAlimentar.AlimentaEstaIdeia.Web

- **Type:** ASP.NET Core 9 (Razor Pages, Admin/Identity/RoleManagement, `Api/` controllers).
- **References:** Full stack including Sas.*, payments, Repository.
- **Notes:** Owns `ApplicationDbContext` migrations; composition root; multi-language; `web.config` for IIS OAuth query limits.

### BancoAlimentar.AlimentaEstaIdeia.Model

- **Type:** Class library (EF + Identity entities).
- **Notes:** Database-centric; no repository abstractions in domain.

### BancoAlimentar.AlimentaEstaIdeia.Repository

- **Type:** Data access + orchestration.
- **Notes:** `DonationRepository` is the main complexity hotspot.

### BancoAlimentar.AlimentaEstaIdeia.Common

- **Type:** Shared library (Sas.Model, EasyPay).
- **Notes:** Keep small; avoid becoming a second god library.

### BancoAlimentar.AlimentaEstaIdeia.Sas.*

- **Sas.Model:** Tenant infrastructure EF model and migrations.
- **Sas.Repository:** Infrastructure unit of work.
- **Sas.ConfigurationProvider:** Key Vault + tenant-aware configuration (`WarningLevel` 0).
- **Sas.Core:** HTTP pipeline, middleware, static files, hosted sync.
- **Sas.Web.TenantManagement:** Operator web host.

### BancoAlimentar.AlimentaEstaIdeia.Function

- **Type:** Azure Functions (.NET 9 isolated worker).
- **Notes:** Shares Key Vault pattern; `local.settings.json` not tracked.

### Easypay.Rest.Client & Paypal

- **Type:** Integration libraries (vendored / thin wrapper).
- **Notes:** Treat as external dependency boundary.

### BancoAlimentar.AlimentaEstaIdeia.Tools

- **Type:** Console maintenance.
- **Notes:** Key Vault utilities use broad `AdditionallyAllowedTenants`; keep out of routine CI publish.

### Test projects

- **Repository.Tests:** Strongest unit coverage.
- **Sas.Core.Tests:** Light middleware/naming coverage.
- **Integration / Selenium:** Environment- and secret-dependent.

---

## Top 10 risks / issues (priority order)

| P | Issue | Impact |
|---|--------|--------|
| 1 | Payment endpoints security model / shared query secrets | Fraudulent payment confirmation |
| 2 | Secrets in **git history** (Unicre.AccessKey, etc.) | Credential abuse, compliance |
| 3 | `DonationRepository` size + static cache | Wrong totals, race bugs, maintenance |
| 4 | CI does not run tests; missing core pipeline file | Regressions ship |
| 5 | `AdditionallyAllowedTenants = "*"` on Azure credentials | Cross-tenant Azure access |
| 6 | Tests compile against entire Web project | Slow, brittle suite |
| 7 | `BuildServiceProvider` / static `ServiceCollection` in Startup | Startup bugs, test difficulty |
| 8 | Layering violations (Repository → Configuration, EasyPay in data layer) | Expensive changes |
| 9 | EasyPay net8.0 vs app net9.0 + vendored SDK bulk | Supply-chain friction |
| 10 | `HttpsPort = 5001` in Staging/Production redirect options | Broken HTTPS behind Azure |

---

## Practical refactoring roadmap

### Phase 0 — Stabilize (1–2 sprints)

- Rotate secrets flagged by GitHub secret scanning; purge git history where policy requires.
- Enable automated tests in CI; add webhook smoke tests.
- Move Selenium credentials to GitHub Actions secrets / user secrets.
- Fix solution/pipeline broken references and production `HttpsPort`.

### Phase 1 — Contain complexity (1–2 months)

- Split donation/payment **application services** from `DonationRepository`.
- Remove `BuildServiceProvider` / static `ServiceCollection` from `Startup`.
- Decouple test projects from direct Web csproj reference where possible.
- Document two-database migration deploy procedure.

### Phase 2 — Platform hardening (quarter)

- Extract payment abstractions; move webhooks toward Function + outbox.
- Unify SAS and Web startup configuration.
- Upgrade/consolidate EasyPay client packaging.
- Expand tenant isolation tests and security review on notification APIs.

### Phase 3 — Structural evolution (optional)

- Application layer projects and bounded-context documentation.
- Evaluate TenantManagement host vs policy-separated single host.

---

## Evidence limitations

The following were **not** fully verified in this review:

- Runtime EasyPay **signature/HMAC** validation on all notification types.
- Complete **authorization** on every API/controller action.
- Production **App Service** configuration and network restrictions.
- Database **row-level security** per tenant on `ApplicationDbContext` (tenant filter may be application-level only).
- Exact **Azure DevOps** pipeline steps (external to repo).

Where marked **“not enough evidence”**, validate with targeted code review, integration tests, or penetration test follow-up per [`Documentation/Penetration-Test-Setup/`](Documentation/Penetration-Test-Setup/) if present.

---

*Architecture review artifact for the Alimenta Esta Ideia solution. Update when major structural, security, or hosting changes land.*
