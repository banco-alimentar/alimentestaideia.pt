// -----------------------------------------------------------------------
// <copyright file="DuplicateGroupService.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Data
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// Caches the group overview and loads group details on demand.
    /// </summary>
    public sealed class DuplicateGroupService : IDisposable
    {
        private readonly IDbContextFactory<ApplicationDbContext> factory;
        private readonly SemaphoreSlim gate = new SemaphoreSlim(1, 1);
        private GroupOverview? overview;

        /// <summary>
        /// Initializes a new instance of the <see cref="DuplicateGroupService"/> class.
        /// </summary>
        /// <param name="factory">Context factory.</param>
        public DuplicateGroupService(IDbContextFactory<ApplicationDbContext> factory)
        {
            this.factory = factory;
        }

        /// <summary>
        /// Raised after the overview is reloaded or invalidated.
        /// </summary>
        public event EventHandler? OverviewChanged;

        /// <summary>
        /// Gets the cached overview, if loaded.
        /// </summary>
        public GroupOverview? Current => this.overview;

        /// <summary>
        /// Gets the overview, loading it when missing or when asked to refresh.
        /// </summary>
        /// <param name="refresh">Reload from the database.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The overview.</returns>
        public async Task<GroupOverview> GetOverviewAsync(bool refresh, CancellationToken cancellationToken)
        {
            GroupOverview? cached = this.overview;
            if (!refresh && cached != null)
            {
                return cached;
            }

            await this.gate.WaitAsync(cancellationToken);
            try
            {
                cached = this.overview;
                if (refresh || cached == null)
                {
                    await using ApplicationDbContext db = await this.factory.CreateDbContextAsync(cancellationToken);
                    GroupAnalyzer.AnalysisResult result = await GroupAnalyzer.AnalyzeAsync(db, null, false, cancellationToken);
                    cached = new GroupOverview(result.Groups, DateTime.UtcNow, result.Warnings);
                    this.overview = cached;
                }
            }
            finally
            {
                this.gate.Release();
            }

            this.OverviewChanged?.Invoke(this, EventArgs.Empty);
            return cached;
        }

        /// <summary>
        /// Loads a fresh detail for one group.
        /// </summary>
        /// <param name="groupId">Group ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The detail, or null when the group no longer has duplicates.</returns>
        public async Task<GroupDetail?> GetDetailAsync(int groupId, CancellationToken cancellationToken)
        {
            await using ApplicationDbContext db = await this.factory.CreateDbContextAsync(cancellationToken);
            GroupAnalyzer.AnalysisResult result = await GroupAnalyzer.AnalyzeAsync(db, groupId, false, cancellationToken);
            AnalyzedGroup? group = result.Groups.SingleOrDefault(candidate => candidate.GroupId == groupId);
            if (group == null)
            {
                return null;
            }

            AuditTablePresence audits = await DuplicateGroupLoader.LoadAuditTablePresenceAsync(db, cancellationToken);
            return await GroupDetailLoader.LoadAsync(db, group, result.Environments[groupId], audits, cancellationToken);
        }

        /// <summary>
        /// Drops the cached overview so the next read reloads it.
        /// </summary>
        public void Invalidate()
        {
            this.overview = null;
            this.OverviewChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.gate.Dispose();
        }
    }
}
