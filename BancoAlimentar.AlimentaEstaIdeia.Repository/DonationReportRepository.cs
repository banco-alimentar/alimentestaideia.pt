// -----------------------------------------------------------------------
// <copyright file="DonationReportRepository.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.ViewModel.DonationReport;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// Builds aggregated donation analytics for static reporting.
    /// </summary>
    public class DonationReportRepository
    {
        private readonly ApplicationDbContext dbContext;
        private readonly DonationRepository donationRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="DonationReportRepository"/> class.
        /// </summary>
        /// <param name="dbContext">Application database context.</param>
        /// <param name="donationRepository">Donation repository for payment helpers.</param>
        public DonationReportRepository(ApplicationDbContext dbContext, DonationRepository donationRepository)
        {
            this.dbContext = dbContext;
            this.donationRepository = donationRepository;
        }

        /// <summary>
        /// Builds a full analytics snapshot for the active campaign reporting window.
        /// </summary>
        /// <param name="tenantDisplayName">Tenant label shown on reports.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Aggregated snapshot.</returns>
        public async Task<DonationReportSnapshot> BuildSnapshotAsync(
            string tenantDisplayName,
            CancellationToken cancellationToken = default)
        {
            Campaign currentCampaign = await this.dbContext.Campaigns
                .AsNoTracking()
                .OrderByDescending(c => c.Start)
                .FirstOrDefaultAsync(
                    c => c.Start < DateTime.Now && c.End > DateTime.Now && !c.IsDefaultCampaign,
                    cancellationToken);

            if (currentCampaign == null)
            {
                currentCampaign = await this.dbContext.Campaigns
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.IsDefaultCampaign, cancellationToken);
            }

            DateTime periodStart = currentCampaign?.Start ?? DateTime.MinValue;
            DateTime periodEnd = this.ResolveReportEnd(currentCampaign);

            string campaignLabel = currentCampaign == null
                ? "Todas as campanhas"
                : $"Campanha {currentCampaign.Number} ({currentCampaign.Start:yyyy-MM-dd} – {periodEnd:yyyy-MM-dd})";

            List<Donation> periodDonationEntities = await this.dbContext.Donations
                .AsNoTracking()
                .Include(d => d.FoodBank)
                .Include(d => d.ConfirmedPayment)
                .Where(d => d.DonationDate >= periodStart && d.DonationDate <= periodEnd)
                .ToListAsync(cancellationToken);

            List<DonationProjection> donations = periodDonationEntities.Select(this.MapDonation).ToList();

            List<DonationItemProjection> items = await this.dbContext.DonationItems
                .AsNoTracking()
                .Include(i => i.ProductCatalogue)
                .Include(i => i.Donation)
                .ThenInclude(d => d.FoodBank)
                .Where(i => i.Donation.DonationDate >= periodStart && i.Donation.DonationDate <= periodEnd)
                .Where(i => i.Donation.PaymentStatus == PaymentStatus.Payed)
                .Select(i => new DonationItemProjection
                {
                    DonationId = i.Donation.Id,
                    FoodBankName = i.Donation.FoodBank != null ? i.Donation.FoodBank.Name : "(não atribuído)",
                    ProductName = i.ProductCatalogue.Name,
                    UnitOfMeasure = i.ProductCatalogue.UnitOfMeasure,
                    Quantity = i.Quantity,
                    LineValue = i.Quantity * i.Price,
                })
                .ToListAsync(cancellationToken);

            List<Donation> allDonationEntities = await this.dbContext.Donations
                .AsNoTracking()
                .Include(d => d.FoodBank)
                .Include(d => d.ConfirmedPayment)
                .ToListAsync(cancellationToken);

            List<DonationProjection> allCampaignDonations = allDonationEntities.Select(this.MapDonation).ToList();

            var snapshot = new DonationReportSnapshot
            {
                GeneratedAtUtc = DateTime.UtcNow,
                TenantDisplayName = tenantDisplayName,
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                CampaignLabel = campaignLabel,
            };

            snapshot.Summary = this.BuildSummary(donations, items);
            snapshot.DailyTrend = this.BuildDailyTrend(donations);
            snapshot.Campaigns = this.BuildCampaignRows(allCampaignDonations);
            snapshot.FoodBanks = this.BuildFoodBankRows(donations, items, snapshot.Summary.TotalPaidAmount);
            snapshot.Products = this.BuildProductRows(items);
            snapshot.Payments = this.BuildPaymentRows(donations);
            snapshot.PaymentStatuses = this.BuildStatusRows(donations);
            snapshot.FoodBankByProduct = this.BuildFoodBankProductCross(items);
            snapshot.CampaignByPayment = this.BuildCampaignPaymentCross(donations);
            snapshot.FoodBankByPayment = this.BuildFoodBankPaymentCross(donations);

            return snapshot;
        }

        private DonationProjection MapDonation(Donation d)
        {
            return new DonationProjection
            {
                Id = d.Id,
                DonationAmount = d.DonationAmount,
                DonationDate = d.DonationDate,
                PaymentStatus = d.PaymentStatus,
                CampaignName = d.CampaignName ?? "(sem campanha)",
                IsCashDonation = d.IsCashDonation,
                FoodBankId = d.FoodBank?.Id ?? 0,
                FoodBankName = d.FoodBank?.Name ?? "(não atribuído)",
                ConfirmedPayment = d.ConfirmedPayment,
            };
        }

        private DonationReportSummary BuildSummary(
            List<DonationProjection> donations,
            List<DonationItemProjection> items)
        {
            var paid = donations.Where(d => d.PaymentStatus == PaymentStatus.Payed).ToList();
            int pending = donations.Count(d =>
                d.PaymentStatus == PaymentStatus.NotPayed || d.PaymentStatus == PaymentStatus.WaitingPayment);
            int failed = donations.Count(d => d.PaymentStatus == PaymentStatus.ErrorPayment);
            int totalInitiated = donations.Count;
            double totalPaid = paid.Sum(d => d.DonationAmount);
            long totalUnits = items.Sum(i => (long)i.Quantity);
            double totalProductValue = items.Sum(i => i.LineValue);
            int cashCount = paid.Count(d => d.IsCashDonation);
            int foodBankCount = paid.Where(d => d.FoodBankId > 0).Select(d => d.FoodBankId).Distinct().Count();

            return new DonationReportSummary
            {
                TotalPaidAmount = totalPaid,
                PaidDonationCount = paid.Count,
                PendingDonationCount = pending,
                FailedDonationCount = failed,
                AveragePaidAmount = paid.Count == 0 ? 0 : totalPaid / paid.Count,
                TotalProductUnits = totalUnits,
                TotalProductValue = totalProductValue,
                CashDonationSharePercent = paid.Count == 0 ? 0 : (cashCount * 100.0) / paid.Count,
                PaymentConversionPercent = totalInitiated == 0 ? 0 : (paid.Count * 100.0) / totalInitiated,
                ActiveFoodBankCount = foodBankCount,
            };
        }

        private IList<DonationReportDailyPoint> BuildDailyTrend(List<DonationProjection> donations)
        {
            return donations
                .Where(d => d.PaymentStatus == PaymentStatus.Payed)
                .GroupBy(d => d.DonationDate.Date)
                .OrderBy(g => g.Key)
                .Select(g => new DonationReportDailyPoint
                {
                    Date = g.Key,
                    PaidAmount = g.Sum(d => d.DonationAmount),
                    PaidCount = g.Count(),
                })
                .ToList();
        }

        private IList<DonationReportCampaignRow> BuildCampaignRows(List<DonationProjection> donations)
        {
            return donations
                .GroupBy(d => d.CampaignName)
                .Select(g =>
                {
                    int paidCount = g.Count(d => d.PaymentStatus == PaymentStatus.Payed);
                    int pending = g.Count(d =>
                        d.PaymentStatus == PaymentStatus.NotPayed || d.PaymentStatus == PaymentStatus.WaitingPayment);
                    double paidAmount = g.Where(d => d.PaymentStatus == PaymentStatus.Payed).Sum(d => d.DonationAmount);
                    int total = g.Count();
                    return new DonationReportCampaignRow
                    {
                        CampaignName = g.Key,
                        PaidAmount = paidAmount,
                        PaidCount = paidCount,
                        PendingCount = pending,
                        AveragePaidAmount = paidCount == 0 ? 0 : paidAmount / paidCount,
                        ConversionPercent = total == 0 ? 0 : (paidCount * 100.0) / total,
                    };
                })
                .OrderByDescending(r => r.PaidAmount)
                .ToList();
        }

        private IList<DonationReportFoodBankRow> BuildFoodBankRows(
            List<DonationProjection> donations,
            List<DonationItemProjection> items,
            double totalPaidAmount)
        {
            var paidByBank = donations
                .Where(d => d.PaymentStatus == PaymentStatus.Payed)
                .GroupBy(d => new { d.FoodBankId, d.FoodBankName })
                .ToDictionary(
                    g => g.Key.FoodBankName,
                    g => new { g.Key.FoodBankId, PaidAmount = g.Sum(d => d.DonationAmount), PaidCount = g.Count() });

            var unitsByBank = items
                .GroupBy(i => i.FoodBankName)
                .ToDictionary(g => g.Key, g => g.Sum(i => (long)i.Quantity));

            return paidByBank
                .Select(kvp =>
                {
                    unitsByBank.TryGetValue(kvp.Key, out long units);
                    return new DonationReportFoodBankRow
                    {
                        FoodBankId = kvp.Value.FoodBankId,
                        FoodBankName = kvp.Key,
                        PaidAmount = kvp.Value.PaidAmount,
                        PaidCount = kvp.Value.PaidCount,
                        ProductUnits = units,
                        SharePercent = totalPaidAmount == 0 ? 0 : (kvp.Value.PaidAmount * 100.0) / totalPaidAmount,
                    };
                })
                .OrderByDescending(r => r.PaidAmount)
                .ToList();
        }

        private IList<DonationReportProductRow> BuildProductRows(List<DonationItemProjection> items)
        {
            long totalUnits = items.Sum(i => (long)i.Quantity);
            return items
                .GroupBy(i => new { i.ProductName, i.UnitOfMeasure })
                .Select(g => new DonationReportProductRow
                {
                    ProductName = g.Key.ProductName,
                    UnitOfMeasure = g.Key.UnitOfMeasure,
                    Quantity = g.Sum(i => (long)i.Quantity),
                    Value = g.Sum(i => i.LineValue),
                    SharePercent = totalUnits == 0 ? 0 : (g.Sum(i => (long)i.Quantity) * 100.0) / totalUnits,
                })
                .OrderByDescending(r => r.Quantity)
                .ToList();
        }

        private IList<DonationReportPaymentRow> BuildPaymentRows(List<DonationProjection> donations)
        {
            var paid = donations.Where(d => d.PaymentStatus == PaymentStatus.Payed).ToList();
            double totalPaid = paid.Sum(d => d.DonationAmount);

            return paid
                .GroupBy(d => this.ResolvePaymentLabel(d))
                .Select(g =>
                {
                    double amount = g.Sum(d => d.DonationAmount);
                    return new DonationReportPaymentRow
                    {
                        PaymentTypeKey = g.Key.Key,
                        PaymentTypeLabel = g.Key.Label,
                        PaidAmount = amount,
                        PaidCount = g.Count(),
                        AveragePaidAmount = g.Count() == 0 ? 0 : amount / g.Count(),
                        SharePercent = totalPaid == 0 ? 0 : (amount * 100.0) / totalPaid,
                    };
                })
                .OrderByDescending(r => r.PaidAmount)
                .ToList();
        }

        private IList<DonationReportStatusRow> BuildStatusRows(List<DonationProjection> donations)
        {
            int total = donations.Count;
            return donations
                .GroupBy(d => d.PaymentStatus)
                .Select(g => new DonationReportStatusRow
                {
                    StatusLabel = this.GetStatusLabel(g.Key),
                    Count = g.Count(),
                    SharePercent = total == 0 ? 0 : (g.Count() * 100.0) / total,
                })
                .OrderByDescending(r => r.Count)
                .ToList();
        }

        private IList<DonationReportCrossRow> BuildFoodBankProductCross(List<DonationItemProjection> items)
        {
            return items
                .GroupBy(i => new { i.FoodBankName, i.ProductName })
                .Select(g => new DonationReportCrossRow
                {
                    DimensionA = g.Key.FoodBankName,
                    DimensionB = g.Key.ProductName,
                    Amount = g.Sum(i => i.LineValue),
                    Count = g.Sum(i => (long)i.Quantity),
                })
                .OrderByDescending(r => r.Count)
                .Take(40)
                .ToList();
        }

        private IList<DonationReportCrossRow> BuildCampaignPaymentCross(List<DonationProjection> donations)
        {
            return donations
                .Where(d => d.PaymentStatus == PaymentStatus.Payed)
                .GroupBy(d => new { d.CampaignName, Payment = this.ResolvePaymentLabel(d) })
                .Select(g => new DonationReportCrossRow
                {
                    DimensionA = g.Key.CampaignName,
                    DimensionB = g.Key.Payment.Label,
                    Amount = g.Sum(d => d.DonationAmount),
                    Count = g.Count(),
                })
                .OrderByDescending(r => r.Amount)
                .ToList();
        }

        private IList<DonationReportCrossRow> BuildFoodBankPaymentCross(List<DonationProjection> donations)
        {
            return donations
                .Where(d => d.PaymentStatus == PaymentStatus.Payed)
                .GroupBy(d => new { d.FoodBankName, Payment = this.ResolvePaymentLabel(d) })
                .Select(g => new DonationReportCrossRow
                {
                    DimensionA = g.Key.FoodBankName,
                    DimensionB = g.Key.Payment.Label,
                    Amount = g.Sum(d => d.DonationAmount),
                    Count = g.Count(),
                })
                .OrderByDescending(r => r.Amount)
                .ToList();
        }

        private (string Key, string Label) ResolvePaymentLabel(DonationProjection donation)
        {
            if (donation.IsCashDonation)
            {
                return ("cash", "Numerário");
            }

            PaymentType type = this.donationRepository.GetPaymentType(donation.ConfirmedPayment);
            string label = this.donationRepository.GetPaymentHumanName(donation.ConfirmedPayment);
            if (type == PaymentType.None)
            {
                return ("unknown", string.IsNullOrWhiteSpace(label) || label == "desconhecido" ? "Outro" : label);
            }

            return (type.ToString(), label);
        }

        private string GetStatusLabel(PaymentStatus status)
        {
            return status switch
            {
                PaymentStatus.Payed => "Pago",
                PaymentStatus.NotPayed => "Não pago",
                PaymentStatus.WaitingPayment => "A aguardar pagamento",
                PaymentStatus.ErrorPayment => "Erro de pagamento",
                _ => status.ToString(),
            };
        }

        private DateTime ResolveReportEnd(Campaign campaign)
        {
            if (campaign == null)
            {
                return DateTime.UtcNow;
            }

            if (campaign.ReportEnd > campaign.End)
            {
                return campaign.ReportEnd;
            }

            return campaign.End > DateTime.UtcNow ? DateTime.UtcNow : campaign.End;
        }

        private sealed class DonationProjection
        {
            public int Id { get; set; }

            public double DonationAmount { get; set; }

            public DateTime DonationDate { get; set; }

            public PaymentStatus PaymentStatus { get; set; }

            public string CampaignName { get; set; }

            public bool IsCashDonation { get; set; }

            public int FoodBankId { get; set; }

            public string FoodBankName { get; set; }

            public BasePayment ConfirmedPayment { get; set; }
        }

        private sealed class DonationItemProjection
        {
            public int DonationId { get; set; }

            public string FoodBankName { get; set; }

            public string ProductName { get; set; }

            public string UnitOfMeasure { get; set; }

            public int Quantity { get; set; }

            public double LineValue { get; set; }
        }
    }
}
