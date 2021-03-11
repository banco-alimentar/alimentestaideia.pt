namespace BancoAlimentar.AlimentaEstaIdeia.Web.Api
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Model.Payment;
    using Microsoft.AspNetCore.Http;
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

        public IActionResult Post(EasyPayPaymentNotificationModel value)
        {
            if (value != null)
            {
                this.context.Donation.CompleteMultiBankPayment(
                    value.id,
                    value.key,
                    value.type,
                    value.status,
                    value.messages.FirstOrDefault());

                return Ok();
            }
            else
            {
                return NotFound();
            }
        }
    }
}
