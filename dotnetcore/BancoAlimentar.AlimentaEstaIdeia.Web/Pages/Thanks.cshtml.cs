namespace BancoAlimentar.AlimentaEstaIdeia.Web.Pages
{
    using System;
    using System.IO;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Pages.Account.Manage;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Extensions;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Telemetry;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Localization;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Localization;

    public class ThanksModel : PageModel
    {
        private readonly UserManager<WebUser> userManager;
        private readonly IUnitOfWork context;
        private readonly IStringLocalizerFactory stringLocalizerFactory;
        private readonly IConfiguration configuration;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IViewRenderService renderService;
        private readonly IStringLocalizer localizer;

        public ThanksModel(
            UserManager<WebUser> userManager,
            IUnitOfWork context,
            IViewLocalizer localizer,
            IHtmlLocalizer<ThanksModel> html,
            IStringLocalizerFactory stringLocalizerFactory,
            IConfiguration configuration,
            IWebHostEnvironment webHostEnvironment,
            IViewRenderService renderService)
        {
            this.userManager = userManager;
            this.context = context;
            this.stringLocalizerFactory = stringLocalizerFactory;
            this.configuration = configuration;
            this.webHostEnvironment = webHostEnvironment;
            this.renderService = renderService;
            this.localizer = stringLocalizerFactory.Create("Pages.Thanks", System.Reflection.Assembly.GetExecutingAssembly().GetName().Name);
        }

        public int DonationId { get; set; }

        public WebUser CurrentUser { get; set; }

        [BindProperty]
        public string TwittMessage { get; set; }

        public async Task OnGet(int id)
        {
#if RELEASE
            id = 0;
#endif
            if (TempData["Donation"] != null)
            {
                id = (int)TempData["Donation"];
            }

            CurrentUser = await userManager.GetUserAsync(new ClaimsPrincipal(User.Identity));
            this.DonationId = id;

            Donation donation = this.context.Donation.GetFullDonationById(id);
            if (donation != null)
            {
                string foodBank = "Lisbon";
                if (donation.FoodBank != null && !string.IsNullOrEmpty(donation.FoodBank.Name))
                {
                    foodBank = donation.FoodBank.Name;
                }

                TwittMessage = string.Format(localizer.GetString("TwittMessage"), donation.DonationAmount, foodBank);
                if (this.configuration.IsSendingEmailEnabled())
                {
                    await SendThanksEmail(donation.User.Email, donation.PublicId.ToString(), donation);
                }
            }

            CompleteDonationFlow(HttpContext);
        }

        public static void CompleteDonationFlow(HttpContext context)
        {
            if (context != null)
            {
                context.Items.Remove(DonationFlowTelemetryInitializer.DonationSessionKey);
                context.Session.Remove(DonationFlowTelemetryInitializer.DonationSessionKey);
            }
        }

        public async Task SendThanksEmail(string email, string publicDonationId, Donation donation)
        {
            string bodyFilePath = Path.Combine(this.webHostEnvironment.WebRootPath, this.configuration.GetFilePath("Email.PaymentToDonor.Body.Path"));
            string html = System.IO.File.ReadAllText(bodyFilePath);
            html = string.Format(html, publicDonationId);
            if (donation.WantsReceipt.HasValue && donation.WantsReceipt.Value)
            {
                GenerateInvoiceModel generateInvoiceModel = new GenerateInvoiceModel(
                    this.userManager,
                    this.context,
                    this.renderService,
                    this.webHostEnvironment,
                    this.configuration,
                    this.stringLocalizerFactory);

                Tuple<Invoice, Stream> pdfFile = await generateInvoiceModel.GenerateInvoiceInternalAsync(donation.PublicId.ToString());
                Mail.SendMail(
                    html,
                    this.configuration["Email.PaymentToDonor.Subject"],
                    email,
                    pdfFile.Item2,
                    $"RECIBO Nº B{DateTime.Now.Year}-{pdfFile.Item1.Id}.pdf",
                    this.configuration);
            }
            else
            {
                Mail.SendMail(html, this.configuration["Email.PaymentToDonor.Subject"], email, null, null, this.configuration);
            }
        }
    }
}
