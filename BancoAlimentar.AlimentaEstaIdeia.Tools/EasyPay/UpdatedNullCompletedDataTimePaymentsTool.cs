using BancoAlimentar.AlimentaEstaIdeia.Model;
using BancoAlimentar.AlimentaEstaIdeia.Repository;
using BancoAlimentar.AlimentaEstaIdeia.Common.EasyPay;
using Easypay.Rest.Client.Api;
using Easypay.Rest.Client.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.EasyPay
{
    internal class UpdatedNullCompletedDataTimePaymentsTool : EasyPayTool
    {
        public UpdatedNullCompletedDataTimePaymentsTool(
            ApplicationDbContext context,
            IUnitOfWork unitOfWork,
            IConfiguration configuration)
                : base(context, unitOfWork, configuration)
        {

        }

        public override void ExecuteTool()
        {
            List<BasePayment> paymentsWithNullCompleted = this.Context.Donations
                .Include(p => p.ConfirmedPayment)
                .Where(p => p.PaymentStatus == PaymentStatus.Payed && p.ConfirmedPayment.Completed == null)
                .Select(p => p.ConfirmedPayment)
                .ToList();

            SinglePaymentApi client = this.GetSinglePaymentApi();

            foreach (EasyPayBaseClass item in paymentsWithNullCompleted.OfType<EasyPayBaseClass>())
            {
                if (!string.IsNullOrEmpty(item.EasyPayPaymentId))
                {
                    InlineObject9 payment = client.SingleIdGet(Guid.Parse(item.EasyPayPaymentId));
                    SingleCaptureFull capture = payment?.Capture;
                    if (capture != null && capture.Status == CaptureStatus.Success)
                    {
                        item.Completed = capture.CaptureDate.ToDateTime(TimeOnly.MinValue);
                        this.Context.Entry(item).State = EntityState.Modified;
                    }
                    else
                    {
                        Console.WriteLine("We didn't found a completed payment");
                    }
                }
            }

            this.Context.SaveChanges();
        }
    }
}
