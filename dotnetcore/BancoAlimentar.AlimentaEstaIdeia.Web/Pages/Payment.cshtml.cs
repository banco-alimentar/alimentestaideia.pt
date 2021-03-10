namespace BancoAlimentar.AlimentaEstaIdeia.Web.Pages
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.Extensions.Configuration;
    using PayPal.Api;

    public class PaymentModel : PageModel
    {
        private readonly IConfiguration configuration;
        private readonly IUnitOfWork context;

        public PaymentModel(
            IConfiguration configuration,
            IUnitOfWork context)
        {
            this.configuration = configuration;
            this.context = context;
        }

        public Donation Donation { get; set; }

        [BindProperty]
        public bool IsMultibanco { get; set; }

        [BindProperty]
        public int DonationId { get; set; }

        public void OnGet(int donationId)
        {
            if (TempData["Donation"] != null)
            {
                donationId = (int)TempData["Donation"];
            }

            Donation = this.context.Donation.GetFullDonationById(donationId);
        }

        public IActionResult OnPostPayWithMultibanco()
        {
            Donation = this.context.Donation.GetFullDonationById(DonationId);

            Donation.ServiceReference = SibsHelper.GenerateReference(configuration["Sibs:Entity"], Donation.Id, (decimal)Donation.ServiceAmount);
            Donation.ServiceEntity = configuration["Sibs:Entity"];

            this.context.Complete();

            return this.RedirectToPage("./Payments/Multibanco", new { id = Donation.Id });
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
                return RedirectToPage("./Thanks", new { id = Donation.Id });

            }

            return RedirectToAction("./Payment", new { id = Donation.Id, status = executedPayment.state });
        }

        private Dictionary<string, string> GetPayPalConfiguration()
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            result.Add("mode", configuration["PayPal:mode"]);
            result.Add("clientId", configuration["PayPal:clientId"]);
            result.Add("clientSecret", configuration["PayPal:clientSecret"]);
            return result;
        }

        public void EasyPay()
        {

        }
    }
}