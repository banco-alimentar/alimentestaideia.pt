// -----------------------------------------------------------------------
// <copyright file="SimulatedFiscalIdentifierAllocator.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System;
    using System.Linq;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model.Strategy;

    /// <summary>
    /// Allocates development-only receipt identifiers from a counter separate from AT series.
    /// </summary>
    public class SimulatedFiscalIdentifierAllocator : ISimulatedFiscalIdentifierAllocator
    {
        private readonly ApplicationDbContext applicationDbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="SimulatedFiscalIdentifierAllocator"/> class.
        /// </summary>
        /// <param name="applicationDbContext">The current tenant database.</param>
        public SimulatedFiscalIdentifierAllocator(ApplicationDbContext applicationDbContext)
        {
            this.applicationDbContext = applicationDbContext ?? throw new ArgumentNullException(nameof(applicationDbContext));
        }

        /// <inheritdoc />
        public SimulatedFiscalIdentifierAllocation Allocate(SimulatedFiscalScope scope)
        {
            ValidateScope(scope);

            SimulatedFiscalSequence counter = this.applicationDbContext.SimulatedFiscalSequences
                .SingleOrDefault(item => item.ScopeKey == scope.ScopeKey);
            if (counter == null)
            {
                counter = new SimulatedFiscalSequence
                {
                    ScopeKey = scope.ScopeKey,
                    InvoicingStrategy = scope.InvoicingStrategy,
                    FoodBankId = scope.FoodBankId,
                    NextSequence = 2,
                };
                this.applicationDbContext.SimulatedFiscalSequences.Add(counter);
                return CreateAllocation(scope.ScopeKey, 1);
            }

            if (counter.InvoicingStrategy != scope.InvoicingStrategy || counter.FoodBankId != scope.FoodBankId ||
                counter.NextSequence <= 0)
            {
                throw new InvalidOperationException("The simulated receipt counter does not match its trusted scope.");
            }

            int sequence = counter.NextSequence;
            checked
            {
                counter.NextSequence++;
            }

            return CreateAllocation(scope.ScopeKey, sequence);
        }

        private static SimulatedFiscalIdentifierAllocation CreateAllocation(string scopeKey, int sequence)
        {
            string normalizedScope = scopeKey.ToUpperInvariant();
            return new SimulatedFiscalIdentifierAllocation
            {
                Sequence = sequence,
                SyntheticIdentifier = $"SIM-{normalizedScope}-{sequence}",
            };
        }

        private static void ValidateScope(SimulatedFiscalScope scope)
        {
            if (scope == null || string.IsNullOrWhiteSpace(scope.ScopeKey))
            {
                throw new ArgumentException("A simulated receipt scope is required.", nameof(scope));
            }

            bool isSingleInvoiceTable = scope.InvoicingStrategy == InvoicingStrategy.SingleInvoiceTable;
            bool isMultipleTablesPerFoodBank = scope.InvoicingStrategy == InvoicingStrategy.MultipleTablesPerFoodBank;
            if ((!isSingleInvoiceTable && !isMultipleTablesPerFoodBank) ||
                (isSingleInvoiceTable && scope.FoodBankId.HasValue) ||
                (isMultipleTablesPerFoodBank && (!scope.FoodBankId.HasValue || scope.FoodBankId.Value <= 0)))
            {
                throw new InvalidOperationException("The simulated receipt scope does not match the invoicing strategy.");
            }
        }
    }
}
