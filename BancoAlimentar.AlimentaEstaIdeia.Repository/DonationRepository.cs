// -----------------------------------------------------------------------
// <copyright file="DonationRepository.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.ViewModel;
    using Easypay.Rest.Client.Model;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Caching.Memory;

    /// <summary>
    /// Default implementation for the <see cref="Donation"/> repository pattern.
    /// </summary>
    public class DonationRepository : GenericRepository<Donation>
    {
        private static ConcurrentBag<TotalDonationsResult> totalDonationItems = new ConcurrentBag<TotalDonationsResult>();

        /// <summary>
        /// Initializes a new instance of the <see cref="DonationRepository"/> class.
        /// </summary>
        /// <param name="context"><see cref="ApplicationDbContext"/> instance.</param>
        /// <param name="memoryCache">A reference to the Memory cache system.</param>
        /// <param name="telemetryClient">Telemetry Client.</param>
        public DonationRepository(ApplicationDbContext context, IMemoryCache memoryCache, TelemetryClient telemetryClient)
            : base(context, memoryCache, telemetryClient)
        {
        }

        /// <summary>
        /// Gets total donations sum for all the elements in the product catalogues.
        /// </summary>
        /// <param name="items">The list of <see cref="ProductCatalogue"/> that belong to the current campaign.</param>
        /// <returns>Return a <see cref="TotalDonationsResult"/> list.</returns>
        public List<TotalDonationsResult> GetTotalDonations(IReadOnlyCollection<ProductCatalogue> items)
        {
            List<TotalDonationsResult> result = new List<TotalDonationsResult>();

            foreach (var product in items)
            {
                TotalDonationsResult totalDonationsResult = this.MemoryCache.Get<TotalDonationsResult>($"{nameof(TotalDonationsResult)}-{product.Id}");
                if (totalDonationsResult == null)
                {
                    int sum = this.DbContext.DonationItems
                        .Where(p => p.ProductCatalogue == product && p.Donation.PaymentStatus == PaymentStatus.Payed)
                        .Sum(p => p.Quantity);
                    double total = product.Quantity.Value * sum;
                    totalDonationsResult = new TotalDonationsResult()
                    {
                        Cost = product.Cost,
                        Description = product.Description,
                        IconUrl = product.IconUrl,
                        Name = product.Name,
                        Quantity = product.Quantity,
                        Total = sum,
                        TotalCost = total,
                        UnitOfMeasure = product.UnitOfMeasure,
                        ProductCatalogueId = product.Id,
                    };

                    this.MemoryCache.Set($"{nameof(TotalDonationsResult)}-{product.Id}", totalDonationsResult, DateTime.UtcNow.AddMinutes(60));
                    totalDonationItems.Add(totalDonationsResult);
                }

                result.Add(totalDonationsResult);
            }

            return result;
        }

        /// <summary>
        /// Associated that donation to the user.
        /// </summary>
        /// <param name="publicDonationId">The public donation id.</param>
        /// <param name="user">A reference to the <see cref="WebUser"/>.</param>
        /// <returns>Returns true if donation is claimed successfully.</returns>
        public bool ClaimDonationToUser(string publicDonationId, WebUser user)
        {
            if (!string.IsNullOrEmpty(publicDonationId) && user != null)
            {
                Guid targetPublicId;
                if (Guid.TryParse(publicDonationId, out targetPublicId))
                {
                    Donation donation = this.DbContext.Donations
                        .Where(p => p.PublicId == targetPublicId)
                        .FirstOrDefault();

                    donation.User = user;

                    this.DbContext.SaveChanges();
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the donation id from the public donation id.
        /// </summary>
        /// <param name="publicDonationId">The public donation id.</param>
        /// <returns>The id of the donation.</returns>
        public int GetDonationIdFromPublicId(Guid publicDonationId)
        {
            return this.DbContext.Donations.Where(p => p.PublicId == publicDonationId).Select(p => p.Id).FirstOrDefault();
        }

        /// <summary>
        /// Gets the donation id from the transaction id.
        /// </summary>
        /// <param name="transactionKey">Transaction id.</param>
        /// <returns>Donation id.</returns>
        public int GetDonationIdFromPaymentTransactionId(string transactionKey)
        {
            return this.DbContext.PaymentItems
                .Where(p => p.Payment.TransactionKey == transactionKey)
                .Select(p => p.Donation.Id)
                .FirstOrDefault();
        }

        /// <summary>
        /// Find a payment based on the donationId and the type.
        /// </summary>
        /// <typeparam name="TPaymentType">This the payment type class.</typeparam>
        /// <param name="donationId">Donation id.</param>
        /// <returns>A reference to the <see cref="BasePayment"/> specified in the type parameter.</returns>
        public TPaymentType FindPaymentByType<TPaymentType>(int donationId)
            where TPaymentType : BasePayment
        {
            TPaymentType result = default(TPaymentType);

            List<BasePayment> payments = this.DbContext.PaymentItems
                .Where(p => p.Donation.Id == donationId)
                .Include(p => p.Payment)
                .Select(p => p.Payment)
                .ToList();

            if (payments != null)
            {
                result = payments.OfType<TPaymentType>().FirstOrDefault();
            }

            return result;
        }

        /// <summary>
        /// Update the status of the credit card payment.
        /// </summary>
        /// <param name="publicId">Public donation id.</param>
        /// <param name="status">New status for the credit card payment.</param>
        /// <returns>Returns true on successfull update.</returns>
        public bool UpdateCreditCardPayment(Guid publicId, string status)
        {
            bool result = false;
            Donation donation = this.DbContext.Donations.Where(p => p.PublicId == publicId).FirstOrDefault();
            if (donation != null)
            {
                if (status == "ok")
                {
                    donation.PaymentStatus = PaymentStatus.Payed;
                }
                else if (status == "err")
                {
                    donation.PaymentStatus = PaymentStatus.ErrorPayment;
                }
                else
                {
                    donation.PaymentStatus = PaymentStatus.NotPayed;
                }

                CreditCardPayment targetPayment = this.FindPaymentByType<CreditCardPayment>(donation.Id);
                if (targetPayment != null)
                {
                    targetPayment.Status = status;
                }

                this.DbContext.SaveChanges();
                result = true;
            }

            return result;
        }

        /// <summary>
        /// Updated the paypal payment status.
        /// </summary>
        /// <param name="donation">A reference to the <see cref="Donation"/>.</param>
        /// <param name="status">Payment's status.</param>
        /// <param name="token">Paypal token.</param>
        /// <param name="payerId">Paypal payer id.</param>
        /// <returns>Returns true on successfull update.</returns>
        public bool UpdateDonationPaymentId(Donation donation, string status, string token = null, string payerId = null)
        {
            if (donation != null && !string.IsNullOrEmpty(token))
            {
                PayPalPayment paypalPayment = this.DbContext.PayPalPayments
                    .Where(p => p.PayPalPaymentId == token)
                    .FirstOrDefault();

                if (paypalPayment == null)
                {
                    paypalPayment = new PayPalPayment();
                    paypalPayment.Created = DateTime.UtcNow;

                    this.DbContext.PayPalPayments.Add(paypalPayment);
                }

                paypalPayment.PayPalPaymentId = token;
                paypalPayment.Status = status;
                paypalPayment.PayerId = payerId;
                paypalPayment.Completed = DateTime.UtcNow;
                donation.ConfirmedPayment = paypalPayment;
                if (donation.Payments == null)
                {
                    donation.Payments = new List<PaymentItem>();
                }

                donation.Payments.Add(new PaymentItem()
                {
                    Donation = donation,
                    Payment = paypalPayment,
                });

                this.DbContext.SaveChanges();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Update the multibanco payment status.
        /// </summary>
        /// <param name="donation">A reference to the <see cref="Donation"/>.</param>
        /// <param name="easyPayId">EasyPay transaction id.</param>
        /// <param name="transactionKey">Easypay transaction key. Used to update in the future the status of the payment.</param>
        /// <param name="entity">Multibanco entity id.</param>
        /// <param name="reference">Multibanco reference id.</param>
        /// <returns>Returns true on successfull update.</returns>
        public bool UpdateMultiBankPayment(Donation donation, string easyPayId, string transactionKey, string entity, string reference)
        {
            if (donation != null && !string.IsNullOrEmpty(transactionKey))
            {
                donation.ServiceEntity = entity;
                donation.ServiceReference = reference;
                MultiBankPayment multiBankPayment = this.DbContext.MultiBankPayments
                    .Where(p => p.TransactionKey == transactionKey)
                    .FirstOrDefault();

                if (multiBankPayment == null)
                {
                    multiBankPayment = new MultiBankPayment();
                    multiBankPayment.Created = DateTime.UtcNow;
                    multiBankPayment.EasyPayPaymentId = easyPayId;

                    this.DbContext.MultiBankPayments.Add(multiBankPayment);
                }

                multiBankPayment.TransactionKey = transactionKey;
                if (donation.Payments == null)
                {
                    donation.Payments = new List<PaymentItem>();
                }

                donation.Payments.Add(new PaymentItem()
                {
                    Donation = donation,
                    Payment = multiBankPayment,
                });

                this.DbContext.SaveChanges();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Updated easy pay transaction.
        /// </summary>
        /// <param name="easyPayId">EasyPay Id.</param>
        /// <param name="transactionkey">Our transaction key.</param>
        /// <param name="status">Payment status.</param>
        /// <param name="message">Message.</param>
        /// <returns>Returns the base payment id.</returns>
        public int UpdatePaymentTransaction(string easyPayId, string transactionkey, GenericNotificationRequest.StatusEnum? status, string message)
        {
            int basePaymentId = 0;
            BasePayment payment = this.DbContext.Payments
                .Where(p => p.TransactionKey == transactionkey)
                .FirstOrDefault();
            if (payment != null)
            {
                basePaymentId = payment.Id;
                payment.Status = status.ToString();
                Donation donation = this.DbContext.PaymentItems
                    .Where(p => p.Payment.TransactionKey == transactionkey)
                    .Select(p => p.Donation)
                    .FirstOrDefault();
                if (donation != null)
                {
                    switch (status)
                    {
                        case GenericNotificationRequest.StatusEnum.Failed:
                            {
                                donation.PaymentStatus = PaymentStatus.ErrorPayment;
                                break;
                            }

                        case GenericNotificationRequest.StatusEnum.Success:
                            {
                                donation.PaymentStatus = PaymentStatus.Payed;
                                break;
                            }
                    }
                }

                this.DbContext.SaveChanges();
            }

            return basePaymentId;
        }

        /// <summary>
        /// Create a new MBWay payment.
        /// </summary>
        /// <param name="donation">A reference to the <see cref="Donation"/>.</param>
        /// <param name="easyPayId">EasyPay transaction id.</param>
        /// <param name="transactionKey">Our internal tranaction key.</param>
        /// <param name="alias">Easypay alias for the payment type.</param>
        /// <returns>Returns true after successfull creation of MBWayPayment.</returns>
        public bool CreateMBWayPayment(Donation donation, string easyPayId, string transactionKey, string alias)
        {
            if (donation != null && !string.IsNullOrEmpty(transactionKey))
            {
                MBWayPayment value = new MBWayPayment();
                if (donation.Payments == null)
                {
                    donation.Payments = new List<PaymentItem>();
                }

                donation.Payments.Add(new PaymentItem() { Donation = donation, Payment = value });
                value.Created = DateTime.UtcNow;
                value.Alias = alias;
                value.TransactionKey = transactionKey;
                value.EasyPayPaymentId = easyPayId;
                this.DbContext.MBWayPayments.Add(value);
                this.DbContext.SaveChanges();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Create a credit card payment.
        /// </summary>
        /// <param name="donation">A reference to the <see cref="Donation"/>.</param>
        /// <param name="easyPayId">EasyPay transaction id.</param>
        /// <param name="transactionKey">Our internal tranaction key.</param>
        /// <param name="url">The url to redirect the user.</param>
        /// <param name="creationDateTime">DateTime of the credit card payment.</param>
        /// <param name="status">Status for the payment.</param>
        /// <returns>Returns true after successfull creation of payment.</returns>
        public bool CreateCreditCardPaymnet(
            Donation donation,
            string easyPayId,
            string transactionKey,
            string url,
            DateTime creationDateTime,
            string status = null)
        {
            if (donation != null && !string.IsNullOrEmpty(transactionKey))
            {
                CreditCardPayment value = new CreditCardPayment();
                if (donation.Payments == null)
                {
                    donation.Payments = new List<PaymentItem>();
                }

                donation.Payments.Add(new PaymentItem() { Donation = donation, Payment = value });
                value.Created = creationDateTime;
                value.TransactionKey = transactionKey;
                value.Url = url;
                value.EasyPayPaymentId = easyPayId;
                value.Status = status;
                this.DbContext.CreditCardPayments.Add(value);
                this.DbContext.SaveChanges();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Complete the credit card payment.
        /// </summary>
        /// <typeparam name="TPaymentType">Type of the payment.</typeparam>
        /// <param name="easyPayId">This is the Easy Pay id for the transaction.</param>
        /// <param name="transactionKey">Our internal tranaction key.</param>
        /// <param name="easypayPaymentTransactionId">This is the inner easypay transaction id.</param>
        /// <param name="transactionDateTime">Datetime for the payment transaction.</param>
        /// <param name="requested">Amount of money requested.</param>
        /// <param name="paid">Amount of money payed.</param>
        /// <param name="fixedFee">Fixed fee for the transaction.</param>
        /// <param name="variableFee">Variable fee for the transaction.</param>
        /// <param name="tax">Tax associated to the transaction.</param>
        /// <param name="transfer">Amount of money trasnfer.</param>
        /// <returns>The donation id.</returns>
        public int CompleteEasyPayPayment<TPaymentType>(
            string easyPayId,
            string transactionKey,
            string easypayPaymentTransactionId,
            DateTime transactionDateTime,
            float requested,
            float paid,
            float fixedFee,
            float variableFee,
            float tax,
            float transfer)
            where TPaymentType : EasyPayWithValuesBaseClass
        {
            int donationId = 0;

            TPaymentType payment = null;
            if (this.IsTransactionKeySubcriptionBased(transactionKey))
            {
                SubscriptionRepository subscriptionRepository = new SubscriptionRepository(
                        this.DbContext,
                        this.MemoryCache,
                        this.TelemetryClient);

                subscriptionRepository.CreateSubscriptionDonationAndPayment(
                    easypayPaymentTransactionId,
                    transactionKey,
                    GenericNotificationRequest.StatusEnum.Success,
                    transactionDateTime);

                payment = this.DbContext.Payments
                    .Cast<TPaymentType>()
                    .Where(p =>
                            p.TransactionKey == transactionKey &&
                            p.Created.Date == transactionDateTime.Date)
                    .FirstOrDefault();
            }
            else
            {
                payment = this.DbContext.Payments
                    .Cast<TPaymentType>()
                    .Where(p => p.TransactionKey == transactionKey)
                    .FirstOrDefault();
            }

            if (payment != null)
            {
                PaymentItem paymentItem = this.DbContext.PaymentItems
                    .Include(p => p.Donation)
                    .Where(p => p.Payment.Id == payment.Id)
                    .FirstOrDefault();

                if (paymentItem != null && paymentItem.Donation != null)
                {
                    donationId = paymentItem.Donation.Id;
                    paymentItem.Donation.PaymentStatus = PaymentStatus.Payed;
                    paymentItem.Donation.ConfirmedPayment = payment;
                    this.DbContext.Entry(paymentItem.Donation).State = EntityState.Modified;
                }
                else
                {
                    EventTelemetry donationNotFound = new EventTelemetry($"Donation-{typeof(TPaymentType).Name}-NotFound");
                    donationNotFound.Properties.Add($"{typeof(TPaymentType).Name}TransactionKey", transactionKey);
                    donationNotFound.Properties.Add("PaymentId", payment.Id.ToString());
                    this.TelemetryClient.TrackEvent(donationNotFound);
                }

                payment.EasyPayPaymentId = easypayPaymentTransactionId;
                payment.Requested = requested;
                payment.Paid = paid;
                payment.FixedFee = fixedFee;
                payment.VariableFee = variableFee;
                payment.Tax = tax;
                payment.Transfer = transfer;
                payment.Completed = DateTime.UtcNow;

                this.DbContext.SaveChanges();
            }
            else
            {
                EventTelemetry creditCardPaymentNotFound = new EventTelemetry($"{typeof(TPaymentType).Name}-NotFound");
                creditCardPaymentNotFound.Properties.Add($"{typeof(TPaymentType).Name}TransactionKey", transactionKey);
                creditCardPaymentNotFound.Properties.Add("EasyPayId", easyPayId);
                this.TelemetryClient.TrackEvent(creditCardPaymentNotFound);
            }

            return donationId;
        }

        /// <summary>
        /// Gets a reference to the current <see cref="MultiBankPayment"/> for this donation.
        /// </summary>
        /// <param name="donationId">The donation id.</param>
        /// <returns>A reference, if exits, to the <see cref="MultiBankPayment"/>.</returns>
        public MultiBankPayment GetCurrentMultiBankPayment(int donationId)
        {
            MultiBankPayment result = null;

            List<PaymentItem> payments = this.DbContext.PaymentItems
                .Include(p => p.Payment)
                .Where(p => p.Donation.Id == donationId).ToList();
            if (payments != null && payments.Count > 0)
            {
                result = payments
                    .Where(p => p.Payment is MultiBankPayment)
                    .Select(p => p.Payment)
                    .Cast<MultiBankPayment>()
                    .FirstOrDefault();
            }

            return result;
        }

        /// <summary>
        /// Gets the full <see cref="Donation"/> object that contains the user and the donation users.
        /// </summary>
        /// <param name="donationId">Donation id.</param>
        /// <returns>A reference to the <see cref="Donation"/>.</returns>
        public Donation GetFullDonationById(int donationId)
        {
            return this.DbContext.Donations
                .Where(p => p.Id == donationId)
                .Include(p => p.User)
                .Include(p => p.User.Address)
                .Include(p => p.DonationItems)
                .Include("DonationItems.ProductCatalogue")
                .Include(p => p.FoodBank)
                .Include(p => p.ConfirmedPayment)
                .FirstOrDefault();
        }

        /// <summary>
        /// Gets the list of payments for the donation id.
        /// </summary>
        /// <param name="donationId">Donation id.</param>
        /// <returns>A list of payments associated to this donation id.</returns>
        public List<BasePayment> GetPaymentsForDonation(int donationId)
        {
            return this.DbContext.PaymentItems
                .Where(p => p.Donation.Id == donationId)
                .Select(p => p.Payment)
                .ToList();
        }

        /// <summary>
        /// Get all the user donations in time.
        /// </summary>
        /// <param name="userId">A reference to the user id.</param>
        /// <returns>A <see cref="List{Donation}"/> of donations.</returns>
        public List<Donation> GetUserDonation(string userId)
        {
            return this.DbContext.Donations
                .Include(p => p.DonationItems)
                .Include(p => p.FoodBank)
                .Include(p => p.ConfirmedPayment)
                .Include("Payments.Payment")
                .Where(p => p.User.Id == userId)
                .OrderByDescending(p => p.DonationDate)
                .ToList();
        }

        /// <summary>
        /// Get the total ammount donated by a user.
        /// </summary>
        /// <param name="userId">the user id.</param>
        /// <returns>Total Ammount donated.</returns>
        public (double, int, DateTime) GetTotalUserDonations(string userId)
        {
            double total = -1;
            int count = -1;
            DateTime firstDate;

            var data = this.DbContext.Donations
                .Where(p => p.User.Id == userId && p.PaymentStatus == PaymentStatus.Payed);
            total = data.Sum(p => p.DonationAmount);
            firstDate = data.Min(p => p.DonationDate);
            count = data.Count();

            return (total, count, firstDate);
        }

        /// <summary>
        /// Gets payment type.
        /// </summary>
        /// <param name="payment">A reference to the base class <see cref="BasePayment"/>.</param>
        /// <returns>The payment type.</returns>
        public PaymentType GetPaymentType(BasePayment payment)
        {
            PaymentType result = PaymentType.None;

            if (payment != null)
            {
                if (payment is PayPalPayment)
                {
                    result = PaymentType.Paypal;
                }
                else if (payment is CreditCardPayment)
                {
                    result = PaymentType.CreditCard;
                }
                else if (payment is MBWayPayment)
                {
                    result = PaymentType.MBWay;
                }
                else if (payment is MultiBankPayment)
                {
                    result = PaymentType.MultiBanco;
                }
            }

            return result;
        }

        /// <summary>
        /// Invalidate the memory cache for the total donation.
        /// </summary>
        public void InvalidateTotalCache()
        {
            foreach (var item in totalDonationItems)
            {
                this.MemoryCache.Remove($"{nameof(TotalDonationsResult)}-{item.ProductCatalogueId}");
            }
        }

        /// <summary>
        /// Checks if the the transaction key is part of a subscription.
        /// This is being used when completing a credit card payment.
        /// </summary>
        /// <param name="transactionKey">Transaction key.</param>
        /// <returns>True if the transaction key is being used by a subscription, false otherwise.</returns>
        public bool IsTransactionKeySubcriptionBased(string transactionKey)
        {
            bool result = false;

            if (!string.IsNullOrEmpty(transactionKey))
            {
                result = this.DbContext.Subscriptions
                    .Where(p => p.TransactionKey == transactionKey)
                    .Count() > 0;
            }

            return result;
        }
    }
}
