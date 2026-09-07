// -----------------------------------------------------------------------
// <copyright file="SubscriptionRepository.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Common;
    using BancoAlimentar.AlimentaEstaIdeia.Common.EasyPay;
    using BancoAlimentar.AlimentaEstaIdeia.Common.Repository.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using Easypay.Rest.Client.Api;
    using Easypay.Rest.Client.Model;
    using Microsoft.ApplicationInsights;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Caching.Memory;
    using static Easypay.Rest.Client.Model.SubscriptionPostRequest;
    using Subscription = BancoAlimentar.AlimentaEstaIdeia.Model.Subscription;

    /// <summary>
    /// Default implementation for the <see cref="SubscriptionRepository"/> repository pattern.
    /// </summary>
    public class SubscriptionRepository : GenericRepository<Subscription, ApplicationDbContext>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionRepository"/> class.
        /// </summary>
        /// <param name="context"><see cref="ApplicationDbContext"/> instance.</param>
        /// <param name="memoryCache">A reference to the Memory cache system.</param>
        /// <param name="telemetryClient">Telemetry Client.</param>
        public SubscriptionRepository(ApplicationDbContext context, IMemoryCache memoryCache, TelemetryClient telemetryClient)
            : base(context, memoryCache, telemetryClient)
        {
        }

        /// <summary>
        /// Complete the creation on the subscription from the easyapy API.
        /// </summary>
        /// <param name="transactionKey">Easypay transaction id.</param>
        /// <param name="status">Status of the subscription_creationg operation.</param>
        /// <returns>Subscription id.</returns>
        public int CompleteSubcriptionCreate(string transactionKey, NotificationGeneric.StatusEnum status)
        {
            int result = -1;
            if (!string.IsNullOrEmpty(transactionKey))
            {
                Subscription value = this.DbContext.Subscriptions
                    .Where(p => p.TransactionKey == transactionKey)
                    .FirstOrDefault();
                if (value != null)
                {
                    if (status == NotificationGeneric.StatusEnum.Success)
                    {
                        value.Status = SubscriptionStatus.Active;
                    }
                    else if (status == NotificationGeneric.StatusEnum.Failed)
                    {
                        value.Status = SubscriptionStatus.Error;
                    }

                    this.DbContext.SaveChanges();
                    result = value.Id;
                }
            }

            return result;
        }

        /// <summary>
        /// Complete the capture of the subscription.
        /// </summary>
        /// <param name="easyPayId">EasyPayId.</param>
        /// <param name="transactionKey">Easypay transaction id.</param>
        /// <param name="status">Capture status.</param>
        /// <param name="dateTime">Subscription capture.</param>
        /// <returns>Donation id.</returns>
        public (int DonationId, string Reason) SubscriptionCapture(
            string easyPayId,
            string transactionKey,
            NotificationGeneric.StatusEnum status,
            DateTime dateTime)
        {
            int donationId = -1;
            string reason = "None";
            if (!string.IsNullOrEmpty(transactionKey))
            {
                Subscription value = this.DbContext.Subscriptions
                    .Include(p => p.InitialDonation)
                    .Where(p => p.TransactionKey == transactionKey)
                    .FirstOrDefault();

                if (value != null && value.InitialDonation.DonationDate.Date != dateTime.Date)
                {
                    CreditCardPayment payment = this.DbContext.Payments
                        .Cast<CreditCardPayment>()
                        .Where(p =>
                                p.TransactionKey == transactionKey &&
                                p.Created.Date == dateTime.Date)
                        .FirstOrDefault();

                    if (payment != null)
                    {
                        donationId = this.DbContext.Payments
                            .Where(p => p.Id == payment.Id)
                            .Select(p => p.Donation.Id)
                            .First();
                        payment.Status = status.ToString();
                        this.DbContext.SaveChanges();
                    }
                    else
                    {
                        reason = "Payment is null";
                    }
                }
                else if (value == null)
                {
                    this.TelemetryClient.TrackEvent(
                        "SubscriptionNotFound",
                        new Dictionary<string, string>()
                        {
                            { "Operation", "SubscriptionCreate" },
                            { nameof(easyPayId), easyPayId },
                            { nameof(transactionKey), transactionKey },
                            { nameof(status), status.ToString() },
                        });
                    reason = "Subscription is not found";
                }
                else if (value != null && value.InitialDonation.DonationDate.Date == dateTime.Date)
                {
                    this.TelemetryClient.TrackEvent(
                        "PaymentDateIsEqual",
                        new Dictionary<string, string>()
                        {
                            { "Operation", "SubscriptionCreate" },
                            { nameof(easyPayId), easyPayId },
                            { nameof(transactionKey), transactionKey },
                            { nameof(status), status.ToString() },
                            { "Date", dateTime.Date.ToString() },
                        });
                    reason = $"PaymentDate is equal {dateTime.Date}";
                }
            }
            else
            {
                this.TelemetryClient.TrackEvent(
                    "TransactionKeyIsNull",
                    new Dictionary<string, string>()
                    {
                        { "Operation", "SubscriptionCreate" },
                        { nameof(easyPayId), easyPayId },
                        { nameof(transactionKey), transactionKey },
                        { nameof(status), status.ToString() },
                    });
                reason = "TransactionKey is null";
            }

            return (donationId, reason);
        }

        /// <summary>
        /// Completes a subscription capture using the embedded Easypay transaction evidence.
        /// </summary>
        /// <param name="evidence">Verified subscription transaction evidence.</param>
        /// <param name="status">Capture status.</param>
        /// <returns>Donation id and processing reason.</returns>
        public (int DonationId, string Reason) CompleteSubscriptionCapture(
            EasyPaySubscriptionPaymentEvidence evidence,
            NotificationGeneric.StatusEnum status = NotificationGeneric.StatusEnum.Success)
        {
            if (status != NotificationGeneric.StatusEnum.Success)
            {
                return (-1, "Capture is not successful");
            }

            if (evidence == null
                || string.IsNullOrWhiteSpace(evidence.EasypayPaymentId)
                || string.IsNullOrWhiteSpace(evidence.TransactionKey)
                || evidence.PaymentDate == default
                || evidence.Requested <= 0
                || evidence.Paid <= 0)
            {
                return (-1, "Subscription payment evidence is incomplete");
            }

            Subscription subscription = this.DbContext.Subscriptions
                .Include(s => s.InitialDonation)
                .Where(s => s.TransactionKey == evidence.TransactionKey)
                .FirstOrDefault();
            if (subscription?.InitialDonation == null)
            {
                this.TelemetryClient.TrackEvent(
                    "SubscriptionCaptureSubscriptionNotFound",
                    new Dictionary<string, string>
                    {
                        { nameof(evidence.EasypayPaymentId), evidence.EasypayPaymentId },
                        { nameof(evidence.TransactionKey), evidence.TransactionKey },
                    });
                return (-1, "Subscription is not found");
            }

            if (!string.IsNullOrWhiteSpace(evidence.EasypaySubscriptionId)
                && !string.Equals(
                    subscription.EasyPaySubscriptionId,
                    evidence.EasypaySubscriptionId,
                    StringComparison.OrdinalIgnoreCase))
            {
                return (-1, "Easypay subscription id does not match local subscription");
            }

            if (string.Equals(
                subscription.EasyPaySubscriptionId,
                evidence.EasypayPaymentId,
                StringComparison.OrdinalIgnoreCase))
            {
                return (-1, "Easypay subscription id cannot be stored as payment id");
            }

            if (!PaymentAmountReconciliation.AmountsMatchDonation(
                    subscription.InitialDonation.DonationAmount,
                    (double)evidence.Requested,
                    (double)evidence.Paid))
            {
                return (-1, "Payment amount does not match donation");
            }

            List<CreditCardPayment> paymentsWithProviderId = this.DbContext.Payments
                .OfType<CreditCardPayment>()
                .Include(p => p.Donation)
                .Where(p => p.EasyPayPaymentId == evidence.EasypayPaymentId)
                .ToList();
            if (paymentsWithProviderId.Count > 1)
            {
                return (-1, "Multiple payments match the Easypay payment id");
            }

            CreditCardPayment payment = paymentsWithProviderId.SingleOrDefault();
            if (payment != null && !string.Equals(payment.TransactionKey, evidence.TransactionKey, StringComparison.Ordinal))
            {
                return (-1, "Easypay payment id belongs to another transaction");
            }

            if (payment == null)
            {
                List<CreditCardPayment> paymentsOnCaptureDate = this.DbContext.Payments
                    .OfType<CreditCardPayment>()
                    .Include(p => p.Donation)
                    .Where(p => p.TransactionKey == evidence.TransactionKey
                        && p.Created.Date == evidence.PaymentDate.Date)
                    .ToList();
                if (paymentsOnCaptureDate.Count > 1)
                {
                    return (-1, "Multiple payments match the capture date");
                }

                payment = paymentsOnCaptureDate.SingleOrDefault();
            }

            Donation donation;
            if (payment != null)
            {
                donation = payment.Donation;
                if (donation == null)
                {
                    return (-1, "Payment donation is null");
                }

                bool isLinkedToTargetSubscription = this.DbContext.SubscriptionDonations.Any(link =>
                    link.Subscription.Id == subscription.Id
                    && link.DonationId == donation.Id);
                bool isLinkedToAnotherSubscription = this.DbContext.SubscriptionDonations.Any(link =>
                    link.DonationId == donation.Id
                    && link.Subscription.Id != subscription.Id);
                bool donationBelongsToSubscription = SubscriptionDonationOwnership.IsEligible(
                    donation.Id == subscription.InitialDonation.Id,
                    isLinkedToTargetSubscription,
                    isLinkedToAnotherSubscription,
                    payment.TransactionKey,
                    evidence.TransactionKey);
                if (!donationBelongsToSubscription)
                {
                    this.TelemetryClient.TrackEvent(
                        "EasypaySubscriptionPaymentOwnershipConflict",
                        new Dictionary<string, string>
                        {
                            { "SubscriptionId", subscription.Id.ToString() },
                            { "DonationId", donation.Id.ToString() },
                            { "PaymentId", payment.Id.ToString() },
                            { "EasypayPaymentId", evidence.EasypayPaymentId },
                        });
                    return (donation.Id, "Payment donation does not belong to the subscription");
                }

                this.DbContext.Entry(donation).Reference(d => d.ConfirmedPayment).Load();

                if (donation.ConfirmedPayment != null
                    && donation.ConfirmedPayment.Id != payment.Id)
                {
                    return (donation.Id, "Donation is already associated with another confirmed payment");
                }

                if (!PaymentAmountReconciliation.ProviderValueMatchesDonation(
                        donation.DonationAmount,
                        (double)evidence.Paid))
                {
                    return (donation.Id, "Payment amount does not match donation");
                }

                if (!this.DbContext.SubscriptionDonations.Any(link =>
                    link.Subscription.Id == subscription.Id
                    && link.DonationId == donation.Id))
                {
                    this.DbContext.SubscriptionDonations.Add(new SubscriptionDonations
                    {
                        Donation = donation,
                        Subscription = subscription,
                    });
                }
            }
            else
            {
                if (evidence.PaymentDate.Date == subscription.InitialDonation.DonationDate.Date)
                {
                    return (subscription.InitialDonation.Id, "Initial payment is missing");
                }

                DonationRepository donationRepository = new DonationRepository(
                    this.DbContext,
                    this.MemoryCache,
                    this.TelemetryClient);
                donation = donationRepository.CloneDonation(
                    donationRepository.GetFullDonationById(subscription.InitialDonation.Id));
                donation.DonationDate = evidence.PaymentDate;
                this.DbContext.Donations.Add(donation);
                this.DbContext.SubscriptionDonations.Add(new SubscriptionDonations
                {
                    Donation = donation,
                    Subscription = subscription,
                });

                payment = new CreditCardPayment
                {
                    Created = evidence.PaymentDate,
                    TransactionKey = evidence.TransactionKey,
                    EasyPayPaymentId = evidence.EasypayPaymentId,
                    Status = status.ToString(),
                    Donation = donation,
                };
                this.DbContext.CreditCardPayments.Add(payment);
            }

            payment.EasyPayPaymentId = evidence.EasypayPaymentId;
            payment.TransactionKey = evidence.TransactionKey;
            payment.Status = status.ToString();
            payment.Completed ??= evidence.PaymentDate;
            payment.Requested = (float)evidence.Requested;
            payment.Paid = (float)evidence.Paid;
            payment.FixedFee = (float)evidence.FixedFee;
            payment.VariableFee = (float)evidence.VariableFee;
            payment.Tax = (float)evidence.Tax;
            payment.Transfer = (float)evidence.Transfer;

            DonationRepository repository = new DonationRepository(
                this.DbContext,
                this.MemoryCache,
                this.TelemetryClient);
            if (!repository.TryCompleteDonationPayment(
                    donation,
                    payment,
                    payment.Requested,
                    payment.Paid,
                    evidence.TransactionKey))
            {
                return (donation.Id, "Payment amount does not match donation");
            }

            this.DbContext.SaveChanges();
            this.TelemetryClient.TrackEvent(
                "EasypaySubscriptionPaymentCompleted",
                new Dictionary<string, string>
                {
                    { "EasypaySubscriptionId", evidence.EasypaySubscriptionId ?? subscription.EasyPaySubscriptionId },
                    { "EasypayPaymentId", evidence.EasypayPaymentId },
                    { "TransactionKey", evidence.TransactionKey },
                    { "DonationId", donation.Id.ToString() },
                    { "PaymentId", payment.Id.ToString() },
                });
            return (donation.Id, "Subscription capture completed");
        }

        /// <summary>
        /// New subscription capture process happen from easypay. Donation has to be created.
        /// </summary>
        /// <param name="easyPayId">EasyPayId.</param>
        /// <param name="transactionKey">Easypay transaction id.</param>
        /// <param name="status">Capture status.</param>
        /// <param name="dateTime">Subscription capture.</param>
        /// <param name="requested">Amount requested by Easypay.</param>
        /// <param name="paid">Amount paid according to Easypay.</param>
        /// <returns>Donation id.</returns>
        public int CreateSubscriptionDonationAndPayment(
            string easyPayId,
            string transactionKey,
            NotificationGeneric.StatusEnum status,
            DateTime dateTime,
            float requested,
            float paid)
        {
            int result = -1;
            if (!string.IsNullOrEmpty(transactionKey))
            {
                Subscription value = this.DbContext.Subscriptions
                    .Include(p => p.InitialDonation)
                    .Where(p => p.TransactionKey == transactionKey)
                    .FirstOrDefault();

                if (value?.InitialDonation == null
                    || status != NotificationGeneric.StatusEnum.Success
                    || requested <= 0
                    || paid <= 0
                    || !PaymentAmountReconciliation.AmountsMatchDonation(
                        value.InitialDonation.DonationAmount,
                        requested,
                        paid))
                {
                    return result;
                }

                // For the intial capture we already have a initial donation that we're going to process.
                // In the future we will copy this donation, the payment and process it.
                if (value != null && value.InitialDonation.DonationDate.Date != dateTime.Date)
                {
                    Donation donation = new DonationRepository(
                        this.DbContext,
                        this.MemoryCache,
                        this.TelemetryClient)
                        .GetFullDonationById(value.InitialDonation.Id);

                    DonationRepository donationRepository = new DonationRepository(this.DbContext, this.MemoryCache, this.TelemetryClient);
                    donationRepository.CloneDonation(donation);

                    Donation newDonation = donationRepository.CloneDonation(donation);
                    newDonation.DonationDate = dateTime;

                    SubscriptionDonations subscriptionDonation = new SubscriptionDonations()
                    {
                        Donation = newDonation,
                        Subscription = value,
                    };

                    this.DbContext.SubscriptionDonations.Add(subscriptionDonation);
                    this.DbContext.SaveChanges();

                    result = newDonation.Id;

                    donationRepository.CreateCreditCardPaymnet(newDonation, easyPayId, transactionKey, null, dateTime, status.ToString());
                }
            }

            return result;
        }

        /// <summary>
        /// Create a subscription.
        /// </summary>
        /// <param name="donation">Initial <see cref="Donation"/> that trigger the subscription.</param>
        /// <param name="transactionKey">Transaction key.</param>
        /// <param name="easyPayId">Easy pay id.</param>
        /// <param name="url">Payment url.</param>
        /// <param name="user">The current user.</param>
        /// <param name="originalRequest">Original Request.</param>
        /// <param name="frequency">Subscription Frequency.</param>
        public void CreateSubscription(
            Donation donation,
            string transactionKey,
            string easyPayId,
            string url,
            WebUser user,
            SubscriptionPostRequest originalRequest,
            FrequencyEnum frequency)
        {
            if (donation != null && !string.IsNullOrEmpty(transactionKey) && !string.IsNullOrEmpty(url))
            {
                Subscription value = new Subscription()
                {
                    Created = DateTime.UtcNow,

                    StartTime = originalRequest.StartTime.FromEasyPayDateTimeString(),
                    ExpirationTime = originalRequest.ExpirationTime.FromEasyPayDateTimeString(),
                    TransactionKey = transactionKey,
                    EasyPaySubscriptionId = easyPayId,
                    Url = url,
                    InitialDonation = donation,
                    Frequency = frequency.ToString(),
                    PublicId = Guid.NewGuid(),
                    User = user,
                };

                SubscriptionDonations subscriptionDonations = new SubscriptionDonations()
                {
                    Donation = donation,
                    Subscription = value,
                };

                this.DbContext.SubscriptionDonations.Add(subscriptionDonations);
                this.DbContext.Subscriptions.Add(value);
                this.DbContext.SaveChanges();
            }
        }

        /// <summary>
        /// Gets a list of the <see cref="WebUser"/> subscriptions.
        /// </summary>
        /// <param name="user">A reference to the user.</param>
        /// <returns>A <see cref="List{T}"/> of <see cref="Subscription"/> for the user.</returns>
        public List<Subscription> GetUserSubscription(WebUser user)
        {
            List<Subscription> result = null;

            if (user != null)
            {
                result = this.DbContext.Subscriptions
                    .Include(p => p.InitialDonation)
                    .Where(p => p.User.Id == user.Id && p.Status != SubscriptionStatus.Created)
                    .ToList();
            }

            return result;
        }

        /// <summary>
        /// Gets if the subscription associated to the donation.
        /// </summary>
        /// <param name="donationId">Donation id.</param>
        /// <returns>The <see cref="Subscription"/> that belong to the donation id.</returns>
        public Subscription GetSubscriptionFromDonationId(int donationId)
        {
            return this.DbContext.SubscriptionDonations
            .Where(p => p.Donation.Id == donationId)
            .Select(p => p.Subscription)
            .FirstOrDefault();
        }

        /// <summary>
        /// Gets subscriptions linked to the given donation ids in a single query.
        /// </summary>
        /// <param name="donationIds">Donation ids.</param>
        /// <returns>A map of donation id to subscription.</returns>
        public IReadOnlyDictionary<int, Subscription> GetSubscriptionsByDonationIds(IEnumerable<int> donationIds)
        {
            var ids = donationIds.Distinct().ToList();
            if (ids.Count == 0)
            {
                return new Dictionary<int, Subscription>();
            }

            return this.DbContext.SubscriptionDonations
                .AsNoTracking()
                .Where(sd => sd.DonationId.HasValue && ids.Contains(sd.DonationId.Value))
                .GroupBy(sd => sd.DonationId.Value)
                .Select(g => new { DonationId = g.Key, Subscription = g.Select(sd => sd.Subscription).First() })
                .ToDictionary(x => x.DonationId, x => x.Subscription);
        }

        /// <summary>
        /// Mark a subscription as deleted.
        /// </summary>
        /// <param name="subscriptionId">Subscription Id.</param>
        /// <returns>true if the operation is succeed, false otherwise.</returns>
        public bool DeleteSubscription(int subscriptionId)
        {
            bool result = false;

            Subscription value = this.DbContext.Subscriptions
                .Where(p => p.Id == subscriptionId)
                .FirstOrDefault();

            if (value != null)
            {
                value.IsDeleted = true;
                value.Status = SubscriptionStatus.Inactive;
                this.DbContext.SaveChanges();
                result = true;
            }

            return result;
        }

        /// <summary>
        /// Gets the subscription based on the public id.
        /// </summary>
        /// <param name="publicId">Public Id for the subscription.</param>
        /// <returns>A reference to the <see cref="Subscription"/>.</returns>
        public Subscription GetSubscriptionByPublicId(Guid publicId)
        {
            return this.DbContext.Subscriptions
                .Where(p => p.PublicId == publicId)
                .FirstOrDefault();
        }

        /// <summary>
        /// Gets the subscription based on the id.
        /// </summary>
        /// <param name="id">Public Id for the subscription.</param>
        /// <returns>A reference to the <see cref="Subscription"/>.</returns>
        public Subscription GetSubscriptionById(int id)
        {
            return this.DbContext.Subscriptions
                .Include(p => p.User)
                .Include(p => p.InitialDonation)
                .Where(p => p.Id == id)
                .FirstOrDefault();
        }

        /// <summary>
        /// Gets the subscription based on the easypay id.
        /// </summary>
        /// <param name="publicId">EasyPay Id for the subscription.</param>
        /// <returns>A reference to the <see cref="Subscription"/>.</returns>
        public Subscription GetSubscriptionByEasyPayId(Guid publicId)
        {
            return this.DbContext.Subscriptions
                .Include(p => p.InitialDonation)
                .Where(p => p.EasyPaySubscriptionId == publicId.ToString())
                .FirstOrDefault();
        }

        /// <summary>
        /// Gets the list of donations for a particular subscription.
        /// </summary>
        /// <param name="id">Subscription id.</param>
        /// <returns>A collection of <see cref="List{Donation}"/>.</returns>
        public List<Donation> GetDonationsForSubscription(int id)
        {
            return this.DbContext.SubscriptionDonations
                .Include(p => p.Donation.FoodBank)
                .Include(p => p.Donation.PaymentList)
                .Where(p => p.Subscription.Id == id)
                .OrderByDescending(p => p.Donation.DonationDate)
                .Select(p => p.Donation)
                .ToList();
        }

        /// <summary>
        /// Gets the donation of the same day for the given transaction key.
        /// </summary>
        /// <param name="transactionKey">Subscription transaction key.</param>
        /// <param name="dateTime">Donation datetime.</param>
        /// <returns>A reference to the <see cref="Donation"/>.</returns>
        public Donation GetDonationFromSubscriptionTransactionKey(string transactionKey, DateTime dateTime)
        {
            Donation result = null;
            if (!string.IsNullOrEmpty(transactionKey))
            {
                Subscription subscription = this.DbContext.Subscriptions
                    .Include(p => p.Donations)
                    .Include("Donations.Donation")
                    .Where(p => p.TransactionKey == transactionKey)
                    .FirstOrDefault();
                if (subscription != null)
                {
                    result = subscription.Donations
                        .Where(p => p.Donation.DonationDate.Date == dateTime.Date)
                        .Select(p => p.Donation)
                        .FirstOrDefault();
                }
            }

            return result;
        }

        /// <summary>
        /// Update the status of the subscription from easy pay.
        /// </summary>
        /// <param name="apiClient">A refrence to the <see cref="SubscriptionPaymentApi"/>.</param>
        /// <param name="user">The current user.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task SyncSubscriptionFromEasyPay(ISubscriptionPaymentApi apiClient, WebUser user)
        {
            List<Subscription> subscriptions = this.GetUserSubscription(user);
            if (subscriptions == null || subscriptions.Count == 0)
            {
                return;
            }

            foreach (var item in subscriptions)
            {
                SubscriptionIdGet200Response paymentSubscriptionWithTransactions =
                    await apiClient.SubscriptionIdGetAsync(Guid.Parse(item.EasyPaySubscriptionId));

                if (paymentSubscriptionWithTransactions == null)
                {
                    continue;
                }

                if (paymentSubscriptionWithTransactions.ExpirationTime.FromEasyPayDateTimeString() < DateTime.UtcNow)
                {
                    item.Status = AlimentaEstaIdeia.Model.SubscriptionStatus.Inactive;
                }

                item.ExpirationTime = paymentSubscriptionWithTransactions.ExpirationTime.FromEasyPayDateTimeString();
                item.StartTime = paymentSubscriptionWithTransactions.StartTime.FromEasyPayDateTimeString();
                item.Created = paymentSubscriptionWithTransactions.CreatedAt.FromEasyPayDateTimeString();

                await this.DbContext.SaveChangesAsync();
            }
        }
    }
}
