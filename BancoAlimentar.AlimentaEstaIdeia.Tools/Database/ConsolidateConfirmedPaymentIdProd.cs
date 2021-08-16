namespace BancoAlimentar.AlimentaEstaIdeia.Tools.Database
{
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class ConsolidateConfirmedPaymentIdProd : BaseDatabaseTool
    {
        public ConsolidateConfirmedPaymentIdProd(ApplicationDbContext context, IUnitOfWork unitOfWork) : base(context, unitOfWork)
        {
        }

        public override void ExecuteTool()
        {
            var donations = this.Context.Donations
                .Where(p => p.ConfirmedPayment == null)
                .ToList();

            foreach (var donation in donations)
            {
                var payments = this.Context.PaymentItems
                    .Where(p => p.Donation == donation)
                    .Select(p => p.Payment)
                    .ToList();

                BasePayment currentPayment = payments
                    .Where(p => PaymentStatusMessages.SuccessPaymentMessages.Any(i => i == p.Status))
                    .FirstOrDefault();

                if (currentPayment != null)
                {
                    donation.ConfirmedPayment = currentPayment;
                }
            }
        }
    }
}
