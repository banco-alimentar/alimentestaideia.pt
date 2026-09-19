// -----------------------------------------------------------------------
// <copyright file="FiscalDocumentCancellationService.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System;
    using System.Linq;
    using System.Security.Claims;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// Cancels an original issued fiscal document and appends a trusted audit event.
    /// </summary>
    public class FiscalDocumentCancellationService : IFiscalDocumentCancellationService
    {
        private readonly ApplicationDbContext applicationDbContext;
        private readonly IHttpContextAccessor httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="FiscalDocumentCancellationService"/> class.
        /// </summary>
        /// <param name="applicationDbContext">The tenant fiscal persistence context.</param>
        /// <param name="httpContextAccessor">The middleware-resolved tenant and actor context.</param>
        public FiscalDocumentCancellationService(
            ApplicationDbContext applicationDbContext,
            IHttpContextAccessor httpContextAccessor)
        {
            this.applicationDbContext = applicationDbContext ?? throw new ArgumentNullException(nameof(applicationDbContext));
            this.httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        /// <inheritdoc />
        public FiscalDocument Cancel(FiscalDocumentCancellationRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (request.FiscalDocumentId <= 0)
            {
                throw new InvalidOperationException("Fiscal cancellation requires a fiscal document identifier.");
            }

            string motive = this.NormalizeMotive(request.Motive);
            Tenant tenant = this.GetTrustedTenant();
            string actorId = this.GetTrustedActorId();
            FiscalDocument document = this.applicationDbContext.FiscalDocuments
                .SingleOrDefault(item => item.Id == request.FiscalDocumentId);
            if (document == null)
            {
                throw new InvalidOperationException("Fiscal cancellation requires a persisted fiscal document.");
            }

            if (document.InvoicingStrategy != tenant.InvoicingStrategy)
            {
                throw new InvalidOperationException("Fiscal cancellation document scope does not match the current tenant.");
            }

            if (document.Kind != FiscalDocumentKind.Original || document.Status != FiscalDocumentStatus.Issued)
            {
                throw new InvalidOperationException("Only an issued original fiscal document can be cancelled.");
            }

            document.Status = FiscalDocumentStatus.Cancelled;
            this.applicationDbContext.FiscalDocumentEvents.Add(new FiscalDocumentEvent
            {
                FiscalDocumentId = document.Id,
                EventType = "Cancelled",
                ActorId = actorId,
                OccurredAtUtc = DateTimeOffset.UtcNow,
                Reason = motive,
            });
            this.applicationDbContext.SaveChanges();
            return document;
        }

        private Tenant GetTrustedTenant()
        {
            Tenant tenant = this.httpContextAccessor.HttpContext?.GetTenant();
            if (tenant == null || ReferenceEquals(tenant, Tenant.EmptyTenant) || tenant.Id <= 0)
            {
                throw new InvalidOperationException("Fiscal cancellation requires a trusted current tenant.");
            }

            return tenant;
        }

        private string GetTrustedActorId()
        {
            string actorId = this.httpContextAccessor.HttpContext?.User?
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(actorId))
            {
                throw new InvalidOperationException("Fiscal cancellation requires an authenticated actor.");
            }

            return actorId;
        }

        private string NormalizeMotive(string motive)
        {
            string normalizedMotive = motive?.Trim();
            if (string.IsNullOrWhiteSpace(normalizedMotive) || normalizedMotive.Length > 1_024)
            {
                throw new InvalidOperationException("Fiscal cancellation requires a nonblank statutory motive.");
            }

            return normalizedMotive;
        }
    }
}
