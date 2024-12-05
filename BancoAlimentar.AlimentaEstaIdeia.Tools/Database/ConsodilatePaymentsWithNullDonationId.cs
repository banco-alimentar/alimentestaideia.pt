namespace BancoAlimentar.AlimentaEstaIdeia.Tools.Database
{
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;


    public class ConsodilatePaymentsWithNullDonationId : BaseDatabaseTool
    {
        public ConsodilatePaymentsWithNullDonationId(ApplicationDbContext context, IUnitOfWork unitOfWork) : base(context, unitOfWork)
        {
        }

        public override void ExecuteTool()
        {
            //var payments = this.Context.Payments
            //    .Where(p => p.Donation == null)
            //    .ToList();

            //Console.WriteLine($"There are {payments.Count} payments with no donation id so far.");
            //int count = 0;

            //foreach (var payment in payments)
            //{
            //    var donation = this.Context.Donations
            //        .Where(d => d.ConfirmedPayment == payment)
            //        .FirstOrDefault();

            //    if (donation != null)
            //    {
            //        payment.Donation = donation;
            //        Console.WriteLine($"Payment {payment.Id} has been associated with donation {donation.Id}.");
            //        count++;
            //    }
            //    else if (payment.Status != null)
            //    {
            //        Console.WriteLine($"Payment {payment.Id} has a different status {payment.Status}");
            //    }
            //}

            //Console.WriteLine($"There are {count} payments with no donation id so far.");


            //Dictionary<string, List<BasePayment>> samePayments = new Dictionary<string, List<BasePayment>>();
            //foreach (var item in payments)
            //{
            //    if (!string.IsNullOrEmpty(item.TransactionKey))
            //    {
            //        if (!samePayments.ContainsKey(item.TransactionKey))
            //        {
            //            samePayments.Add(item.TransactionKey, new List<BasePayment>() { item });
            //        }
            //        else
            //        {
            //            samePayments[item.TransactionKey].Add(item);
            //        }
            //    }
            //}

            //samePayments = samePayments.Where(p => p.Value.Count > 1).ToDictionary(p => p.Key, p => p.Value);

            //foreach (var item in samePayments)
            //{
            //    BasePayment completedPayment = item.Value.Where(p => !string.IsNullOrEmpty(p.Status)).FirstOrDefault();
            //    if (completedPayment != null)
            //    {
            //        foreach (var basePaymentitem in item.Value)
            //        {
            //            if (basePaymentitem != completedPayment)
            //            {
            //                this.Context.Entry(basePaymentitem).State = Microsoft.EntityFrameworkCore.EntityState.Deleted;
            //            }
            //        }
            //    }
            //}

            int rowsAffected = this.Context.SaveChanges();
        }
    }
}
