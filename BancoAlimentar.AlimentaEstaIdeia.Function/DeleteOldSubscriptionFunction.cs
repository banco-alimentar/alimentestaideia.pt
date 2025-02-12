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
            using (IDbContextTransaction transaction = applicationDbContext.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    List<Subscription> expiredSubscriptions = applicationDbContext.Subscriptions
                    .Include(p => p.InitialDonation)
                    .Include(p => p.Donations)
                    .Where(p => p.Status == SubscriptionStatus.Created && EF.Functions.DateDiffDay(p.Created, DateTime.UtcNow) >= 1)
                    .ToList();
                    foreach (var item in expiredSubscriptions)
                    {
                        int otherActiveSubscription = applicationDbContext.Subscriptions
                            .Where(p => p.Status == SubscriptionStatus.Active && p.InitialDonation.Id == item.InitialDonation.Id)
                            .Count();
                        if (otherActiveSubscription == 0)
                        {
                            // delete initial donation as well
                            context.Donation.DeleteDonation(item.InitialDonation.Id);
                        }

                        item.Donations.Clear();
                        applicationDbContext.SaveChanges();

                        applicationDbContext.Entry(item).State = EntityState.Deleted;
                        applicationDbContext.SaveChanges();
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
                    transaction.Rollback();
                }
                finally
                {
                    transaction.Dispose();
                }
            }

            return Task.CompletedTask;
        }
    }
}
