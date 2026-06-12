using BancoAlimentar.AlimentaEstaIdeia.Model;
using BancoAlimentar.AlimentaEstaIdeia.Repository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.Database
{
    public class ConsolidateErrorOrWaitPaymentThatAreCompleted : BaseDatabaseTool
    {
        public ConsolidateErrorOrWaitPaymentThatAreCompleted(ApplicationDbContext context, IUnitOfWork unitOfWork) : base(context, unitOfWork)
        {
        }

        public override void ExecuteTool()
        {
            List<Donation> donations = this.Context.Donations
                .Include(p => p.PaymentList)
                .Where(p => p.PaymentStatus == PaymentStatus.WaitingPayment
                    || p.PaymentStatus == PaymentStatus.ErrorPayment
                    || p.PaymentStatus == PaymentStatus.NotPayed)
                .Where(p => p.PaymentList.Any(i => i.Completed.HasValue))
                .ToList();

            foreach (var donation in donations)
            {
                BasePayment successfulPayment = donation.PaymentList.FirstOrDefault(payment =>
                    DonationPaymentCompletion.CanCompleteDonationPayment(donation, payment, null, null)
                    || (payment.Completed.HasValue
                        && DonationPaymentCompletion.IsSuccessfulPaymentStatus(payment.Status)));

                if (successfulPayment != null)
                {
                    donation.PaymentStatus = PaymentStatus.Payed;
                    donation.ConfirmedPayment ??= successfulPayment;
                }
            }

            int rowsAffected = this.Context.SaveChanges();
            Console.WriteLine($"Updated {rowsAffected} rows.");
        }
    }
}
