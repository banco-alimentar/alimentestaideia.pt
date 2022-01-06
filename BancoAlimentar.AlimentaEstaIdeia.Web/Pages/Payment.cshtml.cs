// -----------------------------------------------------------------------
// <copyright file="Payment.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Pages
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.AzureTables;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Services;
    using Easypay.Rest.Client.Client;
    using Easypay.Rest.Client.Model;
    using Microsoft.ApplicationInsights;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.Extensions.Configuration;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using PayPalCheckoutSdk.Orders;

    /// <summary>
    /// Payments model.
    /// </summary>
    public class PaymentModel : PageModel
    {
        private readonly IConfiguration configuration;
        private readonly IUnitOfWork context;
        private readonly TelemetryClient telemetryClient;
        private readonly EasyPayBuilder easyPayBuilder;

        /// <summary>
        /// Initializes a new instance of the <see cref="PaymentModel"/> class.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        /// <param name="context">Unit of work.</param>
        /// <param name="easyPayBuilder">Easypay API builder.</param>
        /// <param name="telemetryClient">Telemetry client.</param>
        public PaymentModel(
            IConfiguration configuration,
            IUnitOfWork context,
            EasyPayBuilder easyPayBuilder,
            TelemetryClient telemetryClient)
        {
            this.configuration = configuration;
            this.context = context;
            this.telemetryClient = telemetryClient;
            this.easyPayBuilder = easyPayBuilder;
        }

        /// <summary>
        /// Gets or sets the current donation.
        /// </summary>
        public Donation Donation { get; set; }

        /// <summary>
        /// Gets or sets the donation id.
        /// </summary>
        [BindProperty]
        public int DonationId { get; set; }

        /// <summary>
        /// Gets or sets the phone number.
        /// </summary>
        [BindProperty]
        [Phone]
        [Required]
        public string PhoneNumber { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the current error in the payment system.
        /// </summary>
        [BindProperty]
        public bool PaymentStatusError { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this is a multibanco payment.
        /// </summary>
        [BindProperty]
        public MultiBankPayment MultiBankPayment { get; set; }

        /// <summary>
        /// Gets or sets the MBWay error.
        /// </summary>
        [BindProperty]
        public string MBWayError { get; set; }

        /// <summary>
        /// Execute the get operation.
        /// </summary>
        /// <param name="publicId">Public donation id.</param>
        /// <param name="paymentStatus">Payment status parameter.</param>
        /// <param name="paymentMbwayError">Paymnet status error.</param>
        /// <returns>Page.</returns>
        public IActionResult OnGet(Guid publicId, string paymentStatus = null, string paymentMbwayError = null)
        {
            int donationId = 0;

            if (publicId != default(Guid))
            {
                donationId = this.context.Donation.GetDonationIdFromPublicId(publicId);
            }
            else
            {
                var targetDonationId = HttpContext.Session.GetInt32(DonationModel.DonationIdKey);
                if (targetDonationId.HasValue)
                {
                    donationId = targetDonationId.Value;
                }
            }

            if (!string.IsNullOrEmpty(paymentStatus) && paymentStatus == "err")
            {
                PaymentStatusError = true;
            }

            if (!string.IsNullOrEmpty(paymentMbwayError))
            {
                MBWayError = paymentMbwayError;
            }

            Donation = this.context.Donation.GetFullDonationById(donationId);
            if (Donation != null && Donation.User != null)
            {
                PhoneNumber = Donation.User.PhoneNumber;
            }

            MultiBankPayment = this.context.Donation.GetCurrentMultiBankPayment(donationId);
            if (MultiBankPayment != null && MultiBankPayment.Status == "Success")
            {
                MultiBankPayment = null;
            }

            if (Donation != null && Donation.PaymentStatus == PaymentStatus.Payed)
            {
                return RedirectToPage("./Thanks", new { PublicId = Donation.PublicId });
            }
            else
            {
                if (Donation == null)
                {
                    this.telemetryClient.TrackEvent("DonationIsNull", new Dictionary<string, string>()
                    {
                        { "OriginalDonationId", donationId.ToString() },
                        { "PublicDonationId", publicId.ToString() },
                    });

                    return RedirectToPage("./Donation");
                }
                else
                {
                    return Page();
                }
            }
        }

        /// <summary>
        /// Execute the payment in MBWay.
        /// </summary>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<IActionResult> OnPostMbWayAsync()
        {
            if (PhoneNumber.StartsWith("+351"))
            {
                PhoneNumber = PhoneNumber.Substring(0, "+351".Length);
            }

            if (PhoneNumber.StartsWith("+"))
            {
                // greater than 10 means phone number is +(xx)xxxxxxxxx, so we can take the last 9 numbers
                if (PhoneNumber.Length > 10)
                {
                    int portugalPhoneNumbersLenght = 9;
                    PhoneNumber = PhoneNumber.Substring(PhoneNumber.Length - portugalPhoneNumbersLenght, portugalPhoneNumbersLenght);
                }
            }

            string transactionKey = Guid.NewGuid().ToString();
            SinglePaymentResponse targetPayment = await CreateEasyPayPaymentAsync(transactionKey, SinglePaymentRequest.MethodEnum.Mbw);

            if (targetPayment != null)
            {
                if (targetPayment.Status == "error")
                {
                    return this.RedirectToPage("./Payment", new { Donation.PublicId, paymentStatus = "err" });
                }
                else
                {
                    this.context.Donation.CreateMBWayPayment(
                        Donation,
                        targetPayment.Id.ToString(),
                        transactionKey,
                        targetPayment.Method.Alias);
                }

                return this.RedirectToPage("./Payments/MBWayPayment", new { Donation.PublicId, paymentId = targetPayment.Id });
            }

            return RedirectToPage("./Payment", new { Donation.PublicId, paymentMbwayError = MBWayError });
        }

        /// <summary>
        /// Execute the payment using credit card.
        /// </summary>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<IActionResult> OnPostCreditCardAsync()
        {
            string transactionKey = Guid.NewGuid().ToString();
            SinglePaymentResponse targetPayment = await CreateEasyPayPaymentAsync(transactionKey, SinglePaymentRequest.MethodEnum.Cc);
            string url = targetPayment.Method.Url;
            this.context.Donation.CreateCreditCardPaymnet(
                Donation,
                targetPayment.Id.ToString(),
                transactionKey,
                url,
                DateTime.UtcNow);

            return this.Redirect(url);
        }

        /// <summary>
        /// Execute the payment using multibanco.
        /// </summary>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<IActionResult> OnPostPayWithMultibancoAsync()
        {
            string transactionKey = Guid.NewGuid().ToString();
            SinglePaymentResponse targetPayment = await CreateEasyPayPaymentAsync(transactionKey, SinglePaymentRequest.MethodEnum.Mb);
            this.context.Donation.UpdateMultiBankPayment(
                Donation,
                targetPayment.Id.ToString(),
                transactionKey,
                targetPayment.Method.Entity.ToString(),
                targetPayment.Method.Reference);

            return this.RedirectToPage("./Payments/Multibanco", new { Donation.PublicId });
        }

        /// <summary>
        /// This is the starting operation for paying with PayPal. Here we're setuping the PayPal api to redirect the user to the payment web site.
        /// </summary>
        /// <returns>A reference to <see cref="IActionResult"/>.</returns>
        public async Task<IActionResult> OnPostPaypalAsync()
        {
            var currency = "EUR";
            Donation = this.context.Donation.GetFullDonationById(DonationId);

            IActionResult result = null;

            if (Donation != null)
            {
                // Construct a request object and set desired parameters
                // Here, OrdersCreateRequest() creates a POST request to /v2/checkout/orders
                var order = new OrderRequest()
                {
                    CheckoutPaymentIntent = "CAPTURE",
                    PurchaseUnits = new List<PurchaseUnitRequest>()
                    {
                    new PurchaseUnitRequest()
                        {
                        AmountWithBreakdown = new AmountWithBreakdown()
                            {
                                CurrencyCode = currency,
                                Value = Convert.ToString(Donation.DonationAmount, new CultureInfo("en-US")),
                                AmountBreakdown = new AmountBreakdown
                                {
                                    ItemTotal = new Money
                                    {
                                        CurrencyCode = currency,
                                        Value = Convert.ToString(Donation.DonationAmount, new CultureInfo("en-US")),
                                    },
                                    Shipping = new Money
                                    {
                                        CurrencyCode = currency,
                                        Value = "0.00",
                                    },
                                    TaxTotal = new Money
                                    {
                                        CurrencyCode = currency,
                                        Value = "0.00",
                                    },
                                },
                            },
                        Items = new List<PayPalCheckoutSdk.Orders.Item>
                            {
                                new PayPalCheckoutSdk.Orders.Item
                                {
                                Name = "Donativo Banco Alimentar",
                                UnitAmount = new Money
                                    {
                                        CurrencyCode = currency,
                                        Value = Convert.ToString(Donation.DonationAmount, new CultureInfo("en-US")),
                                    },
                                Quantity = "1",
                                Sku = Donation.ServiceReference,
                                },
                            },
                        },
                    },

                    ApplicationContext = new ApplicationContext()
                    {
                        ReturnUrl = string.Format("{0}://{1}{2}", this.Request.Scheme, this.Request.Host.Value, "/Payment?handler=ReferencePayedViaPayPal&donationId=" + Donation.Id),
                        CancelUrl = string.Format("{0}://{1}{2}", this.Request.Scheme, this.Request.Host.Value, "/Payment?donationId=" + Donation.Id),
                    },
                };

                // Call API with your client and get a response for your call
                var request = new OrdersCreateRequest();
                request.Prefer("return=representation");
                request.RequestBody(order);

                var response = await PayPalClient.GetPayPalClient(configuration).Execute(request);

                var statusCode = response.StatusCode;
                var createdPayment = response.Result<PayPalCheckoutSdk.Orders.Order>();

                var link = createdPayment.Links
                        .Where(p => p.Rel.ToLowerInvariant() == "approve")
                        .FirstOrDefault();

                if (link != null)
                {
                    result = Redirect(link.Href);
                }
            }
            else
            {
                result = this.RedirectToPage("./Index");
            }

            return result;
        }

        /// <summary>
        /// This is end process for the PayPal payment. When the user successfully pay using PayPal.
        /// </summary>
        /// <param name="donationId">This is our Id for the <see cref="Donation"/>.</param>
        /// <param name="paymentId">PayPal payment id.</param>
        /// <param name="token">PayPal Token.</param>
        /// <param name="payerId">PayPal payer id.</param>
        /// <returns>A reference to <see cref="IActionResult"/>.</returns>
        public async Task<IActionResult> OnGetReferencePayedViaPayPalAsync(int donationId, string paymentId, string token, string payerId)
        {
            Donation = this.context.Donation.GetFullDonationById(donationId);

            var request = new OrdersCaptureRequest(token);
            request.Prefer("return=representation");
            request.RequestBody(new OrderActionRequest());
            var response = await PayPalClient.GetPayPalClient(configuration).Execute(request);

            var result = response.Result<PayPalCheckoutSdk.Orders.Order>();

            if (result.Status.Equals("COMPLETED"))
            {
                Donation.PaymentStatus = PaymentStatus.Payed;
                this.context.Complete();
                this.context.Donation.UpdateDonationPaymentId(Donation, result.Status, token, payerId);
                return RedirectToPage("./Thanks", new { Donation.PublicId });
            }

            return RedirectToAction("./Payment", new { Donation.PublicId, paymentStatus = result.Status });
        }

        private async Task<SinglePaymentResponse> CreateEasyPayPaymentAsync(string transactionKey, SinglePaymentRequest.MethodEnum method)
        {
            Donation = this.context.Donation.GetFullDonationById(DonationId);
            SinglePaymentAuditingTable auditingTable = new SinglePaymentAuditingTable(
                this.configuration,
                this.Donation.PublicId.ToString(),
                this.Donation.User.Id);

            auditingTable.AddProperty("TransactionKey", transactionKey);
            auditingTable.AddProperty("PaymentMethod", method.ToString());
            auditingTable.AddProperty("DonationId", Donation.Id);
            auditingTable.AddProperty("UserId", Donation.User.Id);
            auditingTable.AddProperty("Amount", Donation.DonationAmount);

            if (Donation.User.PhoneNumber != PhoneNumber)
            {
                Donation.User.PhoneNumber = PhoneNumber;
                this.context.Complete();
            }

            SinglePaymentRequest request = new SinglePaymentRequest()
            {
                Key = Donation.PublicId.ToString(),
                Type = SinglePaymentRequest.TypeEnum.Sale,
                Currency = SinglePaymentRequest.CurrencyEnum.EUR,
                Customer = new SinglePaymentUpdateRequestCustomer()
                {
                    Email = Donation.User.Email,
                    Name = Donation.User.UserName,
                    Phone = Donation.User.PhoneNumber,
                    PhoneIndicative = "+351",
                    FiscalNumber = Donation.User.Nif,
                    Language = Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName,
                    Key = Donation.User.Id,
                },
                Value = (float)Donation.DonationAmount,
                Method = method,
                Capture = new SinglePaymentRequestCapture(transactionKey: transactionKey, descriptive: "Alimente esta ideia Donation"),
            };
            SinglePaymentResponse response = null;
            try
            {
                response = await this.easyPayBuilder.GetSinglePaymentApi().CreateSinglePaymentAsync(request, CancellationToken.None);
                auditingTable.AddProperty("EasyPayId", response.Id);
            }
            catch (ApiException ex)
            {
                this.telemetryClient.TrackException(ex, new Dictionary<string, string>()
                    {
                        { "PublicId", request.Key },
                    });

                try
                {
                    auditingTable.AddProperty("Exception", ex.ToString());
                    if (ex.ErrorContent is string)
                    {
                        string json = (string)ex.ErrorContent;
                        JObject obj = JObject.Parse(json);
                        JArray errorList = (JArray)obj["message"];
                        StringBuilder stringBuilder = new StringBuilder();
                        foreach (var item in errorList.Children())
                        {
                            stringBuilder.Append(item.Value<string>());
                            stringBuilder.Append(Environment.NewLine);
                        }

                        MBWayError = stringBuilder.ToString();
                    }
                }
                catch (Exception excAuditing)
                {
                    this.telemetryClient.TrackException(excAuditing);
                }
            }

            this.telemetryClient.TrackEvent("CreateSinglePayment", auditingTable.GetProperties());
            auditingTable.SaveEntity();

            return response;
        }
    }
}
