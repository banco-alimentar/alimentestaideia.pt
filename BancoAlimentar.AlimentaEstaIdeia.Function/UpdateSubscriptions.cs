// -----------------------------------------------------------------------
// <copyright file="UpdateSubscriptions.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Function
{
    using System;
    using System.Data;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.Azure.Functions.Worker;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Storage;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Update subscriptions Azure Function.
    /// </summary>
    public class UpdateSubscriptions : MultiTenantFunction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateSubscriptions"/> class.
        /// </summary>
        public UpdateSubscriptions(TelemetryConfiguration telemetryConfiguration, IServiceProvider serviceProvider)
            : base(telemetryConfiguration, serviceProvider)
        {
            this.ExecuteFunction = new Func<IUnitOfWork, ApplicationDbContext, Task>(this.UpdateSubscriptionsFunction);
        }

        /// <summary>
        /// Execute the function.
        /// </summary>
        /// <param name="timer">Timer.</param>
        /// <param name="log">Logger.</param>
        [Function("UpdateSubscriptions")]
        public async Task Run([TimerTrigger("* * */24 * * *")] TimerInfo timer, ILogger log)
        {
            await this.RunFunctionCore();
        }

        private async Task UpdateSubscriptionsFunction(IUnitOfWork context, ApplicationDbContext applicationDbContext)
        {
            using (IDbContextTransaction transaction = await applicationDbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable))
            {
                try
                {
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    this.TelemetryClient.TrackException(ex);
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
