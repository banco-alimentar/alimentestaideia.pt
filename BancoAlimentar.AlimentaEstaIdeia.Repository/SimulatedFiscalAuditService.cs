// -----------------------------------------------------------------------
// <copyright file="SimulatedFiscalAuditService.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System;
    using System.Security.Claims;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// Appends development-simulation audit events from trusted in-process context.
    /// </summary>
    public class SimulatedFiscalAuditService : ISimulatedFiscalAuditService
    {
        private const string DevelopmentSimulationMode = "DevelopmentSimulation";
        private readonly ApplicationDbContext applicationDbContext;
        private readonly IHttpContextAccessor httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="SimulatedFiscalAuditService"/> class.
        /// </summary>
        /// <param name="applicationDbContext">The database isolated to the current tenant.</param>
        /// <param name="httpContextAccessor">The optional browser request context.</param>
        public SimulatedFiscalAuditService(
            ApplicationDbContext applicationDbContext,
            IHttpContextAccessor httpContextAccessor)
        {
            this.applicationDbContext = applicationDbContext ?? throw new ArgumentNullException(nameof(applicationDbContext));
            this.httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        /// <inheritdoc/>
        public void Append(Tenant tenant, SimulatedFiscalDocument document, SimulatedFiscalAuditEventRequest request)
        {
            Validate(tenant, document, request);
            HttpContext httpContext = this.httpContextAccessor.HttpContext;
            var auditEvent = new SimulatedFiscalAuditEvent
            {
                TenantId = tenant.Id,
                FoodBankId = document.FoodBankId,
                SimulatedFiscalDocument = document,
                Action = request.Action.Trim(),
                Result = request.Result.Trim(),
                Reason = request.Reason.Trim(),
                Mode = DevelopmentSimulationMode,
                ActorId = httpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                OccurredAtUtc = DateTimeOffset.UtcNow,
                CorrelationId = httpContext?.TraceIdentifier,
            };

            this.applicationDbContext.SimulatedFiscalAuditEvents.Add(auditEvent);
        }

        private static void Validate(Tenant tenant, SimulatedFiscalDocument document, SimulatedFiscalAuditEventRequest request)
        {
            if (tenant == null || tenant.Id <= 0 || document == null || document.TenantId != tenant.Id ||
                request == null || string.IsNullOrWhiteSpace(request.Action) ||
                string.IsNullOrWhiteSpace(request.Result) || string.IsNullOrWhiteSpace(request.Reason) ||
                request.Action.Trim().Length > 64 || request.Result.Trim().Length > 64 ||
                request.Reason.Trim().Length > 1_024)
            {
                throw new InvalidOperationException("The simulated fiscal audit event contains invalid simulation identifiers.");
            }
        }
    }
}
