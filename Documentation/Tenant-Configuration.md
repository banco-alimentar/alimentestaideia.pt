# Tenant Configuration And Runtime Caches

## Configuration Boundaries

`TenantConfigurationRoot` overlays host configuration with the current tenant's
secrets. Indexed reads, sections, enumeration, and binding use the same overlay.
The overlay belongs to the HTTP request; database configuration updates do not
mutate the process-wide secret dictionary. Reads before tenant resolution fall
back to host configuration without fixing the request to that early result.

Use `TenantConfigurationRoot.HostConfiguration` for process-only controls.
In particular, local email capture, expected local databases, and its fixed SMTP
transport must not be overridden by tenant settings.

## Initialization And Authentication

Tenant database initialization uses asynchronous per-tenant coordination and
records success only after seeding completes. An unsuccessful attempt remains
retryable. HTTP disconnection does not cancel server-side initialization.
Every request with valid configuration awaits tenant readiness, including when
secrets were preloaded by the static-asset startup service. Secret-cache load
results do not control database initialization or retries.
Keep deployment migrations as the ordered step described in
[CI-Azure-DevOps.md](CI-Azure-DevOps.md); runtime initialization does not replace it.

External authentication options are cached by tenant ID and scheme name. Their
configuration adapters are singleton-safe and read the current request overlay.
Cache entries also track the tenant secret snapshot, preventing requests after
reload from reusing options created from an older snapshot.
OAuth 2 callback state is protected with a tenant-specific purpose, so another
tenant cannot accept it. Deploying this state format invalidates in-flight login
redirects from the previous version; restart the sign-in flow if needed.
The SuperAdmin settings-reload action invalidates all tenant authentication
option caches as well as the existing configuration caches.

Vault clients and secrets belong to a singleton cache per application host,
shared by transient configuration managers. Bootstrap is coordinated across
managers and uses asynchronous storage and database operations. Shared platform
or registry failures return configuration-unavailable responses. Invalid client
configuration for one tenant is recorded separately and does not block healthy
tenants. Failed bootstrap and tenant loads remain retryable with bounded
exponential backoff; concurrent secret requests share a per-tenant load.
Database-backed request overrides never modify these cached secrets.
Application request and hosted-service call sites use the asynchronous API;
the synchronous loader remains only as a compatibility entry point.

Settings reload also invalidates vault clients and secrets, so the next request
reloads credentials and tenant metadata. In-flight loads finish against their
old cache generation and cannot repopulate the replacement cache. Requests
racing with reload may receive the existing retryable configuration-unavailable
response. Database initialization remains once per tenant per process; reload
does not rerun successful seeding or replace ordered deployment migrations.

## Shared Invalidation And Diagnostics

The settings-reload POST first publishes a new revision in
`InfrastructureDbContext.RuntimeCacheRevisions`, keyed by the host environment.
It clears local caches only after publication succeeds. Instances sharing the
same infrastructure database and environment observe revisions every 24 hours
and invalidate vault, database-setting, authentication-option, and lookup caches.
Polling failures are logged and visible in runtime diagnostics; they retain the
last usable caches. Instances in other environments are not invalidated.
The initiating instance reloads immediately; peers can retain old settings for
up to 24 hours. Restart all instances for urgent changes, or configure a shorter
host-level polling interval.
An infrastructure provider that is only in memory does not provide a durable
cross-instance signal.

Apply [RuntimeCacheInvalidation](../BancoAlimentar.AlimentaEstaIdeia.Sas.Model/Migrations/20261121121253_RuntimeCacheInvalidation.cs)
to each shared infrastructure database before deploying the polling workers.
This creates one table and does not rewrite tenant data or alter deployed identity
keys. Existing domain-ownership preflight checks still apply to earlier pending
migrations. Do not run an infrastructure migration separately for each tenant's
application database.

Generate and review SQL without opening a database connection (from the web
project directory, where the repository EF tool manifest is located):

```bash
dotnet ef migrations script 20261101120201_TenantDomainOwnership 20261121121253_RuntimeCacheInvalidation \
  --idempotent --context InfrastructureDbContext \
  --project ../BancoAlimentar.AlimentaEstaIdeia.Sas.Model \
  --startup-project ../BancoAlimentar.AlimentaEstaIdeia.Sas.Model \
  --configuration Release
```

