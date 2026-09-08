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
    using Microsoft.Data.SqlClient;
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
        /// <param name="subject">Email subject.</param>
        public void AddEmailNotification(WebUser user, BasePayment payment, string subject = null)
        {
            this.TryAddEmailNotification(user, payment, subject);
        }

        /// <summary>
        /// Claims the email notification for a payment before sending it.
        /// </summary>
        /// <param name="user">User receiving the notification.</param>
        /// <param name="payment">Payment associated with the notification.</param>
        /// <param name="subject">Email subject.</param>
        /// <returns>True when this call created the notification claim.</returns>
        public bool TryAddEmailNotification(WebUser user, BasePayment payment, string subject = null)
        {
            if (user == null || payment == null || this.EmailNotificationExits(payment.Id))
            {
                return false;
            }

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

            PaymentNotifications notification = new PaymentNotifications()
            {
                Created = DateTime.UtcNow,
                NotificationType = NotificationType.Email,
                Subject = subject,
                User = user,
                Payment = payment,
            };
            this.DbContext.PaymentNotifications.Add(notification);

            try
            {
                this.DbContext.SaveChanges();
                return true;
            }
            catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
            {
                this.DbContext.Entry(notification).State = EntityState.Detached;
                return false;
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

            DateTime oldestReminderDate = DateTime.UtcNow.AddDays(-6);
            DateTime newestReminderDate = DateTime.UtcNow.AddDays(-3);
            List<MultiBankPayment> candidatePayments = this.DbContext.MultiBankPayments
                .Include(p => p.Donation)
                .Where(p => p.Created >= oldestReminderDate &&
                            p.Created <= newestReminderDate)
                .ToList();
            foreach (var payment in candidatePayments)
            {
                if (!DonationPaymentCompletion.IsAwaitingMultiBankPayment(payment.Donation, payment))
                {
                    continue;
                }

                if (!this.EmailNotificationExits(payment.Id))
                {
                    result.Add(payment);
                }
            }

            return result;
        }

        private static bool IsUniqueConstraintViolation(DbUpdateException exception)
        {
            return exception.InnerException is SqlException sqlException
                && (sqlException.Number == 2601 || sqlException.Number == 2627);
        }
    }
}
