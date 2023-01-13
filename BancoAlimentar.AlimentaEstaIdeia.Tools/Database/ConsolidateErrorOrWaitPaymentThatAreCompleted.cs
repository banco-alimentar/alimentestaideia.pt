using BancoAlimentar.AlimentaEstaIdeia.Model;
using BancoAlimentar.AlimentaEstaIdeia.Repository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                .Where(p => p.PaymentStatus == PaymentStatus.WaitingPayment || p.PaymentStatus == PaymentStatus.ErrorPayment)
                .Where(p => p.PaymentList
                    .Any(i => i.Completed.HasValue))
                .ToList();

            foreach (var donation in donations)
            {
                donation.PaymentStatus = PaymentStatus.Payed;
            }

            int rowsAffected = this.Context.SaveChanges();
        }
    }
}