The script above assumes the domain migration is already applied. Generate from
the actual last applied migration when earlier migrations are pending. Select
`InfrastructureDbContext` explicitly in the ordered migration step; updating
only `ApplicationDbContext` does not create the revision table. Existing
infrastructure permissions must allow the web reload action to insert/update
revision rows and the web/function workers to read them.

The existing SuperAdmin-only `/Admin/ReloadSettings` page reports instance cache
generation, tenant configuration load/failure state, initialization outcome,
asset refresh timestamps and retry schedule, and shared revision observation.
Only timestamps, identifiers, and failure categories are displayed; exception
messages, secret values, and connection strings are not exposed.
Authentication-provider registration remains host-wide and is unchanged.

## Host-Level Runtime Limits

Defaults are a 10-second configuration-load deadline, 24-hour asset refresh,
and 24-hour revision polling. Retry delays start at five seconds and grow to a
maximum of one minute. These process controls are bound from host configuration,
not tenant overrides:

```json
{
  "TenantRuntime": {
    "ConfigurationLoadTimeout": "00:00:10",
    "InitialRetryDelay": "00:00:05",
    "MaxRetryDelay": "00:01:00",
    "StaticAssetRefreshInterval": "1.00:00:00",
    "InvalidationPollInterval": "1.00:00:00"
  }
}
```

Intervals must be positive. Load deadlines and retry caps are bounded to five
minutes; revision polling is bounded to 24 hours and asset refresh to 30 days. The load deadline
does not cancel tenant seeding or database migrations when a client disconnects.

## Static Assets

Tenant asset synchronization records ETags in a local version manifest. The
existing `onlyIfSizeChanged` argument now requires both size and recorded version
to match before skipping a blob. Versionless blobs are refreshed conservatively.

Downloads are staged beside the destination and published after the download
pass completes. Conditional downloads reject blobs changed after listing. Failed
downloads retain the last complete cached files and clean up temporary files.
Publication retains rollback copies to recover from an I/O failure; this is not
a crash-atomic filesystem transaction and requires staging/backup disk space.
Deleted blobs are reconciled only after successful enumeration. Paths are
canonicalized and must remain within the tenant's cache root.

Synchronization and clearing are coordinated per tenant. A clear request during
synchronization fails with a retryable message instead of deleting active files.
The hosted service runs a startup pass and refreshes successful tenants every
24 hours. Failed tenants have independent bounded retries, without repeatedly
refreshing healthy tenants. A cache revision wakes the worker and schedules fresh
downloads. Host shutdown cancels pending work without recording a spurious
failure. Local files remain the preferred serving path. Blob fallback uses
known metadata rather than repeated existence checks, but the synchronous
`IFileProvider` contract still permits remote work on cache misses.
Request-owned file providers are disposed with the response; the application's
web-root provider is disposed when the host stops.

## Domain Ownership Migration

`TenantDomainOwnership` requires a unique `(DomainName, Environment)` pair and
bounds the two required columns to 253 and 64 characters respectively. Tenant
lookup caches include the environment and have a five-minute lifetime; the
existing settings-reload action can invalidate them earlier.

Before deploying, audit existing infrastructure mappings for null, blank,
oversized, or duplicate values. The SQL Server migration raises an error before
altering the columns when such records exist; it does not delete, truncate, or
silently invent mapping values. Resolve these records deliberately and rerun the
ordered infrastructure migration step. Review the generated SQL for the target
database. No production or preproduction database is migrated by local tests.

The infrastructure design-time factory builds a SQL Server model without a
connection string, application startup, or secret access. Existing identity
key lengths remain pinned to their deployed 128-character schema.

## Verification

The SAS Core tests cover asynchronous initialization and middleware retries with
cached secrets, shared secret loads and reload races, routing candidate validity,
configuration binding, tenant-keyed authentication and callback state, static
cache freshness and interrupted downloads, and domain ownership constraints.
Storage tests use mocked clients; database constraint tests use native SQLite.
SQL migration generation checks do not contact a SQL Server or Key Vault.
Vault-loading regression tests also use mocked clients. Web integration tests
verify authenticated settings reload and actual two-domain HTTP isolation for
effective configuration, application database values, static files, application
cookies, and OAuth callback state. Shared revision tests use native SQLite and
separate local service providers to simulate peer instances.

On machines whose process locale is not English, run the web integration suite
with `LANG=en_US.UTF-8 LC_ALL=en_US.UTF-8` before `dotnet test`. Its existing
localized validation assertions compare process resources with the test host's
English responses. Runtime-status labels have resources in Portuguese, English,
Spanish, and French.