﻿// -----------------------------------------------------------------------
// <copyright file="TenantRepository.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Repository
{
    using System.Linq;
    using BancoAlimentar.AlimentaEstaIdeia.Common.Repository.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using Microsoft.ApplicationInsights;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Caching.Memory;

    /// <summary>
    /// Default implementation for the <see cref="Tenant"/> repository pattern.
    /// </summary>
    public class TenantRepository : GenericRepository<Tenant, InfrastructureDbContext>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TenantRepository"/> class.
        /// </summary>
        /// <param name="context"><see cref="InfrastructureDbContext"/> instance.</param>
        /// <param name="memoryCache">A reference to the Memory cache system.</param>
        /// <param name="telemetryClient">Telemetry Client.</param>
        public TenantRepository(
            InfrastructureDbContext context,
            IMemoryCache memoryCache,
            TelemetryClient telemetryClient)
            : base(context, memoryCache, telemetryClient)
        {
        }

        /// <summary>
        /// Finds the tenant information by domain identifier.
        /// </summary>
        /// <param name="value">Domain identifier.</param>
        /// <returns>A reference to the <see cref="Tenant"/>.</returns>
        public Tenant FindTenantByDomainIdentifier(string value)
        {
            return this.DbContext.Tenants
                .Include(p => p.KeyVaultConfigurations)
                .Where(p => p.DomainIdentifier == value)
                .FirstOrDefault();
        }
    }
}