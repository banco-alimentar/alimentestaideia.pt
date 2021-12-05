namespace BancoAlimentar.AlimentaEstaIdeia.Function
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration.AzureKeyVault;
    using Azure.Identity;
    using Azure.Security.KeyVault.Secrets;
    using System.Data;


    /// <summary>
    /// Delete old subscriptions.
    /// </summary>
    public class DeleteOldSubscriptionFunction
    {
        private TelemetryClient telemetryClient;

        /// <summary>
        /// Default constructor for <see cref="DeleteOldSubscriptionFunction"/>.
        /// </summary>
        /// <param name="telemetryConfiguration">Telemetry configuration.</param>
        public DeleteOldSubscriptionFunction(TelemetryConfiguration telemetryConfiguration)
        {
            this.telemetryClient = new TelemetryClient(telemetryConfiguration);
        }

        /// <summary>
        /// Execute the function.
        /// </summary>
        /// <param name="timer">Timer.</param>
        /// <param name="log">Logger.</param>
        [FunctionName("DeleteOldSubscriptionFunction")]
        public void Run([TimerTrigger("* * */24 * * *", RunOnStartup = false)] TimerInfo timer, ILogger log)
        {
            var config = FunctionInitializer.GetUnitOfWork(telemetryClient);
            IUnitOfWork context = config.UnitOfWork;
            ApplicationDbContext applicationDbContext = config.ApplicationDbContext;

            using (var transaction = applicationDbContext.Database.BeginTransaction(IsolationLevel.Serializable))
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
                        WebUserSubscriptions userSubscription = applicationDbContext.UsersSubscriptions
                            .Where(p => p.Subscription.Id == item.Id)
                            .FirstOrDefault();
                        if (userSubscription != null)
                        {
                            applicationDbContext.Entry(userSubscription).State = EntityState.Deleted;
                            applicationDbContext.SaveChanges();
                        }

                        applicationDbContext.Entry(item).State = EntityState.Deleted;
                        applicationDbContext.SaveChanges();
                        this.telemetryClient.TrackTrace(
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
