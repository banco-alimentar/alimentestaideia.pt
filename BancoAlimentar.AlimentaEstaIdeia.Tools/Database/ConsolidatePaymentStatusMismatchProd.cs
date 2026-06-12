namespace BancoAlimentar.AlimentaEstaIdeia.Tools.Database
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// Repairs donations where ConfirmedPayment is set but PaymentStatus is not Payed.
    /// </summary>
    public class ConsolidatePaymentStatusMismatchProd : BaseDatabaseTool
    {
        public ConsolidatePaymentStatusMismatchProd(ApplicationDbContext context, IUnitOfWork unitOfWork)
            : base(context, unitOfWork)
        {
        }

        public override void ExecuteTool()
        {
            List<Donation> donations = this.Context.Donations
                .Include(donation => donation.ConfirmedPayment)
                .Where(donation => donation.ConfirmedPayment != null
                    && donation.PaymentStatus != PaymentStatus.Payed)
                .ToList();

            Console.WriteLine($"Found {donations.Count} donations with ConfirmedPayment set but PaymentStatus != Payed.");

            int repaired = 0;
            foreach (Donation donation in donations)
            {
                if (donation.ConfirmedPayment != null
                    && DonationPaymentCompletion.CanCompleteDonationPayment(
                        donation,
                        donation.ConfirmedPayment,
                        null,
                        null))
                {
                    donation.PaymentStatus = PaymentStatus.Payed;
                    repaired++;
                }
                else
                {
                    Console.WriteLine($"Skipped donation {donation.Id}: confirmed payment could not be validated.");
                }
            }

            int rows = this.Context.SaveChanges();
            Console.WriteLine($"Repaired {repaired} donations. Updated {rows} rows.");
        }
    }
}
