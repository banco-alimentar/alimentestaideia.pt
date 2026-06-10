// -----------------------------------------------------------------------
// <copyright file="AdminErrorSqlQueries.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages.Errors
{
    /// <summary>
    /// SQL definitions for admin data integrity checks.
    /// </summary>
    public static class AdminErrorSqlQueries
    {
        /// <summary>
        /// Negative or zero seconds between donation date and payment completion.
        /// </summary>
        public const string NegativePaymentCompletionTime = @"SELECT dbo.Donations.id,Payments.id,dbo.Donations.DonationDate, dbo.Payments.Created, dbo.Payments.Completed,Datediff (second,dbo.Donations.DonationDate, Payments.Completed) as Diff
FROM  dbo.Donations INNER JOIN
         dbo.Payments ON dbo.Donations.ConfirmedPaymentId = dbo.Payments.Id
WHERE (dbo.Donations.PaymentStatus = 1) AND Datediff (second,dbo.Donations.DonationDate, Payments.Completed)<1  and Donations.id>3935
Order by Diff asc";

        /// <summary>
        /// Large paid donations after 2024-11-28.
        /// </summary>
        public const string BigDonations = @"SELECT   TOP (10) dbo.AspNetUsers.FullName, dbo.AspNetUsers.Email, dbo.Donations.DonationDate, dbo.Donations.DonationAmount, dbo.FoodBanks.Name, dbo.Donations.PaymentStatus, 
                         dbo.Donations.ConfirmedPaymentId, dbo.Donations.NIF, dbo.Donations.WantsReceipt
FROM         dbo.FoodBanks INNER JOIN
                         dbo.Donations ON dbo.FoodBanks.Id = dbo.Donations.FoodBankId LEFT OUTER JOIN
                         dbo.AspNetUsers ON dbo.Donations.UserId = dbo.AspNetUsers.Id
WHERE     (dbo.Donations.DonationDate > CONVERT(DATETIME, '2024-11-28 00:00:00', 102)) AND (dbo.Donations.DonationAmount > 1000) AND (dbo.Donations.PaymentStatus = 1)
ORDER BY dbo.Donations.DonationAmount DESC";

        /// <summary>
        /// Paid donations with no payment completed date.
        /// </summary>
        public const string PaidStatusNoCompletedDate = @"SELECT   dbo.Donations.DonationDate, dbo.Donations.DonationAmount, dbo.Donations.PaymentStatus, dbo.Payments.Discriminator, dbo.Payments.Paid, dbo.Payments.Completed, 
                         dbo.Payments.FixedFee, dbo.Payments.VariableFee, dbo.Payments.Tax, dbo.Payments.Transfer, dbo.Donations.ServiceReference, dbo.Donations.ServiceEntity, dbo.Payments.Type, 
                         dbo.Payments.Message, dbo.Payments.PayPalPaymentId, dbo.Payments.EasyPayPaymentId
FROM         dbo.Donations INNER JOIN
                         dbo.Payments ON dbo.Donations.ConfirmedPaymentId = dbo.Payments.Id
WHERE     (dbo.Donations.PaymentStatus = 1) AND (Completed is null) AND DonationDate>'2021-06-18 14:03:24.8499132'
GROUP BY dbo.Donations.DonationDate, dbo.Donations.DonationAmount, dbo.Donations.PaymentStatus, dbo.Payments.Discriminator, dbo.Payments.Paid, dbo.Payments.Completed, 
                         dbo.Payments.FixedFee, dbo.Payments.VariableFee, dbo.Payments.Tax, dbo.Payments.Transfer, dbo.Donations.ServiceReference, dbo.Donations.ServiceEntity, dbo.Payments.Type, 
                         dbo.Payments.Message, dbo.Payments.PayPalPaymentId, dbo.Payments.EasyPayPaymentId";

        /// <summary>
        /// Paid donations with zero paid value on the payment.
        /// </summary>
        public const string PaidStatusZeroPaidValue = @"SELECT   dbo.Donations.DonationDate, dbo.Donations.DonationAmount, dbo.Donations.PaymentStatus, dbo.Payments.Discriminator, dbo.Payments.Paid, dbo.Payments.Completed, 
                         dbo.Payments.FixedFee, dbo.Payments.VariableFee, dbo.Payments.Tax, dbo.Payments.Transfer, dbo.Donations.ServiceReference, dbo.Donations.ServiceEntity, dbo.Payments.Type, 
                         dbo.Payments.Message, dbo.Payments.PayPalPaymentId, dbo.Payments.EasyPayPaymentId
FROM         dbo.Donations INNER JOIN
                         dbo.Payments ON dbo.Donations.ConfirmedPaymentId = dbo.Payments.Id
WHERE     (dbo.Donations.PaymentStatus = 1) AND (Paid=0)  AND DonationDate>'2021-06-18 14:03:24.8499132'
GROUP BY dbo.Donations.DonationDate, dbo.Donations.DonationAmount, dbo.Donations.PaymentStatus, dbo.Payments.Discriminator, dbo.Payments.Paid, dbo.Payments.Completed, 
                         dbo.Payments.FixedFee, dbo.Payments.VariableFee, dbo.Payments.Tax, dbo.Payments.Transfer, dbo.Donations.ServiceReference, dbo.Donations.ServiceEntity, dbo.Payments.Type, 
                         dbo.Payments.Message, dbo.Payments.PayPalPaymentId, dbo.Payments.EasyPayPaymentId";

        /// <summary>
        /// Paid credit card payments with no completed date.
        /// </summary>
        public const string NoCompletedDateCreditCard = @"SELECT   dbo.Donations.DonationDate, dbo.Donations.DonationAmount, dbo.Donations.PaymentStatus, dbo.Payments.Discriminator, dbo.Payments.Paid, dbo.Payments.Completed, 
                         dbo.Payments.FixedFee, dbo.Payments.VariableFee, dbo.Payments.Tax, dbo.Payments.Transfer, dbo.Donations.ServiceReference, dbo.Donations.ServiceEntity, dbo.Payments.Type, 
                         dbo.Payments.Message, dbo.Payments.PayPalPaymentId, dbo.Payments.EasyPayPaymentId
FROM         dbo.Donations INNER JOIN
                         dbo.Payments ON dbo.Donations.ConfirmedPaymentId = dbo.Payments.Id
WHERE     (dbo.Donations.PaymentStatus = 1) AND (dbo.Payments.Completed IS NULL) AND (Discriminator='CreditCardPayment') AND DonationDate>'2021-06-18 10:11:23.1336878'
GROUP BY dbo.Donations.DonationDate, dbo.Donations.DonationAmount, dbo.Donations.PaymentStatus, dbo.Payments.Discriminator, dbo.Payments.Paid, dbo.Payments.Completed, 
                         dbo.Payments.FixedFee, dbo.Payments.VariableFee, dbo.Payments.Tax, dbo.Payments.Transfer, dbo.Donations.ServiceReference, dbo.Donations.ServiceEntity, dbo.Payments.Type, 
                         dbo.Payments.Message, dbo.Payments.PayPalPaymentId, dbo.Payments.EasyPayPaymentId";

        /// <summary>
        /// Paid PayPal payments with no completed date.
        /// </summary>
        public const string NoCompletedDatePayPal = @"SELECT   dbo.Donations.DonationDate, dbo.Donations.DonationAmount, dbo.Donations.PaymentStatus, dbo.Payments.Discriminator, dbo.Payments.Paid, dbo.Payments.Completed, 
                         dbo.Payments.FixedFee, dbo.Payments.VariableFee, dbo.Payments.Tax, dbo.Payments.Transfer, dbo.Donations.ServiceReference, dbo.Donations.ServiceEntity, dbo.Payments.Type, 
                         dbo.Payments.Message, dbo.Payments.PayPalPaymentId, dbo.Payments.EasyPayPaymentId
FROM         dbo.Donations INNER JOIN
                         dbo.Payments ON dbo.Donations.ConfirmedPaymentId = dbo.Payments.Id
WHERE     (dbo.Donations.PaymentStatus = 1) AND (dbo.Payments.Completed IS NULL) AND (Discriminator='PayPalPayment')  AND DonationDate>'2021-06-18 10:11:23.1336878'
GROUP BY dbo.Donations.DonationDate, dbo.Donations.DonationAmount, dbo.Donations.PaymentStatus, dbo.Payments.Discriminator, dbo.Payments.Paid, dbo.Payments.Completed, 
                         dbo.Payments.FixedFee, dbo.Payments.VariableFee, dbo.Payments.Tax, dbo.Payments.Transfer, dbo.Donations.ServiceReference, dbo.Donations.ServiceEntity, dbo.Payments.Type, 
                         dbo.Payments.Message, dbo.Payments.PayPalPaymentId, dbo.Payments.EasyPayPaymentId";
    }
}
