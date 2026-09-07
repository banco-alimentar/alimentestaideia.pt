// -----------------------------------------------------------------------
// <copyright file="DeleteOldSubscriptionFunction.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Function
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.FunctionExecutionReports;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.Azure.Functions.Worker;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Storage;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Delete old subscriptions.
    /// </summary>
    public class DeleteOldSubscriptionFunction : MultiTenantFunction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteOldSubscriptionFunction"/> class.
        /// </summary>
        public DeleteOldSubscriptionFunction(TelemetryConfiguration telemetryConfiguration, IServiceProvider serviceProvider)
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
        [Function("DeleteOldSubscriptionFunction")]
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
                        { "FunctionName", "DeleteOldSubscriptionFunction" },
                    });
            }
        }

        private Task UpdateSubscriptionsFunction(IUnitOfWork context, ApplicationDbContext applicationDbContext)
        {
            return this.UpdateSubscriptionsFunctionWithReport(context, applicationDbContext, null);
        }

        private Task UpdateSubscriptionsFunctionWithReport(
            IUnitOfWork context,
            ApplicationDbContext applicationDbContext,
            IFunctionExecutionReportExecution report)
        {
            int subscriptionsDeleted = 0;
            int donationsDeleted = 0;
            int retainedInitialDonations = 0;
            using (IDbContextTransaction transaction = applicationDbContext.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    List<Subscription> expiredSubscriptions = applicationDbContext.Subscriptions
                    .Include(p => p.InitialDonation)
                    .Include(p => p.Donations)
                    .Where(p => p.Status == SubscriptionStatus.Created && p.Created <= DateTime.UtcNow.AddDays(-1))
                    .ToList();
                    int candidates = expiredSubscriptions.Count;
                    report?.SetCounter("candidates", candidates);
                    foreach (var item in expiredSubscriptions)
                    {
                        int otherActiveSubscription = applicationDbContext.Subscriptions
                            .Where(p => p.Status == SubscriptionStatus.Active && p.InitialDonation.Id == item.InitialDonation.Id)
                            .Count();
                        if (otherActiveSubscription == 0)
                        {
                            // delete initial donation as well
                            context.Donation.DeleteDonation(item.InitialDonation.Id);
                            donationsDeleted++;
                        }
                        else
                        {
                            retainedInitialDonations++;
                        }

                        item.Donations?.Clear();
                        applicationDbContext.SaveChanges();

                        applicationDbContext.Entry(item).State = EntityState.Deleted;
                        applicationDbContext.SaveChanges();
                        subscriptionsDeleted++;
                        report?.RecordActivity(
                            "subscription-deleted",
                            FunctionExecutionReportActivitySeverity.Information,
                            "An expired subscription and its eligible donation records were deleted.");
                        this.TelemetryClient.TrackTrace(
                            $"Subscription {item.Id} has been deleted.",
                            new Dictionary<string, string>()
                            {
                                { "SubsctionStatus", item.Status.ToString() },
                            });
                    }

                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    this.TelemetryClient.TrackException(ex);
                    report?.MarkOutcome(FunctionExecutionReportOutcome.Failed);
                    report?.RecordError("Subscription cleanup failed and the transaction was rolled back.");
                    transaction.Rollback();
                }
                finally
                {
                    transaction.Dispose();
                }
            }

            report?.SetCounter("subscriptionsDeleted", subscriptionsDeleted);
            report?.SetCounter("donationsDeleted", donationsDeleted);
            report?.SetCounter("retainedInitialDonations", retainedInitialDonations);
            report?.SetCounter("recordsChanged", subscriptionsDeleted + donationsDeleted);
            report?.RecordActivity(
                "subscription-cleanup-completed",
                FunctionExecutionReportActivitySeverity.Information,
                "Expired subscription cleanup completed.",
                new Dictionary<string, long>
                {
                    { "subscriptionsDeleted", subscriptionsDeleted },
                    { "donationsDeleted", donationsDeleted },
                    { "retainedInitialDonations", retainedInitialDonations },
                });

            return Task.CompletedTask;
        }
    }
}
