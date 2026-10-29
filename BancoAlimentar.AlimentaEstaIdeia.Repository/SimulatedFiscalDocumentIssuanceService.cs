// -----------------------------------------------------------------------
// <copyright file="SimulatedFiscalDocumentIssuanceService.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System;
    using System.Data;
    using System.Linq;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model.Strategy;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Storage;

    /// <summary>
    /// Creates development-only receipt simulations without resolving AT profiles, series,
    /// certificates, fiscal signing, fiscal QR payloads or SAF-T dependencies.
    /// </summary>
    public class SimulatedFiscalDocumentIssuanceService : ISimulatedFiscalDocumentIssuanceService
    {
        private const string DevelopmentEnvironment = "Development";
        private const int MaximumReservationAttempts = 3;

        private readonly ApplicationDbContext applicationDbContext;
        private readonly ISimulatedFiscalIdentifierAllocator identifierAllocator;
        private readonly ISimulationChecksumService checksumService;
        private readonly ISimulatedReceiptQrPayloadBuilder qrPayloadBuilder;
        private readonly ISimulatedReceiptQrCodeImageGenerator qrCodeImageGenerator;
        private readonly IFiscalIssuanceModeResolver fiscalIssuanceModeResolver;
        private readonly ISimulatedFiscalAuditService simulatedFiscalAuditService;

        /// <summary>
        /// Initializes a new instance of the <see cref="SimulatedFiscalDocumentIssuanceService"/> class.
        /// </summary>
        /// <param name="applicationDbContext">The database isolated to the current tenant.</param>
        /// <param name="identifierAllocator">The synthetic scoped sequence allocator.</param>
        /// <param name="checksumService">The non-fiscal local checksum calculator.</param>
        /// <param name="qrPayloadBuilder">The private simulation QR payload builder.</param>
        /// <param name="qrCodeImageGenerator">The private simulation QR image generator.</param>
        /// <param name="fiscalIssuanceModeResolver">The process-selected fiscal issuance mode.</param>
        /// <param name="simulatedFiscalAuditService">The append-only development simulation audit service.</param>
        public SimulatedFiscalDocumentIssuanceService(
            ApplicationDbContext applicationDbContext,
            ISimulatedFiscalIdentifierAllocator identifierAllocator,
            ISimulationChecksumService checksumService,
            ISimulatedReceiptQrPayloadBuilder qrPayloadBuilder,
            ISimulatedReceiptQrCodeImageGenerator qrCodeImageGenerator,
            IFiscalIssuanceModeResolver fiscalIssuanceModeResolver,
            ISimulatedFiscalAuditService simulatedFiscalAuditService)
        {
            this.applicationDbContext = applicationDbContext ?? throw new ArgumentNullException(nameof(applicationDbContext));
            this.identifierAllocator = identifierAllocator ?? throw new ArgumentNullException(nameof(identifierAllocator));
            this.checksumService = checksumService ?? throw new ArgumentNullException(nameof(checksumService));
            this.qrPayloadBuilder = qrPayloadBuilder ?? throw new ArgumentNullException(nameof(qrPayloadBuilder));
            this.qrCodeImageGenerator = qrCodeImageGenerator ?? throw new ArgumentNullException(nameof(qrCodeImageGenerator));
            this.fiscalIssuanceModeResolver = fiscalIssuanceModeResolver ?? throw new ArgumentNullException(nameof(fiscalIssuanceModeResolver));
            this.simulatedFiscalAuditService = simulatedFiscalAuditService ?? throw new ArgumentNullException(nameof(simulatedFiscalAuditService));
        }

        /// <inheritdoc />
        public SimulatedFiscalDocument IssueOrGet(Tenant tenant, Donation donation)
        {
            if (this.fiscalIssuanceModeResolver.Resolve() != FiscalIssuanceMode.Simulation)
            {
                throw new InvalidOperationException("Simulated receipt issuance is available only in the Development simulation mode.");
            }

            ValidateRequestedEntities(tenant, donation);
            Donation persistedDonation = LoadPaidDonation(donation.Id);
            SimulatedFiscalDocument existingDocument = FindExistingDocument(persistedDonation.Id);
            if (existingDocument != null)
            {
                return existingDocument;
            }

            SimulatedFiscalScope scope = ResolveScope(tenant, persistedDonation);
            for (int attempt = 0; attempt < MaximumReservationAttempts; attempt++)
            {
                SimulatedFiscalDocument issuedDocument = TryIssue(tenant, persistedDonation, scope);
                if (issuedDocument != null)
                {
                    return issuedDocument;
                }
            }

            throw new InvalidOperationException("The simulated receipt could not be reserved after a concurrency conflict.");
        }

        private SimulatedFiscalDocument TryIssue(Tenant tenant, Donation donation, SimulatedFiscalScope scope)
        {
            IDbContextTransaction transaction = this.applicationDbContext.Database.IsRelational()
                ? this.applicationDbContext.Database.BeginTransaction(IsolationLevel.Serializable)
                : null;
            try
            {
                SimulatedFiscalDocument existingDocument = FindExistingDocument(donation.Id);
                if (existingDocument != null)
                {
                    transaction?.Commit();
                    return existingDocument;
                }

                SimulatedFiscalIdentifierAllocation allocation = this.identifierAllocator.Allocate(scope);
                DateTimeOffset issuedAtUtc = DateTimeOffset.UtcNow;
                var document = new SimulatedFiscalDocument
                {
                    TenantId = tenant.Id,
                    DonationId = donation.Id,
                    PaymentId = donation.ConfirmedPayment.Id,
                    ScopeKey = scope.ScopeKey,
                    InvoicingStrategy = scope.InvoicingStrategy,
                    FoodBankId = scope.FoodBankId,
                    Sequence = allocation.Sequence,
                    SyntheticIdentifier = allocation.SyntheticIdentifier,
                    ArtifactReference = BuildArtifactReference(tenant.Id, scope.ScopeKey, allocation.SyntheticIdentifier),
                    OriginEnvironment = DevelopmentEnvironment,
                    IssuedAtUtc = issuedAtUtc,
                    CreatedAtUtc = issuedAtUtc,
                };
                document.SimulationQrPayload = this.qrPayloadBuilder.Build(document);
                document.SimulationQrPng = this.qrCodeImageGenerator.GeneratePng(document.SimulationQrPayload);
                document.LocalChecksum = this.checksumService.Calculate(document);
                this.applicationDbContext.SimulatedFiscalDocuments.Add(document);
                this.simulatedFiscalAuditService.Append(
                    tenant,
                    document,
                    new SimulatedFiscalAuditEventRequest
                    {
                        Action = "SimulationReceiptIssued",
                        Result = "Succeeded",
                        Reason = DevelopmentEnvironment + "Simulation",
                    });
                this.applicationDbContext.SaveChanges();
                transaction?.Commit();
                return document;
            }
            catch (DbUpdateConcurrencyException)
            {
                transaction?.Rollback();
                this.applicationDbContext.ChangeTracker.Clear();
                return FindExistingDocument(donation.Id);
            }
            catch (DbUpdateException)
            {
                transaction?.Rollback();
                this.applicationDbContext.ChangeTracker.Clear();
                return FindExistingDocument(donation.Id);
            }
            catch
            {
                transaction?.Rollback();
                throw;
            }
            finally
            {
                transaction?.Dispose();
            }
        }

        private static string BuildArtifactReference(int tenantId, string scopeKey, string syntheticIdentifier)
        {
            return $"simulation/development/tenant-{tenantId}/{scopeKey}/{syntheticIdentifier}.pdf";
        }

        private static SimulatedFiscalScope ResolveScope(Tenant tenant, Donation donation)
        {
            if (tenant.InvoicingStrategy == InvoicingStrategy.SingleInvoiceTable)
            {
                return new SimulatedFiscalScope
                {
                    ScopeKey = $"tenant-{tenant.Id}",
                    InvoicingStrategy = InvoicingStrategy.SingleInvoiceTable,
                    FoodBankId = null,
                };
            }

            if (tenant.InvoicingStrategy == InvoicingStrategy.MultipleTablesPerFoodBank &&
                donation.FoodBank != null && donation.FoodBank.Id > 0)
            {
                return new SimulatedFiscalScope
                {
                    ScopeKey = $"tenant-{tenant.Id}-foodbank-{donation.FoodBank.Id}",
                    InvoicingStrategy = InvoicingStrategy.MultipleTablesPerFoodBank,
                    FoodBankId = donation.FoodBank.Id,
                };
            }

            throw new InvalidOperationException("A simulated receipt requires a food bank for MultipleTablesPerFoodBank.");
        }

        private SimulatedFiscalDocument FindExistingDocument(int donationId)
        {
            return this.applicationDbContext.SimulatedFiscalDocuments
                .SingleOrDefault(item => item.DonationId == donationId);
        }

        private Donation LoadPaidDonation(int donationId)
        {
            Donation donation = this.applicationDbContext.Donations
                .Include(item => item.ConfirmedPayment)
                .Include(item => item.FoodBank)
                .SingleOrDefault(item => item.Id == donationId);
            if (donation == null || donation.PaymentStatus != PaymentStatus.Payed || donation.ConfirmedPayment == null ||
                donation.ConfirmedPayment.Id <= 0)
            {
                throw new InvalidOperationException("A simulated receipt requires a paid donation with a confirmed payment.");
            }

            return donation;
        }

        private static void ValidateRequestedEntities(Tenant tenant, Donation donation)
        {
            if (tenant == null || tenant.Id <= 0)
            {
                throw new InvalidOperationException("A simulated receipt requires a trusted tenant.");
            }

            if (donation == null || donation.Id <= 0)
            {
                throw new InvalidOperationException("A simulated receipt requires a persisted donation.");
            }
        }
    }
}
