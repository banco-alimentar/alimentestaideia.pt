// -----------------------------------------------------------------------
// <copyright file="ReconcileWaitingSubscriptionDonationsTool.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.EasyPay
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using BancoAlimentar.AlimentaEstaIdeia.Common;
    using BancoAlimentar.AlimentaEstaIdeia.Common.EasyPay;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using Easypay.Rest.Client.Api;
    using Easypay.Rest.Client.Client;
    using Easypay.Rest.Client.Model;
    using Microsoft.Data.SqlClient;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using EasyPayTransaction = Easypay.Rest.Client.Model.SubscriptionIdGet200ResponseTransactionsInner;
    using Subscription = BancoAlimentar.AlimentaEstaIdeia.Model.Subscription;

    /// <summary>
    /// Verifies all local subscription donations against Easypay and optionally repairs local records.
    /// </summary>
    internal sealed class ReconcileWaitingSubscriptionDonationsTool : EasyPayTool
    {
        private const int StaleZeroValueDonationAgeDays = 7;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReconcileWaitingSubscriptionDonationsTool"/> class.
        /// </summary>
        /// <param name="context">Application database context.</param>
        /// <param name="unitOfWork">Application unit of work.</param>
        /// <param name="configuration">Application configuration.</param>
        /// <param name="dryRun">Whether to report changes without changing the database.</param>
        /// <param name="subscriptionId">Optional local database subscription ID to process.</param>
        /// <param name="easypaySubscriptionId">Optional Easypay subscription ID to process.</param>
        public ReconcileWaitingSubscriptionDonationsTool(
            ApplicationDbContext context,
            IUnitOfWork unitOfWork,
            IConfiguration configuration,
            bool dryRun,
            int? subscriptionId,
            string easypaySubscriptionId)
            : base(context, unitOfWork, configuration)
        {
            this.DryRun = dryRun;
            this.SubscriptionId = subscriptionId;
            this.EasypaySubscriptionId = easypaySubscriptionId;
            this.configuration = configuration;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the tool only reports changes.
        /// </summary>
        public bool DryRun { get; set; }

        /// <summary>
        /// Gets or sets the optional local database subscription ID to process.
        /// </summary>
        public int? SubscriptionId { get; set; }

        /// <summary>
        /// Gets or sets the optional Easypay subscription ID to process.
        /// </summary>
        public string EasypaySubscriptionId { get; set; }

        private IConfiguration configuration { get; }

        /// <inheritdoc />
        public override void ExecuteTool()
        {
            this.PrintExecutionContext();

            SubscriptionPaymentApi subscriptionApi = this.GetSubscriptionPaymentApi();
            if (!this.CheckEasypayAuthentication(subscriptionApi))
            {
                Environment.ExitCode = 1;
                return;
            }

            List<Subscription> subscriptions = this.GetLocalSubscriptions();
            Console.WriteLine($"Local subscriptions found: {subscriptions.Count}");
            Console.WriteLine(
                this.DryRun
                    ? "Mode: dry-run (read-only; no local database changes)"
                    : "Mode: apply (local database changes are enabled)");
            Console.WriteLine("Easypay operations: GET subscription only; embedded transaction data is used; no POST, PATCH, or DELETE operations.");
            Console.WriteLine();

            ReconciliationSummary summary = new ReconciliationSummary();
            foreach (Subscription subscription in subscriptions)
            {
                this.ProcessSubscription(subscription, subscriptionApi, summary);
                Console.WriteLine();
            }

            this.PrintSummary(summary);
        }

        private void PrintExecutionContext()
        {
            Console.WriteLine($"Database: {DescribeDatabase(this.configuration.GetConnectionString("DefaultConnection"))}");
            Console.WriteLine($"Easypay endpoint: {DisplayValue(this.configuration["Easypay:BaseUrl"])} /2.0");
            Console.WriteLine($"Easypay account id: {DisplayValue(this.configuration["Easypay:AccountId"])}");
            Console.WriteLine(
                this.SubscriptionId.HasValue
                    ? $"Subscription filter: database subscription id={this.SubscriptionId.Value}"
                    : "Subscription filter: all local subscriptions");
            if (!string.IsNullOrWhiteSpace(this.EasypaySubscriptionId))
            {
                Console.WriteLine($"Subscription filter: Easypay subscription id={this.EasypaySubscriptionId}");
            }
        }

        private List<Subscription> GetLocalSubscriptions()
        {
            IQueryable<Subscription> query = this.Context.Subscriptions
                .Include(subscription => subscription.InitialDonation)
                    .ThenInclude(donation => donation.PaymentList)
                .Include(subscription => subscription.InitialDonation)
                    .ThenInclude(donation => donation.ConfirmedPayment)
                .Include(subscription => subscription.Donations)
                    .ThenInclude(link => link.Donation)
                        .ThenInclude(donation => donation.PaymentList)
                .Include(subscription => subscription.Donations)
                    .ThenInclude(link => link.Donation)
                        .ThenInclude(donation => donation.ConfirmedPayment)
                .OrderByDescending(subscription => subscription.Created)
                .ThenByDescending(subscription => subscription.Id);

            if (this.SubscriptionId.HasValue)
            {
                query = query.Where(subscription => subscription.Id == this.SubscriptionId.Value);
            }

            if (!string.IsNullOrWhiteSpace(this.EasypaySubscriptionId))
            {
                query = query.Where(subscription => subscription.EasyPaySubscriptionId == this.EasypaySubscriptionId);
            }

            return query.ToList();
        }

        private void ProcessSubscription(
            Subscription subscription,
            SubscriptionPaymentApi subscriptionApi,
            ReconciliationSummary summary)
        {
            summary.SubscriptionsProcessed++;
            List<LocalDonation> allLocalDonations = this.GetLocalDonations(subscription);
            List<LocalDonation> localDonations = allLocalDonations.ToList();
            summary.LocalDonationsChecked += localDonations.Count;
            DateTime staleDonationCutoff = DateTime.UtcNow.AddDays(-StaleZeroValueDonationAgeDays);
            Console.WriteLine(
                $"Subscription: local database id={subscription.Id}, "
                + $"Easypay subscription id={DisplayValue(subscription.EasyPaySubscriptionId)}, "
                + $"value={GetSubscriptionValue(subscription):F2}, "
                + $"transaction key={DisplayValue(subscription.TransactionKey)}, "
                + $"associated donations={localDonations.Count}");
            Console.WriteLine(
                $"  Local cleanup rule: recurring donations with PaymentStatus=WaitingPayment, "
                + "Requested=0.00, Paid=0.00, and DonationDate older than "
                + $"{StaleZeroValueDonationAgeDays} days (before {staleDonationCutoff:u}) are eligible for removal.");
            this.RemoveStaleZeroValueWaitingDonations(localDonations, staleDonationCutoff, summary);

            if (!Guid.TryParse(subscription.EasyPaySubscriptionId, out Guid easypaySubscriptionId))
            {
                summary.SubscriptionMismatches++;
                Console.WriteLine(
                    $"  Subscription lookup skipped: invalid Easypay subscription id "
                    + $"'{DisplayValue(subscription.EasyPaySubscriptionId)}'.");
                this.PrintUnmatchedLocalDonations(localDonations, "provider subscription lookup was not possible", summary);
                return;
            }

            string subscriptionEndpoint = this.GetEasypayEndpoint($"/subscription/{easypaySubscriptionId}");
            Console.WriteLine(
                $"  Easypay subscription lookup: GET {subscriptionEndpoint}; "
                + $"stored id={subscription.EasyPaySubscriptionId}, parsed id={easypaySubscriptionId}");

            SubscriptionIdGet200Response providerSubscription;
            try
            {
                providerSubscription = subscriptionApi.SubscriptionIdGet(easypaySubscriptionId);
            }
            catch (ApiException exception)
            {
                if (IsEasypaySubscriptionNotFound(exception))
                {
                    summary.EasypaySubscriptionsNotFound++;
                    this.ReportEasypaySubscriptionNotFound(subscription, allLocalDonations, summary);
                }
                else
                {
                    summary.SubscriptionLookupFailures++;
                }

                Console.WriteLine(
                    $"  Subscription lookup failed for Easypay id '{subscription.EasyPaySubscriptionId}': "
                    + $"HTTP {exception.ErrorCode}; {FormatApiException(exception)}");
                if (!IsEasypaySubscriptionNotFound(exception))
                {
                    this.PrintUnmatchedLocalDonations(localDonations, "provider subscription lookup failed", summary);
                }

                return;
            }
            catch (Exception exception)
            {
                summary.SubscriptionLookupFailures++;
                Console.WriteLine(
                    $"  Subscription lookup failed for Easypay id '{subscription.EasyPaySubscriptionId}': "
                    + $"{exception.GetType().Name}: {exception.Message}");
                this.PrintUnmatchedLocalDonations(localDonations, "provider subscription lookup failed", summary);
                return;
            }

            if (providerSubscription == null)
            {
                summary.SubscriptionLookupFailures++;
                Console.WriteLine("  Subscription lookup returned an empty response.");
                this.PrintUnmatchedLocalDonations(localDonations, "provider subscription response was empty", summary);
                return;
            }

            this.ReportSubscriptionStatus(subscription, allLocalDonations, providerSubscription, summary);

            bool subscriptionMatches = this.PrintSubscriptionComparison(subscription, providerSubscription, summary);
            if (!subscriptionMatches)
            {
                this.PrintUnmatchedLocalDonations(localDonations, "local subscription data does not match Easypay", summary);
                return;
            }

            List<ProviderPayment> providerPayments = this.GetProviderPayments(providerSubscription, summary);
            Console.WriteLine($"  Easypay subscription transactions available: {providerPayments.Count}");

            this.MatchAndReconcileDonations(subscription, localDonations, providerPayments, summary);
        }

        private bool PrintSubscriptionComparison(
            Subscription localSubscription,
            SubscriptionIdGet200Response providerSubscription,
            ReconciliationSummary summary)
        {
            string providerId = providerSubscription.Id.ToString();
            double localValue = GetSubscriptionValue(localSubscription);
            bool idMatches = string.Equals(
                providerId,
                localSubscription.EasyPaySubscriptionId,
                StringComparison.OrdinalIgnoreCase);
            bool keyMatches = string.Equals(
                providerSubscription.Key,
                localSubscription.TransactionKey,
                StringComparison.Ordinal);
            bool valueMatches = PaymentAmountReconciliation.ProviderValueMatchesDonation(
                localValue,
                (double)providerSubscription.Value);

            Console.WriteLine(
                $"  Easypay subscription response: id={providerId}, value={providerSubscription.Value:F2}, "
                + $"key={DisplayValue(providerSubscription.Key)}, created={DisplayValue(providerSubscription.CreatedAt)}");
            Console.WriteLine(
                $"  Subscription mapping: id={(idMatches ? "OK" : "MISMATCH")}, "
                + $"transaction key={(keyMatches ? "OK" : "MISMATCH")}, "
                + $"value={(valueMatches ? "OK" : "MISMATCH")}");

            if (!idMatches || !keyMatches || !valueMatches)
            {
                summary.SubscriptionMismatches++;
                return false;
            }

            return true;
        }

        private List<ProviderPayment> GetProviderPayments(
            SubscriptionIdGet200Response providerSubscription,
            ReconciliationSummary summary)
        {
            string providerKey = providerSubscription.Key;
            if (string.IsNullOrWhiteSpace(providerKey))
            {
                summary.TransactionLookupFailures++;
                Console.WriteLine("  Easypay transaction mapping skipped: provider subscription key is empty.");
                return new List<ProviderPayment>();
            }

            IReadOnlyCollection<EasyPayTransaction> transactions = providerSubscription.Transactions == null
                ? new List<EasyPayTransaction>()
                : providerSubscription.Transactions;
            summary.ProviderTransactionRecordsFound += transactions.Count;

            List<ProviderPayment> providerPayments = new List<ProviderPayment>();
            foreach (EasyPayTransaction transaction in transactions
                .Where(transaction => transaction != null)
                .GroupBy(transaction => transaction.Id, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .OrderBy(transaction => transaction.Date ?? transaction.CreatedAt))
            {
                summary.ProviderTransactionsChecked++;
                if (string.IsNullOrWhiteSpace(transaction.Id))
                {
                    summary.TransactionLookupFailures++;
                    Console.WriteLine("  Easypay transaction skipped: transaction id is empty.");
                    continue;
                }

                if (!Guid.TryParse(transaction.Id, out _))
                {
                    summary.TransactionLookupFailures++;
                    Console.WriteLine($"  Easypay transaction skipped: invalid transaction id '{transaction.Id}'.");
                    continue;
                }

                if (!string.Equals(transaction.Key, providerKey, StringComparison.Ordinal))
                {
                    summary.TransactionLookupFailures++;
                    Console.WriteLine(
                        $"  Easypay transaction skipped: id='{transaction.Id}' has key "
                        + $"'{DisplayValue(transaction.Key)}', expected subscription key '{providerKey}'.");
                    continue;
                }

                if (transaction.Values == null)
                {
                    summary.TransactionLookupFailures++;
                    Console.WriteLine($"  Easypay transaction skipped: no values returned for id '{transaction.Id}'.");
                    continue;
                }

                if (!TryGetProviderTransactionDate(transaction, out DateTime paymentDate))
                {
                    summary.TransactionLookupFailures++;
                    Console.WriteLine(
                        $"  Easypay transaction skipped: no usable date for id '{transaction.Id}'.");
                    continue;
                }

                double requested = (double)transaction.Values.Requested;
                double paid = (double)transaction.Values.Paid;
                bool isPaid = paid > 0
                    && PaymentAmountReconciliation.ProviderValueMatchesDonation(requested, paid);
                Console.WriteLine(
                    $"    Easypay subscription transaction: id={transaction.Id}, key={providerKey}, "
                    + $"requested={requested:F2}, paid={paid:F2}, status={(isPaid ? "Paid" : "NotPaid")}, "
                    + $"date={paymentDate:yyyy-MM-dd}");
                providerPayments.Add(new ProviderPayment
                {
                    Id = transaction.Id,
                    Key = transaction.Key,
                    Requested = requested,
                    Paid = paid,
                    FixedFee = (double)transaction.Values.FixedFee,
                    VariableFee = (double)transaction.Values.VariableFee,
                    Tax = (double)transaction.Values.Tax,
                    Transfer = (double)transaction.Values.Transfer,
                    PaymentDate = paymentDate,
                    IsPaid = isPaid,
                    Status = isPaid ? "Success" : "WaitingPayment",
                });
            }

            return providerPayments;
        }

        private void MatchAndReconcileDonations(
            Subscription subscription,
            List<LocalDonation> localDonations,
            List<ProviderPayment> providerPayments,
            ReconciliationSummary summary)
        {
            HashSet<int> matchedDonationIds = new HashSet<int>();
            HashSet<string> matchedProviderPaymentIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (ProviderPayment providerPayment in providerPayments.OrderBy(payment => payment.PaymentDate))
            {
                List<LocalDonation> idMatches = localDonations
                    .Where(localDonation => localDonation.Donation.PaymentList != null
                        && localDonation.Donation.PaymentList
                            .OfType<CreditCardPayment>()
                            .Any(payment => string.Equals(
                                payment.EasyPayPaymentId,
                                providerPayment.Id,
                                StringComparison.OrdinalIgnoreCase)))
                    .Where(localDonation => !matchedDonationIds.Contains(localDonation.Donation.Id))
                    .ToList();
                List<LocalDonation> dateAndAmountMatches = localDonations
                    .Where(localDonation => !matchedDonationIds.Contains(localDonation.Donation.Id))
                    .Where(localDonation => localDonation.Donation.DonationDate.Date == providerPayment.PaymentDate.Date)
                    .Where(localDonation => ProviderPaymentMatchesDonation(localDonation.Donation, providerPayment))
                    .ToList();

                if (idMatches.Count == 1
                    && (idMatches[0].Donation.DonationDate.Date != providerPayment.PaymentDate.Date
                        || !ProviderPaymentMatchesDonation(idMatches[0].Donation, providerPayment)))
                {
                    summary.LocalMappingConflicts++;
                    Console.WriteLine(
                        $"  Provider payment {providerPayment.Id}: its existing local payment-id match "
                        + "has a different date or value; no change proposed.");
                    continue;
                }

                List<LocalDonation> matches = idMatches.Count == 1
                    ? idMatches
                    : dateAndAmountMatches;
                if (matches.Count == 2)
                {
                    Console.WriteLine("  Local candidates for the ambiguous provider payment:");
                    foreach (LocalDonation candidate in matches)
                    {
                        Console.WriteLine($"    {DescribeLocalDonation(candidate)}");
                    }

                    List<LocalDonation> zeroValueConfirmedPaymentMatches = matches
                        .Where(IsWaitingDonationWithZeroValueConfirmedPayment)
                        .ToList();
                    if (zeroValueConfirmedPaymentMatches.Count > 0)
                    {
                        LocalDonation donationToRemove = zeroValueConfirmedPaymentMatches.Count == 1
                            ? zeroValueConfirmedPaymentMatches[0]
                            : zeroValueConfirmedPaymentMatches
                                .OrderByDescending(candidate => candidate.Donation.Id)
                                .First();
                        if (this.RemoveInvalidSubscriptionDonation(
                            donationToRemove,
                            providerPayment,
                            summary))
                        {
                            localDonations.Remove(donationToRemove);
                            idMatches.Remove(donationToRemove);
                            dateAndAmountMatches.Remove(donationToRemove);
                            matches = idMatches.Count == 1
                                ? idMatches
                                : dateAndAmountMatches;
                            summary.ZeroValueConfirmedPaymentAmbiguitiesResolved++;
                        }
                    }
                }

                if (matches.Count != 1)
                {
                    summary.UnmatchedProviderPayments++;
                    Console.WriteLine(
                        matches.Count == 0
                            ? $"  Provider payment {providerPayment.Id}: no local donation matched by payment id, date, and value."
                            : $"  Provider payment {providerPayment.Id}: ambiguous local match ({matches.Count} donations share its date and value); no change proposed.");
                    continue;
                }

                LocalDonation localDonation = matches[0];
                matchedDonationIds.Add(localDonation.Donation.Id);
                matchedProviderPaymentIds.Add(providerPayment.Id);
                summary.MatchedDonations++;
                this.ReconcileDonation(subscription, localDonation, providerPayment, summary);
            }

            foreach (LocalDonation localDonation in localDonations
                .Where(localDonation => !matchedDonationIds.Contains(localDonation.Donation.Id))
                .OrderBy(localDonation => localDonation.Donation.DonationDate))
            {
                summary.UnmatchedLocalDonations++;
                Console.WriteLine(
                    $"  Local donation {localDonation.Donation.Id} ({localDonation.Donation.DonationDate:yyyy-MM-dd}, "
                    + $"{localDonation.Donation.DonationAmount:F2}): no Easypay payment matched; no change proposed.");
            }

            foreach (ProviderPayment providerPayment in providerPayments
                .Where(providerPayment => !matchedProviderPaymentIds.Contains(providerPayment.Id)))
            {
                Console.WriteLine(
                    $"  Easypay payment {providerPayment.Id}: not associated with a local donation for this subscription.");
            }
        }

        private void ReconcileDonation(
            Subscription subscription,
            LocalDonation localDonation,
            ProviderPayment providerPayment,
            ReconciliationSummary summary)
        {
            Donation donation = localDonation.Donation;
            List<CreditCardPayment> localPayments = (donation.PaymentList ?? Array.Empty<BasePayment>())
                .OfType<CreditCardPayment>()
                .ToList();
            CreditCardPayment payment = localPayments
                .FirstOrDefault(item => string.Equals(
                    item.EasyPayPaymentId,
                    providerPayment.Id,
                    StringComparison.OrdinalIgnoreCase));
            if (payment == null && localPayments.Count == 1)
            {
                payment = localPayments[0];
            }

            if (payment == null && localPayments.Count > 1)
            {
                summary.LocalMappingConflicts++;
                Console.WriteLine(
                    $"  Donation {donation.Id}: multiple local credit-card payments exist and none has "
                    + $"Easypay payment id {providerPayment.Id}; no change proposed.");
                return;
            }

            bool needsPaymentCreation = payment == null;
            string currentPaymentId = payment?.EasyPayPaymentId;
            bool paymentIdNeedsUpdate = !string.Equals(
                currentPaymentId,
                providerPayment.Id,
                StringComparison.OrdinalIgnoreCase);
            bool transactionKeyNeedsUpdate = !string.Equals(
                payment?.TransactionKey,
                subscription.TransactionKey,
                StringComparison.Ordinal);
            bool statusNeedsUpdate = payment == null
                || !string.Equals(payment.Status, providerPayment.Status, StringComparison.OrdinalIgnoreCase)
                || (providerPayment.IsPaid && (!payment.Completed.HasValue || donation.PaymentStatus != PaymentStatus.Payed));
            bool amountsNeedUpdate = payment == null
                || !PaymentAmountReconciliation.ProviderValueMatchesDonation(
                    payment.Requested,
                    providerPayment.Requested)
                || !PaymentAmountReconciliation.ProviderValueMatchesDonation(
                    payment.Paid,
                    providerPayment.Paid)
                || !PaymentAmountReconciliation.ProviderValueMatchesDonation(
                    payment.FixedFee,
                    providerPayment.FixedFee)
                || !PaymentAmountReconciliation.ProviderValueMatchesDonation(
                    payment.VariableFee,
                    providerPayment.VariableFee)
                || !PaymentAmountReconciliation.ProviderValueMatchesDonation(
                    payment.Tax,
                    providerPayment.Tax)
                || !PaymentAmountReconciliation.ProviderValueMatchesDonation(
                    payment.Transfer,
                    providerPayment.Transfer);
            bool donationNeedsCompletion = providerPayment.IsPaid && donation.PaymentStatus != PaymentStatus.Payed;

            Console.WriteLine(
                $"  Donation mapping: local donation id={donation.Id}, provider payment id={providerPayment.Id}, "
                + $"date={providerPayment.PaymentDate:yyyy-MM-dd}, requested={providerPayment.Requested:F2}, "
                + $"paid={providerPayment.Paid:F2}, "
                + $"local payment id={payment?.Id.ToString() ?? "<missing>"}, "
                + $"local payment EasyPay id={DisplayValue(currentPaymentId)}");

            if (!needsPaymentCreation && paymentIdNeedsUpdate && this.IsProviderPaymentIdUsedByAnotherPayment(payment, providerPayment.Id))
            {
                summary.LocalMappingConflicts++;
                Console.WriteLine(
                    $"    Easypay payment id {providerPayment.Id} is already stored on another local payment; no change proposed.");
                return;
            }

            if (!needsPaymentCreation && !paymentIdNeedsUpdate && !transactionKeyNeedsUpdate && !statusNeedsUpdate && !amountsNeedUpdate && !donationNeedsCompletion)
            {
                summary.AlreadyCorrect++;
                Console.WriteLine("    Local mapping is already correct; no change needed.");
                return;
            }

            summary.ChangesProposed++;
            Console.WriteLine(
                $"    {(this.DryRun ? "Would update" : "Updating")} local donation {donation.Id}: "
                + $"EasyPay payment id {DisplayValue(currentPaymentId)} -> {providerPayment.Id}; "
                + $"provider status={providerPayment.Status}; requested={providerPayment.Requested:F2}; "
                + $"paid={providerPayment.Paid:F2}; "
                + $"provider date={providerPayment.PaymentDate:yyyy-MM-dd}; "
                + $"create local payment={needsPaymentCreation}; mark paid={donationNeedsCompletion}.");

            if (this.DryRun)
            {
                summary.ChangesWouldApply++;
                return;
            }

            if (payment == null)
            {
                payment = new CreditCardPayment
                {
                    Donation = donation,
                    Created = providerPayment.PaymentDate,
                };
                this.Context.CreditCardPayments.Add(payment);
                donation.PaymentList ??= new List<BasePayment>();
                donation.PaymentList.Add(payment);
            }

            payment.EasyPayPaymentId = providerPayment.Id;
            payment.TransactionKey = subscription.TransactionKey;
            payment.Requested = (float)providerPayment.Requested;
            payment.Paid = (float)providerPayment.Paid;
            payment.FixedFee = (float)providerPayment.FixedFee;
            payment.VariableFee = (float)providerPayment.VariableFee;
            payment.Tax = (float)providerPayment.Tax;
            payment.Transfer = (float)providerPayment.Transfer;
            payment.Status = providerPayment.Status;
            if (providerPayment.IsPaid)
            {
                payment.Completed = providerPayment.PaymentDate;
                if (!this.UnitOfWork.Donation.TryCompleteDonationPayment(
                    donation,
                    payment,
                    payment.Requested,
                    payment.Paid,
                    subscription.TransactionKey,
                    trustProviderPaidStatus: true))
                {
                    summary.LocalMappingConflicts++;
                    Console.WriteLine(
                        "    Local payment was not completed because the donation completion validation failed.");
                    this.Context.ChangeTracker.Clear();
                    return;
                }

                donation.ConfirmedPayment = payment;
                DonationPaymentCompletion.MarkSuccessfulEasyPayPayment(payment);
                payment.Completed = providerPayment.PaymentDate;
            }

            try
            {
                this.Context.SaveChanges();
                summary.ChangesApplied++;
            }
            catch (DbUpdateException exception)
            {
                summary.LocalMappingConflicts++;
                this.Context.ChangeTracker.Clear();
                Console.WriteLine($"    Local database update failed; no change was retained: {exception.Message}");
            }
        }

        private bool IsProviderPaymentIdUsedByAnotherPayment(CreditCardPayment payment, string providerPaymentId)
        {
            return this.Context.CreditCardPayments.Any(item =>
                item.Id != payment.Id
                && item.EasyPayPaymentId == providerPaymentId);
        }

        private List<LocalDonation> GetLocalDonations(Subscription subscription)
        {
            Dictionary<int, LocalDonation> donations = new Dictionary<int, LocalDonation>();
            if (subscription.InitialDonation != null)
            {
                donations[subscription.InitialDonation.Id] = new LocalDonation
                {
                    Donation = subscription.InitialDonation,
                    IsInitialDonation = true,
                };
            }

            foreach (SubscriptionDonations link in subscription.Donations ?? Array.Empty<SubscriptionDonations>())
            {
                if (link.Donation != null && !donations.ContainsKey(link.Donation.Id))
                {
                    donations.Add(link.Donation.Id, new LocalDonation
                    {
                        Donation = link.Donation,
                    });
                }
            }

            return donations.Values
                .OrderBy(localDonation => localDonation.Donation.DonationDate)
                .ThenBy(localDonation => localDonation.Donation.Id)
                .ToList();
        }

        private void PrintUnmatchedLocalDonations(
            List<LocalDonation> localDonations,
            string reason,
            ReconciliationSummary summary)
        {
            foreach (LocalDonation localDonation in localDonations)
            {
                summary.UnmatchedLocalDonations++;
                Console.WriteLine(
                    $"  Local donation {localDonation.Donation.Id} ({localDonation.Donation.DonationDate:yyyy-MM-dd}, "
                    + $"{localDonation.Donation.DonationAmount:F2}): {reason}; no change proposed.");
            }
        }

        private bool CheckEasypayAuthentication(SubscriptionPaymentApi subscriptionApi)
        {
            string endpoint = this.GetEasypayEndpoint("/subscription?page=1&records_per_page=1");
            Console.WriteLine($"Easypay authentication preflight: GET {endpoint}");

            try
            {
                ApiResponse<SubscriptionGet200Response> response =
                    subscriptionApi.SubscriptionGetWithHttpInfo(1m, 1m);
                Console.WriteLine(
                    response.Data == null
                        ? $"Easypay authentication preflight succeeded (HTTP {(int)response.StatusCode}; empty response)."
                        : $"Easypay authentication preflight succeeded (HTTP {(int)response.StatusCode}).");
                return true;
            }
            catch (ApiException exception)
            {
                Console.Error.WriteLine(
                    $"Easypay authentication preflight failed using GET {endpoint}; "
                    + $"account id={DisplayValue(this.configuration["Easypay:AccountId"])}; "
                    + $"HTTP {exception.ErrorCode}; {FormatApiException(exception)}");
                return false;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(
                    $"Easypay authentication preflight failed using GET {endpoint}; "
                    + $"account id={DisplayValue(this.configuration["Easypay:AccountId"])}; "
                    + $"{exception.GetType().Name}: {exception.Message}");
                return false;
            }
        }

        private static bool TryGetProviderTransactionDate(
            EasyPayTransaction transaction,
            out DateTime paymentDate)
        {
            foreach (string value in new[] { transaction.Date, transaction.CreatedAt, transaction.TransferDate })
            {
                if (DateTime.TryParse(
                    value,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.RoundtripKind,
                    out paymentDate))
                {
                    return true;
                }
            }

            paymentDate = default;
            return false;
        }

        private static bool ProviderPaymentMatchesDonation(Donation donation, ProviderPayment providerPayment)
        {
            return PaymentAmountReconciliation.AmountsMatchDonation(
                donation.DonationAmount,
                providerPayment.Requested,
                providerPayment.Paid);
        }

        private static bool IsWaitingDonationWithZeroValueConfirmedPayment(LocalDonation localDonation)
        {
            return localDonation.Donation.PaymentStatus == PaymentStatus.WaitingPayment
                && localDonation.Donation.ConfirmedPayment is EasyPayWithValuesBaseClass confirmedPayment
                && confirmedPayment.Requested <= 0
                && confirmedPayment.Paid <= 0;
        }

        private static EasyPayWithValuesBaseClass GetZeroValuePayment(LocalDonation localDonation)
        {
            return (localDonation.Donation.PaymentList ?? Array.Empty<BasePayment>())
                .OfType<EasyPayWithValuesBaseClass>()
                .FirstOrDefault(payment => payment.Requested <= 0 && payment.Paid <= 0);
        }

        private static bool IsWaitingDonationWithZeroValuePayment(LocalDonation localDonation)
        {
            return localDonation.Donation.PaymentStatus == PaymentStatus.WaitingPayment
                && GetZeroValuePayment(localDonation) != null;
        }

        private void ReportSubscriptionStatus(
            Subscription localSubscription,
            List<LocalDonation> localDonations,
            SubscriptionIdGet200Response providerSubscription,
            ReconciliationSummary summary)
        {
            SubscriptionIdGet200ResponseMethod.StatusEnum? providerStatus =
                providerSubscription.Method?.Status;
            string providerStatusValue = providerStatus?.ToString() ?? "<not returned>";
            string localStatusValue =
                $"{localSubscription.Status} (IsDeleted={localSubscription.IsDeleted})";

            Console.WriteLine(
                $"  Subscription status audit: local={localStatusValue}, "
                + $"Easypay method status={providerStatusValue}");

            if (!providerStatus.HasValue)
            {
                summary.SubscriptionStatusUnavailable++;
                Console.WriteLine(
                    "    Subscription status could not be compared because Easypay did not return "
                    + "a payment-method status; no change proposed.");
                return;
            }

            if (providerStatus == SubscriptionIdGet200ResponseMethod.StatusEnum.Deleted)
            {
                summary.EasypayDeletedSubscriptions++;
                Console.WriteLine(
                    $"    Easypay reports this subscription as deleted; local subscription status is "
                    + $"{localStatusValue}. No local status or deletion flag will be changed.");
            }

            if (IsSubscriptionStatusInSync(localSubscription, providerStatus.Value))
            {
                Console.WriteLine("    Subscription status audit: in sync.");
                return;
            }

            summary.SubscriptionStatusMismatches++;
            Console.WriteLine(
                "    Subscription status audit: OUT OF SYNC; affected local donations "
                + "are listed below. No local status, deletion flag, donation, or payment will be changed by this audit.");
            foreach (LocalDonation localDonation in localDonations)
            {
                Console.WriteLine(
                    $"      Donation {localDonation.Donation.Id}: date={localDonation.Donation.DonationDate:u}, "
                    + $"PaymentStatus={localDonation.Donation.PaymentStatus}");
            }
        }

        private void ReportEasypaySubscriptionNotFound(
            Subscription localSubscription,
            List<LocalDonation> localDonations,
            ReconciliationSummary summary)
        {
            bool localDeletionMatches = localSubscription.IsDeleted
                && localSubscription.Status == SubscriptionStatus.Inactive;
            Console.WriteLine(
                $"  Easypay subscription status audit: subscription id "
                + $"'{localSubscription.EasyPaySubscriptionId}' was not found (HTTP 404; likely deleted). "
                + $"Local={localSubscription.Status} (IsDeleted={localSubscription.IsDeleted}).");
            Console.WriteLine(
                localDeletionMatches
                    ? "    Local deletion state is consistent with a deleted Easypay subscription."
                    : "    Subscription status audit: OUT OF SYNC; affected local donations are listed below. "
                        + "No local status or deletion flag will be changed.");

            if (!localDeletionMatches)
            {
                summary.SubscriptionStatusMismatches++;
            }

            foreach (LocalDonation localDonation in localDonations)
            {
                Console.WriteLine(
                    $"      Donation {localDonation.Donation.Id}: date={localDonation.Donation.DonationDate:u}, "
                    + $"PaymentStatus={localDonation.Donation.PaymentStatus}");
            }
        }

        private static bool IsSubscriptionStatusInSync(
            Subscription localSubscription,
            SubscriptionIdGet200ResponseMethod.StatusEnum providerStatus)
        {
            return providerStatus switch
            {
                SubscriptionIdGet200ResponseMethod.StatusEnum.Active =>
                    !localSubscription.IsDeleted && localSubscription.Status == SubscriptionStatus.Active,
                SubscriptionIdGet200ResponseMethod.StatusEnum.Inactive =>
                    !localSubscription.IsDeleted && localSubscription.Status == SubscriptionStatus.Inactive,
                SubscriptionIdGet200ResponseMethod.StatusEnum.Deleted =>
                    localSubscription.IsDeleted && localSubscription.Status == SubscriptionStatus.Inactive,
                SubscriptionIdGet200ResponseMethod.StatusEnum.Waiting =>
                    !localSubscription.IsDeleted && localSubscription.Status != SubscriptionStatus.Active,
                SubscriptionIdGet200ResponseMethod.StatusEnum.Pending =>
                    !localSubscription.IsDeleted && localSubscription.Status != SubscriptionStatus.Active,
                _ => false,
            };
        }

        private void RemoveStaleZeroValueWaitingDonations(
            List<LocalDonation> localDonations,
            DateTime staleDonationCutoff,
            ReconciliationSummary summary)
        {
            foreach (LocalDonation localDonation in localDonations
                .Where(localDonation => !localDonation.IsInitialDonation)
                .Where(localDonation => localDonation.Donation.DonationDate < staleDonationCutoff)
                .Where(IsWaitingDonationWithZeroValuePayment)
                .ToList())
            {
                EasyPayWithValuesBaseClass zeroValuePayment = GetZeroValuePayment(localDonation);
                string paymentDescription = zeroValuePayment == null
                    ? "<not found>"
                    : $"local payment id={zeroValuePayment.Id}, Easypay payment id={DisplayValue(zeroValuePayment.EasyPayPaymentId)}";
                string reason = "PaymentStatus=WaitingPayment with Requested=0.00 and Paid=0.00, "
                    + $"DonationDate={localDonation.Donation.DonationDate:u} older than {StaleZeroValueDonationAgeDays} days";

                if (this.RemoveSubscriptionDonation(
                    localDonation,
                    reason,
                    paymentDescription,
                    summary))
                {
                    localDonations.Remove(localDonation);
                    summary.StaleZeroValueWaitingDonationsRemoved++;
                }
            }
        }

        private static string DescribeLocalDonation(LocalDonation localDonation)
        {
            EasyPayWithValuesBaseClass confirmedPayment =
                localDonation.Donation.ConfirmedPayment as EasyPayWithValuesBaseClass;
            return $"donation {localDonation.Donation.Id}: PaymentStatus={localDonation.Donation.PaymentStatus}, "
                + $"ConfirmedPayment={(confirmedPayment == null ? "<none or no monetary values>" :
                    $"requested={confirmedPayment.Requested:F2}, paid={confirmedPayment.Paid:F2}, "
                    + $"EasyPayPaymentId={DisplayValue(confirmedPayment.EasyPayPaymentId)}")}";
        }

        private bool RemoveInvalidSubscriptionDonation(
            LocalDonation localDonation,
            ProviderPayment providerPayment,
            ReconciliationSummary summary)
        {
            return this.RemoveSubscriptionDonation(
                localDonation,
                "zero-value confirmed payment cleanup for an ambiguous provider match",
                providerPayment.Id,
                summary);
        }

        private bool RemoveSubscriptionDonation(
            LocalDonation localDonation,
            string reason,
            string paymentDescription,
            ReconciliationSummary summary)
        {
            if (localDonation.IsInitialDonation)
            {
                Console.WriteLine(
                    $"  Donation {localDonation.Donation.Id}: subscription donation cleanup skipped "
                    + "because it is the subscription's initial donation.");
                return false;
            }

            List<SubscriptionDonations> links = this.Context.SubscriptionDonations
                .Where(link => EF.Property<int?>(link, "DonationId") == localDonation.Donation.Id)
                .ToList();
            if (links.Count != 1)
            {
                summary.LocalMappingConflicts++;
                Console.WriteLine(
                    $"  Donation {localDonation.Donation.Id}: subscription donation cleanup skipped; "
                    + $"expected one subscription link but found {links.Count}.");
                return false;
            }

            if (this.Context.Invoices.Any(invoice =>
                EF.Property<int?>(invoice, "DonationId") == localDonation.Donation.Id))
            {
                summary.LocalMappingConflicts++;
                Console.WriteLine(
                    $"  Donation {localDonation.Donation.Id}: subscription donation cleanup skipped "
                    + "because an invoice is linked to the donation.");
                return false;
            }

            List<BasePayment> payments = this.Context.Payments
                .Where(payment => EF.Property<int?>(payment, "DonationId") == localDonation.Donation.Id)
                .ToList();
            if (localDonation.Donation.ConfirmedPayment != null
                && payments.All(payment => payment.Id != localDonation.Donation.ConfirmedPayment.Id))
            {
                payments.Add(localDonation.Donation.ConfirmedPayment);
            }

            if (payments.Any(payment => this.Context.Donations.Any(donation =>
                donation.Id != localDonation.Donation.Id
                && EF.Property<int?>(donation, "ConfirmedPaymentId") == payment.Id)))
            {
                summary.LocalMappingConflicts++;
                Console.WriteLine(
                    $"  Donation {localDonation.Donation.Id}: subscription donation cleanup skipped "
                    + "because a payment is confirmed by another donation.");
                return false;
            }

            Console.WriteLine(
                $"  Invalid subscription donation detected: local donation {localDonation.Donation.Id}, "
                + $"date={localDonation.Donation.DonationDate:yyyy-MM-dd}, "
                + $"reason={reason}, "
                + $"payment={paymentDescription}, "
                + $"payment records to remove={payments.Count}.");
            Console.WriteLine(
                $"    {(this.DryRun ? "Would remove" : "Removing")} donation "
                + $"{localDonation.Donation.Id} and its {payments.Count} local payment record(s); "
                + "no Easypay payment or subscription is changed.");

            summary.InvalidSubscriptionDonationsRemoved++;
            summary.PaymentRecordsRemoved += payments.Count;
            summary.ChangesProposed++;
            if (this.DryRun)
            {
                summary.ChangesWouldApply++;
                return true;
            }

            using (Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction =
                this.Context.Database.BeginTransaction())
            {
                try
                {
                    // Clear the restrictive ConfirmedPayment relationship before deleting either
                    // side of the DonationId/ConfirmedPaymentId cycle.
                    localDonation.Donation.ConfirmedPayment = null;
                    this.Context.SaveChanges();

                    List<int> paymentIds = payments.Select(payment => payment.Id).ToList();
                    List<PaymentNotifications> notifications = this.Context.PaymentNotifications
                        .Where(notification =>
                            paymentIds.Contains(EF.Property<int?>(notification, "PaymentId").Value))
                        .ToList();
                    this.Context.PaymentNotifications.RemoveRange(notifications);
                    this.Context.SubscriptionDonations.Remove(links[0]);
                    this.Context.Payments.RemoveRange(payments);
                    this.Context.Donations.Remove(localDonation.Donation);
                    this.Context.SaveChanges();
                    transaction.Commit();
                    summary.ChangesApplied++;
                    return true;
                }
                catch (DbUpdateException exception)
                {
                    transaction.Rollback();
                    summary.LocalMappingConflicts++;
                    this.Context.ChangeTracker.Clear();
                    Console.WriteLine(
                        $"    Local cleanup failed; no donation or payment was removed: {exception.Message}");
                    return false;
                }
                catch (InvalidOperationException exception)
                {
                    transaction.Rollback();
                    summary.LocalMappingConflicts++;
                    this.Context.ChangeTracker.Clear();
                    Console.WriteLine(
                        $"    Local cleanup failed; no donation or payment was removed: {exception.Message}");
                    return false;
                }
            }
        }

        private static double GetSubscriptionValue(Subscription subscription)
        {
            return subscription.InitialDonation?.DonationAmount
                ?? subscription.Donations?
                    .Where(link => link.Donation != null)
                    .Select(link => link.Donation.DonationAmount)
                    .FirstOrDefault()
                ?? 0;
        }

        private void PrintSummary(ReconciliationSummary summary)
        {
            Console.WriteLine("Reconciliation summary:");
            Console.WriteLine($"  Local subscriptions processed: {summary.SubscriptionsProcessed}");
            Console.WriteLine($"  Local donations checked: {summary.LocalDonationsChecked}");
            Console.WriteLine($"  Easypay subscription transaction records found: {summary.ProviderTransactionRecordsFound}");
            Console.WriteLine($"  Easypay subscription transactions checked: {summary.ProviderTransactionsChecked}");
            Console.WriteLine($"  Donations matched: {summary.MatchedDonations}");
            Console.WriteLine($"  Donations already correct: {summary.AlreadyCorrect}");
            Console.WriteLine($"  Subscription mismatches: {summary.SubscriptionMismatches}");
            Console.WriteLine($"  Subscription lookup failures: {summary.SubscriptionLookupFailures}");
            Console.WriteLine($"  Easypay subscriptions not found (likely deleted): {summary.EasypaySubscriptionsNotFound}");
            Console.WriteLine($"  Subscription status mismatches: {summary.SubscriptionStatusMismatches}");
            Console.WriteLine($"  Subscription statuses unavailable: {summary.SubscriptionStatusUnavailable}");
            Console.WriteLine($"  Easypay subscriptions reported deleted: {summary.EasypayDeletedSubscriptions}");
            Console.WriteLine($"  Transaction lookup or mapping failures: {summary.TransactionLookupFailures}");
            Console.WriteLine($"  Unmatched local donations: {summary.UnmatchedLocalDonations}");
            Console.WriteLine($"  Unmatched Easypay payments: {summary.UnmatchedProviderPayments}");
            Console.WriteLine($"  Local mapping conflicts: {summary.LocalMappingConflicts}");
            Console.WriteLine(
                $"  Ambiguous matches resolved using zero-value confirmed payment rule: "
                + summary.ZeroValueConfirmedPaymentAmbiguitiesResolved);
            Console.WriteLine(
                $"  Invalid subscription donations removed: {summary.InvalidSubscriptionDonationsRemoved}");
            Console.WriteLine(
                $"  Stale zero-value WaitingPayment donations removed: {summary.StaleZeroValueWaitingDonationsRemoved}");
            Console.WriteLine($"  Payment records removed: {summary.PaymentRecordsRemoved}");
            Console.WriteLine($"  Changes proposed: {summary.ChangesProposed}");
            if (this.DryRun)
            {
                Console.WriteLine($"  Changes that would be applied: {summary.ChangesWouldApply}");
                Console.WriteLine("  Database writes: 0 (dry-run is read-only)");
            }
            else
            {
                Console.WriteLine($"  Changes applied: {summary.ChangesApplied}");
            }
        }

        private static string DescribeDatabase(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return "DefaultConnection is not configured";
            }

            try
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connectionString);
                return $"server={builder.DataSource}, database={builder.InitialCatalog}, "
                    + $"integrated security={builder.IntegratedSecurity}, encrypt={builder.Encrypt}";
            }
            catch (ArgumentException)
            {
                return "DefaultConnection is configured but could not be parsed";
            }
        }

        private static string DisplayValue(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "<not configured>" : value;
        }

        private string GetEasypayEndpoint(string resourcePath)
        {
            string baseUrl = DisplayValue(this.configuration["Easypay:BaseUrl"]).TrimEnd('/');
            return $"{baseUrl}/2.0{resourcePath}";
        }

        private static string FormatApiException(ApiException exception)
        {
            string content = exception.ErrorContent?.ToString();
            return string.IsNullOrWhiteSpace(content)
                ? $"message={exception.Message}"
                : $"message={exception.Message}; response={content}";
        }

        private static bool IsEasypaySubscriptionNotFound(ApiException exception)
        {
            return exception.ErrorCode == 404
                || exception.ErrorContent?.ToString()?.Contains(
                    "Subscription Not Found",
                    StringComparison.OrdinalIgnoreCase) == true;
        }

        private sealed class LocalDonation
        {
            public Donation Donation { get; set; }

            public bool IsInitialDonation { get; set; }
        }

        private sealed class ProviderPayment
        {
            public string Id { get; set; }

            public string Key { get; set; }

            public double Requested { get; set; }

            public double Paid { get; set; }

            public double FixedFee { get; set; }

            public double VariableFee { get; set; }

            public double Tax { get; set; }

            public double Transfer { get; set; }

            public DateTime PaymentDate { get; set; }

            public bool IsPaid { get; set; }

            public string Status { get; set; }

        }

        private sealed class ReconciliationSummary
        {
            public int SubscriptionsProcessed { get; set; }

            public int LocalDonationsChecked { get; set; }

            public int ProviderTransactionRecordsFound { get; set; }

            public int ProviderTransactionsChecked { get; set; }

            public int MatchedDonations { get; set; }

            public int AlreadyCorrect { get; set; }

            public int SubscriptionMismatches { get; set; }

            public int SubscriptionLookupFailures { get; set; }

            public int EasypaySubscriptionsNotFound { get; set; }

            public int SubscriptionStatusMismatches { get; set; }

            public int SubscriptionStatusUnavailable { get; set; }

            public int EasypayDeletedSubscriptions { get; set; }

            public int TransactionLookupFailures { get; set; }

            public int UnmatchedLocalDonations { get; set; }

            public int UnmatchedProviderPayments { get; set; }

            public int LocalMappingConflicts { get; set; }

            public int ZeroValueConfirmedPaymentAmbiguitiesResolved { get; set; }

            public int InvalidSubscriptionDonationsRemoved { get; set; }

            public int StaleZeroValueWaitingDonationsRemoved { get; set; }

            public int PaymentRecordsRemoved { get; set; }

            public int ChangesProposed { get; set; }

            public int ChangesWouldApply { get; set; }

            public int ChangesApplied { get; set; }
        }
    }
}
