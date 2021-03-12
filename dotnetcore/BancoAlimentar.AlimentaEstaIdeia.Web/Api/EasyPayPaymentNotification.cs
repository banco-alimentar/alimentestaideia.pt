namespace BancoAlimentar.AlimentaEstaIdeia.Web.Api
{
    using System.Net;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Api.Model;
    using Microsoft.AspNetCore.Mvc;

    [Route("easypay/payment")]
    [ApiController]
    public class EasyPayPaymentNotification : ControllerBase
    {
        private readonly IUnitOfWork context;

        public EasyPayPaymentNotification(IUnitOfWork context)
        {
            this.context = context;
        }

        public IActionResult Post(EasyPayPaymentNotificationModel value)
        {
            if (value != null)
            {
                this.context.Donation.CompleteCreditCardPayment(
                    value.id,
                    value.transaction.key,
                    value.transaction.values.requested,
                    value.transaction.values.paid,
                    value.transaction.values.fixed_fee,
                    value.transaction.values.variable_fee,
                    value.transaction.values.tax,
                    value.transaction.values.transfer);

                return new JsonResult(new NotificationResponse() { Status = "ok" }) { StatusCode = (int)HttpStatusCode.OK };
            }
            else
            {
                return new JsonResult(new NotificationResponse() { Status = "not found" }) { StatusCode = (int)HttpStatusCode.NotFound };
            }
        }
    }
}
