namespace BancoAlimentar.AlimentaEstaIdeia.Tools.Database
{

    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using Microsoft.EntityFrameworkCore;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading.Tasks;

    public class ConsolidateDonationIdToPayment : BaseDatabaseTool
    {
        public ConsolidateDonationIdToPayment(ApplicationDbContext context, IUnitOfWork unitOfWork) : base(context, unitOfWork)
        {

        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.LogTo(Console.WriteLine)
                .EnableSensitiveDataLogging();
        }

        public override void ExecuteTool()
        {
            //int counter = 0;
            //Console.WriteLine($"========================================");
            //Console.WriteLine($"Executing ConsolidateDonationIdToPayment");
            //Console.WriteLine($"========================================");

            //var paymentItems = this.Context.PaymentItems
            //    .Include(p => p.Donation)
            //    .Include(p=>p.Payment)
            //    .OrderBy(p => p.Id)
            //    .ToList();

            //Console.WriteLine($"There are {paymentItems.Count} paymentItems.");

            //foreach (var pItem in paymentItems)
            //{

            //    if (pItem.Payment.Donation == null)
            //    {
            //        Console.WriteLine($"pItem.id={pItem.Id} Donation.Id= {pItem.Donation.Id} Payment.id={pItem.Payment.Id} Payment.Donation ={ pItem.Payment.Donation}");

            //        pItem.Payment.Donation = pItem.Donation;
            //        counter++;
            //        if (counter % 10 == 0)
            //        {
            //            this.Context.SaveChanges();
            //            counter = 0;
            //        }
            //    }
            //    else
            //    {
            //        Console.WriteLine($"SKIPPED pItem.id={pItem.Id}");
            //    }


                
            //}
            //this.Context.SaveChanges();
        }

    }
}
