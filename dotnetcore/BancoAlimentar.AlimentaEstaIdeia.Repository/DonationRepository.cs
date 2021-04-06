namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.ViewModel;
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

        public int GetDonationIdFromPublicId(Guid publicId)
        {
            int result = 0;

            result = this.DbContext.Donations.Where(p => p.PublicId == publicId).Select(p => p.Id).FirstOrDefault();

            return result;
        }

        public T FindPaymentByType<T>(int donationId)
            where T : class
        {
            T result = default(T);

            List<BasePayment> payments = this.DbContext.PaymentItems
                .Where(p => p.Donation.Id == donationId)
                .Include(p => p.Payment)
                .Select(p => p.Payment)
                .ToList();

            if (payments != null)
            {
                result = payments.OfType<T>().FirstOrDefault();
            }

            return result;
        }

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

        public void UpdateMultiBankPayment(Donation donation, string transactionKey, string entity, string reference)
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

        public int CompleteMultiBankPayment(string id, string transactionkey, string type, string status, string message)
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
                }

                payment.EasyPayPaymentId = id;
                payment.Type = type;
                payment.Status = status;
                payment.Message = message;
                this.DbContext.SaveChanges();
            }

            return result;
        }

        public void CreateMBWayPayment(Donation donation, string transactionKey, string alias)
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
                this.DbContext.MBWayPayments.Add(value);
                this.DbContext.SaveChanges();
            }
        }

        public void CreateCreditCardPaymnet(Donation donation, string transactionKey, string url)
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

        public void CompleteCreditCardPayment(
            string id,
            string transactionkey,
            float requested,
            float paid,
            float fixedFee,
            float variableFee,
            float tax,
            float transfer)
        {
            CreditCardPayment payment = this.DbContext.CreditCardPayments
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
                }

                payment.EasyPayPaymentId = id;
                payment.Requested = requested;
                payment.Paid = paid;
                payment.FixedFee = fixedFee;
                payment.VariableFee = variableFee;
                payment.Tax = tax;
                payment.Transfer = transfer;
                this.DbContext.SaveChanges();
            }
        }

        public int CompleteMBWayPayment(
            string id,
            string transactionkey,
            float requested,
            float paid,
            float fixedFee,
            float variableFee,
            float tax,
            float transfer)
        {
            int result = -1;
            MBWayPayment payment = this.DbContext.MBWayPayments
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
                }

                payment.EasyPayPaymentId = id;
                payment.Requested = requested;
                payment.Paid = paid;
                payment.FixedFee = fixedFee;
                payment.VariableFee = variableFee;
                payment.Tax = tax;
                payment.Transfer = transfer;
                this.DbContext.SaveChanges();
            }

            return result;
        }

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
