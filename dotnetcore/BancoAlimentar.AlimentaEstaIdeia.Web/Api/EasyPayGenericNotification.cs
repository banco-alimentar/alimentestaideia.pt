namespace BancoAlimentar.AlimentaEstaIdeia.Web.Api
{
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Net;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using Easypay.Rest.Client.Model;
    using Microsoft.AspNetCore.Mvc;

    [Route("easypay/generic")]
    [ApiController]
    public class EasyPayGenericNotification : ControllerBase
    {
        private readonly IUnitOfWork context;

        public EasyPayGenericNotification(IUnitOfWork context)
        {
            this.context = context;
        }

        public IActionResult Post(GenericNotificationRequest notif)
        {
            if (notif != null)
            {
                this.context.Donation.CompleteMultiBankPayment(
                    notif.Id.ToString(),
                    notif.Key,
                    notif.Type.ToString(),
                    notif.Status.ToString(),
                    notif.Messages.FirstOrDefault());

                return new JsonResult(new StatusDetails() {
                    Status = "ok",
                    Message = new Collection<string>() { "Alimenteestaideia: MultiBank Payment Completed" },
                }) { StatusCode = (int)HttpStatusCode.OK };
            }
            else
            {
                return new JsonResult(new StatusDetails() {
                    Status = "not found",
                    Message = new Collection<string>() { "Alimenteestaideia: Easypay Generic notification not provided" },
                }) { StatusCode = (int)HttpStatusCode.NotFound };
            }
        }
    }
}
