// -----------------------------------------------------------------------
// <copyright file="FiscalDocumentAdjustmentP1TddTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.Tests
{
    using System;
    using System.Linq;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using Microsoft.AspNetCore.Http;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata;
    using Xunit;

    /// <summary>
    /// Defines P1 integrity contracts for fiscal-document cancellation and correction.
    /// </summary>
    public class FiscalDocumentAdjustmentP1TddTests
    {
        private const string CancellationRequestTypeName = "BancoAlimentar.AlimentaEstaIdeia.Repository.FiscalDocumentCancellationRequest";
        private const string CancellationServiceTypeName = "BancoAlimentar.AlimentaEstaIdeia.Repository.FiscalDocumentCancellationService";
        private const string CorrectionRequestTypeName = "BancoAlimentar.AlimentaEstaIdeia.Repository.FiscalDocumentCorrectionRequest";
        private const string CorrectionServiceTypeName = "BancoAlimentar.AlimentaEstaIdeia.Repository.FiscalDocumentCorrectionService";

        /// <summary>
        /// Two simultaneous application instances must not create two corrections for the same
        /// original. This is a relational database invariant, expressed as a unique filtered
        /// index rather than only an in-memory pre-check that races under concurrency.
        /// </summary>
        [Fact]
        public void FiscalDocumentModel_EnforcesOneCorrectionPerOriginalWithUniqueFilteredIndex()
        {
            using var context = CreateContext();
            IEntityType fiscalDocumentEntity = context.Model.FindEntityType(typeof(FiscalDocument));
            IIndex correctionIndex = fiscalDocumentEntity.GetIndexes().SingleOrDefault(index =>
                index.IsUnique &&
                index.Properties.Select(property => property.Name)
                    .SequenceEqual(new[] { nameof(FiscalDocument.OriginalFiscalDocumentId) }) &&
                index.GetFilter()?.Contains("Kind", StringComparison.Ordinal) == true &&
                index.GetFilter()?.Contains("1", StringComparison.Ordinal) == true);

            Assert.NotNull(correctionIndex);
        }

        /// <summary>
        /// A correction request has no actor identifier. The service obtains the actor from the
        /// authenticated HTTP context and records that trusted identity in an append-only fiscal
        /// event; a caller cannot attribute a correction to somebody else.
        /// </summary>
        [Fact]
        public void CorrectionContract_UsesTrustedHttpActorRatherThanCallerSuppliedActor()
        {
            Type requestType = GetRequiredRepositoryType(CorrectionRequestTypeName);
            Type serviceType = GetRequiredRepositoryType(CorrectionServiceTypeName);

            Assert.Null(requestType.GetProperty("ActorId"));
            Assert.Null(requestType.GetProperty("UserId"));
            Assert.Contains(
                serviceType.GetConstructors().SelectMany(constructor => constructor.GetParameters()),
                parameter => parameter.ParameterType == typeof(IHttpContextAccessor));
            Assert.Contains(
                typeof(FiscalDocumentEvent).GetProperties(),
                property => property.Name == nameof(FiscalDocumentEvent.ActorId) && property.PropertyType == typeof(string));
        }

        /// <summary>
        /// Cancellation must independently resolve the persisted original's fiscal scope within
        /// the trusted tenant. A caller-provided or stale issuer/series/food-bank scope is not a
        /// cancellation authority, so the cancellation service requires the same resolver used
        /// by issuance/reservation and the cancellation request cannot carry scope fields.
        /// </summary>
        [Fact]
        public void CancellationContract_ValidatesCompleteInternallyResolvedFiscalScope()
        {
            Type requestType = GetRequiredRepositoryType(CancellationRequestTypeName);
            Type serviceType = GetRequiredRepositoryType(CancellationServiceTypeName);

            Assert.Null(requestType.GetProperty("Tenant"));
            Assert.Null(requestType.GetProperty("FiscalScope"));
            Assert.Null(requestType.GetProperty("FiscalIssuerProfileId"));
            Assert.Null(requestType.GetProperty("SeriesId"));
            Assert.Null(requestType.GetProperty("FoodBankId"));
            Assert.NotNull(serviceType.GetConstructor(new[]
            {
                typeof(ApplicationDbContext),
                typeof(IHttpContextAccessor),
                typeof(IFiscalScopeResolver),
            }));
        }

        private static ApplicationDbContext CreateContext()
        {
            DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var context = new ApplicationDbContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        private static Type GetRequiredRepositoryType(string typeName)
        {
            return Assert.IsAssignableFrom<Type>(typeof(IFiscalScopeResolver).Assembly.GetType(typeName));
        }
    }
}
