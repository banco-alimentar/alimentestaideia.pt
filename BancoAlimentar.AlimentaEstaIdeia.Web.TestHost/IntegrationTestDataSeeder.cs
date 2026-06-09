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

            await EnsureUserInRoleAsync(services, user, UserRoles.Admin.ToString());

            return user;
        }

        /// <summary>
        /// Creates or returns a super-admin user for RoleArea admin integration tests.
        /// </summary>
        /// <param name="services">Application services.</param>
        /// <param name="email">User email.</param>
        /// <param name="password">User password.</param>
        /// <returns>The super-admin user.</returns>
        public static async Task<WebUser> EnsureSuperAdminUserAsync(IServiceProvider services, string email, string password)
        {
            var user = await EnsureAdminUserAsync(services, email, password);
            await EnsureUserInRoleAsync(services, user, UserRoles.SuperAdmin.ToString());

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
            var user = await EnsureUserAsync(services, email, IntegrationTestCredentials.DefaultPassword);
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
        /// Seeds a donation with a pending credit-card payment for webhook tests.
        /// </summary>
        /// <param name="services">Application services.</param>
        /// <param name="publicId">Donation public identifier.</param>
        /// <returns>Seed metadata including transaction key.</returns>
        public static async Task<PendingDonationSeed> SeedPendingDonationWithCreditCardAsync(
            IServiceProvider services,
            Guid publicId)
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var email = $"webhook-cc-{publicId:N}@integration.test";
            var user = await EnsureUserAsync(services, email, IntegrationTestCredentials.DefaultPassword);
            await EnsureUserHasAddressAsync(context, user);

            var foodBank = await context.FoodBanks.FirstAsync();
            var product = await context.ProductCatalogues.FirstAsync();
            var transactionKey = Guid.NewGuid().ToString();
            var easyPayId = Guid.NewGuid().ToString();

            var donation = new Donation
            {
                PublicId = publicId,
                DonationAmount = 5,
                DonationDate = DateTime.UtcNow,
                FoodBank = foodBank,
                User = user,
                PaymentStatus = PaymentStatus.WaitingPayment,
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
            context.Donations.Add(donation);
            await context.SaveChangesAsync();

            var nextPaymentId = (await context.Payments.MaxAsync(p => (int?)p.Id) ?? 0) + 1;
            var payment = new CreditCardPayment
            {
                Id = nextPaymentId,
                Created = DateTime.UtcNow,
                TransactionKey = transactionKey,
                Url = "https://example.com/pay",
                EasyPayPaymentId = easyPayId,
                Donation = donation,
            };
            donation.PaymentList = new List<BasePayment> { payment };
            context.Payments.Add(payment);
            await context.SaveChangesAsync();

            return new PendingDonationSeed
            {
                Donation = donation,
                TransactionKey = transactionKey,
                EasyPayId = easyPayId,
                PaymentId = payment.Id,
            };
        }

        /// <summary>
        /// Seeds a donation with a pending multibanco payment for webhook tests.
        /// </summary>
        /// <param name="services">Application services.</param>
        /// <param name="publicId">Donation public identifier.</param>
        /// <returns>Seed metadata including transaction key.</returns>
        public static async Task<PendingDonationSeed> SeedPendingDonationWithMultiBankAsync(
            IServiceProvider services,
            Guid publicId)
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var email = $"webhook-mb-{publicId:N}@integration.test";
            var user = await EnsureUserAsync(services, email, IntegrationTestCredentials.DefaultPassword);
            await EnsureUserHasAddressAsync(context, user);

            var foodBank = await context.FoodBanks.FirstAsync();
            var product = await context.ProductCatalogues.FirstAsync();
            var transactionKey = Guid.NewGuid().ToString();
            var easyPayId = Guid.NewGuid().ToString();

            var donation = new Donation
            {
                PublicId = publicId,
                DonationAmount = 5,
                DonationDate = DateTime.UtcNow,
                FoodBank = foodBank,
                User = user,
                PaymentStatus = PaymentStatus.WaitingPayment,
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
            context.Donations.Add(donation);
            await context.SaveChangesAsync();

            var nextPaymentId = (await context.Payments.MaxAsync(p => (int?)p.Id) ?? 0) + 1;
            var payment = new MultiBankPayment
            {
                Id = nextPaymentId,
                Created = DateTime.UtcNow,
                TransactionKey = transactionKey,
                EasyPayPaymentId = easyPayId,
                Donation = donation,
            };
            donation.ServiceEntity = "12345";
            donation.ServiceReference = "123456789";
            donation.PaymentList = new List<BasePayment> { payment };
            context.Payments.Add(payment);
            await context.SaveChangesAsync();

            return new PendingDonationSeed
            {
                Donation = donation,
                TransactionKey = transactionKey,
                EasyPayId = easyPayId,
                PaymentId = payment.Id,
            };
        }

        /// <summary>
        /// Seeds a donation with a pending MBWay payment for webhook tests.
        /// </summary>
        /// <param name="services">Application services.</param>
        /// <param name="publicId">Donation public identifier.</param>
        /// <returns>Seed metadata including transaction key.</returns>
        public static async Task<PendingDonationSeed> SeedPendingDonationWithMBWayAsync(
            IServiceProvider services,
            Guid publicId)
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var email = $"webhook-mbw-{publicId:N}@integration.test";
            var user = await EnsureUserAsync(services, email, IntegrationTestCredentials.DefaultPassword);
            await EnsureUserHasAddressAsync(context, user);

            var foodBank = await context.FoodBanks.FirstAsync();
            var product = await context.ProductCatalogues.FirstAsync();
            var transactionKey = Guid.NewGuid().ToString();
            var easyPayId = Guid.NewGuid().ToString();

            var donation = new Donation
            {
                PublicId = publicId,
                DonationAmount = 5,
                DonationDate = DateTime.UtcNow,
                FoodBank = foodBank,
                User = user,
                PaymentStatus = PaymentStatus.WaitingPayment,
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
            context.Donations.Add(donation);
            await context.SaveChangesAsync();

            var nextPaymentId = (await context.Payments.MaxAsync(p => (int?)p.Id) ?? 0) + 1;
            var payment = new MBWayPayment
            {
                Id = nextPaymentId,
                Created = DateTime.UtcNow,
                TransactionKey = transactionKey,
                EasyPayPaymentId = easyPayId,
                Alias = "912345678",
                Donation = donation,
            };
            donation.PaymentList = new List<BasePayment> { payment };
            context.Payments.Add(payment);
            await context.SaveChangesAsync();

            return new PendingDonationSeed
            {
                Donation = donation,
                TransactionKey = transactionKey,
                EasyPayId = easyPayId,
                PaymentId = payment.Id,
            };
        }

        /// <summary>
        /// Seeds an active referral campaign for donation flow tests.
        /// </summary>
        /// <param name="services">Application services.</param>
        /// <param name="code">Optional referral code; generated when null.</param>
        /// <returns>Referral seed metadata.</returns>
        public static async Task<ReferralSeed> SeedActiveReferralAsync(
            IServiceProvider services,
            string code = null)
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var ownerEmail = $"referral-owner-{Guid.NewGuid():N}@integration.test";
            var owner = await EnsureUserAsync(services, ownerEmail, IntegrationTestCredentials.DefaultPassword);
            var referralCode = (code ?? $"int-{Guid.NewGuid():N}".Substring(0, 16)).ToLowerInvariant();
            var referral = new Referral
            {
                Code = referralCode,
                Active = true,
                IsPublic = true,
                Name = "Integration Referral Campaign",
                CreateDate = DateTime.UtcNow,
                User = owner,
            };
            context.Referrals.Add(referral);
            await context.SaveChangesAsync();

            return new ReferralSeed
            {
                ReferralId = referral.Id,
                Code = referralCode,
            };
        }

        /// <summary>
        /// Seeds a subscription awaiting Easypay subscription_create confirmation.
        /// </summary>
        /// <param name="services">Application services.</param>
        /// <param name="transactionKey">Optional transaction key; generated when null.</param>
        /// <returns>Subscription seed metadata.</returns>
        public static async Task<SubscriptionSeed> SeedCreatedSubscriptionAsync(
            IServiceProvider services,
            string transactionKey = null)
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var email = $"webhook-sub-{Guid.NewGuid():N}@integration.test";
            var user = await EnsureUserAsync(services, email, IntegrationTestCredentials.DefaultPassword);
            await EnsureUserHasAddressAsync(context, user);

            var foodBank = await context.FoodBanks.FirstAsync();
            var product = await context.ProductCatalogues.FirstAsync();
            var key = transactionKey ?? Guid.NewGuid().ToString();
            var easyPaySubscriptionId = Guid.NewGuid().ToString();
            var donation = new Donation
            {
                PublicId = Guid.NewGuid(),
                DonationAmount = 5,
                DonationDate = DateTime.UtcNow,
                FoodBank = foodBank,
                User = user,
                PaymentStatus = PaymentStatus.Payed,
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
            context.Donations.Add(donation);
            await context.SaveChangesAsync();

            var subscription = new Subscription
            {
                Created = DateTime.UtcNow,
                StartTime = DateTime.UtcNow,
                ExpirationTime = DateTime.UtcNow.AddYears(1),
                TransactionKey = key,
                EasyPaySubscriptionId = easyPaySubscriptionId,
                Url = "https://example.com/subscription",
                Status = SubscriptionStatus.Created,
                PublicId = Guid.NewGuid(),
                Frequency = "1M",
                InitialDonation = donation,
                User = user,
            };
            context.Subscriptions.Add(subscription);
            context.SubscriptionDonations.Add(new SubscriptionDonations
            {
                Donation = donation,
                Subscription = subscription,
            });
            await context.SaveChangesAsync();

            return new SubscriptionSeed
            {
                SubscriptionId = subscription.Id,
                TransactionKey = key,
                EasyPaySubscriptionId = easyPaySubscriptionId,
                InitialDonationId = donation.Id,
            };
        }

        /// <summary>
        /// Seeds an active subscription ready for a first recurring capture (no capture payment yet).
        /// </summary>
        /// <param name="services">Application services.</param>
        /// <param name="captureDate">Date of the recurring capture.</param>
        /// <returns>Seed metadata for recurring subscription_capture webhook tests.</returns>
        public static async Task<SubscriptionRecurringCaptureSeed> SeedActiveSubscriptionForRecurringCaptureAsync(
            IServiceProvider services,
            DateTime captureDate)
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var email = $"webhook-sub-rec-{Guid.NewGuid():N}@integration.test";
            var user = await EnsureUserAsync(services, email, IntegrationTestCredentials.DefaultPassword);
            await EnsureUserHasAddressAsync(context, user);

            var foodBank = await context.FoodBanks.FirstAsync();
            var product = await context.ProductCatalogues.FirstAsync();
            var transactionKey = Guid.NewGuid().ToString();
            var easyPayId = Guid.NewGuid();
            var initialDonationDate = captureDate.AddDays(-3);

            var initialDonation = new Donation
            {
                PublicId = Guid.NewGuid(),
                DonationAmount = 5,
                DonationDate = initialDonationDate,
                FoodBank = foodBank,
                User = user,
                PaymentStatus = PaymentStatus.Payed,
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
            initialDonation.DonationItems.First().Donation = initialDonation;
            context.Donations.Add(initialDonation);
            await context.SaveChangesAsync();

            var subscription = new Subscription
            {
                Created = DateTime.UtcNow,
                StartTime = DateTime.UtcNow,
                ExpirationTime = DateTime.UtcNow.AddYears(1),
                TransactionKey = transactionKey,
                EasyPaySubscriptionId = easyPayId.ToString(),
                Url = "https://example.com/subscription",
                Status = SubscriptionStatus.Active,
                PublicId = Guid.NewGuid(),
                Frequency = "1M",
                InitialDonation = initialDonation,
                User = user,
            };
            context.Subscriptions.Add(subscription);
            context.SubscriptionDonations.Add(new SubscriptionDonations
            {
                Donation = initialDonation,
                Subscription = subscription,
            });
            await context.SaveChangesAsync();

            return new SubscriptionRecurringCaptureSeed
            {
                SubscriptionId = subscription.Id,
                TransactionKey = transactionKey,
                EasyPayId = easyPayId,
                InitialDonationId = initialDonation.Id,
            };
        }

        /// <summary>
        /// Seeds an active subscription owned by the given user (for manage/delete integration tests).
        /// </summary>
        /// <param name="services">Application services.</param>
        /// <param name="email">Owner email (created if missing).</param>
        /// <param name="password">Owner password when creating the user.</param>
        /// <returns>Seed metadata for subscription management tests.</returns>
        public static async Task<ActiveSubscriptionSeed> SeedActiveSubscriptionForUserAsync(
            IServiceProvider services,
            string email,
            string password)
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var user = await EnsureUserAsync(services, email, password);
            var foodBank = await context.FoodBanks.FirstAsync();
            var product = await context.ProductCatalogues.FirstAsync();
            var easyPaySubscriptionId = Guid.NewGuid().ToString();

            var initialDonation = new Donation
            {
                PublicId = Guid.NewGuid(),
                DonationAmount = 5,
                DonationDate = DateTime.UtcNow,
                FoodBank = foodBank,
                User = user,
                PaymentStatus = PaymentStatus.Payed,
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
            initialDonation.DonationItems.First().Donation = initialDonation;
            context.Donations.Add(initialDonation);
            await context.SaveChangesAsync();

            var subscription = new Subscription
            {
                Created = DateTime.UtcNow,
                StartTime = DateTime.UtcNow,
                ExpirationTime = DateTime.UtcNow.AddYears(1),
                TransactionKey = Guid.NewGuid().ToString(),
                EasyPaySubscriptionId = easyPaySubscriptionId,
                Url = "https://example.com/subscription-manage",
                Status = SubscriptionStatus.Active,
                PublicId = Guid.NewGuid(),
                Frequency = "1M",
                InitialDonation = initialDonation,
                User = user,
            };
            context.Subscriptions.Add(subscription);
            context.SubscriptionDonations.Add(new SubscriptionDonations
            {
                Donation = initialDonation,
                Subscription = subscription,
            });
            await context.SaveChangesAsync();

            return new ActiveSubscriptionSeed
            {
                SubscriptionId = subscription.Id,
                EasyPaySubscriptionId = easyPaySubscriptionId,
                UserId = user.Id,
            };
        }

        /// <summary>
        /// Seeds an active subscription with a capture payment on a later date.
        /// </summary>
        /// <param name="services">Application services.</param>
        /// <param name="captureDate">Date of the recurring capture payment.</param>
        /// <returns>Seed metadata for subscription_capture webhook tests.</returns>
        public static async Task<SubscriptionCaptureSeed> SeedSubscriptionWithCapturePaymentAsync(
            IServiceProvider services,
            DateTime captureDate)
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var email = $"webhook-sub-cap-{Guid.NewGuid():N}@integration.test";
            var user = await EnsureUserAsync(services, email, IntegrationTestCredentials.DefaultPassword);
            await EnsureUserHasAddressAsync(context, user);

            var foodBank = await context.FoodBanks.FirstAsync();
            var product = await context.ProductCatalogues.FirstAsync();
            var transactionKey = Guid.NewGuid().ToString();
            var easyPayId = Guid.NewGuid();
            var initialDonationDate = captureDate.AddDays(-3);

            var initialDonation = new Donation
            {
                PublicId = Guid.NewGuid(),
                DonationAmount = 5,
                DonationDate = initialDonationDate,
                FoodBank = foodBank,
                User = user,
                PaymentStatus = PaymentStatus.Payed,
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
            initialDonation.DonationItems.First().Donation = initialDonation;
            context.Donations.Add(initialDonation);
            await context.SaveChangesAsync();

            var subscription = new Subscription
            {
                Created = DateTime.UtcNow,
                StartTime = DateTime.UtcNow,
                ExpirationTime = DateTime.UtcNow.AddYears(1),
                TransactionKey = transactionKey,
                EasyPaySubscriptionId = easyPayId.ToString(),
                Url = "https://example.com/subscription",
                Status = SubscriptionStatus.Active,
                PublicId = Guid.NewGuid(),
                Frequency = "1M",
                InitialDonation = initialDonation,
                User = user,
            };
            context.Subscriptions.Add(subscription);
            context.SubscriptionDonations.Add(new SubscriptionDonations
            {
                Donation = initialDonation,
                Subscription = subscription,
            });
            await context.SaveChangesAsync();

            var captureDonation = new Donation
            {
                PublicId = Guid.NewGuid(),
                DonationAmount = 5,
                DonationDate = captureDate,
                FoodBank = foodBank,
                User = user,
                PaymentStatus = PaymentStatus.WaitingPayment,
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
            captureDonation.DonationItems.First().Donation = captureDonation;
            context.Donations.Add(captureDonation);
            await context.SaveChangesAsync();

            var nextPaymentId = (await context.Payments.MaxAsync(p => (int?)p.Id) ?? 0) + 1;
            var payment = new CreditCardPayment
            {
                Id = nextPaymentId,
                Created = captureDate,
                TransactionKey = transactionKey,
                Url = "https://example.com/pay",
                EasyPayPaymentId = easyPayId.ToString(),
                Status = "pending",
                Donation = captureDonation,
            };
            captureDonation.PaymentList = new List<BasePayment> { payment };
            context.Payments.Add(payment);
            await context.SaveChangesAsync();

            return new SubscriptionCaptureSeed
            {
                TransactionKey = transactionKey,
                EasyPayId = easyPayId,
                CaptureDonationId = captureDonation.Id,
                InitialDonationId = initialDonation.Id,
            };
        }

        /// <summary>
        /// Creates an invoice for an existing paid donation.
        /// </summary>
        /// <param name="services">Application services.</param>
        /// <param name="donation">Paid donation.</param>
        /// <returns>The invoice.</returns>
        public static Invoice AttachInvoiceToDonation(IServiceProvider services, Donation donation)
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var trackedDonation = context.Donations
                .Include(d => d.User)
                .First(d => d.Id == donation.Id);
            var nextId = (context.Invoices.Max(i => (int?)i.Id) ?? 0) + 1;
            var invoice = new Invoice
            {
                Id = nextId,
                Created = DateTime.UtcNow,
                Year = DateTime.UtcNow.Year,
                Sequence = nextId,
                Number = $"INT-{nextId}",
                IsCanceled = false,
                Donation = trackedDonation,
                User = trackedDonation.User,
            };
            context.Invoices.Add(invoice);
            context.SaveChanges();
            return invoice;
        }

        private static async Task EnsureUserInRoleAsync(IServiceProvider services, WebUser user, string roleName)
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var normalizedRoleName = roleName.ToUpperInvariant();
            var role = await context.Roles.FirstOrDefaultAsync(r => r.NormalizedName == normalizedRoleName);
            if (role == null)
            {
                return;
            }

            var alreadyInRole = await context.UserRoles.AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == role.Id);
            if (!alreadyInRole)
            {
                context.UserRoles.Add(new ApplicationUserRole
                {
                    UserId = user.Id,
                    RoleId = role.Id,
                });
                await context.SaveChangesAsync();
            }
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

        /// <summary>
        /// Result of seeding a donation awaiting payment completion.
        /// </summary>
        public sealed class PendingDonationSeed
        {
            /// <summary>
            /// Gets or sets the seeded donation.
            /// </summary>
            public Donation Donation { get; set; }

            /// <summary>
            /// Gets or sets the easypay transaction key.
            /// </summary>
            public string TransactionKey { get; set; }

            /// <summary>
            /// Gets or sets the easypay payment identifier.
            /// </summary>
            public string EasyPayId { get; set; }

            /// <summary>
            /// Gets or sets the pending payment identifier.
            /// </summary>
            public int PaymentId { get; set; }
        }

        /// <summary>
        /// Result of seeding an active referral campaign.
        /// </summary>
        public sealed class ReferralSeed
        {
            /// <summary>
            /// Gets or sets the referral identifier.
            /// </summary>
            public int ReferralId { get; set; }

            /// <summary>
            /// Gets or sets the referral code.
            /// </summary>
            public string Code { get; set; }
        }

        /// <summary>
        /// Result of seeding a subscription awaiting creation confirmation.
        /// </summary>
        public sealed class SubscriptionSeed
        {
            /// <summary>
            /// Gets or sets the subscription identifier.
            /// </summary>
            public int SubscriptionId { get; set; }

            /// <summary>
            /// Gets or sets the easypay transaction key.
            /// </summary>
            public string TransactionKey { get; set; }

            /// <summary>
            /// Gets or sets the easypay subscription identifier.
            /// </summary>
            public string EasyPaySubscriptionId { get; set; }

            /// <summary>
            /// Gets or sets the initial donation identifier.
            /// </summary>
            public int InitialDonationId { get; set; }
        }

        /// <summary>
        /// Result of seeding a subscription awaiting a recurring capture donation.
        /// </summary>
        public sealed class SubscriptionRecurringCaptureSeed
        {
            /// <summary>
            /// Gets or sets the subscription identifier.
            /// </summary>
            public int SubscriptionId { get; set; }

            /// <summary>
            /// Gets or sets the easypay transaction key.
            /// </summary>
            public string TransactionKey { get; set; }

            /// <summary>
            /// Gets or sets the easypay payment identifier for the capture.
            /// </summary>
            public Guid EasyPayId { get; set; }

            /// <summary>
            /// Gets or sets the initial donation identifier.
            /// </summary>
            public int InitialDonationId { get; set; }
        }

        /// <summary>
        /// Result of seeding a subscription with a capture payment.
        /// </summary>
        public sealed class SubscriptionCaptureSeed
        {
            /// <summary>
            /// Gets or sets the easypay transaction key.
            /// </summary>
            public string TransactionKey { get; set; }

            /// <summary>
            /// Gets or sets the easypay payment identifier for the capture.
            /// </summary>
            public Guid EasyPayId { get; set; }

            /// <summary>
            /// Gets or sets the capture donation identifier.
            /// </summary>
            public int CaptureDonationId { get; set; }

            /// <summary>
            /// Gets or sets the initial donation identifier.
            /// </summary>
            public int InitialDonationId { get; set; }
        }

        /// <summary>
        /// Result of seeding an active subscription for a known user.
        /// </summary>
        public sealed class ActiveSubscriptionSeed
        {
            /// <summary>
            /// Gets or sets the subscription identifier.
            /// </summary>
            public int SubscriptionId { get; set; }

            /// <summary>
            /// Gets or sets the easypay subscription identifier.
            /// </summary>
            public string EasyPaySubscriptionId { get; set; }

            /// <summary>
            /// Gets or sets the owner user identifier.
            /// </summary>
            public string UserId { get; set; }
        }
    }
}
