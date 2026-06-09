// -----------------------------------------------------------------------
// <copyright file="DonationReportRepository.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
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
        private static readonly CultureInfo PtCulture = CultureInfo.GetCultureInfo("pt-PT");
        private static readonly DayOfWeek[] WeekdayOrder =
        {
            DayOfWeek.Monday,
            DayOfWeek.Tuesday,
            DayOfWeek.Wednesday,
            DayOfWeek.Thursday,
            DayOfWeek.Friday,
            DayOfWeek.Saturday,
            DayOfWeek.Sunday,
        };

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
            string campaignLabel = "Todas as campanhas";

            List<Donation> allDonationEntities = await this.dbContext.Donations
                .AsNoTracking()
                .Include(d => d.Campaign)
                .Include(d => d.FoodBank)
                .Include(d => d.ConfirmedPayment)
                .ToListAsync(cancellationToken);

            List<DonationProjection> allCampaignDonations = allDonationEntities.Select(this.MapDonation).ToList();

            List<DonationItemProjection> allItems = await this.dbContext.DonationItems
                .AsNoTracking()
                .Include(i => i.ProductCatalogue)
                .Include(i => i.Donation)
                .ThenInclude(d => d.Campaign)
                .Include(i => i.Donation)
                .ThenInclude(d => d.FoodBank)
                .Where(i => i.Donation.PaymentStatus == PaymentStatus.Payed)
                .Select(i => new DonationItemProjection
                {
                    DonationId = i.Donation.Id,
                    CampaignId = i.Donation.CampaignId,
                    CampaignName = i.Donation.Campaign != null
                        ? i.Donation.Campaign.Number
                        : i.Donation.CampaignName ?? "(sem campanha)",
                    FoodBankName = i.Donation.FoodBank != null ? i.Donation.FoodBank.Name : "(não atribuído)",
                    ProductName = i.ProductCatalogue.Name,
                    UnitOfMeasure = i.ProductCatalogue.UnitOfMeasure,
                    Quantity = i.Quantity,
                    LineValue = i.Quantity * i.Price,
                })
                .ToListAsync(cancellationToken);

            List<SubscriptionProjection> subscriptionProjections = await this.LoadSubscriptionProjectionsAsync(cancellationToken);

            DateTime periodStart = allCampaignDonations.Count == 0
                ? DateTime.MinValue
                : allCampaignDonations.Min(d => d.DonationDate);
            DateTime periodEnd = allCampaignDonations.Count == 0
                ? DateTime.UtcNow
                : allCampaignDonations.Max(d => d.DonationDate);

            var snapshot = new DonationReportSnapshot
            {
                GeneratedAtUtc = DateTime.UtcNow,
                TenantDisplayName = tenantDisplayName,
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                CampaignLabel = campaignLabel,
            };

            snapshot.Summary = this.BuildSummary(allCampaignDonations, allItems);
            snapshot.DailyTrend = this.BuildDailyTrend(allCampaignDonations);
            snapshot.Campaigns = this.BuildCampaignRows(allCampaignDonations);
            snapshot.FoodBanks = this.BuildFoodBankRows(allCampaignDonations, allItems, snapshot.Summary.TotalPaidAmount);
            snapshot.Products = this.BuildProductRows(allItems);
            snapshot.Payments = this.BuildPaymentRows(allCampaignDonations);
            snapshot.PaymentStatuses = this.BuildStatusRows(allCampaignDonations);
            snapshot.FoodBankByProduct = this.BuildFoodBankProductCross(allItems);
            snapshot.CampaignByPayment = this.BuildCampaignPaymentCross(allCampaignDonations);
            snapshot.FoodBankByPayment = this.BuildFoodBankPaymentCross(allCampaignDonations);
            snapshot.Filters = this.BuildFilterPayload(allCampaignDonations, allItems, subscriptionProjections);
            snapshot.TemporalAnalysis = this.BuildTemporalAnalysis(allCampaignDonations);
            snapshot.Subscriptions = this.BuildSubscriptionSection(subscriptionProjections, campaignId: null, allCampaigns: true);

            return snapshot;
        }

        private async Task<List<SubscriptionProjection>> LoadSubscriptionProjectionsAsync(
            CancellationToken cancellationToken)
        {
            List<SubscriptionDonations> links = await this.dbContext.SubscriptionDonations
                .AsNoTracking()
                .Include(link => link.Subscription)
                .Include(link => link.Donation)
                .Where(link => link.Subscription != null && !link.Subscription.IsDeleted)
                .ToListAsync(cancellationToken);

            return links
                .GroupBy(link => link.Subscription.Id)
                .Select(group =>
                {
                    Subscription subscription = group.First().Subscription;
                    return new SubscriptionProjection
                    {
                        Id = subscription.Id,
                        PublicId = subscription.PublicId,
                        Status = subscription.Status,
                        Frequency = subscription.Frequency,
                        Created = subscription.Created,
                        Donations = group
                            .Where(link => link.Donation != null)
                            .Select(link => new LinkedDonationProjection
                            {
                                DonationId = link.Donation.Id,
                                CampaignId = link.Donation.CampaignId,
                                PaymentStatus = link.Donation.PaymentStatus,
                                DonationAmount = link.Donation.DonationAmount,
                            })
                            .ToList(),
                    };
                })
                .ToList();
        }

        private DonationReportSubscriptionSection BuildSubscriptionSection(
            List<SubscriptionProjection> subscriptions,
            int? campaignId,
            bool allCampaigns)
        {
            List<SubscriptionProjection> scopedSubscriptions;
            if (allCampaigns)
            {
                scopedSubscriptions = subscriptions;
            }
            else
            {
                scopedSubscriptions = subscriptions
                    .Select(subscription =>
                    {
                        List<LinkedDonationProjection> scopedDonations = campaignId.HasValue
                            ? subscription.Donations
                                .Where(donation => donation.CampaignId == campaignId.Value)
                                .ToList()
                            : subscription.Donations
                                .Where(donation => !donation.CampaignId.HasValue)
                                .ToList();

                        if (scopedDonations.Count == 0)
                        {
                            return null;
                        }

                        return new SubscriptionProjection
                        {
                            Id = subscription.Id,
                            PublicId = subscription.PublicId,
                            Status = subscription.Status,
                            Frequency = subscription.Frequency,
                            Created = subscription.Created,
                            Donations = scopedDonations,
                        };
                    })
                    .Where(subscription => subscription != null)
                    .ToList();
            }

            return this.BuildSubscriptionSectionFromScoped(scopedSubscriptions);
        }

        private DonationReportSubscriptionSection BuildSubscriptionSectionFromScoped(
            List<SubscriptionProjection> scopedSubscriptions)
        {
            List<LinkedDonationProjection> scopedPaidDonations = scopedSubscriptions
                .SelectMany(subscription => subscription.Donations)
                .Where(donation => donation.PaymentStatus == PaymentStatus.Payed)
                .ToList();

            var section = new DonationReportSubscriptionSection
            {
                TotalPaidAmount = scopedPaidDonations.Sum(donation => donation.DonationAmount),
                PaidDonationCount = scopedPaidDonations.Count,
                SubscriptionCount = scopedSubscriptions.Count,
            };

            int totalSubscriptions = scopedSubscriptions.Count;
            section.StatusBreakdown = scopedSubscriptions
                .GroupBy(subscription => subscription.Status)
                .OrderByDescending(group => group.Count())
                .ThenBy(group => group.Key.ToString())
                .Select(group => new DonationReportSubscriptionStatusRow
                {
                    StatusKey = group.Key.ToString(),
                    StatusLabel = this.GetSubscriptionStatusLabel(group.Key),
                    Count = group.Count(),
                    SharePercent = totalSubscriptions == 0 ? 0 : (group.Count() * 100.0) / totalSubscriptions,
                })
                .ToList();

            section.Subscriptions = scopedSubscriptions
                .Select(subscription =>
                {
                    List<LinkedDonationProjection> paidDonations = subscription.Donations
                        .Where(donation => donation.PaymentStatus == PaymentStatus.Payed)
                        .ToList();

                    return new DonationReportSubscriptionRow
                    {
                        PublicId = subscription.PublicId,
                        StatusLabel = this.GetSubscriptionStatusLabel(subscription.Status),
                        Frequency = string.IsNullOrWhiteSpace(subscription.Frequency)
                            ? "—"
                            : subscription.Frequency,
                        Created = subscription.Created,
                        PaidDonationCount = paidDonations.Count,
                        TotalPaidAmount = paidDonations.Sum(donation => donation.DonationAmount),
                    };
                })
                .OrderByDescending(row => row.TotalPaidAmount)
                .ThenByDescending(row => row.PaidDonationCount)
                .ThenBy(row => row.Created)
                .ToList();

            return section;
        }

        private string GetSubscriptionStatusLabel(SubscriptionStatus status)
        {
            return status switch
            {
                SubscriptionStatus.Created => "Criada",
                SubscriptionStatus.Capture => "Captura",
                SubscriptionStatus.Active => "Ativa",
                SubscriptionStatus.Inactive => "Inativa",
                SubscriptionStatus.Error => "Erro",
                _ => status.ToString(),
            };
        }

        private DonationReportFilterPayload BuildFilterPayload(
            List<DonationProjection> allDonations,
            List<DonationItemProjection> allItems,
            List<SubscriptionProjection> subscriptionProjections)
        {
            var payload = new DonationReportFilterPayload
            {
                All = this.BuildCampaignDetail(
                    null,
                    "Todas as campanhas",
                    allDonations,
                    allItems,
                    subscriptionProjections,
                    useAllItems: true),
                Options = new List<DonationReportCampaignFilterOption>
                {
                    new DonationReportCampaignFilterOption
                    {
                        Key = DonationReportFilterPayload.AllCampaignsKey,
                        Label = "Todas as campanhas",
                    },
                },
            };

            payload.All.CampaignKey = DonationReportFilterPayload.AllCampaignsKey;
            payload.All.CampaignName = "Todas as campanhas";

            List<IGrouping<int?, DonationProjection>> campaignGroups = this.OrderCampaignGroups(
                allDonations
                    .GroupBy(d => d.CampaignId)
                    .ToList());

            foreach (IGrouping<int?, DonationProjection> group in campaignGroups)
            {
                string campaignDisplayName = this.ResolveGroupDisplayName(group);
                DonationReportCampaignDetail detail = this.BuildCampaignDetail(
                    group.Key,
                    campaignDisplayName,
                    group.ToList(),
                    allItems,
                    subscriptionProjections);
                payload.Campaigns.Add(detail);
                payload.Options.Add(new DonationReportCampaignFilterOption
                {
                    Key = this.BuildCampaignKey(group.Key),
                    Label = campaignDisplayName,
                });
            }

            payload.Comparison = this.BuildCampaignComparison(campaignGroups, allItems);
            return payload;
        }

        private DonationReportCampaignDetail BuildCampaignDetail(
            int? campaignId,
            string campaignDisplayName,
            List<DonationProjection> campaignDonations,
            List<DonationItemProjection> allItems,
            List<SubscriptionProjection> subscriptionProjections,
            bool useAllItems = false)
        {
            string campaignKey = campaignId.HasValue || !useAllItems
                ? this.BuildCampaignKey(campaignId)
                : DonationReportFilterPayload.AllCampaignsKey;

            List<DonationItemProjection> campaignItems = useAllItems
                ? allItems
                : allItems.Where(i => i.CampaignId == campaignId).ToList();

            double totalPaid = campaignDonations
                .Where(d => d.PaymentStatus == PaymentStatus.Payed)
                .Sum(d => d.DonationAmount);

            int paidCount = campaignDonations.Count(d => d.PaymentStatus == PaymentStatus.Payed);
            int pending = campaignDonations.Count(d =>
                d.PaymentStatus == PaymentStatus.NotPayed || d.PaymentStatus == PaymentStatus.WaitingPayment);
            int total = campaignDonations.Count;

            return new DonationReportCampaignDetail
            {
                CampaignKey = campaignKey,
                CampaignName = campaignDisplayName,
                PeriodStart = campaignDonations.Count == 0 ? null : campaignDonations.Min(d => d.DonationDate),
                PeriodEnd = campaignDonations.Count == 0 ? null : campaignDonations.Max(d => d.DonationDate),
                Summary = this.BuildSummary(campaignDonations, campaignItems),
                DailyTrend = this.BuildDailyTrend(campaignDonations),
                FoodBanks = this.BuildFoodBankRows(campaignDonations, campaignItems, totalPaid),
                Products = this.BuildProductRows(campaignItems),
                Payments = this.BuildPaymentRows(campaignDonations),
                PaymentStatuses = this.BuildStatusRows(campaignDonations),
                TemporalAnalysis = this.BuildTemporalAnalysis(campaignDonations),
                PendingCount = pending,
                ConversionPercent = total == 0 ? 0 : (paidCount * 100.0) / total,
                Subscriptions = this.BuildSubscriptionSection(
                    subscriptionProjections,
                    campaignId,
                    useAllItems),
            };
        }

        private DonationReportCampaignComparison BuildCampaignComparison(
            List<IGrouping<int?, DonationProjection>> campaignGroups,
            List<DonationItemProjection> allItems)
        {
            var orderedGroups = campaignGroups;

            var comparison = new DonationReportCampaignComparison
            {
                CampaignLabels = orderedGroups.Select(this.ResolveGroupDisplayName).ToList(),
            };

            comparison.CampaignTotals = orderedGroups
                .Select(g => g.Where(d => d.PaymentStatus == PaymentStatus.Payed).Sum(d => d.DonationAmount))
                .ToList();

            comparison.CampaignAverageDonations = new List<double>();
            comparison.CampaignMedianDonations = new List<double>();
            comparison.CampaignMaxDonations = new List<double>();
            comparison.CampaignMinDonations = new List<double>();
            foreach (IGrouping<int?, DonationProjection> group in orderedGroups)
            {
                (double average, double median, double max, double min) = this.ComputePaidDonationStats(group);
                comparison.CampaignAverageDonations.Add(average);
                comparison.CampaignMedianDonations.Add(median);
                comparison.CampaignMaxDonations.Add(max);
                comparison.CampaignMinDonations.Add(min);
            }

            Dictionary<string, double[]> bankTotals = new Dictionary<string, double[]>();
            Dictionary<string, double[]> productTotals = new Dictionary<string, double[]>();
            int columnCount = orderedGroups.Count;

            for (int i = 0; i < orderedGroups.Count; i++)
            {
                IGrouping<int?, DonationProjection> group = orderedGroups[i];
                int? campaignId = group.Key;

                foreach (IGrouping<string, DonationProjection> bankGroup in group
                    .Where(d => d.PaymentStatus == PaymentStatus.Payed)
                    .GroupBy(d => d.FoodBankName))
                {
                    if (!bankTotals.TryGetValue(bankGroup.Key, out double[] amounts))
                    {
                        amounts = new double[columnCount];
                        bankTotals[bankGroup.Key] = amounts;
                    }

                    amounts[i] = bankGroup.Sum(d => d.DonationAmount);
                }

                foreach (IGrouping<string, DonationItemProjection> productGroup in allItems
                    .Where(item => item.CampaignId == campaignId)
                    .GroupBy(item => item.ProductName))
                {
                    if (!productTotals.TryGetValue(productGroup.Key, out double[] units))
                    {
                        units = new double[columnCount];
                        productTotals[productGroup.Key] = units;
                    }

                    units[i] = productGroup.Sum(item => item.Quantity);
                }
            }

            comparison.FoodBankAmountSeries = bankTotals
                .Select(kvp => new DonationReportSeriesRow
                {
                    Label = kvp.Key,
                    Values = kvp.Value,
                })
                .OrderByDescending(s => s.Values.Sum())
                .Take(10)
                .ToList();

            comparison.ProductUnitSeries = productTotals
                .Select(kvp => new DonationReportSeriesRow
                {
                    Label = kvp.Key,
                    Values = kvp.Value,
                })
                .OrderByDescending(s => s.Values.Sum())
                .Take(10)
                .ToList();

            return comparison;
        }

        private (double Average, double Median, double Max, double Min) ComputePaidDonationStats(
            IEnumerable<DonationProjection> donations)
        {
            List<double> amounts = donations
                .Where(d => d.PaymentStatus == PaymentStatus.Payed)
                .Select(d => d.DonationAmount)
                .OrderBy(amount => amount)
                .ToList();

            if (amounts.Count == 0)
            {
                return (0, 0, 0, 0);
            }

            double average = amounts.Average();
            double min = amounts[0];
            double max = amounts[^1];
            int mid = amounts.Count / 2;
            double median = amounts.Count % 2 == 1
                ? amounts[mid]
                : (amounts[mid - 1] + amounts[mid]) / 2.0;

            return (average, median, max, min);
        }

        private DonationProjection MapDonation(Donation d)
        {
            return new DonationProjection
            {
                Id = d.Id,
                DonationAmount = d.DonationAmount,
                DonationDate = d.DonationDate,
                PaymentStatus = d.PaymentStatus,
                CampaignId = d.CampaignId,
                CampaignName = this.ResolveCampaignDisplayName(d),
                CampaignStart = d.Campaign?.Start,
                IsDefaultCampaign = d.Campaign?.IsDefaultCampaign ?? false,
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
                MaxSingleDonation = paid.Count == 0 ? 0 : paid.Max(d => d.DonationAmount),
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
            return this.OrderCampaignGroups(donations.GroupBy(d => d.CampaignId).ToList())
                .Select(g =>
                {
                    int paidCount = g.Count(d => d.PaymentStatus == PaymentStatus.Payed);
                    int pending = g.Count(d =>
                        d.PaymentStatus == PaymentStatus.NotPayed || d.PaymentStatus == PaymentStatus.WaitingPayment);
                    double paidAmount = g.Where(d => d.PaymentStatus == PaymentStatus.Payed).Sum(d => d.DonationAmount);
                    int total = g.Count();
                    return new DonationReportCampaignRow
                    {
                        CampaignName = this.ResolveGroupDisplayName(g),
                        PaidAmount = paidAmount,
                        PaidCount = paidCount,
                        PendingCount = pending,
                        AveragePaidAmount = paidCount == 0 ? 0 : paidAmount / paidCount,
                        ConversionPercent = total == 0 ? 0 : (paidCount * 100.0) / total,
                    };
                })
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
                        MaxPaidAmount = g.Max(d => d.DonationAmount),
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
                .GroupBy(d => new { d.CampaignId, Payment = this.ResolvePaymentLabel(d) })
                .Select(g => new DonationReportCrossRow
                {
                    DimensionA = g.Select(d => d.CampaignName).First(),
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

        private DonationReportTemporalAnalysis BuildTemporalAnalysis(List<DonationProjection> donations)
        {
            List<DonationProjection> paid = donations
                .Where(d => d.PaymentStatus == PaymentStatus.Payed)
                .ToList();

            var analysis = new DonationReportTemporalAnalysis();
            Dictionary<int, List<DonationProjection>> byHour = paid
                .GroupBy(d => d.DonationDate.Hour)
                .ToDictionary(g => g.Key, g => g.ToList());

            for (int hour = 0; hour < 24; hour++)
            {
                byHour.TryGetValue(hour, out List<DonationProjection> hourDonations);
                hourDonations ??= new List<DonationProjection>();
                analysis.HourlyDistribution.Add(new DonationReportHourlyPoint
                {
                    Hour = hour,
                    HourLabel = hour.ToString("00", PtCulture) + "h",
                    PaidCount = hourDonations.Count,
                    PaidAmount = hourDonations.Sum(d => d.DonationAmount),
                });
            }

            DonationReportHourlyPoint peakHour = analysis.HourlyDistribution
                .OrderByDescending(h => h.PaidCount)
                .ThenByDescending(h => h.PaidAmount)
                .FirstOrDefault();
            if (peakHour != null)
            {
                analysis.PeakHour = peakHour.Hour;
                analysis.PeakHourLabel = peakHour.HourLabel;
                analysis.PeakHourCount = peakHour.PaidCount;
            }

            foreach (DayOfWeek day in WeekdayOrder)
            {
                List<DonationProjection> dayDonations = paid
                    .Where(d => d.DonationDate.DayOfWeek == day)
                    .ToList();
                analysis.WeekdayDistribution.Add(new DonationReportWeekdayPoint
                {
                    DayOfWeek = day,
                    DayLabel = PtCulture.DateTimeFormat.GetDayName(day),
                    PaidCount = dayDonations.Count,
                    PaidAmount = dayDonations.Sum(d => d.DonationAmount),
                });
            }

            DonationReportWeekdayPoint peakDay = analysis.WeekdayDistribution
                .OrderByDescending(d => d.PaidCount)
                .ThenByDescending(d => d.PaidAmount)
                .FirstOrDefault();
            if (peakDay != null)
            {
                analysis.PeakDayOfWeek = peakDay.DayOfWeek;
                analysis.PeakDayLabel = peakDay.DayLabel;
                analysis.PeakDayCount = peakDay.PaidCount;
            }

            return analysis;
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

        private string BuildCampaignKey(int? campaignId)
        {
            return campaignId.HasValue
                ? campaignId.Value.ToString(PtCulture)
                : DonationReportFilterPayload.NoCampaignKey;
        }

        private string ResolveCampaignDisplayName(Donation donation)
        {
            if (donation.Campaign != null)
            {
                return donation.Campaign.Number;
            }

            if (!string.IsNullOrWhiteSpace(donation.CampaignName))
            {
                return donation.CampaignName;
            }

            return "(sem campanha)";
        }

        private string ResolveGroupDisplayName(IEnumerable<DonationProjection> donations)
        {
            DonationProjection sample = donations.FirstOrDefault();
            if (sample == null)
            {
                return "(sem campanha)";
            }

            return sample.CampaignName ?? "(sem campanha)";
        }

        private List<IGrouping<int?, DonationProjection>> OrderCampaignGroups(
            List<IGrouping<int?, DonationProjection>> campaignGroups)
        {
            return campaignGroups
                .OrderBy(this.GetCampaignSortRank)
                .ThenBy(this.GetCampaignSortStart)
                .ThenBy(g => this.ResolveGroupDisplayName(g), StringComparer.Ordinal)
                .ToList();
        }

        private int GetCampaignSortRank(IGrouping<int?, DonationProjection> group)
        {
            if (!group.Key.HasValue)
            {
                return 2;
            }

            return group.FirstOrDefault()?.IsDefaultCampaign == true ? 0 : 1;
        }

        private DateTime GetCampaignSortStart(IGrouping<int?, DonationProjection> group)
        {
            return group.FirstOrDefault()?.CampaignStart ?? DateTime.MaxValue;
        }

        private sealed class DonationProjection
        {
            public int Id { get; set; }

            public double DonationAmount { get; set; }

            public DateTime DonationDate { get; set; }

            public PaymentStatus PaymentStatus { get; set; }

            public int? CampaignId { get; set; }

            public string CampaignName { get; set; }

            public DateTime? CampaignStart { get; set; }

            public bool IsDefaultCampaign { get; set; }

            public bool IsCashDonation { get; set; }

            public int FoodBankId { get; set; }

            public string FoodBankName { get; set; }

            public BasePayment ConfirmedPayment { get; set; }
        }

        private sealed class DonationItemProjection
        {
            public int DonationId { get; set; }

            public int? CampaignId { get; set; }

            public string CampaignName { get; set; }

            public string FoodBankName { get; set; }

            public string ProductName { get; set; }

            public string UnitOfMeasure { get; set; }

            public int Quantity { get; set; }

            public double LineValue { get; set; }
        }

        private sealed class SubscriptionProjection
        {
            public int Id { get; set; }

            public Guid PublicId { get; set; }

            public SubscriptionStatus Status { get; set; }

            public string Frequency { get; set; }

            public DateTime Created { get; set; }

            public List<LinkedDonationProjection> Donations { get; set; } = new List<LinkedDonationProjection>();
        }

        private sealed class LinkedDonationProjection
        {
            public int DonationId { get; set; }

            public int? CampaignId { get; set; }

            public PaymentStatus PaymentStatus { get; set; }

            public double DonationAmount { get; set; }
        }
    }
}
