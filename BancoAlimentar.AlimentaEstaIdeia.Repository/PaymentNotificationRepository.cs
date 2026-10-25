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
        private static readonly TimeSpan PendingClaimTimeout = TimeSpan.FromMinutes(15);

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
            return this.DbContext.PaymentNotifications.Any(
                notification => notification.Payment.Id == paymentId
                    && notification.NotificationType == NotificationType.Email);
        }

        /// <summary>
        /// Adds the payment notification to the database.
        /// </summary>
        /// <param name="user">User.</param>
        /// <param name="payment">Payment.</param>
        /// <param name="subject">Email subject.</param>
        public void AddEmailNotification(WebUser user, BasePayment payment, string subject = null)
        {
            if (this.TryAddEmailNotification(user, payment, subject))
            {
                this.MarkEmailNotificationSent(payment.Id);
            }
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
            if (user == null || payment == null)
            {
                return false;
            }

            DateTime now = DateTime.UtcNow;
            PaymentNotifications notification = this.DbContext.PaymentNotifications.SingleOrDefault(
                value => value.Payment.Id == payment.Id && value.NotificationType == NotificationType.Email);
            if (notification != null)
            {
                bool activeClaim = notification.DeliveryStatus == PaymentNotificationDeliveryStatus.Pending
                    && notification.LastAttemptAtUtc >= now.Subtract(PendingClaimTimeout);
                if (notification.DeliveryStatus == PaymentNotificationDeliveryStatus.Sent || activeClaim)
                {
                    return false;
                }

                notification.DeliveryStatus = PaymentNotificationDeliveryStatus.Pending;
                notification.LastAttemptAtUtc = now;
                notification.AttemptCount++;
                notification.LastError = null;
                notification.Subject = subject;

                try
                {
                    this.DbContext.SaveChanges();
                    return true;
                }
                catch (DbUpdateConcurrencyException)
                {
                    this.DbContext.Entry(notification).State = EntityState.Detached;
                    return false;
                }
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

            notification = new PaymentNotifications()
            {
                Created = now,
                NotificationType = NotificationType.Email,
                Subject = subject,
                User = user,
                Payment = payment,
                DeliveryStatus = PaymentNotificationDeliveryStatus.Pending,
                LastAttemptAtUtc = now,
                AttemptCount = 1,
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
        /// Marks a claimed email notification as delivered.
        /// </summary>
        /// <param name="paymentId">Payment id.</param>
        public void MarkEmailNotificationSent(int paymentId)
        {
            PaymentNotifications notification = this.GetEmailNotification(paymentId);
            if (notification == null)
            {
                throw new InvalidOperationException($"No email notification claim exists for payment {paymentId}.");
            }

            notification.DeliveryStatus = PaymentNotificationDeliveryStatus.Sent;
            notification.SentAtUtc = DateTime.UtcNow;
            notification.LastError = null;
            this.DbContext.SaveChanges();
        }

        /// <summary>
        /// Marks a claimed email notification as failed so it can be retried.
        /// </summary>
        /// <param name="paymentId">Payment id.</param>
        /// <param name="error">Delivery error.</param>
        public void MarkEmailNotificationFailed(int paymentId, string error)
        {
            PaymentNotifications notification = this.GetEmailNotification(paymentId);
            if (notification == null)
            {
                throw new InvalidOperationException($"No email notification claim exists for payment {paymentId}.");
            }

            notification.DeliveryStatus = PaymentNotificationDeliveryStatus.Failed;
            notification.LastError = string.IsNullOrEmpty(error)
                ? null
                : error.Substring(0, Math.Min(error.Length, 2048));
            this.DbContext.SaveChanges();
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

                if (!this.HasActiveOrSentEmailNotification(payment.Id))
                {
                    result.Add(payment);
                }
            }

            return result;
        }

        private PaymentNotifications GetEmailNotification(int paymentId)
        {
            return this.DbContext.PaymentNotifications.SingleOrDefault(
                notification => notification.Payment.Id == paymentId
                    && notification.NotificationType == NotificationType.Email);
        }

        private bool HasActiveOrSentEmailNotification(int paymentId)
        {
            DateTime activeClaimThreshold = DateTime.UtcNow.Subtract(PendingClaimTimeout);
            return this.DbContext.PaymentNotifications.Any(
                notification => notification.Payment.Id == paymentId
                    && notification.NotificationType == NotificationType.Email
                    && (notification.DeliveryStatus == PaymentNotificationDeliveryStatus.Sent
                        || (notification.DeliveryStatus == PaymentNotificationDeliveryStatus.Pending
                            && notification.LastAttemptAtUtc >= activeClaimThreshold)));
        }

        private static bool IsUniqueConstraintViolation(DbUpdateException exception)
        {
            return exception.InnerException is SqlException sqlException
                && (sqlException.Number == 2601 || sqlException.Number == 2627);
        }
    }
}
