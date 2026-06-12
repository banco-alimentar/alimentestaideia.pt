// -----------------------------------------------------------------------
// <copyright file="AdminDonationErrorQueryService.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages.Errors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// Executes admin donation/payment integrity checks.
    /// </summary>
    public sealed class AdminDonationErrorQueryService
    {
        private static readonly DateTime BigDonationsCutoff = new DateTime(2024, 11, 28);
        private static readonly DateTime PaidIssuesCutoff = new DateTime(2021, 6, 18, 14, 3, 24, 849);
        private static readonly DateTime NoCompletedCutoff = new DateTime(2021, 6, 18, 10, 11, 23, 133);

        private readonly ApplicationDbContext dbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="AdminDonationErrorQueryService"/> class.
        /// </summary>
        /// <param name="dbContext">Application database context.</param>
        public AdminDonationErrorQueryService(ApplicationDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        /// <summary>
        /// Loads donations where payment completion time is zero or negative.
        /// </summary>
        /// <returns>Matching rows ordered by diff ascending.</returns>
        public async Task<IList<NegativePaymentCompletionTimeRow>> GetNegativePaymentCompletionTimeAsync()
        {
            return await this.dbContext.Donations
                .AsNoTracking()
                .Where(donation =>
                    donation.PaymentStatus == PaymentStatus.Payed
                    && donation.Id > 3935
                    && donation.ConfirmedPayment != null
                    && donation.ConfirmedPayment.Completed != null
                    && EF.Functions.DateDiffSecond(donation.DonationDate, donation.ConfirmedPayment.Completed.Value) < 1)
                .Select(donation => new NegativePaymentCompletionTimeRow
                {
                    DonationId = donation.Id,
                    PaymentId = donation.ConfirmedPayment.Id,
                    DonationDate = donation.DonationDate,
                    PaymentCreated = donation.ConfirmedPayment.Created,
                    PaymentCompleted = donation.ConfirmedPayment.Completed,
                    DiffSeconds = EF.Functions.DateDiffSecond(donation.DonationDate, donation.ConfirmedPayment.Completed.Value),
                })
                .OrderBy(row => row.DiffSeconds)
                .ToListAsync();
        }

        /// <summary>
        /// Loads the top paid donations above 1000 EUR after the cutoff date.
        /// </summary>
        /// <returns>Up to ten matching donations.</returns>
        public async Task<IList<BigDonationRow>> GetBigDonationsAsync()
        {
            return await this.dbContext.Donations
                .AsNoTracking()
                .Where(donation =>
                    donation.DonationDate > BigDonationsCutoff
                    && donation.DonationAmount > 1000
                    && donation.PaymentStatus == PaymentStatus.Payed
                    && donation.FoodBank != null)
                .OrderByDescending(donation => donation.DonationAmount)
                .Take(10)
                .Select(donation => new BigDonationRow
                {
                    DonationId = donation.Id,
                    DonationDate = donation.DonationDate,
                    DonationAmount = donation.DonationAmount,
                    FoodBankName = donation.FoodBank.Name,
                    PaymentStatus = donation.PaymentStatus,
                    ConfirmedPaymentId = donation.ConfirmedPayment != null ? donation.ConfirmedPayment.Id : null,
                    Nif = donation.Nif,
                    WantsReceipt = donation.WantsReceipt,
                })
                .ToListAsync();
        }

        /// <summary>
        /// Loads paid donations whose confirmed payment has no completed date.
        /// </summary>
        /// <returns>Matching rows.</returns>
        public async Task<IList<DonationPaymentIssueRow>> GetPaidStatusNoCompletedDateAsync()
        {
            return await this.LoadDonationPaymentIssuesAsync(
                donation =>
                    donation.PaymentStatus == PaymentStatus.Payed
                    && donation.DonationDate > PaidIssuesCutoff
                    && donation.ConfirmedPayment != null
                    && donation.ConfirmedPayment.Completed == null);
        }

        /// <summary>
        /// Loads a page of paid donations whose confirmed payment has zero paid value.
        /// </summary>
        /// <param name="pageIndex">One-based page index.</param>
        /// <param name="pageSize">Number of rows per page.</param>
        /// <returns>Matching rows and total row count.</returns>
        public async Task<(IList<DonationPaymentIssueRow> Rows, int TotalCount)> GetPaidStatusZeroPaidValueAsync(int pageIndex, int pageSize)
        {
            IQueryable<Donation> query = this.BuildPaidStatusZeroPaidValueQuery();
            int totalCount = await query.CountAsync();

            if (totalCount == 0)
            {
                return (new List<DonationPaymentIssueRow>(), 0);
            }

            List<Donation> donations = await query
                .Include(donation => donation.ConfirmedPayment)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (donations.Select(this.MapDonationPaymentIssue).ToList(), totalCount);
        }

        /// <summary>
        /// Loads paid credit card donations with no completed date.
        /// </summary>
        /// <returns>Matching rows.</returns>
        public async Task<IList<DonationPaymentIssueRow>> GetNoCompletedDateCreditCardAsync()
        {
            return await this.LoadDonationPaymentIssuesAsync(
                donation =>
                    donation.PaymentStatus == PaymentStatus.Payed
                    && donation.DonationDate > NoCompletedCutoff
                    && donation.ConfirmedPayment != null
                    && donation.ConfirmedPayment.Completed == null
                    && donation.ConfirmedPayment is CreditCardPayment);
        }

        /// <summary>
        /// Loads a page of donations where total paid across payments exceeds the donation amount.
        /// </summary>
        /// <param name="pageIndex">One-based page index.</param>
        /// <param name="pageSize">Number of rows per page.</param>
        /// <returns>Matching rows and total row count.</returns>
        public async Task<(IList<OverPaidDonationRow> Rows, int TotalCount)> GetOverPaidDonationsAsync(int pageIndex, int pageSize)
        {
            int skip = (pageIndex - 1) * pageSize;

            int totalCount = await this.dbContext.Database
                .SqlQuery<int>(FormattableStringFactory.Create(AdminErrorSqlQueries.OverPaidDonationsCount))
                .SingleAsync();

            if (totalCount == 0)
            {
                return (new List<OverPaidDonationRow>(), 0);
            }

            List<OverPaidDonationRow> rows = await this.dbContext.Database
                .SqlQuery<OverPaidDonationRow>(FormattableStringFactory.Create(AdminErrorSqlQueries.OverPaidDonationsPage, skip, pageSize))
                .ToListAsync();

            return (rows, totalCount);
        }

        /// <summary>
        /// Loads paid PayPal donations with no completed date.
        /// </summary>
        /// <returns>Matching rows.</returns>
        public async Task<IList<DonationPaymentIssueRow>> GetNoCompletedDatePayPalAsync()
        {
            return await this.LoadDonationPaymentIssuesAsync(
                donation =>
                    donation.PaymentStatus == PaymentStatus.Payed
                    && donation.DonationDate > NoCompletedCutoff
                    && donation.ConfirmedPayment != null
                    && donation.ConfirmedPayment.Completed == null
                    && donation.ConfirmedPayment is PayPalPayment);
        }

        private IQueryable<Donation> BuildPaidStatusZeroPaidValueQuery()
        {
            IQueryable<int> zeroPaidPaymentIds = this.dbContext.CreditCardPayments
                .Where(payment => payment.Paid == 0)
                .Select(payment => payment.Id)
                .Union(this.dbContext.MBWayPayments.Where(payment => payment.Paid == 0).Select(payment => payment.Id))
                .Union(this.dbContext.MultiBankPayments.Where(payment => payment.Paid == 0).Select(payment => payment.Id));

            return this.dbContext.Donations
                .AsNoTracking()
                .Where(donation =>
                    donation.PaymentStatus == PaymentStatus.Payed
                    && donation.DonationDate > PaidIssuesCutoff
                    && donation.ConfirmedPayment != null
                    && zeroPaidPaymentIds.Contains(donation.ConfirmedPayment.Id))
                .OrderBy(donation => donation.DonationDate);
        }

        private async Task<IList<DonationPaymentIssueRow>> LoadDonationPaymentIssuesAsync(
            System.Linq.Expressions.Expression<Func<Donation, bool>> predicate)
        {
            List<Donation> donations = await this.dbContext.Donations
                .AsNoTracking()
                .Include(donation => donation.ConfirmedPayment)
                .Where(predicate)
                .OrderBy(donation => donation.DonationDate)
                .ToListAsync();

            return donations.Select(this.MapDonationPaymentIssue).ToList();
        }

        private DonationPaymentIssueRow MapDonationPaymentIssue(Donation donation)
        {
            BasePayment payment = donation.ConfirmedPayment;
            DonationPaymentIssueRow row = new DonationPaymentIssueRow
            {
                DonationId = donation.Id,
                DonationDate = donation.DonationDate,
                DonationAmount = donation.DonationAmount,
                PaymentStatus = donation.PaymentStatus,
                Discriminator = payment?.GetType().Name,
                Completed = payment?.Completed,
                ServiceReference = donation.ServiceReference,
                ServiceEntity = donation.ServiceEntity,
            };

            if (payment is EasyPayWithValuesBaseClass valuedPayment)
            {
                row.Paid = valuedPayment.Paid;
                row.FixedFee = valuedPayment.FixedFee;
                row.VariableFee = valuedPayment.VariableFee;
                row.Tax = valuedPayment.Tax;
                row.Transfer = valuedPayment.Transfer;
            }

            if (payment is MultiBankPayment multiBankPayment)
            {
                row.Type = multiBankPayment.Type;
                row.Message = multiBankPayment.Message;
            }

            if (payment is PayPalPayment payPalPayment)
            {
                row.PayPalPaymentId = payPalPayment.PayPalPaymentId;
            }

            if (payment is EasyPayBaseClass easyPayPayment)
            {
                row.EasyPayPaymentId = easyPayPayment.EasyPayPaymentId;
            }

            return row;
        }
    }
}
