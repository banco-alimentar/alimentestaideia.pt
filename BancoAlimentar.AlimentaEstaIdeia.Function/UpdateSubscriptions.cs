// -----------------------------------------------------------------------
// <copyright file="UpdateSubscriptions.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Function
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Host;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Storage;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Update subscriptions Azure Function.
    /// </summary>
    public class UpdateSubscriptions
    {
        private TelemetryClient telemetryClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateSubscriptions"/> class.
        /// </summary>
        /// <param name="telemetryConfiguration">Telemetry configuration.</param>
        public UpdateSubscriptions(TelemetryConfiguration telemetryConfiguration)
        {
            this.telemetryClient = new TelemetryClient(telemetryConfiguration);
        }

        /// <summary>
        /// Execute the function.
        /// </summary>
        /// <param name="timer">Timer.</param>
        /// <param name="log">Logger.</param>
        [FunctionName("UpdateSubscriptions")]
        public async Task Run([TimerTrigger("* * */24 * * *")] TimerInfo timer, ILogger log)
        {
            var config = FunctionInitializer.GetUnitOfWork(this.telemetryClient);
            IUnitOfWork context = config.UnitOfWork;
            ApplicationDbContext applicationDbContext = config.ApplicationDbContext;

            using (IDbContextTransaction transaction = await applicationDbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable))
            {
                try
                {
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    this.telemetryClient.TrackException(ex);
                    transaction.Rollback();
                }
                finally
                {
                    transaction.Dispose();
                }
            }
        }
    }
}
