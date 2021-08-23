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
        public SubscriptionRepository(ApplicationDbContext context, IMemoryCache memoryCache)
            : base(context, memoryCache)
        {
        }

        /// <summary>
        /// Complete the creation on the subscription from the easyapy API.
        /// </summary>
        /// <param name="transactionKey">Easypay transaction id.</param>
        /// <param name="status">Status of the subscription_creationg operation.</param>
        public void CompleteSubcriptionCreate(string transactionKey, GenericNotificationRequest.StatusEnum status)
        {
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
                }
            }
        }

        /// <summary>
        /// New subscription capture process happen from easypay. Donation has to be created.
        /// </summary>
        /// <param name="transactionKey">Easypay transaction id.</param>
        /// <param name="status">Capture status.</param>
        public void SubscriptionCapture(string transactionKey, GenericNotificationRequest.StatusEnum status)
        {
            if (!string.IsNullOrEmpty(transactionKey))
            {
                Subscription value = this.DbContext.Subscriptions
                    .Include(p => p.InitialDonation)
                    .Where(p => p.EasyPayTransactionId == transactionKey)
                    .FirstOrDefault();
                if (value != null)
                {
                    Donation donation = new DonationRepository(
                        this.DbContext,
                        this.MemoryCache)
                        .GetFullDonationById(value.InitialDonation.Id);
                    Donation newDonation = new Donation()
                    {
                        DonationAmount = donation.DonationAmount,
                        FoodBank = donation.FoodBank,
                        Nif = donation.Nif,
                        PaymentStatus = PaymentStatus.Payed,
                        Referral = donation.Referral,
                        PublicId = Guid.NewGuid(),
                        DonationDate = DateTime.UtcNow,
                        DonationItems = new List<DonationItem>(),
                        User = donation.User,
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
                }
            }
        }

        /// <summary>
        /// Create a subscription.
        /// </summary>
        /// <param name="donation">Initial <see cref="Donation"/> that trigger the subscription.</param>
        /// <param name="transactionKey">Transaction key.</param>
        /// <param name="easyPayId">Easy pay id.</param>
        /// <param name="url">Payment url.</param>
        /// <param name="user">The current user.</param>
        /// <param name="frequency">Subscription frecuency.</param>
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
                };

                WebUserSubscriptions webUserSubscriptions = new WebUserSubscriptions()
                {
                    Subscription = value,
                    User = user,
                };

                SubscriptionDonations subscriptionDonations = new SubscriptionDonations()
                {
                    Donation = donation,
                    Subscription = value,
                };

                this.DbContext.SubscriptionDonations.Add(subscriptionDonations);
                this.DbContext.UsersSubscriptions.Add(webUserSubscriptions);
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
                result = this.DbContext.UsersSubscriptions
                    .Include(p => p.Subscription.InitialDonation)
                    .Where(p => p.User.Id == user.Id)
                    .Select(p => p.Subscription)
                    .ToList();
            }

            return result;
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
        /// Gets the list of donations for a particular subscription.
        /// </summary>
        /// <param name="id">Subscription id.</param>
        /// <returns>A collection of <see cref="List{Donation}"/>.</returns>
        public List<Donation> GetDonationsForSubscription(int id)
        {
            return this.DbContext.SubscriptionDonations
                .Where(p => p.Subscription.Id == id)
                .Select(p => p.Donation)
                .ToList();
        }

        private Donation ResetDonationObject(Donation value)
        {
            value.Id = 0;
            if (value.ReferralEntity != null)
            {
                value.ReferralEntity.Id = 0;
            }

            value.DonationDate = DateTime.UtcNow;
            foreach (var donationItem in value.DonationItems)
            {
                donationItem.Id = 0;
                donationItem.Donation = value;
            }

            return value;
        }
    }
}
