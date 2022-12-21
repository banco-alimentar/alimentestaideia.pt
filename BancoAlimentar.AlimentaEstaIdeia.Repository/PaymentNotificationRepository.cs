// -----------------------------------------------------------------------
// <copyright file="PaymentNotificationRepository.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BancoAlimentar.AlimentaEstaIdeia.Common.Repository.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using Microsoft.ApplicationInsights;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Caching.Memory;

    /// <summary>
    /// Default implementation for the <see cref="PaymentNotifications"/> repository pattern.
    /// </summary>
    public class PaymentNotificationRepository : GenericRepository<PaymentNotifications, ApplicationDbContext>
    {
        private const string EmptyAddress = "NO-ADDRESS";

        /// <summary>
        /// Initializes a new instance of the <see cref="PaymentNotificationRepository"/> class.
        /// </summary>
        /// <param name="context"><see cref="ApplicationDbContext"/> instance.</param>
        /// <param name="memoryCache">A reference to the Memory cache system.</param>
        /// <param name="telemetryClient">Telemetry Client.</param>
        public PaymentNotificationRepository(ApplicationDbContext context, IMemoryCache memoryCache, TelemetryClient telemetryClient)
            : base(context, memoryCache, telemetryClient)
        {
        }

        /// <summary>
        /// Checks if there is already an email notification for the payment.
        /// </summary>
        /// <param name="paymentId">Payment id.</param>
        /// <returns>True if the notification exits, false otherwise.</returns>
        public bool EmailNotificationExits(int paymentId)
        {
            return this.DbContext.PaymentNotifications
                .Where(p => p.Payment.Id == paymentId && p.NotificationType == NotificationType.Email)
                .FirstOrDefault() != null;
        }

        /// <summary>
        /// Adds the payment notification to the database.
        /// </summary>
        /// <param name="user">User.</param>
        /// <param name="payment">Payment.</param>
        public void AddEmailNotification(WebUser user, BasePayment payment)
        {
            if (user != null && payment != null)
            {
                if (user.Address == null)
                {
                    user.Address = new DonorAddress()
                    {
                        Address1 = EmptyAddress,
                    };
                }
                else if (string.IsNullOrEmpty(user.Address.Address1))
                {
                    user.Address.Address1 = EmptyAddress;
                }

                this.DbContext.PaymentNotifications.Add(new PaymentNotifications()
                {
                    Created = DateTime.UtcNow,
                    NotificationType = NotificationType.Email,
                    User = user,
                    Payment = payment,
                });

                this.DbContext.SaveChanges();
            }
        }

        /// <summary>
        /// Gets the all the <see cref="MultiBankPayment"/> since the last 24 hours that doesn't have an
        /// email notification yet.
        /// </summary>
        /// <returns>The collection of <see cref="List{MultiBankPayment}"/>.</returns>
        public List<MultiBankPayment> GetMultiBankPaymentsSinceLast3DaysWithoutEmailNotifications()
        {
            List<MultiBankPayment> result = new List<MultiBankPayment>();

            List<MultiBankPayment> notPaidMultibanco = this.DbContext.MultiBankPayments
                .Where(p => EF.Functions.DateDiffDay(p.Created, DateTime.UtcNow) >= 3 &&
                            EF.Functions.DateDiffDay(p.Created, DateTime.UtcNow) <= 6 && p.Status == null)
                .ToList();
            foreach (var payment in notPaidMultibanco)
            {
                if (!this.EmailNotificationExits(payment.Id))
                {
                    result.Add(payment);
                }
            }

            return result;
        }
    }
}
