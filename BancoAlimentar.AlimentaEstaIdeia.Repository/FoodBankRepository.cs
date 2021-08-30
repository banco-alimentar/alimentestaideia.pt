// -----------------------------------------------------------------------
// <copyright file="FoodBankRepository.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using Microsoft.ApplicationInsights;
    using Microsoft.Extensions.Caching.Memory;

    /// <summary>
    /// Default implementation for the <see cref="FoodBank"/> repository pattern.
    /// </summary>
    public class FoodBankRepository : GenericRepository<FoodBank>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FoodBankRepository"/> class.
        /// </summary>
        /// <param name="context"><see cref="ApplicationDbContext"/> instance.</param>
        /// <param name="memoryCache">A reference to the Memory cache system.</param>
        /// <param name="telemetryClient">Telemetry Client.</param>
        public FoodBankRepository(ApplicationDbContext context, IMemoryCache memoryCache, TelemetryClient telemetryClient)
            : base(context, memoryCache, telemetryClient)
        {
        }
    }
}
