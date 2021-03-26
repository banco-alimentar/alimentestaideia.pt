namespace BancoAlimentar.AlimentaEstaIdeia.Web.Pages
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Globalization;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using Easypay.Rest.Client.Api;
    using Easypay.Rest.Client.Client;
    using Easypay.Rest.Client.Model;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.Extensions.Configuration;
    using Newtonsoft.Json;
    using PayPal.Api;

    public class PaymentModel : PageModel
    {
        private readonly IConfiguration configuration;
        private readonly IUnitOfWork context;
        private readonly SinglePaymentApi easyPayApiClient;

        public PaymentModel(
            IConfiguration configuration,
            IUnitOfWork context)
        {
            this.configuration = configuration;
            this.context = context;

            Configuration easypayConfig = new Configuration();
            easypayConfig.BasePath = this.configuration["Easypay:BaseUrl"] + "/2.0";
            easypayConfig.ApiKey.Add("AccountId", this.configuration["Easypay:AccountId"]);
            easypayConfig.ApiKey.Add("ApiKey", this.configuration["Easypay:ApiKey"]);
            easypayConfig.DefaultHeaders.Add("Content-Type", "application/json");
            easypayConfig.UserAgent = $" {GetType().Assembly.GetName().Name}/{GetType().Assembly.GetName().Version.ToString()}(Easypay.Rest.Client/{Configuration.Version})";
            this.easyPayApiClient = new SinglePaymentApi(easypayConfig);

        }

        public Donation Donation { get; set; }

        [BindProperty]
        public bool IsMultibanco { get; set; }

        [BindProperty]
        public int DonationId { get; set; }

        [BindProperty]
        [Phone]
        [Required]
        public string PhoneNumber { get; set; }

        [BindProperty]
        public bool PaymentStatusError { get; set; }

        public bool PaymentStatusRecusado { get; set; }

        public void OnGet(int donationId)
        {
            if (TempData["Donation"] != null)
            {
                donationId = (int)TempData["Donation"];
            }

            if (TempData["Paymen-Status"] != null && (string)TempData["Paymen-Status"] == "err")
            {
                PaymentStatusError = true;
            }

            Donation = this.context.Donation.GetFullDonationById(donationId);
            if (Donation != null && Donation.User != null)
            {
                PhoneNumber = Donation.User.PhoneNumber;
            }
        }

        private async Task<SinglePaymentResponse> CreateEasyPayPaymentAsync(string transactionKey, SinglePaymentRequest.MethodEnum method)
        {
            Donation = this.context.Donation.GetFullDonationById(DonationId);
            if (Donation.User.PhoneNumber != PhoneNumber)
            {
                Donation.User.PhoneNumber = PhoneNumber;
                this.context.Complete();
            }

            SinglePaymentRequest spReq = new SinglePaymentRequest()
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
                Value = (float)Donation.ServiceAmount,
                Method = method,
                Capture = new SinglePaymentRequestCapture(transactionKey: transactionKey, descriptive: "Alimente esta ideia Donation"),
            };

            SinglePaymentResponse spResp = await easyPayApiClient.CreateSinglePaymentAsync(spReq, CancellationToken.None);
            return spResp;

        }

        public async Task<IActionResult> OnPostMbWayAsync()
        {
            //string transactionKey = Guid.NewGuid().ToString();
            //ApiResponse<PaymentSingle> apiResponse = await CreateEasyPayPayment(transactionKey, "mbw");
            //PaymentSingle targetPayment = (PaymentSingle)apiResponse.Content;

            //MBWayCreatePaymentResponse response = JsonConvert.DeserializeObject<MBWayCreatePaymentResponse>(apiResponse.RawContent);

            //if (response.status == "error")
            //{
            //    TempData["Paymen-Status"] = "err";
            //    TempData["Donation"] = Donation.Id;
            //    return this.RedirectToPage("./Payment");
            //}
            //else
            //{
            //    this.context.Donation.CreateMBWayPayment(
            //        Donation,
            //        transactionKey,
            //        response.method.alias);
            //}

            return this.RedirectToPage("./Payments/MBWayPayment");
        }

        public async Task<IActionResult> OnPostCreditCardAsync()
        {
            //string transactionKey = Guid.NewGuid().ToString();
            //ApiResponse<PaymentSingle> apiResponse = await CreateEasyPayPayment(transactionKey, "cc");
            //PaymentSingle targetPayment = (PaymentSingle)apiResponse.Content;
            //string url = targetPayment.Method.Url;
            //this.context.Donation.CreateCreditCardPaymnet(
            //    Donation,
            //    transactionKey,
            //    url);

            return this.Redirect(""); // url);
        }

        public async Task<IActionResult> OnPostPayWithMultibancoAsync()
        {
            string transactionKey = Guid.NewGuid().ToString();
            SinglePaymentResponse targetPayment = await CreateEasyPayPaymentAsync(transactionKey, SinglePaymentRequest.MethodEnum.Mb);
            this.context.Donation.UpdateMultiBankPayment(
                Donation,
                transactionKey,
                targetPayment.Method.Entity.ToString(),
                targetPayment.Method.Reference);

            TempData["Donation"] = Donation.Id;

            //string transactionKey = Guid.NewGuid().ToString();
            //ApiResponse<PaymentSingle> apiResponse = await CreateEasyPayPayment(transactionKey, "mb");
            //PaymentSingle targetPayment = (PaymentSingle)apiResponse.Content;
            //this.context.Donation.UpdateMultiBankPayment(
            //    Donation,
            //    transactionKey,
            //    targetPayment.Method.Entity.ToString(),
            //    targetPayment.Method.Reference);

            //TempData["Donation"] = Donation.Id;

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
                    price = Convert.ToString(Donation.ServiceAmount, new CultureInfo("en-US")),
                    quantity = "1",
                    sku = Donation.ServiceReference,
                });

                var details = new Details
                {
                    tax = "0",
                    shipping = "0",
                    subtotal = Convert.ToString(Donation.ServiceAmount, new CultureInfo("en-US")),
                };

                var amount = new Amount
                {
                    currency = "EUR",
                    total = Convert.ToString(Donation.ServiceAmount, new CultureInfo("en-US")),
                    details = details,
                };

                var transactionList = new List<Transaction>();

                transactionList.Add(new Transaction
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

                this.context.Donation.UpdateDonationPaymentId(Donation, createdPayment.id);

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
                this.context.Donation.UpdateDonationPaymentId(Donation, paymentId, token, payerId);
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

        public async Task On()
        {
        }
    }
}
