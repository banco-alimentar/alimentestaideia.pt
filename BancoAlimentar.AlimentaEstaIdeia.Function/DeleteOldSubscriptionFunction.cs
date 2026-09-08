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
            int subscriptionDeletionAttempts = 0;
            int donationDeletionAttempts = 0;
            int candidates = 0;
            int? currentSubscriptionId = null;
            bool transactionCommitted = false;
            bool transactionRolledBack = false;
            using (IDbContextTransaction transaction = applicationDbContext.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    List<Subscription> expiredSubscriptions = applicationDbContext.Subscriptions
                    .Include(p => p.InitialDonation)
                    .Include(p => p.Donations)
                    .Where(p => p.Status == SubscriptionStatus.Created && p.Created <= DateTime.UtcNow.AddDays(-1))
                    .ToList();
                    candidates = expiredSubscriptions.Count;
                    report?.SetCounter("candidates", candidates);
                    report?.RecordActivity(
                        "subscription-cleanup-candidates",
                        FunctionExecutionReportActivitySeverity.Information,
                        $"Found {candidates} expired subscription candidate(s) for cleanup.",
                        new Dictionary<string, long> { { "candidates", candidates } });
                    foreach (var item in expiredSubscriptions)
                    {
                        currentSubscriptionId = item.Id;
                        int initialDonationId = item.InitialDonation?.Id ?? 0;
                        int associatedDonationCount = item.Donations?.Count ?? 0;
                        report?.RecordActivity(
                            "subscription-cleanup-candidate",
                            FunctionExecutionReportActivitySeverity.Information,
                            $"Candidate subscription {item.Id}, created {item.Created:O}, has initial donation {initialDonationId} and {associatedDonationCount} associated donation(s).",
                            new Dictionary<string, long>
                            {
                                { "subscriptionId", item.Id },
                                { "initialDonationId", initialDonationId },
                                { "associatedDonationCount", associatedDonationCount },
                            });
                        int otherActiveSubscription = applicationDbContext.Subscriptions
                            .Where(p => p.Status == SubscriptionStatus.Active && p.InitialDonation.Id == item.InitialDonation.Id)
                            .Count();
                        subscriptionDeletionAttempts++;
                        if (otherActiveSubscription == 0)
                        {
                            // delete initial donation as well
                            donationDeletionAttempts++;
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
                            "subscription-cleanup-delete-attempted",
                            FunctionExecutionReportActivitySeverity.Information,
                            $"Deletion was committed to the transaction for subscription {item.Id}; initial donation {initialDonationId} was {(otherActiveSubscription == 0 ? "also deleted" : "retained because an active sibling subscription exists")}.",
                            new Dictionary<string, long>
                            {
                                { "subscriptionId", item.Id },
                                { "initialDonationId", initialDonationId },
                                { "initialDonationDeleted", otherActiveSubscription == 0 ? 1 : 0 },
                            });
                        this.TelemetryClient.TrackTrace(
                            $"Subscription {item.Id} has been deleted.",
                            new Dictionary<string, string>()
                            {
                                { "SubsctionStatus", item.Status.ToString() },
                            });
                    }

                    transaction.Commit();
                    transactionCommitted = true;
                }
                catch (Exception ex)
                {
                    this.TelemetryClient.TrackException(ex);
                    report?.MarkOutcome(FunctionExecutionReportOutcome.Failed);
                    try
                    {
                        transaction.Rollback();
                        transactionRolledBack = true;
                    }
                    catch (Exception rollbackException)
                    {
                        this.TelemetryClient.TrackException(rollbackException);
                    }

                    int recordsAttempted = subscriptionDeletionAttempts + donationDeletionAttempts;
                    report?.SetCounter("subscriptionsDeleted", 0);
                    report?.SetCounter("donationsDeleted", 0);
                    report?.SetCounter("recordsChanged", 0);
                    report?.SetCounter("subscriptionDeletionAttempts", subscriptionDeletionAttempts);
                    report?.SetCounter("donationDeletionAttempts", donationDeletionAttempts);
                    report?.SetCounter("recordsRolledBack", transactionRolledBack ? recordsAttempted : 0);
                    string failedSubscription = currentSubscriptionId.HasValue
                        ? $" while processing candidate subscription {currentSubscriptionId.Value}"
                        : string.Empty;
                    report?.RecordActivity(
                        "subscription-cleanup-rollback",
                        FunctionExecutionReportActivitySeverity.Error,
                        $"Subscription cleanup failed{failedSubscription}. Transaction rollback {(transactionRolledBack ? "succeeded" : "could not be confirmed")}; no database changes from this execution were committed. Attempted subscription deletions: {subscriptionDeletionAttempts}; attempted initial-donation deletions: {donationDeletionAttempts}.",
                        new Dictionary<string, long>
                        {
                            { "candidates", candidates },
                            { "subscriptionDeletionAttempts", subscriptionDeletionAttempts },
                            { "donationDeletionAttempts", donationDeletionAttempts },
                            { "recordsRolledBack", transactionRolledBack ? recordsAttempted : 0 },
                        });
                    report?.RecordError(
                        $"Subscription cleanup failed{failedSubscription}; the transaction was {(transactionRolledBack ? "rolled back and no database records were changed" : "not confirmed as rolled back")}. Exception type: {ex.GetType().Name}.");
                }
                finally
                {
                    transaction.Dispose();
                }
            }

            int reportedSubscriptionsDeleted = transactionCommitted ? subscriptionsDeleted : 0;
            int reportedDonationsDeleted = transactionCommitted ? donationsDeleted : 0;
            FunctionExecutionReportActivitySeverity completionSeverity = transactionCommitted
                ? FunctionExecutionReportActivitySeverity.Information
                : FunctionExecutionReportActivitySeverity.Warning;
            string completionMessage = transactionCommitted
                ? "Expired subscription cleanup completed and the transaction was committed."
                : transactionRolledBack
                    ? "Expired subscription cleanup completed with a rollback; no database records were changed."
                    : "Expired subscription cleanup failed; the transaction outcome could not be confirmed, so no records are reported as changed.";
            report?.SetCounter("retainedInitialDonations", retainedInitialDonations);
            if (!transactionCommitted)
            {
                report?.SetCounter("subscriptionsDeleted", 0);
                report?.SetCounter("donationsDeleted", 0);
                report?.SetCounter("recordsChanged", 0);
            }
            else
            {
                report?.SetCounter("subscriptionsDeleted", subscriptionsDeleted);
                report?.SetCounter("donationsDeleted", donationsDeleted);
                report?.SetCounter("subscriptionDeletionAttempts", subscriptionDeletionAttempts);
                report?.SetCounter("donationDeletionAttempts", donationDeletionAttempts);
                report?.SetCounter("recordsRolledBack", 0);
                report?.SetCounter("recordsChanged", subscriptionsDeleted + donationsDeleted);
            }

            report?.RecordActivity(
                "subscription-cleanup-completed",
                completionSeverity,
                completionMessage,
                new Dictionary<string, long>
                {
                    { "candidates", candidates },
                    { "subscriptionsDeleted", reportedSubscriptionsDeleted },
                    { "donationsDeleted", reportedDonationsDeleted },
                    { "retainedInitialDonations", retainedInitialDonations },
                    { "recordsChanged", reportedSubscriptionsDeleted + reportedDonationsDeleted },
                });

            this.TelemetryClient.TrackEvent(
                "SubscriptionCleanupCompleted",
                new Dictionary<string, string>
                {
                    { "Candidates", candidates.ToString() },
                    { "SubscriptionsDeleted", reportedSubscriptionsDeleted.ToString() },
                    { "DonationsDeleted", reportedDonationsDeleted.ToString() },
                    { "TransactionCommitted", transactionCommitted.ToString() },
                    { "TransactionRolledBack", transactionRolledBack.ToString() },
                });

            return Task.CompletedTask;
        }
    }
}
