// -----------------------------------------------------------------------
// <copyright file="UserRepository.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System;
    using System.Linq;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using Microsoft.ApplicationInsights;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Caching.Memory;

    /// <summary>
    /// Default implementation for the <see cref="WebUser"/> repository pattern.
    /// </summary>
    public class UserRepository : GenericRepository<WebUser>
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
            else
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

                result.CompanyName = companyName;
                if (result.EmailConfirmed)
                {
                    result.Nif = nif;
                }
            }

            this.DbContext.SaveChanges();

            return result;
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
            foreach (var item in this.DbContext.WebUser.ToList())
            {
                this.DeleteUserAndDonations(item.Id);
                this.DbContext.WebUser.Remove(item);
                this.DbContext.SaveChanges();
            }
        }

        /// <summary>
        /// Delete a single user.
        /// </summary>
        /// <param name="id">User id.</param>
        public void DeleteUserAndDonations(string id)
        {
            if (!string.IsNullOrEmpty(id))
            {
                WebUser user = this.DbContext.WebUser.Where(p => p.Id == id).FirstOrDefault();

                var invoices = this.DbContext.Invoices.Where(p => p.User == user).ToList();
                this.DbContext.Invoices.RemoveRange(invoices);
                this.DbContext.SaveChanges();

                // var paypalPayments = this.DbContext.PayPalPayments.Where(p => p.Donation.User.Id == id).ToList();
                // this.DbContext.PayPalPayments.RemoveRange(paypalPayments);
                // this.DbContext.SaveChanges();

                // var donations = this.DbContext.Donations.Include(p => p.DonationItems).Where(p => p.User == user).ToList();
                // this.DbContext.Donations.RemoveRange(donations);
                // this.DbContext.SaveChanges();

                // this.DbContext.WebUser.Remove(user);
                // this.DbContext.SaveChanges();
            }
        }
    }
}
