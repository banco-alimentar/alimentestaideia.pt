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
    using System.Runtime.Serialization;
    using System.Text;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using Easypay.Rest.Client.Model;
    using Microsoft.ApplicationInsights;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Caching.Memory;

    /// <summary>
    /// Default implementation for the <see cref="SubscriptionRepository"/> repository pattern.
    /// </summary>
    public class SubscriptionRepository : GenericRepository<Subscription>
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
        public int CompleteSubcriptionCreate(string transactionKey, GenericNotificationRequest.StatusEnum status)
        {
            int result = -1;
            if (!string.IsNullOrEmpty(transactionKey))
            {
                Subscription value = this.DbContext.Subscriptions
                    .Where(p => p.TransactionKey == transactionKey)
                    .FirstOrDefault();
                if (value != null)
                {
                    if (status == GenericNotificationRequest.StatusEnum.Success)
                    {
                        value.Status = SubscriptionStatus.Active;
                    }
                    else if (status == GenericNotificationRequest.StatusEnum.Failed)
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
        public (int donationId, string reason) SubscriptionCapture(
            string easyPayId,
            string transactionKey,
            GenericNotificationRequest.StatusEnum status,
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
        /// New subscription capture process happen from easypay. Donation has to be created.
        /// </summary>
        /// <param name="easyPayId">EasyPayId.</param>
        /// <param name="transactionKey">Easypay transaction id.</param>
        /// <param name="status">Capture status.</param>
        /// <param name="dateTime">Subscription capture.</param>
        /// <returns>Donation id.</returns>
        public int CreateSubscriptionDonationAndPayment(
            string easyPayId,
            string transactionKey,
            GenericNotificationRequest.StatusEnum status,
            DateTime dateTime)
        {
            int result = -1;
            if (!string.IsNullOrEmpty(transactionKey))
            {
                Subscription value = this.DbContext.Subscriptions
                    .Include(p => p.InitialDonation)
                    .Where(p => p.TransactionKey == transactionKey)
                    .FirstOrDefault();

                // For the intial capture we already have a initial donation that we're going to process.
                // In the future we will copy this donation, the payment and process it.
                if (value != null && value.InitialDonation.DonationDate.Date != dateTime.Date)
                {
                    Donation donation = new DonationRepository(
                        this.DbContext,
                        this.MemoryCache,
                        this.TelemetryClient)
                        .GetFullDonationById(value.InitialDonation.Id);
                    Donation newDonation = new Donation()
                    {
                        DonationAmount = donation.DonationAmount,
                        FoodBank = donation.FoodBank,
                        Nif = donation.Nif,
                        PaymentStatus = PaymentStatus.WaitingPayment,
                        Referral = donation.Referral,
                        PublicId = Guid.NewGuid(),
                        DonationDate = dateTime,
                        DonationItems = new List<DonationItem>(),
                        User = donation.User,
                        WantsReceipt = true,
                    };

                    foreach (var donationItem in donation.DonationItems)
                    {
                        newDonation.DonationItems.Add(new DonationItem()
                        {
                            Donation = newDonation,
                            Price = donationItem.Price,
                            Quantity = donationItem.Quantity,
                            ProductCatalogue = donationItem.ProductCatalogue,
                        });
                    }

                    SubscriptionDonations subscriptionDonation = new SubscriptionDonations()
                    {
                        Donation = newDonation,
                        Subscription = value,
                    };

                    this.DbContext.SubscriptionDonations.Add(subscriptionDonation);
                    this.DbContext.SaveChanges();

                    result = newDonation.Id;

                    new DonationRepository(this.DbContext, this.MemoryCache, this.TelemetryClient)
                        .CreateCreditCardPaymnet(newDonation, easyPayId, transactionKey, null, dateTime, status.ToString());
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
        /// <param name="frequency">Subscription Frequency.</param>
        public void CreateSubscription(
            Donation donation,
            string transactionKey,
            string easyPayId,
            string url,
            WebUser user,
            PaymentSubscription.FrequencyEnum frequency)
        {
            if (donation != null && !string.IsNullOrEmpty(transactionKey) && !string.IsNullOrEmpty(url))
            {
                Subscription value = new Subscription()
                {
                    Created = DateTime.UtcNow,
                    StartTime = DateTime.UtcNow.AddDays(1),
                    ExpirationTime = DateTime.UtcNow.AddYears(1),
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
                    .Where(p => p.User.Id == user.Id)
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
    }
}
