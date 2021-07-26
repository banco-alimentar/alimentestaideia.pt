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
    using BancoAlimentar.AlimentaEstaIdeia.Web.Services;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.AzureTables;
    using Easypay.Rest.Client.Client;
    using Easypay.Rest.Client.Model;
    using Microsoft.ApplicationInsights;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.Extensions.Configuration;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using PayPal.Api;

    public class PaymentModel : PageModel
    {
        private readonly IConfiguration configuration;
        private readonly IUnitOfWork context;
        private readonly TelemetryClient telemetryClient;
        private readonly EasyPayBuilder easyPayBuilder;

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

        [BindProperty]
        public bool IsMultibanco { get; set; }

        [BindProperty]
        public int DonationId { get; set; }

        [BindProperty]
        [Phone]
        [Required]
        public string PhoneNumber { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the current error in the payment system.
        /// </summary>
        [BindProperty]
        public bool PaymentStatusError { get; set; }

        public bool PaymentStatusRecusado { get; set; }

        [BindProperty]
        public MultiBankPayment MultiBankPayment { get; set; }

        [BindProperty]
        public string MBWayError { get; set; }

        public IActionResult OnGet(int donationId = 0, Guid publicDonationId = default(Guid))
        {
            if (TempData["Donation"] != null)
            {
                donationId = (int)TempData["Donation"];
            }
            else
            {
                if (publicDonationId != default(Guid))
                {
                    donationId = this.context.Donation.GetDonationIdFromPublicId(publicDonationId);
                }
                else
                {
                    var targetDonationId = HttpContext.Session.GetInt32(DonationModel.DonationIdKey);
                    if (targetDonationId.HasValue)
                    {
                        donationId = targetDonationId.Value;
                    }
                }
            }

            if (TempData["Paymen-Status"] != null && (string)TempData["Paymen-Status"] == "err")
            {
                PaymentStatusError = true;
            }

            if (TempData["Paymen-MBWayError"] != null)
            {
                MBWayError = (string)TempData["Paymen-MBWayError"];
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
                return RedirectToPage("./Thanks");
            }
            else
            {
                if (Donation == null)
                {
                    this.telemetryClient.TrackEvent("DonationIsNull", new Dictionary<string, string>()
                    {
                        { "OriginalDonationId", donationId.ToString() },
                        { "PublicDonationId", publicDonationId.ToString() },
                    });

                    return RedirectToPage("./Donation");
                }
                else
                {
                    return Page();
                }
            }
        }

        public async Task<IActionResult> OnPostMbWayAsync()
        {
            string transactionKey = Guid.NewGuid().ToString();
            SinglePaymentResponse targetPayment = await CreateEasyPayPaymentAsync(transactionKey, SinglePaymentRequest.MethodEnum.Mbw);

            if (targetPayment != null)
            {
                if (targetPayment.Status == "error")
                {
                    TempData["Paymen-Status"] = "err";
                    TempData["Donation"] = Donation.Id;
                    return this.RedirectToPage("./Payment");
                }
                else
                {
                    this.context.Donation.CreateMBWayPayment(
                        Donation,
                        targetPayment.Id.ToString(),
                        transactionKey,
                        targetPayment.Method.Alias);

                    TempData["Donation"] = this.DonationId;
                    HttpContext.Session.SetInt32(DonationModel.DonationIdKey, this.DonationId);
                    TempData["mbway.paymend-id"] = targetPayment.Id;
                    HttpContext.Session.SetString("mbway.paymend-id", targetPayment.Id.ToString());
                }

                return this.RedirectToPage("./Payments/MBWayPayment");
            }

            TempData["Donation"] = this.Donation.Id;
            TempData["Paymen-MBWayError"] = MBWayError;
            return RedirectToPage("./Payment");
        }

        public async Task<IActionResult> OnPostCreditCardAsync()
        {
            string transactionKey = Guid.NewGuid().ToString();
            SinglePaymentResponse targetPayment = await CreateEasyPayPaymentAsync(transactionKey, SinglePaymentRequest.MethodEnum.Cc);
            string url = targetPayment.Method.Url;
            this.context.Donation.CreateCreditCardPaymnet(
                Donation,
                targetPayment.Id.ToString(),
                transactionKey,
                url);

            return this.Redirect(url);
        }

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

            TempData["Donation"] = Donation.Id;

            return this.RedirectToPage("./Payments/Multibanco");
        }

        /// <summary>
        /// This is the starting operation for paying with PayPal. Here we're setuping the PayPal api to redirect the user to the payment web site.
        /// </summary>
        /// <returns>A reference to <see cref="IActionResult"/>.</returns>
        public IActionResult OnPostPaypal()
        {
            Donation = this.context.Donation.GetFullDonationById(DonationId);

            IActionResult result = null;

            if (Donation != null)
            {
                var config = GetPayPalConfiguration();
                var accessToken = new OAuthTokenCredential(config).GetAccessToken();
                var apiContext = new APIContext(accessToken);
                apiContext.Config = config;

                var payer = new Payer() { payment_method = "paypal" };

                Uri originalUri = this.Request.GetRequestOriginalRawUri();

                var redirUrls = new RedirectUrls
                {
                    cancel_url = string.Format("{0}://{1}{2}", this.Request.Scheme, this.Request.Host.Value, "/Payment?donationId=" + Donation.Id),
                    return_url = string.Format("{0}://{1}{2}", this.Request.Scheme, this.Request.Host.Value, "/Payment?handler=ReferencePayedViaPayPal&donationId=" + Donation.Id),
                };

                var itemList = new ItemList
                {
                    items = new List<Item>(),
                };

                itemList.items.Add(new Item
                {
                    name = "Donativo Banco Alimentar",
                    currency = "EUR",
                    price = Convert.ToString(Donation.DonationAmount, new CultureInfo("en-US")),
                    quantity = "1",
                    sku = Donation.ServiceReference,
                });

                var details = new Details
                {
                    tax = "0",
                    shipping = "0",
                    subtotal = Convert.ToString(Donation.DonationAmount, new CultureInfo("en-US")),
                };

                var amount = new Amount
                {
                    currency = "EUR",
                    total = Convert.ToString(Donation.DonationAmount, new CultureInfo("en-US")),
                    details = details,
                };

                var transactionList = new List<PayPal.Api.Transaction>();

                transactionList.Add(new PayPal.Api.Transaction
                {
                    description = "Donativo Banco Alimentar",
                    amount = amount,
                    item_list = itemList,
                });

                var payment = new Payment
                {
                    intent = "sale",
                    payer = payer,
                    redirect_urls = redirUrls,
                    transactions = transactionList,
                };

                var createdPayment = payment.Create(apiContext);

                //this.context.Donation.UpdateDonationPaymentId(Donation, createdPayment.id);

                var link = createdPayment.links
                    .Where(p => p.rel.ToLowerInvariant() == "approval_url")
                    .FirstOrDefault();

                if (link != null)
                {
                    result = Redirect(link.href);
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
        public IActionResult OnGetReferencePayedViaPayPal(int donationId, string paymentId, string token, string payerId)
        {
            Donation = this.context.Donation.GetFullDonationById(donationId);
            var config = GetPayPalConfiguration();
            var accessToken = new OAuthTokenCredential(config).GetAccessToken();
            var apiContext = new APIContext(accessToken);
            apiContext.Config = config;

            var paymentExecution = new PaymentExecution { payer_id = payerId };
            var payment = new Payment { id = paymentId };

            var executedPayment = payment.Execute(apiContext, paymentExecution);

            if (executedPayment.state.Equals("approved"))
            {
                Donation.PaymentStatus = PaymentStatus.Payed;
                this.context.Complete();
                this.context.Donation.UpdateDonationPaymentId(Donation, paymentId, executedPayment.state, token, payerId);
                TempData["Donation"] = Donation.Id;
                return RedirectToPage("./Thanks");
            }

            TempData["Donation"] = Donation.Id;
            TempData["Donation-Status"] = executedPayment.state;
            return RedirectToAction("./Payment");
        }

        private Dictionary<string, string> GetPayPalConfiguration()
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            result.Add("mode", configuration["PayPal:mode"]);
            result.Add("clientId", configuration["PayPal:clientId"]);
            result.Add("clientSecret", configuration["PayPal:clientSecret"]);
            return result;
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

            this.telemetryClient.TrackEvent("CreateSinglePayment", auditingTable.GetProperties());
            auditingTable.SaveEntity();

            return response;
        }
    }
}
