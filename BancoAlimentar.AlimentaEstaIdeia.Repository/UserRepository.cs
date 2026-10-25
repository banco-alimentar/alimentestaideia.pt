// -----------------------------------------------------------------------
// <copyright file="UserRepository.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System;
    using System.Linq;
    using BancoAlimentar.AlimentaEstaIdeia.Common.Repository.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using Microsoft.ApplicationInsights;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Caching.Memory;

    /// <summary>
    /// Default implementation for the <see cref="WebUser"/> repository pattern.
    /// </summary>
    public class UserRepository : GenericRepository<WebUser, ApplicationDbContext>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UserRepository"/> class.
        /// </summary>
        /// <param name="context"><see cref="ApplicationDbContext"/> instance.</param>
        /// <param name="memoryCache">A reference to the Memory cache system.</param>
        /// <param name="telemetryClient">Telemetry Client.</param>
        public UserRepository(ApplicationDbContext context, IMemoryCache memoryCache, TelemetryClient telemetryClient)
            : base(context, memoryCache, telemetryClient)
        {
        }

        /// <summary>
        /// Finds or create a new <see cref="WebUser"/> based on the email.
        /// </summary>
        /// <param name="email">Email for the user.</param>
        /// <param name="companyName">Company name for the user.</param>
        /// <param name="nif">Nif for the user.</param>
        /// <param name="fullName">Full name of the user.</param>
        /// <param name="donorAddress">A reference to the <see cref="DonorAddress"/>.</param>
        /// <returns>A reference to the <see cref="WebUser"/>.</returns>
        public WebUser FindOrCreateWebUser(string email, string companyName, string nif, string fullName, DonorAddress donorAddress)
        {
            string normalizedEmail = email.Normalize().ToUpperInvariant();

            WebUser result = this.DbContext.WebUser
                .Include(p => p.Address)
                .Where(p => p.NormalizedEmail == normalizedEmail)
                .FirstOrDefault();

            if (result == null)
            {
                result = new WebUser()
                {
                    Address = donorAddress,
                    UserName = email,
                    Email = email,
                    NormalizedEmail = normalizedEmail,
                    EmailConfirmed = false,
                    CompanyName = companyName,
                    Nif = nif,
                    FullName = fullName,
                    IsAnonymous = true,
                };

                this.DbContext.WebUser.Add(result);
            }

            this.DbContext.SaveChanges();
            return result;
        }

        /// <summary>
        /// Updates the personal data, gathered during the donation, for the anonymous user.
        /// </summary>
        /// <param name="email">Email address of the user.</param>
        /// <param name="companyName">Company name.</param>
        /// <param name="nif">Nif.</param>
        /// <param name="fullName">Full name.</param>
        /// <param name="donorAddress">User address.</param>
        public void UpdateAnonymousUserData(string email, string companyName, string nif, string fullName, DonorAddress donorAddress)
        {
            string normalizedEmail = email.Normalize().ToUpperInvariant();

            WebUser result = this.DbContext.WebUser
                .Include(p => p.Address)
                .Where(p => p.NormalizedEmail == normalizedEmail)
                .FirstOrDefault();

            if (result != null)
            {
                if (donorAddress != null)
                {
                    if (result.Address == null)
                    {
                        result.Address = new DonorAddress();
                    }

                    result.Address.Address1 = donorAddress.Address1;
                    result.Address.Address2 = donorAddress.Address2;
                    result.Address.City = donorAddress.City;
                    result.Address.PostalCode = donorAddress.PostalCode;
                    result.Address.Country = donorAddress.Country;
                }

                result.FullName = fullName;
                result.CompanyName = companyName;
                if (result.EmailConfirmed)
                {
                    result.Nif = nif;
                }

                this.DbContext.SaveChanges();
            }
        }

        /// <summary>
        /// Gets the Anonymous user.
        /// </summary>
        /// <returns>A reference to the <see cref="WebUser"/>.</returns>
        public WebUser GetAnonymousUser()
        {
            string zeroId = default(Guid).ToString();
            return this.DbContext.WebUser.Where(p => p.Id == zeroId).FirstOrDefault();
        }

        /// <summary>
        /// Find the <see cref="WebUser"/> by the ID.
        /// </summary>
        /// <param name="id">A <see cref="string"/> with the user id.</param>
        /// <returns>A reference to the <see cref="WebUser"/>.</returns>
        public WebUser FindUserById(string id)
        {
            WebUser result = null;

            if (!string.IsNullOrEmpty(id))
            {
                result = this.DbContext.WebUser
                    .Include(p => p.Address)
                    .Where(p => p.Id == id)
                    .FirstOrDefault();
            }

            return result;
        }

        /// <summary>
        /// Deleted all users.
        /// </summary>
        public void DeleteAllUsers()
        {
            foreach (string userId in this.DbContext.WebUser.Select(user => user.Id).ToList())
            {
                if (!this.DeleteUserAndDonations(userId))
                {
                    throw new InvalidOperationException($"User '{userId}' could not be deleted.");
                }
            }
        }

        /// <summary>
        /// Deletes a user and removes or anonymizes repository-owned personal data.
        /// </summary>
        /// <param name="id">User id.</param>
        /// <returns><see langword="true"/> when the user was deleted; otherwise, <see langword="false"/>.</returns>
        public bool DeleteUserAndDonations(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return false;
            }

            var strategy = this.DbContext.Database.CreateExecutionStrategy();
            return strategy.Execute(() =>
            {
                using var transaction = this.DbContext.Database.BeginTransaction();
                WebUser user = this.DbContext.WebUser
                    .Include(value => value.Address)
                    .FirstOrDefault(value => value.Id == id);
                if (user == null)
                {
                    return false;
                }

                if (this.DbContext.Subscriptions.Any(subscription =>
                        subscription.User.Id == id
                        && !subscription.IsDeleted
                        && (subscription.Status == SubscriptionStatus.Capture
                            || subscription.Status == SubscriptionStatus.Active)))
                {
                    return false;
                }

                DonorAddress address = user.Address;
                var userPayments = this.DbContext.Payments
                    .Where(payment => payment.Donation.User.Id == id)
                    .ToList();
                this.DbContext.Invoices.RemoveRange(
                    this.DbContext.Invoices.Where(invoice => invoice.User.Id == id));
                this.DbContext.PaymentNotifications.RemoveRange(
                    this.DbContext.PaymentNotifications.Where(notification => notification.User.Id == id));
                this.DbContext.EmailCommunications.RemoveRange(
                    this.DbContext.EmailCommunications.Where(communication => communication.UserId == id));

                foreach (Donation donation in this.DbContext.Donations.Where(value => value.User.Id == id))
                {
                    donation.User = null;
                    donation.Nif = null;
                    donation.WantsReceipt = null;
                    donation.ServiceEntity = null;
                    donation.ServiceReference = null;
                }

                foreach (BasePayment payment in userPayments)
                {
                    if (payment is MBWayPayment mbWayPayment)
                    {
                        mbWayPayment.Alias = null;
                    }
                    else if (payment is PayPalPayment payPalPayment)
                    {
                        payPalPayment.PayerId = null;
                        payPalPayment.Token = null;
                    }
                    else if (payment is CreditCardPayment creditCardPayment)
                    {
                        creditCardPayment.Url = null;
                    }
                }

                foreach (Subscription subscription in this.DbContext.Subscriptions.Where(value => value.User.Id == id))
                {
                    subscription.User = null;
                    subscription.Status = SubscriptionStatus.Inactive;
                    subscription.Url = null;
                }

                var referrals = this.DbContext.Referrals
                    .Where(referral => referral.User.Id == id)
                    .ToList();
                if (referrals.Count > 0)
                {
                    var referralIds = referrals.Select(referral => referral.Id).ToList();
                    foreach (Donation donation in this.DbContext.Donations
                                 .Where(value => referralIds.Contains(EF.Property<int?>(value, "ReferralId").Value)))
                    {
                        donation.ReferralEntity = null;
                    }

                    this.DbContext.Referrals.RemoveRange(referrals);
                }

                user.Address = null;
                this.DbContext.WebUser.Remove(user);
                if (address != null
                    && !this.DbContext.WebUser.Any(value => value.Id != id && value.Address == address))
                {
                    this.DbContext.DonorAddresses.Remove(address);
                }

                this.DbContext.SaveChanges();
                transaction.Commit();
                return true;
            });
        }
    }
}
