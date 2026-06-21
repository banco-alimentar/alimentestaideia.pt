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
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;
    using Azure.Data.Tables;
    using BancoAlimentar.AlimentaEstaIdeia.Common;
    using BancoAlimentar.AlimentaEstaIdeia.Common.Repository.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.ViewModel;
    using Easypay.Rest.Client.Model;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Configuration;
    using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

    /// <summary>
    /// Default implementation for the <see cref="Donation"/> repository pattern.
    /// </summary>
    public class DonationRepository : GenericRepository<Donation, ApplicationDbContext>
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
        /// Gets the total cash donatino amount.
        /// </summary>
        /// <param name="cashDonation">A reference to the <see cref="ProductCatalogue"/>.</param>
        /// <returns>The total donation for the cash donation.</returns>
        public TotalDonationsResult GetTotalCashDonation(ProductCatalogue cashDonation)
        {
            TotalDonationsResult result = new TotalDonationsResult()
            {
                Cost = cashDonation.Cost,
                Description = cashDonation.Description,
                IconUrl = cashDonation.IconUrl,
                Name = cashDonation.Name,
                Quantity = cashDonation.Quantity,
                UnitOfMeasure = cashDonation.UnitOfMeasure,
                ProductCatalogueId = cashDonation.Id,
            };

            result.Total = this.DbContext.DonationItems
                    .Where(p => p.ProductCatalogue == cashDonation && p.Donation.PaymentStatus == PaymentStatus.Payed)
                    .Sum(p => p.Price);

            return result;
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
                    double total = product.Quantity != null ? product.Quantity.Value : 0 * sum;
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
        /// Gets total donations sum for all the elements in the product catalogues.
        /// </summary>
        /// <param name="items">The list of <see cref="ProductCatalogue"/> that belong to the current campaign.</param>
        /// <param name="foodBankId">The id of the food bank that we want to get the totals.</param>
        /// <returns>Return a <see cref="TotalDonationsResult"/> list.</returns>
        public List<TotalDonationsResult> GetTotalDonationsOfFoodBank(IReadOnlyCollection<ProductCatalogue> items, int foodBankId)
        {
            List<TotalDonationsResult> result = new List<TotalDonationsResult>();

            foreach (var product in items)
            {
                TotalDonationsResult totalDonationsResult = this.MemoryCache.Get<TotalDonationsResult>($"{foodBankId}-{nameof(TotalDonationsResult)}-{product.Id}");
                if (totalDonationsResult == null)
                {
                    int sum = this.DbContext.DonationItems
                        .Where(p => p.ProductCatalogue == product && p.Donation.PaymentStatus == PaymentStatus.Payed && p.Donation.FoodBank.Id == foodBankId)
                        .Sum(p => p.Quantity);
                    double total = product.Quantity != null ? product.Quantity.Value : 0 * sum;
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

                    this.MemoryCache.Set($"{foodBankId}-{nameof(TotalDonationsResult)}-{product.Id}", totalDonationsResult, DateTime.UtcNow.AddMinutes(60));
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
                        .First();

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
            // TODELETE
            // return this.DbContext.PaymentItems
            //    .Where(p => p.Payment.TransactionKey == transactionKey)
            //    .Select(p => p.Donation.Id)
            //    .FirstOrDefault();
            return this.DbContext.Payments
                .Where(p => p.TransactionKey == transactionKey)
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

            // TODELETE
            // List<BasePayment> payments = this.DbContext.PaymentItems
            //    .Where(p => p.Donation.Id == donationId)
            //    .Include(p => p.Payment)
            //    .Select(p => p.Payment)
            //    .ToList();
            List<BasePayment> payments = this.DbContext.Payments
                .Where(p => p.Donation.Id == donationId)
                .ToList();

            if (payments != null)
            {
                result = payments.OfType<TPaymentType>().FirstOrDefault();
            }

            return result;
        }

        /// <summary>
        /// Marks a donation as paid and links the confirmed payment in a single update.
        /// </summary>
        /// <param name="donation">Donation being paid.</param>
        /// <param name="payment">Payment that completed successfully.</param>
        /// <param name="requested">Optional requested amount from the provider.</param>
        /// <param name="paid">Optional paid amount from the provider.</param>
        /// <param name="transactionKey">Optional transaction key for telemetry.</param>
        /// <param name="trustProviderPaidStatus">When true, accept a provider-paid signal without stored amounts.</param>
        /// <returns>True when both payment status and confirmed payment were updated.</returns>
        public bool TryCompleteDonationPayment(
            Donation donation,
            BasePayment payment,
            float? requested = null,
            float? paid = null,
            string transactionKey = null,
            bool trustProviderPaidStatus = false)
        {
            if (donation == null || payment == null)
            {
                return false;
            }

            if (trustProviderPaidStatus)
            {
                DonationPaymentCompletion.EnsureEasyPayAmountsFromDonation(donation, payment);
            }

            if (!DonationPaymentCompletion.CanCompleteDonationPayment(
                    donation,
                    payment,
                    requested,
                    paid,
                    trustProviderPaidStatus))
            {
                if (requested.HasValue && paid.HasValue)
                {
                    this.TelemetryClient.TrackEvent(
                        "WebhookDonationAmountMismatch",
                        new Dictionary<string, string>
                        {
                            { "DonationId", donation.Id.ToString() },
                            { "TransactionKey", transactionKey ?? string.Empty },
                            { "ExpectedAmount", donation.DonationAmount.ToString() },
                            { "Requested", requested.Value.ToString() },
                            { "Paid", paid.Value.ToString() },
                        });
                }

                return false;
            }

            donation.PaymentStatus = PaymentStatus.Payed;
            donation.ConfirmedPayment = payment;
            payment.Donation = donation;
            this.DbContext.Entry(donation).State = EntityState.Modified;
            return true;
        }

        /// <summary>
        /// Update the status of the credit card payment.
        /// </summary>
        /// <param name="publicId">Public donation id.</param>
        /// <param name="status">New status for the credit card payment.</param>
        /// <returns>Returns true on successfull update.</returns>
        /// <typeparam name="TPaymentType">Payment type.</typeparam>
        public bool UpdatePaymentStatus<TPaymentType>(Guid publicId, SinglePaymentStatus status)
            where TPaymentType : BasePayment
        {
            bool result = false;
            Donation donation = this.DbContext.Donations.Where(p => p.PublicId == publicId).FirstOrDefault();
            if (donation != null)
            {
                TPaymentType targetPayment = this.FindPaymentByType<TPaymentType>(donation.Id);
                if (status == SinglePaymentStatus.Paid || status == SinglePaymentStatus.Authorised)
                {
                    if (targetPayment == null)
                    {
                        return false;
                    }

                    if (!this.TryCompleteDonationPayment(
                            donation,
                            targetPayment,
                            trustProviderPaidStatus: true))
                    {
                        return false;
                    }

                    targetPayment.Status = status.ToString();
                }
                else if (status == SinglePaymentStatus.Failed)
                {
                    donation.PaymentStatus = PaymentStatus.ErrorPayment;
                    if (targetPayment != null)
                    {
                        targetPayment.Status = status.ToString();
                    }
                }
                else if (status == SinglePaymentStatus.Pending)
                {
                    donation.PaymentStatus = PaymentStatus.WaitingPayment;
                    if (targetPayment != null)
                    {
                        targetPayment.Status = status.ToString();
                    }
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
                paypalPayment.Donation = donation;

                if (string.Equals(status, "COMPLETED", StringComparison.OrdinalIgnoreCase))
                {
                    paypalPayment.Completed = DateTime.UtcNow;
                    if (!this.TryCompleteDonationPayment(
                            donation,
                            paypalPayment,
                            transactionKey: token,
                            trustProviderPaidStatus: true))
                    {
                        return false;
                    }
                }

                // TODELETE
                // if (donation.Payments == null)
                // {
                //    donation.Payments = new List<PaymentItem>();
                // }

                // donation.Payments.Add(new PaymentItem()
                // {
                //    Donation = donation,
                //    Payment = paypalPayment,
                // });
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
                multiBankPayment.Donation = donation;

                // TODELETE
                // if (donation.Payments == null)
                // {
                //    donation.Payments = new List<PaymentItem>();
                // }

                // donation.Payments.Add(new PaymentItem()
                // {
                //    Donation = donation,
                //    Payment = multiBankPayment,
                // });
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
        public (int BasePaymentId, int DonationId) UpdatePaymentTransaction(string easyPayId, string transactionkey, NotificationGeneric.StatusEnum? status, string message)
        {
            int basePaymentId = 0;
            int donationId = 0;
            BasePayment payment = this.DbContext.Payments
                .Where(p => p.TransactionKey == transactionkey)
                .FirstOrDefault();
            if (payment != null)
            {
                basePaymentId = payment.Id;
                payment.Status = status.ToString();
                Donation donation = this.DbContext.Payments
                    .Where(p => p.TransactionKey == transactionkey)
                    .Select(p => p.Donation)
                    .FirstOrDefault();

                if (donation == null)
                {
                    // if the donation is null, we need to find it by the transaction key in the payment list.
                    donation = this.DbContext.Donations
                        .Where(p => p.PaymentList.Any(q => q.TransactionKey == transactionkey))
                        .FirstOrDefault();
                }

                if (donation != null)
                {
                    this.DbContext.Entry(donation).State = EntityState.Modified;
                    switch (status)
                    {
                        case NotificationGeneric.StatusEnum.Failed:
                            {
                                if (donation.PaymentStatus == PaymentStatus.Payed)
                                {
                                    this.TelemetryClient.TrackEvent(
                                        "PayedDonation-To-Failed-Payment-Try",
                                        new Dictionary<string, string>()
                                        {
                                            { "DonationId", donation.Id.ToString() },
                                            { "EasyPayId", easyPayId },
                                            { "TransactionKey", transactionkey },
                                            { "BasePaymentId", basePaymentId.ToString() },
                                            { "Message", $"The donation already has a Payed status but it was trying to set to Failed by EasyPay. Cancelling this." },
                                        });
                                }
                                else
                                {
                                    donation.PaymentStatus = PaymentStatus.ErrorPayment;
                                }

                                break;
                            }

                        case NotificationGeneric.StatusEnum.Success:
                            {
                                if (!this.TryCompleteDonationPayment(
                                        donation,
                                        payment,
                                        transactionKey: transactionkey,
                                        trustProviderPaidStatus: true))
                                {
                                    break;
                                }

                                DonationPaymentCompletion.MarkSuccessfulEasyPayPayment(payment);

                                this.TelemetryClient.TrackEvent(
                                    "UpdatePaymentTransaction-Donation-Payed",
                                    new Dictionary<string, string>()
                                    {
                                        { "EasyPayId", easyPayId },
                                        { "TransactionKey", transactionkey },
                                        { "BasePaymentId", basePaymentId.ToString() },
                                        { "DonationId", donation.Id.ToString() },
                                        { "PaymentStatus", donation.PaymentStatus.ToString() },
                                        { "Message", $"EasyPay Generic Notification status changed." },
                                    });
                                break;
                            }
                    }

                    donationId = donation.Id;
                }
                else
                {
                    this.TelemetryClient.TrackEvent(
                        "UpdatePaymentTransaction-Donation-Is-Null",
                        new Dictionary<string, string>()
                        {
                            { "EasyPayId", easyPayId },
                            { "TransactionKey", transactionkey },
                            { "BasePaymentId", basePaymentId.ToString() },
                            { "Message", $"The transaction key {transactionkey} was found for the BasePaymentId {basePaymentId} but not for a donation. So it's null." },
                        });
                }

                this.DbContext.SaveChanges();
            }

            return (basePaymentId, donationId);
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

                if (donation.PaymentList == null)
                {
                    donation.PaymentList = new List<BasePayment>();
                }

                donation.PaymentList.Add(value);
                value.Created = DateTime.UtcNow;
                value.Alias = alias;
                value.TransactionKey = transactionKey;
                value.EasyPayPaymentId = easyPayId;
                value.Donation = donation;
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

                if (donation.PaymentList == null)
                {
                    donation.PaymentList = new List<BasePayment>();
                }

                donation.PaymentList.Add(value);
                value.Created = creationDateTime;
                value.TransactionKey = transactionKey;
                value.Url = url;
                value.EasyPayPaymentId = easyPayId;
                value.Status = status;
                value.Donation = donation;
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
        /// <param name="configuration">Configuration.</param>
        /// <returns>The donation id.</returns>
        public async Task<(int DonationId, int PaymentId)> CompleteEasyPayPaymentAsync<TPaymentType>(
            string easyPayId,
            string transactionKey,
            string easypayPaymentTransactionId,
            DateTime transactionDateTime,
            float requested,
            float paid,
            float fixedFee,
            float variableFee,
            float tax,
            float transfer,
            IConfiguration configuration)
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
                    NotificationGeneric.StatusEnum.Success,
                    transactionDateTime);

                payment = this.DbContext.Payments
                    .Include(p => p.Donation)
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
                    .Include(p => p.Donation)
                    .Where(p => p.TransactionKey == transactionKey)
                    .FirstOrDefault();

                if (payment != null && payment.Donation == null)
                {
                    // if the payment is not null but the donation is null means that
                    // for whatever reason we are reciving a payment that is not match with a Donation.
                    // This is very bad for us but we don't know why this is happening.
                    // The remedy to this is check the auditing table and try to find the donation id
                    // by looking at the transaction key.
                    SinglePaymentAuditingTableQuery singlePaymentAuditingTableQuery =
                        new SinglePaymentAuditingTableQuery(configuration);
                    TableEntity lostItems = await singlePaymentAuditingTableQuery.GetEntityByTransactionKey(transactionKey);
                    if (lostItems != null)
                    {
                        payment.Donation = this.DbContext.Find<Donation>(int.Parse(lostItems["DonationId"].ToString()));
                        this.DbContext.Entry(payment).State = EntityState.Modified;
                        await this.DbContext.SaveChangesAsync();
                    }
                }

                payment = this.DbContext.Payments
                    .Cast<TPaymentType>()
                    .Include(p => p.Donation)
                    .Where(p => p.TransactionKey == transactionKey)
                    .FirstOrDefault();
            }

            if (payment != null)
            {
                if (payment.Donation == null)
                {
                    // if the donation is null, we need to find it by the transaction key in the payment list.
                    payment.Donation = this.DbContext.Donations
                        .Where(p => p.PaymentList.Any(q => q.TransactionKey == transactionKey))
                        .FirstOrDefault();
                }

                if (payment.Donation != null)
                {
                    donationId = payment.Donation.Id;
                    if (!this.TryCompleteDonationPayment(
                        payment.Donation,
                        payment,
                        requested,
                        paid,
                        transactionKey))
                    {
                        donationId = 0;
                    }
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

                bool donationCompleted = donationId > 0;
                if (donationCompleted)
                {
                    DonationPaymentCompletion.MarkSuccessfulEasyPayPayment(payment);
                }

                await this.DbContext.SaveChangesAsync();
            }
            else
            {
                EventTelemetry creditCardPaymentNotFound = new EventTelemetry($"{typeof(TPaymentType).Name}-NotFound");
                creditCardPaymentNotFound.Properties.Add($"{typeof(TPaymentType).Name}TransactionKey", transactionKey);
                creditCardPaymentNotFound.Properties.Add("EasyPayId", easyPayId);
                this.TelemetryClient.TrackEvent(creditCardPaymentNotFound);
            }

            return (donationId, payment != null ? payment.Id : 0);
        }

        /// <summary>
        /// Gets a reference to the current <see cref="MultiBankPayment"/> for this donation.
        /// </summary>
        /// <param name="donationId">The donation id.</param>
        /// <returns>A reference, if exits, to the <see cref="MultiBankPayment"/>.</returns>
        public MultiBankPayment GetCurrentMultiBankPayment(int donationId)
        {
            MultiBankPayment result = null;

            List<BasePayment> payments = this.DbContext.Payments
                .Where(p => p.Donation.Id == donationId)
                .ToList();
            if (payments != null && payments.Count > 0)
            {
                result = payments
                    .Where(p => p is MultiBankPayment)
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
        /// Delete payment from the transaction key.
        /// </summary>
        /// <param name="easypayId">A reference to the transaction key.</param>
        public void DeletePayment(string easypayId)
        {
            BasePayment payment = this.DbContext.MultiBankPayments
                .Where(p => p.EasyPayPaymentId == easypayId)
                .FirstOrDefault();

            if (payment == null)
            {
                payment = this.DbContext.MBWayPayments
                    .Where(p => p.EasyPayPaymentId == easypayId)
                    .FirstOrDefault();
            }

            if (payment != null)
            {
                this.DbContext.Entry(payment).State = EntityState.Deleted;
                this.DbContext.SaveChanges();
            }
        }

        /// <summary>
        /// Gets the list of payments for the donation id.
        /// </summary>
        /// <param name="donationId">Donation id.</param>
        /// <returns>A list of payments associated to this donation id.</returns>
        public List<BasePayment> GetPaymentsForDonation(int donationId)
        {
            return this.DbContext.Payments
                .Where(p => p.Donation.Id == donationId)
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
                .Include(p => p.PaymentList)
                .Where(p => p.User.Id == userId)
                .OrderByDescending(p => p.DonationDate)
                .ToList();
        }

        /// <summary>
        /// Gets the number of donations shown in the user donation history.
        /// </summary>
        /// <param name="userId">A reference to the user id.</param>
        /// <param name="search">Optional search term.</param>
        /// <returns>The total number of history rows.</returns>
        public int GetUserDonationHistoryCount(string userId, string search = null)
        {
            return this.ApplyUserDonationHistorySearch(this.GetUserDonationHistoryBaseQuery(userId), search).Count();
        }

        /// <summary>
        /// Gets a page of donations for the user donation history.
        /// </summary>
        /// <param name="userId">A reference to the user id.</param>
        /// <param name="skip">The number of rows to skip.</param>
        /// <param name="take">The page size.</param>
        /// <param name="search">Optional search term.</param>
        /// <returns>A page of donations.</returns>
        public List<Donation> GetUserDonationHistoryPaged(string userId, int skip, int take, string search = null)
        {
            return this.ApplyUserDonationHistorySearch(this.GetUserDonationHistoryQuery(userId), search)
                .Skip(skip)
                .Take(take)
                .ToList();
        }

        /// <summary>
        /// Get the total ammount donated by a user.
        /// </summary>
        /// <param name="userId">the user id.</param>
        /// <returns>Total Ammount donated.</returns>
        public (double Total, int Count, DateTime FirstDate) GetTotalUserDonations(string userId)
        {
            var summary = this.DbContext.Donations
                .Where(p => p.User.Id == userId && p.PaymentStatus == PaymentStatus.Payed)
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Total = g.Sum(p => p.DonationAmount),
                    Count = g.Count(),
                    FirstDate = g.Min(p => p.DonationDate),
                })
                .FirstOrDefault();

            if (summary == null || summary.Count == 0)
            {
                return (0, 0, DateTime.Now);
            }

            return (summary.Total, summary.Count, summary.FirstDate);
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
        /// Gets a human readable name of the type of payment.
        /// </summary>
        /// <param name="payment">The type of payment.</param>
        /// <returns>the name of the payment method in human readable format.</returns>
        public string GetPaymentHumanName(BasePayment payment)
        {
            if (payment != null)
            {
                if (payment is PayPalPayment)
                {
                    return "Paypal";
                }
                else if (payment is CreditCardPayment)
                {
                    return "Cartão de Crédito";
                }
                else if (payment is MBWayPayment)
                {
                    return "MBWay";
                }
                else if (payment is MultiBankPayment)
                {
                    return "Multibanco";
                }
            }

            return "desconhecido";
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

        /// <summary>
        /// Delete the donation and the donation items and other payments associated to this.
        /// </summary>
        /// <param name="donationId">Donation id.</param>
        public void DeleteDonation(int donationId)
        {
            Donation donation = this.GetFullDonationById(donationId);
            if (donation == null)
            {
                return;
            }

            List<SubscriptionDonations> subscriptionDonations = this.DbContext.SubscriptionDonations
                .Where(sd => EF.Property<int?>(sd, "DonationId") == donationId)
                .ToList();
            if (subscriptionDonations.Count > 0)
            {
                this.DbContext.SubscriptionDonations.RemoveRange(subscriptionDonations);
            }

            List<Model.Subscription> subscriptionsWithInitialDonation = this.DbContext.Subscriptions
                .Where(s => EF.Property<int?>(s, "InitialDonationId") == donationId)
                .ToList();
            foreach (Model.Subscription subscription in subscriptionsWithInitialDonation)
            {
                subscription.InitialDonation = null;
            }

            donation.ConfirmedPayment = null;

            List<BasePayment> payments = this.DbContext.Payments
                .Where(p => EF.Property<int?>(p, "DonationId") == donationId)
                .ToList();
            foreach (BasePayment payment in payments)
            {
                this.DbContext.Entry(payment).State = EntityState.Deleted;
            }

            this.DbContext.Entry(donation).State = EntityState.Deleted;
            this.DbContext.SaveChanges();
        }

        /// <summary>
        /// Gets the donation id from the EasyPay transaction id.
        /// </summary>
        /// <param name="value">EasyPay Transaction id.</param>
        /// <returns>Donation id.</returns>
        public int GetDonationByTransactionKey(Guid value)
        {
            return this.DbContext.Payments
                .Where(p => p.TransactionKey == value.ToString())
                .Select(p => p.Donation.Id)
                .FirstOrDefault();
        }

        /// <summary>
        /// Clone the donation.
        /// </summary>
        /// <param name="donation">The donation to copy from.</param>
        /// <returns>A newly create donation that can be inserted in the database.</returns>
        public Donation CloneDonation(Donation donation)
        {
            Donation newDonation = new Donation()
            {
                DonationAmount = donation.DonationAmount,
                FoodBank = donation.FoodBank,
                Nif = donation.Nif,
                PaymentStatus = PaymentStatus.WaitingPayment,
                ReferralEntity = donation.ReferralEntity,
                PublicId = Guid.NewGuid(),
                DonationDate = DateTime.UtcNow,
                DonationItems = new List<DonationItem>(),
                User = donation.User,
                WantsReceipt = true,
                CampaignId = donation.CampaignId,
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

            return newDonation;
        }

        /// <summary>
        /// Returns true when the donation already has a successful payment and should not be cloned.
        /// </summary>
        /// <param name="donation">Donation being evaluated.</param>
        /// <param name="payments">Existing payments for the donation.</param>
        /// <returns>True when a payment can complete the donation without cloning.</returns>
        public bool HasSuccessfulPayment(Donation donation, IList<BasePayment> payments)
        {
            if (donation == null || payments == null || payments.Count == 0)
            {
                return false;
            }

            return payments.Any(payment =>
                DonationPaymentCompletion.CanCompleteDonationPayment(donation, payment, null, null)
                || (payment.Completed.HasValue
                    && DonationPaymentCompletion.IsSuccessfulPaymentStatus(payment.Status)));
        }

        /// <summary>
        /// Completes a donation from an existing successful payment when fields are out of sync.
        /// </summary>
        /// <param name="donation">Donation to repair.</param>
        /// <param name="payments">Existing payments for the donation.</param>
        /// <returns>True when the donation was marked paid.</returns>
        public bool TryCompleteDonationFromExistingPayment(Donation donation, IList<BasePayment> payments)
        {
            if (donation == null || payments == null)
            {
                return false;
            }

            BasePayment successfulPayment = payments.FirstOrDefault(payment =>
                DonationPaymentCompletion.CanCompleteDonationPayment(donation, payment, null, null)
                || (payment.Completed.HasValue
                    && DonationPaymentCompletion.IsSuccessfulPaymentStatus(payment.Status)));

            if (successfulPayment == null)
            {
                return false;
            }

            if (this.TryCompleteDonationPayment(donation, successfulPayment))
            {
                this.DbContext.SaveChanges();
                return true;
            }

            return false;
        }

        private IQueryable<Donation> GetUserDonationHistoryBaseQuery(string userId)
        {
            return this.DbContext.Donations
                .AsNoTracking()
                .Where(p => p.User.Id == userId)
                .Where(p => p.PaymentStatus != PaymentStatus.WaitingPayment
                    || !this.DbContext.SubscriptionDonations.Any(sd => sd.Donation.Id == p.Id));
        }

        private IQueryable<Donation> GetUserDonationHistoryQuery(string userId)
        {
            return this.GetUserDonationHistoryBaseQuery(userId)
                .Include(p => p.FoodBank)
                .Include(p => p.PaymentList)
                .OrderByDescending(p => p.DonationDate);
        }

        private IQueryable<Donation> ApplyUserDonationHistorySearch(IQueryable<Donation> query, string search)
        {
            if (string.IsNullOrWhiteSpace(search))
            {
                return query;
            }

            string term = search.Trim();
            string termLower = term.ToLowerInvariant();
            bool hasAmount = double.TryParse(term, NumberStyles.Any, CultureInfo.InvariantCulture, out double amount)
                || double.TryParse(term, NumberStyles.Any, CultureInfo.CurrentCulture, out amount);
            bool hasYear = int.TryParse(term, NumberStyles.Integer, CultureInfo.InvariantCulture, out int year)
                && term.Length == 4;

            return query.Where(p =>
                (p.FoodBank != null && p.FoodBank.Name.ToLower().Contains(termLower))
                || p.PaymentStatus.ToString().ToLower().Contains(termLower)
                || (p.Nif != null && p.Nif.Contains(term))
                || p.PublicId.ToString().ToLower().Contains(termLower)
                || (hasAmount && Math.Abs(p.DonationAmount - amount) < 0.01)
                || (hasYear && p.DonationDate.Year == year)
                || (termLower.Contains("paypal") && p.PaymentList.OfType<PayPalPayment>().Any())
                || ((termLower.Contains("cart") || termLower.Contains("cred") || termLower.Contains("card") || termLower.Contains("crédito") || termLower.Contains("credito"))
                    && p.PaymentList.OfType<CreditCardPayment>().Any())
                || (termLower.Contains("mbway") && p.PaymentList.OfType<MBWayPayment>().Any())
                || ((termLower.Contains("multi") || termLower.Contains("banco"))
                    && p.PaymentList.OfType<MultiBankPayment>().Any()));
        }
    }
}
