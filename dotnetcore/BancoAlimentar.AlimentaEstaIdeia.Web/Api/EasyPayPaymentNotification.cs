namespace BancoAlimentar.AlimentaEstaIdeia.Web.Api
{
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    [Route("easypay/payment")]
    [ApiController]
    public class EasyPayPaymentNotification : ControllerBase
    {
        private readonly IUnitOfWork context;

        public EasyPayPaymentNotification(IUnitOfWork context)
        {
            this.context = context;
        }

        public IActionResult Post(string value)
        {
            return this.Ok();
        }
    }
}
