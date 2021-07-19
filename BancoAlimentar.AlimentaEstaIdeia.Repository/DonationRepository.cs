// -----------------------------------------------------------------------
// <copyright file="DonationRepository.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.ViewModel;
    using Easypay.Rest.Client.Model;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// Default implementation for the <see cref="Donation"/> repository pattern.
    /// </summary>
    public class DonationRepository : GenericRepository<Donation>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DonationRepository"/> class.
        /// </summary>
        /// <param name="context"><see cref="ApplicationDbContext"/> instance.</param>
        public DonationRepository(ApplicationDbContext context)
            : base(context)
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
                int sum = this.DbContext.DonationItems
                    .Where(p => p.ProductCatalogue == product && p.Donation.PaymentStatus == PaymentStatus.Payed)
                    .Sum(p => p.Quantity);
                double total = product.Quantity.Value * sum;
                result.Add(new TotalDonationsResult()
                {
                    Cost = product.Cost,
                    Description = product.Description,
                    IconUrl = product.IconUrl,
                    Name = product.Name,
                    Quantity = product.Quantity,
                    Total = sum,
                    TotalCost = total,
                    UnitOfMeasure = product.UnitOfMeasure,
                });
            }

            return result;
        }

        /// <summary>
        /// Associated that donation to the user.
        /// </summary>
        /// <param name="publicDonationId">The public donation id.</param>
        /// <param name="user">A reference to the <see cref="WebUser"/>.</param>
        public void ClaimDonationToUser(string publicDonationId, WebUser user)
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
                }
            }
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
        public void UpdateCreditCardPayment(Guid publicId, string status)
        {
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
            }

            CreditCardPayment targetPayment = this.FindPaymentByType<CreditCardPayment>(donation.Id);
            if (targetPayment != null)
            {
                targetPayment.Status = status;
            }

            this.DbContext.SaveChanges();
        }

        /// <summary>
        /// Updated the paypal payment status.
        /// </summary>
        /// <param name="donation">A reference to the <see cref="Donation"/>.</param>
        /// <param name="paymentId">Paypal payment id.</param>
        /// <param name="token">Paypal token.</param>
        /// <param name="payerId">Paypal payer id.</param>
        public void UpdateDonationPaymentId(Donation donation, string paymentId, string token = null, string payerId = null)
        {
            if (donation != null && !string.IsNullOrEmpty(paymentId))
            {
                PayPalPayment paypalPayment = this.DbContext.PayPalPayments
                    .Where(p => p.PayPalPaymentId == paymentId)
                    .FirstOrDefault();

                if (paypalPayment == null)
                {
                    paypalPayment = new PayPalPayment();
                    paypalPayment.Created = DateTime.UtcNow;

                    this.DbContext.PayPalPayments.Add(paypalPayment);
                }

                paypalPayment.PayPalPaymentId = paymentId;
                paypalPayment.Token = token;
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
            }
        }

        /// <summary>
        /// Update the multibanco payment status.
        /// </summary>
        /// <param name="donation">A reference to the <see cref="Donation"/>.</param>
        /// <param name="easyPayId">EasyPay transaction id.</param>
        /// <param name="transactionKey">Easypay transaction key. Used to update in the future the status of the payment.</param>
        /// <param name="entity">Multibanco entity id.</param>
        /// <param name="reference">Multibanco reference id.</param>
        public void UpdateMultiBankPayment(Donation donation, string easyPayId, string transactionKey, string entity, string reference)
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
            }
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
        /// Completed the multibanco payment.
        /// </summary>
        /// <param name="easyPayId">This is the Easy Pay id for the transaction.</param>
        /// <param name="transactionkey">Our internal transaction key.</param>
        /// <param name="type">Type of payment.</param>
        /// <param name="status">The new status for the multibanco payment.</param>
        /// <param name="message">Easypay status message.</param>
        /// <returns>Return the donation id for this multibanco payment.</returns>
        public int CompleteMultiBankPayment(string easyPayId, string transactionkey, string type, string status, string message)
        {
            int result = -1;

            MultiBankPayment payment = this.DbContext.MultiBankPayments
                .Where(p => p.TransactionKey == transactionkey)
                .FirstOrDefault();

            if (payment != null)
            {
                PaymentItem paymentItem = this.DbContext.PaymentItems
                    .Include(p => p.Donation)
                    .Where(p => p.Payment.Id == payment.Id)
                    .FirstOrDefault();

                if (paymentItem != null && paymentItem.Donation != null)
                {
                    paymentItem.Donation.PaymentStatus = PaymentStatus.Payed;
                    result = paymentItem.Donation.Id;
                    paymentItem.Donation.ConfirmedPayment = payment;
                }

                payment.EasyPayPaymentId = easyPayId;
                payment.Type = type;
                payment.Status = status;
                payment.Message = message;
                payment.Completed = DateTime.UtcNow;
                this.DbContext.SaveChanges();
            }

            return result;
        }

        /// <summary>
        /// Create a new MBWay payment.
        /// </summary>
        /// <param name="donation">A reference to the <see cref="Donation"/>.</param>
        /// <param name="easyPayId">EasyPay transaction id.</param>
        /// <param name="transactionKey">Our internal tranaction key.</param>
        /// <param name="alias">Easypay alias for the payment type.</param>
        public void CreateMBWayPayment(Donation donation, string easyPayId, string transactionKey, string alias)
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
            }
        }

        /// <summary>
        /// Create a credit card payment.
        /// </summary>
        /// <param name="donation">A reference to the <see cref="Donation"/>.</param>
        /// <param name="easyPayId">EasyPay transaction id.</param>
        /// <param name="transactionKey">Our internal tranaction key.</param>
        /// <param name="url">The url to redirect the user.</param>
        public void CreateCreditCardPaymnet(Donation donation, string easyPayId, string transactionKey, string url)
        {
            if (donation != null && !string.IsNullOrEmpty(transactionKey))
            {
                CreditCardPayment value = new CreditCardPayment();
                if (donation.Payments == null)
                {
                    donation.Payments = new List<PaymentItem>();
                }

                donation.Payments.Add(new PaymentItem() { Donation = donation, Payment = value });
                value.Created = DateTime.UtcNow;
                value.TransactionKey = transactionKey;
                value.Url = url;
                this.DbContext.CreditCardPayments.Add(value);
                this.DbContext.SaveChanges();
            }
        }

        /// <summary>
        /// Complete the credit card payment.
        /// </summary>
        /// <param name="easyPayId">This is the Easy Pay id for the transaction.</param>
        /// <param name="transactionKey">Our internal tranaction key.</param>
        /// <param name="requested">Amount of money requested.</param>
        /// <param name="paid">Amount of money payed.</param>
        /// <param name="fixedFee">Fixed fee for the transaction.</param>
        /// <param name="variableFee">Variable fee for the transaction.</param>
        /// <param name="tax">Tax associated to the transaction.</param>
        /// <param name="transfer">Amount of money trasnfer.</param>
        /// <returns>The donation id.</returns>
        public int CompleteCreditCardPayment(
            string easyPayId,
            string transactionKey,
            float requested,
            float paid,
            float fixedFee,
            float variableFee,
            float tax,
            float transfer)
        {
            int donationId = 0;
            CreditCardPayment payment = this.DbContext.CreditCardPayments
                .Where(p => p.TransactionKey == transactionKey)
                .FirstOrDefault();

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
                }
                else
                {
                    EventTelemetry donationNotFound = new EventTelemetry("Donation-CreditCardPayment-NotFound");
                    donationNotFound.Properties.Add("CreditCardPaymentTransactionKey", transactionKey);
                    donationNotFound.Properties.Add("PaymentId", payment.Id.ToString());
                    this.TelemetryClient.TrackEvent(donationNotFound);
                }

                payment.EasyPayPaymentId = easyPayId;
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
                EventTelemetry creditCardPaymentNotFound = new EventTelemetry("CreditCardPayment-NotFound");
                creditCardPaymentNotFound.Properties.Add("CreditCardTransactionKey", transactionKey);
                creditCardPaymentNotFound.Properties.Add("EasyPayId", easyPayId);
                this.TelemetryClient.TrackEvent(creditCardPaymentNotFound);
            }

            return donationId;
        }

        /// <summary>
        /// Completed the MBWay payment.
        /// </summary>
        /// <param name="easyPayId">This is the Easy Pay id for the transaction.</param>
        /// <param name="transactionKey">Our internal tranaction key.</param>
        /// <param name="requested">Amount of money requested.</param>
        /// <param name="paid">Amount of money payed.</param>
        /// <param name="fixedFee">Fixed fee for the transaction.</param>
        /// <param name="variableFee">Variable fee for the transaction.</param>
        /// <param name="tax">Tax associated to the transaction.</param>
        /// <param name="transfer">Amount of money trasnfer.</param>
        /// <returns>Return the donation id for this MBWay payment.</returns>
        public int CompleteMBWayPayment(
            string easyPayId,
            string transactionKey,
            float requested,
            float paid,
            float fixedFee,
            float variableFee,
            float tax,
            float transfer)
        {
            int result = -1;
            MBWayPayment payment = this.DbContext.MBWayPayments
                .Where(p => p.TransactionKey == transactionKey)
                .FirstOrDefault();

            if (payment != null)
            {
                PaymentItem paymentItem = this.DbContext.PaymentItems
                    .Include(p => p.Donation)
                    .Where(p => p.Payment.Id == payment.Id)
                    .FirstOrDefault();

                if (paymentItem != null && paymentItem.Donation != null)
                {
                    paymentItem.Donation.PaymentStatus = PaymentStatus.Payed;
                    result = paymentItem.Donation.Id;
                    paymentItem.Donation.ConfirmedPayment = payment;
                }
                else
                {
                    EventTelemetry donationNotFound = new EventTelemetry("Donation-MBWayPayment-NotFound");
                    donationNotFound.Properties.Add("MBWayTransactionKey", transactionKey);
                    donationNotFound.Properties.Add("PaymentId", payment.Id.ToString());
                    this.TelemetryClient.TrackEvent(donationNotFound);
                }

                payment.EasyPayPaymentId = easyPayId;
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
                EventTelemetry mbwayPaymentNotFound = new EventTelemetry("MBWayPayment-NotFound");
                mbwayPaymentNotFound.Properties.Add("MBWayTransactionKey", transactionKey);
                mbwayPaymentNotFound.Properties.Add("EasyPayId", easyPayId);
                this.TelemetryClient.TrackEvent(mbwayPaymentNotFound);
            }

            return result;
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
                .Include(p => p.FoodBank)
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
                .Include("Payments.Payment")
                .Where(p => p.User.Id == userId)
                .OrderByDescending(p => p.DonationDate)
                .ToList();
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
    }
}
