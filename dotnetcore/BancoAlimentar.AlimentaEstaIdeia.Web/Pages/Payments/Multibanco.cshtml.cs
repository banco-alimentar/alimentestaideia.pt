namespace BancoAlimentar.AlimentaEstaIdeia.Web.Pages.Payments
{
    using System.IO;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Extensions;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.Extensions.Configuration;

    public class MultibancoModel : PageModel
    {
        private readonly IUnitOfWork context;
        private readonly IConfiguration configuration;
        private readonly IWebHostEnvironment webHostEnvironment;

        public MultibancoModel(
            IUnitOfWork context,
            IConfiguration configuration,
            IWebHostEnvironment webHostEnvironment)
        {
            this.context = context;
            this.configuration = configuration;
            this.webHostEnvironment = webHostEnvironment;
        }

        public Donation Donation { get; set; }

        public void OnGet(int id)
        {
            bool backRequest = false;
            if (TempData["Donation"] != null)
            {
                id = (int)TempData["Donation"];
            }
            else
            {
                var targetDonationId = HttpContext.Session.GetInt32(DonationModel.DonationIdKey);
                if (targetDonationId.HasValue)
                {
                    id = targetDonationId.Value;
                    backRequest = true;
                }
            }

            Donation = this.context.Donation.GetFullDonationById(id);

            if (this.configuration.IsSendingEmailEnabled())
            {
                if (!backRequest)
                {
                    Mail.SendReferenceMailToDonor(
                        this.configuration, Donation, Path.Combine(this.webHostEnvironment.WebRootPath, this.configuration.GetFilePath("Email.ReferenceToDonor.Body.Path")));
                }
            }
        }
    }
}
