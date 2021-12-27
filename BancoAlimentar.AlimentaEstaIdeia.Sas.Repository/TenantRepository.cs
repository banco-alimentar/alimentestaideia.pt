// -----------------------------------------------------------------------
// <copyright file="TenantRepository.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Repository
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Common.Repository.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using Microsoft.ApplicationInsights;
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
    }
}
