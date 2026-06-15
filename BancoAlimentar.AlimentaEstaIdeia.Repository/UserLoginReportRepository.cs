// -----------------------------------------------------------------------
// <copyright file="UserLoginReportRepository.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
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
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.ViewModel.UserLoginReport;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// Builds aggregated user login reports from stored login events and user registrations.
    /// </summary>
    public class UserLoginReportRepository
    {
        private readonly ApplicationDbContext dbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserLoginReportRepository"/> class.
        /// </summary>
        /// <param name="dbContext">Application database context.</param>
        public UserLoginReportRepository(ApplicationDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        /// <summary>
        /// Gets campaign filter options with counts.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Campaign filter options ordered by start date descending.</returns>
        public async Task<IList<UserLoginCampaignFilterOption>> GetCampaignFilterOptionsAsync(
            CancellationToken cancellationToken = default)
        {
            var campaigns = await this.dbContext.Campaigns
                .AsNoTracking()
                .OrderByDescending(c => c.Start)
                .Select(c => new { c.Id, c.Number, c.Start, c.End, c.IsDefaultCampaign })
                .ToListAsync(cancellationToken);

            var loginCounts = await this.dbContext.UserLoginEvents
                .AsNoTracking()
                .Where(e => e.CampaignId != null)
                .GroupBy(e => e.CampaignId)
                .Select(g => new { CampaignId = g.Key.Value, Count = g.Count() })
                .ToListAsync(cancellationToken);

            var registrationCounts = await this.dbContext.Users
                .AsNoTracking()
                .Where(u => !u.IsAnonymous && u.RegistrationCampaignId != null)
                .GroupBy(u => u.RegistrationCampaignId)
                .Select(g => new { CampaignId = g.Key.Value, Count = g.Count() })
                .ToListAsync(cancellationToken);

            return campaigns
                .Select(c =>
                {
                    var label = FormatCampaignLabel(c.Number, c.Start, c.End, c.IsDefaultCampaign);
                    return new UserLoginCampaignFilterOption
                    {
                        CampaignId = c.Id,
                        Label = label,
                        LoginCount = loginCounts.FirstOrDefault(x => x.CampaignId == c.Id)?.Count ?? 0,
                        RegisteredUserCount = registrationCounts.FirstOrDefault(x => x.CampaignId == c.Id)?.Count ?? 0,
                    };
                })
                .ToList();
        }

        /// <summary>
        /// Gets the total registered users across all campaigns.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Registered user count.</returns>
        public async Task<int> GetAllCampaignsRegisteredUserCountAsync(CancellationToken cancellationToken = default)
        {
            return await this.GetRegisteredUsersQuery(null)
                .CountAsync(cancellationToken);
        }

        /// <summary>
        /// Gets the total login count across all campaigns.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Login count.</returns>
        public async Task<int> GetAllCampaignsLoginCountAsync(CancellationToken cancellationToken = default)
        {
            return await this.GetLoginEventsQuery(null)
                .CountAsync(cancellationToken);
        }

        /// <summary>
        /// Builds the user login report snapshot.
        /// </summary>
        /// <param name="campaignId">Optional campaign filter.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Report snapshot.</returns>
        public async Task<UserLoginReportSnapshot> GetSnapshotAsync(
            int? campaignId,
            CancellationToken cancellationToken = default)
        {
            var loginCountsByProvider = await this.GetLoginEventsQuery(campaignId)
                .GroupBy(e => e.LoginProvider)
                .Select(g => new { Provider = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            var users = await this.GetRegisteredUsersQuery(campaignId)
                .Select(u => new
                {
                    u.Id,
                    u.RegistrationLoginProvider,
                    u.PasswordHash,
                    ExternalProviders = u.Logins.Select(l => l.LoginProvider).ToList(),
                })
                .ToListAsync(cancellationToken);

            var registrationCountsByProvider = users
                .GroupBy(u => ResolveRegistrationProvider(
                    u.RegistrationLoginProvider,
                    u.PasswordHash,
                    u.ExternalProviders))
                .ToDictionary(g => g.Key, g => g.Count());

            var providerKeys = loginCountsByProvider
                .Select(x => x.Provider)
                .Union(registrationCountsByProvider.Keys)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var rows = providerKeys
                .Select(providerKey => new UserLoginProviderRow
                {
                    ProviderKey = providerKey,
                    ProviderDisplayName = UserLoginProviders.GetDisplayName(providerKey),
                    LoginCount = loginCountsByProvider.FirstOrDefault(x =>
                        string.Equals(x.Provider, providerKey, StringComparison.OrdinalIgnoreCase))?.Count ?? 0,
                    RegisteredUserCount = registrationCountsByProvider.TryGetValue(providerKey, out var count)
                        ? count
                        : 0,
                })
                .OrderByDescending(r => r.LoginCount)
                .ThenByDescending(r => r.RegisteredUserCount)
                .ThenBy(r => r.ProviderDisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return new UserLoginReportSnapshot
            {
                TotalLogins = loginCountsByProvider.Sum(x => x.Count),
                TotalRegisteredUsers = users.Count,
                Providers = rows,
            };
        }

        private static string ResolveRegistrationProvider(
            string registrationLoginProvider,
            string passwordHash,
            IList<string> externalProviders)
        {
            if (!string.IsNullOrWhiteSpace(registrationLoginProvider))
            {
                return registrationLoginProvider;
            }

            if (externalProviders != null && externalProviders.Count > 0)
            {
                return externalProviders[0];
            }

            if (!string.IsNullOrWhiteSpace(passwordHash))
            {
                return UserLoginProviders.Password;
            }

            return "Unknown";
        }

        private static string FormatCampaignLabel(string number, DateTime start, DateTime end, bool isDefaultCampaign)
        {
            var label = $"{number} ({start:yyyy-MM-dd} - {end:yyyy-MM-dd})";
            return isDefaultCampaign ? $"{label} (default)" : label;
        }

        private IQueryable<UserLoginEvent> GetLoginEventsQuery(int? campaignId)
        {
            var query = this.dbContext.UserLoginEvents.AsNoTracking();
            if (campaignId.HasValue)
            {
                query = query.Where(e => e.CampaignId == campaignId.Value);
            }

            return query;
        }

        private IQueryable<WebUser> GetRegisteredUsersQuery(int? campaignId)
        {
            var query = this.dbContext.Users
                .AsNoTracking()
                .Where(u => !u.IsAnonymous);

            if (campaignId.HasValue)
            {
                query = query.Where(u => u.RegistrationCampaignId == campaignId.Value);
            }

            return query;
        }
    }
}
