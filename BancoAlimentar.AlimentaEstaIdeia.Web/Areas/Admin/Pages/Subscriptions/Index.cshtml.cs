// -----------------------------------------------------------------------
// <copyright file="Index.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages.Subscriptions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// List subscriptions model.
    /// </summary>
    public class IndexModel : PageModel
    {
        private const int PageSize = 25;
        private const int MaxEmailSearchLength = 256;
        private const int MaxPublicIdSearchLength = 36;
        private const int MaxEasyPaySubscriptionIdSearchLength = 64;
        private const string NoFrequencyFilterValue = "__none__";

        private readonly IUnitOfWork context;
        private readonly ApplicationDbContext dbContext;
        private int? defaultCampaignId;

        /// <summary>
        /// Initializes a new instance of the <see cref="IndexModel"/> class.
        /// </summary>
        /// <param name="context">Unit of work.</param>
        /// <param name="dbContext">Application database context.</param>
        public IndexModel(IUnitOfWork context, ApplicationDbContext dbContext)
        {
            this.context = context;
            this.dbContext = dbContext;
        }

        /// <summary>
        /// Gets or sets the list of subscriptions for the current page.
        /// </summary>
        public IList<Subscription> Subscriptions { get; set; } = new List<Subscription>();

        /// <summary>
        /// Gets donation statistics keyed by subscription id.
        /// </summary>
        public IReadOnlyDictionary<int, SubscriptionDonationSummary> DonationStats { get; private set; } =
            new Dictionary<int, SubscriptionDonationSummary>();

        /// <summary>
        /// Gets or sets the subscription status filter.
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public SubscriptionStatus? StatusFilter { get; set; }

        /// <summary>
        /// Gets or sets the owner email search filter.
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public string EmailSearch { get; set; }

        /// <summary>
        /// Gets or sets the public id search filter.
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public string PublicIdSearch { get; set; }

        /// <summary>
        /// Gets or sets the Easypay subscription id search filter.
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public string EasyPaySubscriptionIdSearch { get; set; }

        /// <summary>
        /// Gets or sets the campaign filter (initial donation campaign).
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public int? CampaignFilter { get; set; }

        /// <summary>
        /// Gets or sets the subscription frequency filter.
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public string FrequencyFilter { get; set; }

        /// <summary>
        /// Gets campaign filter options with counts.
        /// </summary>
        public IList<SubscriptionFilterOption> CampaignFilterOptions { get; private set; } =
            new List<SubscriptionFilterOption>();

        /// <summary>
        /// Gets frequency filter options with counts.
        /// </summary>
        public IList<SubscriptionFilterOption> FrequencyFilterOptions { get; private set; } =
            new List<SubscriptionFilterOption>();

        /// <summary>
        /// Gets the subscription count for the all-campaigns filter option.
        /// </summary>
        public int CampaignFilterAllCount { get; private set; }

        /// <summary>
        /// Gets the subscription count for the all-frequencies filter option.
        /// </summary>
        public int FrequencyFilterAllCount { get; private set; }

        /// <summary>
        /// Gets or sets the sort column name.
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public string SortBy { get; set; } = "Created";

        /// <summary>
        /// Gets or sets a value indicating whether sort is descending.
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public bool SortDescending { get; set; } = true;

        /// <summary>
        /// Gets or sets the current page index (1-based).
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public int PageIndex { get; set; } = 1;

        /// <summary>
        /// Gets the number of subscriptions per page.
        /// </summary>
        public int SubscriptionsPerPage => PageSize;

        /// <summary>
        /// Gets the total number of subscriptions.
        /// </summary>
        public int TotalCount { get; private set; }

        /// <summary>
        /// Gets the total number of pages.
        /// </summary>
        public int TotalPages { get; private set; }

        /// <summary>
        /// Gets a value indicating whether a previous page exists.
        /// </summary>
        public bool HasPreviousPage => PageIndex > 1;

        /// <summary>
        /// Gets a value indicating whether a next page exists.
        /// </summary>
        public bool HasNextPage => PageIndex < TotalPages;

        /// <summary>
        /// Gets all subscription status values for the filter dropdown.
        /// </summary>
        public IReadOnlyList<SubscriptionStatus> StatusOptions { get; } =
            Enum.GetValues<SubscriptionStatus>().ToList();

        /// <summary>
        /// Gets a value indicating whether any search filter is active.
        /// </summary>
        public bool HasActiveFilters =>
            StatusFilter.HasValue
            || !string.IsNullOrWhiteSpace(EmailSearch)
            || !string.IsNullOrWhiteSpace(PublicIdSearch)
            || !string.IsNullOrWhiteSpace(EasyPaySubscriptionIdSearch)
            || CampaignFilter.HasValue
            || !string.IsNullOrWhiteSpace(FrequencyFilter);

        /// <summary>
        /// Execute the get operation.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public async Task OnGetAsync()
        {
            EmailSearch = NormalizeSearch(EmailSearch, MaxEmailSearchLength);
            PublicIdSearch = NormalizeSearch(PublicIdSearch, MaxPublicIdSearchLength);
            EasyPaySubscriptionIdSearch = NormalizeSearch(EasyPaySubscriptionIdSearch, MaxEasyPaySubscriptionIdSearchLength);
            FrequencyFilter = NormalizeFrequencyFilter(FrequencyFilter);
            SortBy = NormalizeSortBy(SortBy);

            if (PageIndex < 1)
            {
                PageIndex = 1;
            }

            this.defaultCampaignId = await this.dbContext.Campaigns
                .AsNoTracking()
                .Where(campaign => campaign.IsDefaultCampaign)
                .Select(campaign => (int?)campaign.Id)
                .FirstOrDefaultAsync();

            await this.LoadFilterOptionsAsync();

            IQueryable<Subscription> query = this.GetBaseQuery();
            query = ApplyFilters(query);

            TotalCount = await query.CountAsync();
            TotalPages = TotalCount == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

            if (TotalPages > 0 && PageIndex > TotalPages)
            {
                PageIndex = TotalPages;
            }

            if (TotalCount == 0)
            {
                return;
            }

            if (SortBy is "DonationCount" or "DonationTotal")
            {
                await this.LoadPageSortedByDonationStatsAsync(query);
                return;
            }

            query = ApplySort(query);

            Subscriptions = await query
                .Include(subscription => subscription.User)
                .Include(subscription => subscription.InitialDonation)
                .Skip((PageIndex - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            await this.LoadDonationStatsAsync();
        }

        /// <summary>
        /// Gets the number of donations for a subscription.
        /// </summary>
        /// <param name="subscriptionId">The subscription id.</param>
        /// <returns>The donation count.</returns>
        public int GetDonationCount(int subscriptionId)
        {
            return this.DonationStats.TryGetValue(subscriptionId, out SubscriptionDonationSummary stats)
                ? stats.DonationCount
                : 0;
        }

        /// <summary>
        /// Gets the total donated amount for a subscription.
        /// </summary>
        /// <param name="subscriptionId">The subscription id.</param>
        /// <returns>The total donation amount.</returns>
        public double GetDonationTotal(int subscriptionId)
        {
            return this.DonationStats.TryGetValue(subscriptionId, out SubscriptionDonationSummary stats)
                ? stats.DonationTotal
                : 0;
        }

        /// <summary>
        /// Gets the next sort direction when a column header is clicked.
        /// </summary>
        /// <param name="column">The column name.</param>
        /// <returns>True when the next sort should be descending.</returns>
        public bool GetNextSortDescending(string column)
        {
            string normalizedColumn = NormalizeSortBy(column);
            if (string.Equals(SortBy, normalizedColumn, StringComparison.OrdinalIgnoreCase))
            {
                return !SortDescending;
            }

            return true;
        }

        /// <summary>
        /// Gets the sort direction indicator for a column header.
        /// </summary>
        /// <param name="column">The column name.</param>
        /// <returns>The sort indicator text.</returns>
        public string GetSortIndicator(string column)
        {
            if (!string.Equals(SortBy, NormalizeSortBy(column), StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }

            return SortDescending ? " ▼" : " ▲";
        }

        private static string NormalizeFrequencyKey(string frequency)
        {
            if (string.IsNullOrWhiteSpace(frequency))
            {
                return NoFrequencyFilterValue;
            }

            return frequency.Trim().TrimStart('_');
        }

        private static string GetFrequencyFilterLabel(string frequencyKey)
        {
            return frequencyKey == NoFrequencyFilterValue ? "(sem frequência)" : frequencyKey;
        }

        private IQueryable<Subscription> GetBaseQuery()
        {
            return this.context.SubscriptionRepository.GetAll().AsNoTracking();
        }

        private IQueryable<Subscription> ApplyFilters(
            IQueryable<Subscription> query,
            bool applyCampaign = true,
            bool applyFrequency = true)
        {
            if (StatusFilter.HasValue)
            {
                query = query.Where(subscription => subscription.Status == StatusFilter.Value);
            }

            if (!string.IsNullOrWhiteSpace(EmailSearch))
            {
                query = query.Where(subscription =>
                    subscription.User != null
                    && subscription.User.Email != null
                    && subscription.User.Email.Contains(EmailSearch));
            }

            if (!string.IsNullOrWhiteSpace(PublicIdSearch))
            {
                if (Guid.TryParse(PublicIdSearch, out Guid publicId))
                {
                    query = query.Where(subscription => subscription.PublicId == publicId);
                }
                else
                {
                    query = query.Where(subscription => false);
                }
            }

            if (!string.IsNullOrWhiteSpace(EasyPaySubscriptionIdSearch))
            {
                query = query.Where(subscription =>
                    subscription.EasyPaySubscriptionId != null
                    && subscription.EasyPaySubscriptionId.Contains(EasyPaySubscriptionIdSearch));
            }

            if (applyCampaign && CampaignFilter.HasValue)
            {
                int campaignId = CampaignFilter.Value;
                int? resolvedDefaultCampaignId = this.defaultCampaignId;
                query = query.Where(subscription =>
                    subscription.InitialDonation != null
                    && (subscription.InitialDonation.CampaignId == campaignId
                        || (!subscription.InitialDonation.CampaignId.HasValue
                            && resolvedDefaultCampaignId.HasValue
                            && resolvedDefaultCampaignId.Value == campaignId)));
            }

            if (applyFrequency && !string.IsNullOrWhiteSpace(FrequencyFilter))
            {
                if (FrequencyFilter == NoFrequencyFilterValue)
                {
                    query = query.Where(subscription => subscription.Frequency == null || subscription.Frequency == string.Empty);
                }
                else
                {
                    string frequencyWithPrefix = "_" + FrequencyFilter;
                    query = query.Where(subscription =>
                        subscription.Frequency == FrequencyFilter
                        || subscription.Frequency == frequencyWithPrefix);
                }
            }

            return query;
        }

        private async Task LoadFilterOptionsAsync()
        {
            Dictionary<int, string> campaignNames = await this.dbContext.Campaigns
                .AsNoTracking()
                .ToDictionaryAsync(campaign => campaign.Id, campaign => campaign.Number);

            IQueryable<Subscription> queryForCampaignCounts = ApplyFilters(this.GetBaseQuery(), applyCampaign: false);
            CampaignFilterAllCount = await queryForCampaignCounts.CountAsync();

            List<int?> campaignIds = await queryForCampaignCounts
                .Where(subscription => subscription.InitialDonation != null)
                .Select(subscription => subscription.InitialDonation.CampaignId)
                .ToListAsync();

            CampaignFilterOptions = campaignIds
                .Select(campaignId => campaignId ?? this.defaultCampaignId)
                .Where(campaignId => campaignId.HasValue)
                .GroupBy(campaignId => campaignId.Value)
                .Select(group => new SubscriptionFilterOption
                {
                    Value = group.Key.ToString(),
                    Label = campaignNames.TryGetValue(group.Key, out string name) ? name : group.Key.ToString(),
                    Count = group.Count(),
                })
                .OrderBy(option => option.Label, StringComparer.Ordinal)
                .ToList();

            IQueryable<Subscription> queryForFrequencyCounts = ApplyFilters(this.GetBaseQuery(), applyFrequency: false);
            FrequencyFilterAllCount = await queryForFrequencyCounts.CountAsync();

            List<string> frequencyValues = await queryForFrequencyCounts
                .Select(subscription => subscription.Frequency)
                .ToListAsync();

            FrequencyFilterOptions = frequencyValues
                .GroupBy(NormalizeFrequencyKey)
                .Select(group => new SubscriptionFilterOption
                {
                    Value = group.Key,
                    Label = GetFrequencyFilterLabel(group.Key),
                    Count = group.Count(),
                })
                .OrderBy(option => option.Label, StringComparer.Ordinal)
                .ToList();
        }

        private IQueryable<Subscription> ApplySort(IQueryable<Subscription> query)
        {
            return SortBy switch
            {
                "Status" => SortDescending
                    ? query.OrderByDescending(subscription => subscription.Status).ThenByDescending(subscription => subscription.Id)
                    : query.OrderBy(subscription => subscription.Status).ThenBy(subscription => subscription.Id),
                "ExpirationTime" => SortDescending
                    ? query.OrderByDescending(subscription => subscription.ExpirationTime).ThenByDescending(subscription => subscription.Id)
                    : query.OrderBy(subscription => subscription.ExpirationTime).ThenBy(subscription => subscription.Id),
                "StartTime" => SortDescending
                    ? query.OrderByDescending(subscription => subscription.StartTime).ThenByDescending(subscription => subscription.Id)
                    : query.OrderBy(subscription => subscription.StartTime).ThenBy(subscription => subscription.Id),
                "OwnerEmail" => SortDescending
                    ? query.OrderByDescending(subscription => subscription.User.Email).ThenByDescending(subscription => subscription.Id)
                    : query.OrderBy(subscription => subscription.User.Email).ThenBy(subscription => subscription.Id),
                "OwnerName" => SortDescending
                    ? query.OrderByDescending(subscription => subscription.User.FullName).ThenByDescending(subscription => subscription.Id)
                    : query.OrderBy(subscription => subscription.User.FullName).ThenBy(subscription => subscription.Id),
                "Frequency" => SortDescending
                    ? query.OrderByDescending(subscription => subscription.Frequency).ThenByDescending(subscription => subscription.Id)
                    : query.OrderBy(subscription => subscription.Frequency).ThenBy(subscription => subscription.Id),
                _ => SortDescending
                    ? query.OrderByDescending(subscription => subscription.Created).ThenByDescending(subscription => subscription.Id)
                    : query.OrderBy(subscription => subscription.Created).ThenBy(subscription => subscription.Id),
            };
        }

        private string NormalizeSortBy(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "Created";
            }

            return value switch
            {
                "Created" or "Status" or "ExpirationTime" or "StartTime" or "OwnerEmail" or "OwnerName" or "Frequency" or "DonationCount" or "DonationTotal" => value,
                _ => "Created",
            };
        }

        private string NormalizeSearch(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            string trimmed = value.Trim();
            if (trimmed.Length > maxLength)
            {
                trimmed = trimmed.Substring(0, maxLength);
            }

            return trimmed;
        }

        private string NormalizeFrequencyFilter(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            string trimmed = value.Trim();
            if (trimmed == NoFrequencyFilterValue)
            {
                return NoFrequencyFilterValue;
            }

            return NormalizeFrequencyKey(trimmed);
        }

        private async Task LoadDonationStatsAsync()
        {
            if (Subscriptions.Count == 0)
            {
                DonationStats = new Dictionary<int, SubscriptionDonationSummary>();
                return;
            }

            List<int> subscriptionIds = Subscriptions.Select(subscription => subscription.Id).ToList();
            DonationStats = await this.LoadDonationStatsForIdsAsync(subscriptionIds);
        }

        private async Task LoadPageSortedByDonationStatsAsync(IQueryable<Subscription> query)
        {
            List<int> filteredIds = await query.Select(subscription => subscription.Id).ToListAsync();
            Dictionary<int, SubscriptionDonationSummary> statsBySubscriptionId =
                await this.LoadDonationStatsForIdsAsync(filteredIds);

            IEnumerable<int> sortedIds = SortBy == "DonationCount"
                ? SortSubscriptionIdsByDonationCount(filteredIds, statsBySubscriptionId)
                : SortSubscriptionIdsByDonationTotal(filteredIds, statsBySubscriptionId);

            List<int> pageIds = sortedIds
                .Skip((PageIndex - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            if (pageIds.Count == 0)
            {
                Subscriptions = new List<Subscription>();
                DonationStats = new Dictionary<int, SubscriptionDonationSummary>();
                return;
            }

            List<Subscription> pageSubscriptions = await query
                .Where(subscription => pageIds.Contains(subscription.Id))
                .Include(subscription => subscription.User)
                .Include(subscription => subscription.InitialDonation)
                .ToListAsync();

            Dictionary<int, Subscription> subscriptionsById = pageSubscriptions.ToDictionary(subscription => subscription.Id);
            Subscriptions = pageIds
                .Where(subscriptionsById.ContainsKey)
                .Select(id => subscriptionsById[id])
                .ToList();

            DonationStats = pageIds.ToDictionary(
                id => id,
                id => statsBySubscriptionId.TryGetValue(id, out SubscriptionDonationSummary stats)
                    ? stats
                    : new SubscriptionDonationSummary { SubscriptionId = id });
        }

        private IEnumerable<int> SortSubscriptionIdsByDonationCount(
            List<int> subscriptionIds,
            Dictionary<int, SubscriptionDonationSummary> statsBySubscriptionId)
        {
            return SortDescending
                ? subscriptionIds
                    .OrderByDescending(id => GetDonationCountFromStats(id, statsBySubscriptionId))
                    .ThenByDescending(id => id)
                : subscriptionIds
                    .OrderBy(id => GetDonationCountFromStats(id, statsBySubscriptionId))
                    .ThenBy(id => id);
        }

        private IEnumerable<int> SortSubscriptionIdsByDonationTotal(
            List<int> subscriptionIds,
            Dictionary<int, SubscriptionDonationSummary> statsBySubscriptionId)
        {
            return SortDescending
                ? subscriptionIds
                    .OrderByDescending(id => GetDonationTotalFromStats(id, statsBySubscriptionId))
                    .ThenByDescending(id => id)
                : subscriptionIds
                    .OrderBy(id => GetDonationTotalFromStats(id, statsBySubscriptionId))
                    .ThenBy(id => id);
        }

        private int GetDonationCountFromStats(
            int subscriptionId,
            Dictionary<int, SubscriptionDonationSummary> statsBySubscriptionId)
        {
            return statsBySubscriptionId.TryGetValue(subscriptionId, out SubscriptionDonationSummary stats)
                ? stats.DonationCount
                : 0;
        }

        private double GetDonationTotalFromStats(
            int subscriptionId,
            Dictionary<int, SubscriptionDonationSummary> statsBySubscriptionId)
        {
            return statsBySubscriptionId.TryGetValue(subscriptionId, out SubscriptionDonationSummary stats)
                ? stats.DonationTotal
                : 0;
        }

        private async Task<Dictionary<int, SubscriptionDonationSummary>> LoadDonationStatsForIdsAsync(
            List<int> subscriptionIds)
        {
            if (subscriptionIds.Count == 0)
            {
                return new Dictionary<int, SubscriptionDonationSummary>();
            }

            List<SubscriptionDonationSummary> stats = await (
                from subscriptionDonation in this.dbContext.SubscriptionDonations.AsNoTracking()
                where subscriptionDonation.Subscription != null
                    && subscriptionDonation.Donation != null
                    && subscriptionIds.Contains(subscriptionDonation.Subscription.Id)
                group subscriptionDonation.Donation by subscriptionDonation.Subscription.Id into grouped
                select new SubscriptionDonationSummary
                {
                    SubscriptionId = grouped.Key,
                    DonationCount = grouped.Count(),
                    DonationTotal = grouped.Sum(donation =>
                        donation.PaymentStatus == PaymentStatus.Payed ? donation.DonationAmount : 0),
                }).ToListAsync();

            return stats.ToDictionary(stat => stat.SubscriptionId);
        }

        /// <summary>
        /// Donation statistics for a subscription row.
        /// </summary>
        public sealed class SubscriptionDonationSummary
        {
            /// <summary>
            /// Gets or sets the subscription id.
            /// </summary>
            public int SubscriptionId { get; set; }

            /// <summary>
            /// Gets or sets the donation count.
            /// </summary>
            public int DonationCount { get; set; }

            /// <summary>
            /// Gets or sets the total donation amount.
            /// </summary>
            public double DonationTotal { get; set; }
        }
    }
}
