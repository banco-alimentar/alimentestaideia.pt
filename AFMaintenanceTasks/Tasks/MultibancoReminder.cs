using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BancoAlimentar.AlimentaEstaIdeia.Model;
using BancoAlimentar.AlimentaEstaIdeia.Repository;

namespace BancoAlimentar.AlimentaEstaIdeia.AFMaintenanceTasks.Tasks
{
    internal class MultibancoReminder : BaseDatabaseTool
    {
        /// <summary>
        /// https://github.com/banco-alimentar/alimentestaideia.pt/issues/115
        /// </summary>
        /// <param name="context"></param>
        /// <param name="unitOfWork"></param>
        public MultibancoReminder(ApplicationDbContext context, IUnitOfWork unitOfWork) : base(context, unitOfWork)
        {

        }
        public override void ExecuteTask()
        {
            DateTime filterDate = DateTime.Now.AddDays(-3);
            
            var donations = this.Context.Donations
                .Where(p => p.ConfirmedPayment == null && p.DonationDate> filterDate)
                .ToList();

            foreach (var item in donations)
            {
                foreach (var payment in item.Payments)
                {
                }
            }

            Console.WriteLine($"There are {donations.Count} donations with no confirmed payments so far.");
        }

    }
}