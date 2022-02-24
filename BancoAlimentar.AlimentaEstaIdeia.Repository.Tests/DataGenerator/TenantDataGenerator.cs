// -----------------------------------------------------------------------
// <copyright file="TenantDataGenerator.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.Tests.DataGenerator
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;

    /// <summary>
    /// Generate tenant data for the Repository tests.
    /// </summary>
    public class TenantDataGenerator : IEnumerable<object[]>
    {
        /// <inheritdoc />
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[]
            {
                new Tenant()
                {
                    Created = DateTime.Now,
                    DomainIdentifier = "localhost",
                    Id = 1,
                    Name = "localhost",
                    InvoicingStrategy = Sas.Model.Strategy.InvoicingStrategy.SingleInvoiceTable,
                    PaymentStrategy = Sas.Model.Strategy.PaymentStrategy.SharedPaymentProcessor,
                    PublicId = Guid.NewGuid(),
                },
            };
            yield return new object[]
            {
                new Tenant()
                {
                    Created = DateTime.Now,
                    DomainIdentifier = "localhost",
                    Id = 2,
                    Name = "localhost",
                    InvoicingStrategy = Sas.Model.Strategy.InvoicingStrategy.MultipleTablesPerFoodBank,
                    PaymentStrategy = Sas.Model.Strategy.PaymentStrategy.SharedPaymentProcessor,
                    PublicId = Guid.NewGuid(),
                },
            };
            yield return new object[]
            {
                new Tenant()
                {
                    Created = DateTime.Now,
                    DomainIdentifier = "localhost",
                    Id = 3,
                    Name = "localhost",
                    InvoicingStrategy = Sas.Model.Strategy.InvoicingStrategy.SingleInvoiceTable,
                    PaymentStrategy = Sas.Model.Strategy.PaymentStrategy.IndividualPaymentProcessorPerFoodBank,
                    PublicId = Guid.NewGuid(),
                },
            };
            yield return new object[]
            {
                new Tenant()
                {
                    Created = DateTime.Now,
                    DomainIdentifier = "localhost",
                    Id = 4,
                    Name = "localhost",
                    InvoicingStrategy = Sas.Model.Strategy.InvoicingStrategy.MultipleTablesPerFoodBank,
                    PaymentStrategy = Sas.Model.Strategy.PaymentStrategy.IndividualPaymentProcessorPerFoodBank,
                    PublicId = Guid.NewGuid(),
                },
            };
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}
