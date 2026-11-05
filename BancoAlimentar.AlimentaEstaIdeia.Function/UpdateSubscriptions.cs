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
    using BancoAlimentar.AlimentaEstaIdeia.Repository.FunctionExecutionReports;
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
            this.ExecuteFunctionWithReport = this.UpdateSubscriptionsFunctionWithReport;
        }

        /// <summary>
        /// Execute the function.
        /// </summary>
        /// <param name="timer">Timer.</param>
        /// <param name="log">Logger.</param>
        [Function("UpdateSubscriptions")]
        public async Task Run([TimerTrigger("* * */24 * * *", RunOnStartup = true)] TimerInfo timer, ILogger log)
        {
            try
            {
                await this.RunFunctionCore();
            }
            catch (Exception ex)
            {
                this.TelemetryClient.TrackException(
                    ex,
                    new Dictionary<string, string>()
                    {
                        { "FunctionName", "UpdateSubscriptions" },
                    });
            }
        }

        private async Task UpdateSubscriptionsFunction(IUnitOfWork context, ApplicationDbContext applicationDbContext)
        {
            await this.UpdateSubscriptionsFunctionWithReport(context, applicationDbContext, null).ConfigureAwait(false);
        }

        private async Task UpdateSubscriptionsFunctionWithReport(
            IUnitOfWork context,
            ApplicationDbContext applicationDbContext,
            IFunctionExecutionReportExecution report)
        {
            report?.RecordActivity(
                "subscription-synchronization",
                FunctionExecutionReportActivitySeverity.Information,
                "No subscription synchronization was performed; this function is currently a placeholder.");
            report?.SetCounter("recordsChanged", 0);
            using (IDbContextTransaction transaction = await applicationDbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable))
            {
                try
                {
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    this.TelemetryClient.TrackException(ex);
                    report?.MarkOutcome(FunctionExecutionReportOutcome.Failed);
                    report?.RecordError("Subscription synchronization transaction failed.");
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
