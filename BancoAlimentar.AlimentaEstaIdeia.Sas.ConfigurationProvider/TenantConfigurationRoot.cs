// -----------------------------------------------------------------------
// <copyright file="TenantConfigurationRoot.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Primitives;

    /// <summary>
    /// TenantConfigurationRoot that implements <see cref="IConfiguration"/>.
    /// </summary>
    public class TenantConfigurationRoot : IConfiguration
    {
        private static readonly object SnapshotKey = new object();
        private readonly IConfiguration root;
        private readonly IHttpContextAccessor context;

        /// <summary>
        /// Initializes a new instance of the <see cref="TenantConfigurationRoot"/> class.
        /// </summary>
        /// <param name="root">Root configuration.</param>
        /// <param name="context">Http context.</param>
        public TenantConfigurationRoot(
            IConfiguration root,
            IHttpContextAccessor context)
        {
            this.root = root;
            this.context = context;
        }

        /// <summary>
        /// Gets process-level configuration without tenant overrides.
        /// </summary>
        public IConfiguration HostConfiguration => this.root;

        /// <inheritdoc/>
        public string this[string key]
        {
            get => this.GetEffectiveConfiguration()[key];
            set => this.root[key] = value;
        }

        public void InitTenantDatabaseConfiguration()
        {
            HttpContext? current = this.context.HttpContext;
            TenantConfigurationSnapshot? snapshot = this.GetTenantSnapshot();
            if (current != null && snapshot != null)
            {
                IDictionary<string, string> keyValuePairs = current.GetExtendedTenantProperties();
                foreach (KeyValuePair<string, string> item in keyValuePairs)
                {
                    snapshot.Configuration.Providers.Last().Set(item.Key, item.Value);
                }
            }
        }

        /// <inheritdoc/>
        public IEnumerable<IConfigurationSection> GetChildren()
        {
            return this.GetEffectiveConfiguration().GetChildren();
        }

        /// <inheritdoc/>
        public IChangeToken GetReloadToken()
        {
            return this.root.GetReloadToken();
        }

        /// <inheritdoc/>
        public IConfigurationSection GetSection(string key)
        {
            return this.GetEffectiveConfiguration().GetSection(key);
        }

        private IConfiguration GetEffectiveConfiguration()
        {
            return this.GetTenantSnapshot()?.Configuration ?? this.root;
        }

        private TenantConfigurationSnapshot? GetTenantSnapshot()
        {
            HttpContext? current = this.context.HttpContext;
            Model.Tenant? tenant = current?.Features.Get<Model.Tenant>();
            if (current == null || tenant == null)
            {
                return null;
            }

            // The snapshot is request-owned: once built from the pinned secrets it is never rebuilt, so a reload
            // cannot drop tenant credentials or database overrides applied by InitTenantDatabaseConfiguration.
            bool pinned = current.TryGetCapturedTenantConfiguration(out IDictionary<string, string>? captured);
            if (current.Items.TryGetValue(SnapshotKey, out object? existing) &&
                existing is TenantConfigurationSnapshot snapshot &&
                snapshot.TenantId == tenant.Id && (snapshot.Pinned || !pinned))
            {
                return snapshot;
            }

            IDictionary<string, string>? secrets = pinned ? captured : current.GetTenantSpecificConfiguration();
            if (secrets == null)
            {
                return null;
            }

            IConfigurationRoot configuration = new ConfigurationBuilder()
                .AddConfiguration(this.root)
                .AddInMemoryCollection(secrets)
                .Build();
            snapshot = new TenantConfigurationSnapshot(tenant.Id, pinned, configuration);
            current.Items[SnapshotKey] = snapshot;
            if (configuration is IDisposable disposableConfiguration)
            {
                current.Response.RegisterForDispose(disposableConfiguration);
            }

            return snapshot;
        }

        private sealed class TenantConfigurationSnapshot
        {
            public TenantConfigurationSnapshot(int tenantId, bool pinned, IConfigurationRoot configuration)
            {
                this.TenantId = tenantId;
                this.Pinned = pinned;
                this.Configuration = configuration;
            }

            public int TenantId { get; }

            public bool Pinned { get; }

            public IConfigurationRoot Configuration { get; }
        }
    }
}
