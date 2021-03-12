namespace BancoAlimentar.AlimentaEstaIdeia.Web.Api
{
    using System.Linq;
    using System.Net;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Api.Model;
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

        public IActionResult Post(EasyPayGenericNotificationModel value)
        {
            if (value != null)
            {
                this.context.Donation.CompleteMultiBankPayment(
                    value.id,
                    value.key,
                    value.type,
                    value.status,
                    value.messages.FirstOrDefault());

                return new JsonResult(new NotificationResponse() { Status = "ok" }) { StatusCode = (int)HttpStatusCode.OK };
            }
            else
            {
                return new JsonResult(new NotificationResponse() { Status = "not found" }) { StatusCode = (int)HttpStatusCode.NotFound };
            }
        }
    }
}
