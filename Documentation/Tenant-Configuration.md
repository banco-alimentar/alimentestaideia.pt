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
Keep deployment migrations as the ordered step described in
[CI-Azure-DevOps.md](CI-Azure-DevOps.md); runtime initialization does not replace it.

External authentication options are cached by tenant ID and scheme name. Their
configuration adapters are singleton-safe and read the current request overlay.
OAuth 2 callback state is protected with a tenant-specific purpose, so another
tenant cannot accept it. Deploying this state format invalidates in-flight login
redirects from the previous version; restart the sign-in flow if needed.
The SuperAdmin settings-reload action invalidates all tenant authentication
option caches as well as the existing configuration caches.

## Static Assets

Tenant asset synchronization records ETags in a local version manifest. The
existing `onlyIfSizeChanged` argument now requires both size and recorded version
to match before skipping a blob. Versionless blobs are refreshed conservatively.

Downloads are staged beside the destination and atomically published after
completion. Conditional downloads reject blobs changed after listing. Failed
downloads retain the last complete cached file and clean up temporary files.
Deleted blobs are reconciled only after successful enumeration. Paths are
canonicalized and must remain within the tenant's cache root.

Synchronization and clearing are coordinated per tenant. A clear request during
synchronization fails with a retryable message instead of deleting active files.
The hosted synchronization service remains a startup pass; continuous polling is
not enabled. Local files remain the preferred serving path. Blob fallback uses
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

The SAS Core tests cover asynchronous initialization, routing candidate validity,
configuration binding, tenant-keyed authentication and callback state, static
cache freshness and interrupted downloads, and domain ownership constraints.
Storage tests use mocked clients; database constraint tests use native SQLite.
SQL migration generation checks do not contact a SQL Server or Key Vault.

On machines whose process locale is not English, run the web integration suite
with `LANG=en_US.UTF-8 LC_ALL=en_US.UTF-8` before `dotnet test`. Its existing
localized validation assertions compare process resources with the test host's
English responses. No localization code is changed by these improvements.