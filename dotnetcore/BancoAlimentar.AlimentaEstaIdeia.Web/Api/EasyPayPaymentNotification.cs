namespace BancoAlimentar.AlimentaEstaIdeia.Web.Api
{
    using System;
    using System.Collections.ObjectModel;
    using System.Net;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using Easypay.Rest.Client.Model;
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

        public IActionResult Post(TransactionNotificationRequest notif)
        {
            if (notif != null)
            {
                if (string.Equals(notif.Method, "MBW", StringComparison.OrdinalIgnoreCase))
                {
                    this.context.Donation.CompleteMBWayPayment(
                        notif.Id.ToString(),
                        notif.Transaction.Key,
                        (float) notif.Transaction.Values.Requested,
                        (float) notif.Transaction.Values.Paid,
                        (float) notif.Transaction.Values.FixedFee,
                        (float) notif.Transaction.Values.VariableFee,
                        (float) notif.Transaction.Values.Tax,
                        (float) notif.Transaction.Values.Transfer);
                }
                else if (string.Equals(notif.Method, "CC", StringComparison.OrdinalIgnoreCase))
                {
                    this.context.Donation.CompleteCreditCardPayment(
                        notif.Id.ToString(),
                        notif.Transaction.Key,
                        (float) notif.Transaction.Values.Requested,
                        (float) notif.Transaction.Values.Paid,
                        (float) notif.Transaction.Values.FixedFee,
                        (float) notif.Transaction.Values.VariableFee,
                        (float) notif.Transaction.Values.Tax,
                        (float) notif.Transaction.Values.Transfer);
                }

                return new JsonResult(new StatusDetails()
                {
                    Status = "ok",
                    Message = new Collection<string>() { "Alimenteestaideia: Payment Completed" },
                })
                { StatusCode = (int)HttpStatusCode.OK };

            }
            else
            {
                return new JsonResult(new StatusDetails() { Status = "not found" }) { StatusCode = (int)HttpStatusCode.NotFound };
            }
        }
    }
}
