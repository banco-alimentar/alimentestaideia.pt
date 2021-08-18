namespace BancoAlimentar.AlimentaEstaIdeia.Tools.Database
{
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
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

            List<Donation> zeroPayments = new List<Donation>();

            Console.WriteLine($"There are {donations.Count} donations with no confirmed payments so far.");

            foreach (var donation in donations)
            {
                Console.WriteLine("================================================================================");
                var payments = this.Context.PaymentItems
                    .Where(p => p.Donation == donation)
                    .Select(p => p.Payment)
                    .ToList();

                if (payments.Count == 0)
                {
                    zeroPayments.Add(donation);
                    Console.WriteLine($"Donation {donation.Id} has no payments in the system.");
                }
                else
                {
                    BasePayment currentPayment = payments
                        .Where(p => PaymentStatusMessages.SuccessPaymentMessages.Any(i => i == p.Status))
                        .FirstOrDefault();

                    if (currentPayment != null)
                    {
                        donation.ConfirmedPayment = currentPayment;
                    }
                    else
                    {
                        foreach (var payment in payments)
                        {
                            currentPayment = payment;
                            if (currentPayment is EasyPayWithValuesBaseClass easyPayPayment)
                            {
                                if (easyPayPayment.Paid > 0 && easyPayPayment.Requested > 0)
                                {
                                    donation.ConfirmedPayment = easyPayPayment;
                                    break;
                                }
                            }
                            else if (currentPayment is PayPalPayment payPalPayment)
                            {
                                if (!string.IsNullOrEmpty(payPalPayment.PayPalPaymentId) &&
                                    !string.IsNullOrEmpty(payPalPayment.PayerId))
                                {
                                    donation.ConfirmedPayment = payPalPayment;
                                    break;
                                }
                            }
                        }

                        if (donation.ConfirmedPayment == null)
                        {
                            Console.WriteLine($"Donation {donation.Id} has {payments.Count} payments but all of them are not payed.");
                            foreach (var payment in payments)
                            {
                                DisplayInformation(payment);
                            }
                        }
                    }
                }

                Console.WriteLine("================================================================================");
                Console.WriteLine(Environment.NewLine);
            }

            var missingDonations = donations.Where(p => p.ConfirmedPayment == null).ToList();
            Console.WriteLine("Report :");
            Console.WriteLine($"There are {donations.Count} donations with no confirmed payments.");
            Console.WriteLine($"There are still {missingDonations.Count} donations with no confirmed payments.");
        }

        private void DisplayInformation(object value)
        {
            if (value != null)
            {
                Console.WriteLine(value.GetType().Name);
                foreach (var item in value.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    object itemValue = item.GetValue(value);
                    Console.WriteLine($"{item.Name} => {itemValue}");
                }
            }
        }
    }
}
