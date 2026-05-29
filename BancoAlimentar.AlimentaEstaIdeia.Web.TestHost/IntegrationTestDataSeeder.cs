// -----------------------------------------------------------------------
// <copyright file="IntegrationTestDataSeeder.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.TestHost
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model.Strategy;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Seeds data for web integration tests.
    /// </summary>
    public static class IntegrationTestDataSeeder
    {
        /// <summary>
        /// Default tenant used by integration tests (matches in-memory infrastructure seed).
        /// </summary>
        /// <returns>Tenant instance.</returns>
        public static Tenant CreateDefaultTenant()
        {
            return new Tenant
            {
                Created = DateTime.UtcNow,
                Domains = new List<DomainIdentifier>
                {
                    new DomainIdentifier
                    {
                        Created = DateTime.UtcNow,
                        DomainName = "localhost",
                        Environment = "Testing",
                    },
                },
                Id = 1,
                Name = "alimentestaideia",
                InvoicingStrategy = InvoicingStrategy.SingleInvoiceTable,
                PaymentStrategy = PaymentStrategy.SharedPaymentProcessor,
                PublicId = Guid.NewGuid(),
            };
        }

        /// <summary>
        /// Creates or returns an admin user for admin-area integration tests.
        /// </summary>
        /// <param name="services">Application services.</param>
        /// <param name="email">User email.</param>
        /// <param name="password">User password.</param>
        /// <returns>The admin user.</returns>
        public static async Task<WebUser> EnsureAdminUserAsync(IServiceProvider services, string email, string password)
        {
            var userManager = services.GetRequiredService<UserManager<WebUser>>();
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new WebUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    FullName = "Integration Admin",
                };
                var result = await userManager.CreateAsync(user, password);
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }

            if (!await userManager.IsInRoleAsync(user, UserRoles.Admin.ToString()))
            {
                await userManager.AddToRoleAsync(user, UserRoles.Admin.ToString());
            }

            return user;
        }

        /// <summary>
        /// Creates a confirmed user for authenticated page tests.
        /// </summary>
        /// <param name="services">Application services.</param>
        /// <param name="email">User email.</param>
        /// <param name="password">User password.</param>
        /// <returns>The user.</returns>
        public static async Task<WebUser> EnsureUserAsync(IServiceProvider services, string email, string password)
        {
            var userManager = services.GetRequiredService<UserManager<WebUser>>();
            var user = await userManager.FindByEmailAsync(email);
            if (user != null)
            {
                return user;
            }

            user = new WebUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = "Integration User",
            };
            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            return user;
        }

        /// <summary>
        /// Seeds a paid donation without an invoice (for claim-invoice flows).
        /// </summary>
        /// <param name="services">Application services.</param>
        /// <param name="publicId">Donation public identifier.</param>
        /// <param name="wantsReceipt">Whether the donation already wants a receipt.</param>
        /// <returns>The created donation.</returns>
        public static async Task<Donation> SeedPaidDonationWithoutInvoiceAsync(
            IServiceProvider services,
            Guid publicId,
            bool wantsReceipt = false)
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var email = $"claim-{publicId:N}@integration.test";
            var user = await EnsureUserAsync(services, email, "Test@12345!");
            await EnsureUserHasAddressAsync(context, user);

            var foodBank = await context.FoodBanks.FirstAsync();
            var product = await context.ProductCatalogues.FirstAsync();
            var confirmedPayment = new CreditCardPayment
            {
                Created = DateTime.UtcNow,
                TransactionKey = Guid.NewGuid().ToString(),
                Url = "https://example.com",
                Status = "ok",
            };

            var donation = new Donation
            {
                PublicId = publicId,
                DonationAmount = 5,
                DonationDate = DateTime.UtcNow,
                FoodBank = foodBank,
                User = user,
                Nif = "196807050",
                PaymentStatus = PaymentStatus.Payed,
                WantsReceipt = wantsReceipt,
                ConfirmedPayment = confirmedPayment,
                PaymentList = new List<BasePayment> { confirmedPayment },
                DonationItems = new List<DonationItem>
                {
                    new DonationItem
                    {
                        ProductCatalogue = product,
                        Quantity = 1,
                        Price = product.Cost,
                    },
                },
            };
            donation.DonationItems.First().Donation = donation;
            confirmedPayment.Donation = donation;
            context.Donations.Add(donation);
            await context.SaveChangesAsync();
            return donation;
        }

        /// <summary>
        /// Creates an invoice for an existing paid donation.
        /// </summary>
        /// <param name="services">Application services.</param>
        /// <param name="donation">Paid donation.</param>
        /// <returns>The invoice.</returns>
        public static Invoice CreateInvoiceForDonation(IServiceProvider services, Donation donation)
        {
            var invoiceRepository = services.GetRequiredService<InvoiceRepository>();
            var user = donation.User;
            return invoiceRepository.GetOrCreateInvoiceByDonation(
                donation.Id,
                user,
                CreateDefaultTenant(),
                out InvoiceStatusResult _);
        }

        private static async Task EnsureUserHasAddressAsync(ApplicationDbContext context, WebUser user)
        {
            if (user.Address == null || string.IsNullOrEmpty(user.Address.Address1))
            {
                user.Address ??= new DonorAddress();
                user.Address.Address1 = "Rua Teste";
                user.Address.City = "Lisboa";
                user.Address.Country = "PT";
                user.Address.PostalCode = "1000-001";
                context.Update(user);
                await context.SaveChangesAsync();
            }
        }
    }
}
