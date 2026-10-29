// -----------------------------------------------------------------------
// <copyright file="Simulations.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages.Fiscal
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model.Strategy;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// Provides development-only, read-only evidence of synthetic fiscal receipt simulations.
    /// </summary>
    [Authorize(Policy = "RoleArea")]
    public class SimulationsModel : PageModel
    {
        private readonly ApplicationDbContext applicationDbContext;
        private readonly IFiscalIssuanceModeResolver fiscalIssuanceModeResolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="SimulationsModel"/> class.
        /// </summary>
        /// <param name="applicationDbContext">The database isolated to the current tenant.</param>
        /// <param name="fiscalIssuanceModeResolver">The process-controlled fiscal issuance mode resolver.</param>
        public SimulationsModel(
            ApplicationDbContext applicationDbContext,
            IFiscalIssuanceModeResolver fiscalIssuanceModeResolver)
        {
            this.applicationDbContext = applicationDbContext ?? throw new ArgumentNullException(nameof(applicationDbContext));
            this.fiscalIssuanceModeResolver = fiscalIssuanceModeResolver ?? throw new ArgumentNullException(nameof(fiscalIssuanceModeResolver));
        }

        /// <summary>
        /// Gets the immutable simulated receipt records for the current tenant database.
        /// </summary>
        public IReadOnlyList<SimulatedFiscalRecordViewModel> Records { get; private set; } =
            Array.Empty<SimulatedFiscalRecordViewModel>();

        /// <summary>
        /// Lists synthetic simulation records only while the process is explicitly in simulation mode.
        /// </summary>
        /// <returns>The fiscal simulation page, or a not-found result outside the simulation mode.</returns>
        public async Task<IActionResult> OnGetAsync()
        {
            if (this.fiscalIssuanceModeResolver.Resolve() != FiscalIssuanceMode.Simulation)
            {
                return this.NotFound();
            }

            List<SimulatedFiscalDocument> documents = await this.applicationDbContext.SimulatedFiscalDocuments
                .AsNoTracking()
                .OrderByDescending(document => document.IssuedAtUtc)
                .ThenByDescending(document => document.Id)
                .ToListAsync();

            int[] documentIds = documents.Select(document => document.Id).ToArray();
            Dictionary<int, IReadOnlyList<string>> auditEventsByDocumentId = await this.LoadAuditEventsAsync(documentIds);

            this.Records = documents
                .Select(document => new SimulatedFiscalRecordViewModel
                {
                    Id = document.Id,
                    SyntheticIdentifier = document.SyntheticIdentifier,
                    DonationId = document.DonationId,
                    ScopeKey = document.ScopeKey,
                    InvoicingStrategy = document.InvoicingStrategy,
                    FoodBankId = document.FoodBankId,
                    Sequence = document.Sequence,
                    LocalChecksum = document.LocalChecksum,
                    ArtifactReference = document.ArtifactReference,
                    OriginEnvironment = document.OriginEnvironment,
                    IssuedAtUtc = document.IssuedAtUtc,
                    AuditEvents = auditEventsByDocumentId.TryGetValue(document.Id, out IReadOnlyList<string> auditEvents)
                        ? auditEvents
                        : Array.Empty<string>(),
                })
                .ToList();

            return this.Page();
        }

        private async Task<Dictionary<int, IReadOnlyList<string>>> LoadAuditEventsAsync(int[] documentIds)
        {
            if (documentIds.Length == 0)
            {
                return new Dictionary<int, IReadOnlyList<string>>();
            }

            List<SimulatedFiscalAuditEvent> auditEvents = await this.applicationDbContext.SimulatedFiscalAuditEvents
                .AsNoTracking()
                .Where(auditEvent => documentIds.Contains(auditEvent.SimulatedFiscalDocumentId))
                .OrderByDescending(auditEvent => auditEvent.OccurredAtUtc)
                .ThenByDescending(auditEvent => auditEvent.Id)
                .ToListAsync();

            return auditEvents
                .GroupBy(auditEvent => auditEvent.SimulatedFiscalDocumentId)
                .ToDictionary(
                    group => group.Key,
                    group => (IReadOnlyList<string>)group
                        .Select(auditEvent => $"{auditEvent.OccurredAtUtc.ToLocalTime():yyyy-MM-dd HH:mm:ss zzz} — {auditEvent.Action}: {auditEvent.Result}" +
                            (string.IsNullOrWhiteSpace(auditEvent.Reason) ? string.Empty : $" ({auditEvent.Reason})"))
                        .ToList());
        }

        /// <summary>
        /// Represents a safe, non-fiscal summary of an immutable receipt simulation.
        /// </summary>
        public sealed class SimulatedFiscalRecordViewModel
        {
            /// <summary>
            /// Gets or sets the simulation record identifier.
            /// </summary>
            public int Id { get; set; }

            /// <summary>
            /// Gets or sets the non-fiscal synthetic receipt identifier.
            /// </summary>
            public string SyntheticIdentifier { get; set; }

            /// <summary>
            /// Gets or sets the donation identifier.
            /// </summary>
            public int DonationId { get; set; }

            /// <summary>
            /// Gets or sets the synthetic tenant or food-bank scope.
            /// </summary>
            public string ScopeKey { get; set; }

            /// <summary>
            /// Gets or sets the captured invoicing strategy.
            /// </summary>
            public InvoicingStrategy InvoicingStrategy { get; set; }

            /// <summary>
            /// Gets or sets the optional food-bank boundary.
            /// </summary>
            public int? FoodBankId { get; set; }

            /// <summary>
            /// Gets or sets the synthetic sequence number.
            /// </summary>
            public int Sequence { get; set; }

            /// <summary>
            /// Gets or sets the non-fiscal local checksum.
            /// </summary>
            public string LocalChecksum { get; set; }

            /// <summary>
            /// Gets or sets the private simulation artifact reference.
            /// </summary>
            public string ArtifactReference { get; set; }

            /// <summary>
            /// Gets or sets the host environment in which the simulation was produced.
            /// </summary>
            public string OriginEnvironment { get; set; }

            /// <summary>
            /// Gets or sets when the synthetic receipt was issued.
            /// </summary>
            public DateTimeOffset IssuedAtUtc { get; set; }

            /// <summary>
            /// Gets or sets the matching simulation audit event summaries.
            /// </summary>
            public IReadOnlyList<string> AuditEvents { get; set; } = Array.Empty<string>();
        }
    }
}
