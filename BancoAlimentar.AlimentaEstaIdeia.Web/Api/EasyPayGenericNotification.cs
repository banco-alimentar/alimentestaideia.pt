namespace BancoAlimentar.AlimentaEstaIdeia.Web.Api
{
    using System;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Pages.Account.Manage;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Extensions;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Pages;
    using Easypay.Rest.Client.Model;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Localization;

    [Route("easypay/generic")]
    [ApiController]
    public class EasyPayGenericNotification : EasyPayControllerBase
    {
        private readonly IUnitOfWork context;

        public EasyPayGenericNotification(
            UserManager<WebUser> userManager,
            IUnitOfWork context,
            IConfiguration configuration,
            IWebHostEnvironment webHostEnvironment,
            IViewRenderService renderService,
            IStringLocalizerFactory stringLocalizerFactory)
            : base(userManager, context, configuration, webHostEnvironment, renderService, stringLocalizerFactory)
        {
            this.context = context;
        }

        public async Task<IActionResult> PostAsync(GenericNotificationRequest notificationRequest)
        {
            if (notificationRequest != null)
            {
                int donationId = this.context.Donation.CompleteMultiBankPayment(
                    notificationRequest.Id.ToString(),
                    notificationRequest.Key,
                    notificationRequest.Type.ToString(),
                    notificationRequest.Status.ToString(),
                    notificationRequest.Messages.FirstOrDefault());

                await this.SendInvoiceEmail(donationId);

                return new JsonResult(new StatusDetails()
                {
                    Status = "ok",
                    Message = new Collection<string>() { "Alimenteestaideia: Payment Completed" },
                })
                { StatusCode = (int)HttpStatusCode.OK };
            }
            else
            {
                return new JsonResult(new StatusDetails()
                {
                    Status = "not found",
                    Message = new Collection<string>() { "Alimenteestaideia: Easypay Generic notification not provided" },
                })
                { StatusCode = (int)HttpStatusCode.NotFound };
            }
        }
    }
}
