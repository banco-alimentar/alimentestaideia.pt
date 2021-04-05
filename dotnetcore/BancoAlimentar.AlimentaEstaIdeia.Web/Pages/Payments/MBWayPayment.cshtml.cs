using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using BancoAlimentar.AlimentaEstaIdeia.Model;
using BancoAlimentar.AlimentaEstaIdeia.Repository;
using Easypay.Rest.Client.Api;
using Easypay.Rest.Client.Client;
using Easypay.Rest.Client.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Pages.Payments
{
    public class MBWayPaymentModel : PageModel
    {
        private readonly IUnitOfWork context;
        private readonly IConfiguration configuration;
        private readonly IStringLocalizer localizer;
        private readonly SinglePaymentApi easyPayApiClient;

        public MBWayPaymentModel(
            IUnitOfWork context,
            IConfiguration configuration,
            IStringLocalizerFactory stringLocalizerFactory)
        {
            this.context = context;
            this.configuration = configuration;
            this.localizer = stringLocalizerFactory.Create("Pages.MBWayPayment", System.Reflection.Assembly.GetExecutingAssembly().GetName().Name);

            Configuration easypayConfig = new Configuration();
            easypayConfig.BasePath = this.configuration["Easypay:BaseUrl"] + "/2.0";
            easypayConfig.ApiKey.Add("AccountId", this.configuration["Easypay:AccountId"]);
            easypayConfig.ApiKey.Add("ApiKey", this.configuration["Easypay:ApiKey"]);
            easypayConfig.DefaultHeaders.Add("Content-Type", "application/json");
            easypayConfig.UserAgent = $" {GetType().Assembly.GetName().Name}/{GetType().Assembly.GetName().Version.ToString()}(Easypay.Rest.Client/{Configuration.Version})";
            this.easyPayApiClient = new SinglePaymentApi(easypayConfig);
        }

        [BindProperty]
        public int DonationId { get; set; }

        public Donation Donation { get; set; }

        public PaymentStatus PaymentStatus { get; set; }


        public async Task<IActionResult> OnGetAsync(int donationId, Guid paymentId)
        {
            if (TempData["Donation"] != null)
            {
                donationId = (int)TempData["Donation"];
            }
            else
            {
                var targetDonationId = HttpContext.Session.GetInt32(DonationModel.DonationIdKey);
                if (targetDonationId.HasValue)
                {
                    donationId = targetDonationId.Value;
                }
            }

            //if (TempData["mbway.int-tx-key"] != null)
            //{
            //    transactionKey = (Guid)TempData["mbway.int-tx-key"];
            //}
            //else
            //{
            //    var targetTransactionKey = HttpContext.Session.GetString("mbway.int-tx-key");
            //    if (transactionKey != Guid.Empty)
            //    {
            //        transactionKey = transactionKey;
            //    }
            //}

            if (TempData["mbway.paymend-id"] != null)
            {
                paymentId = (Guid)TempData["mbway.paymend-id"];
            }
            else
            {
                var targetPaymentId = HttpContext.Session.GetString("mbway.paymend-id");
                if (!string.IsNullOrEmpty(targetPaymentId))
                {
                    paymentId = Guid.Parse(targetPaymentId);
                }
            }

            //string.Format(localizer["SuggestOtherPaymentMethod"].Value, HtmlEncoder.Default.Encode("./Payment"));

            Donation = this.context.Donation.GetFullDonationById(donationId);
            PaymentStatus = Donation.PaymentStatus;
            SinglePaymentWithTransactionsResponse spResp = await easyPayApiClient.GetSinglePaymentAsync(paymentId, CancellationToken.None);

            // Validate Payment status (EasyPay+Repository)
            if (spResp.PaymentStatus == "pending" && Donation.PaymentStatus == PaymentStatus.WaitingPayment) {
                PaymentStatus = PaymentStatus.WaitingPayment;
                Response.Headers.Add("Refresh", "5");
            }
            else if (spResp.PaymentStatus == "paid" && Donation.PaymentStatus == PaymentStatus.Payed) {
                PaymentStatus = PaymentStatus.Payed;
                return RedirectToPage("/Thanks");
            }
            else {
                PaymentStatus = Donation.PaymentStatus = PaymentStatus.ErrorPayment;
                this.context.Complete();
            }

            return Page();
        }
    }
}
