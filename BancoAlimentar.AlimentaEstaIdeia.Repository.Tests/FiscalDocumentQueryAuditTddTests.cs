// -----------------------------------------------------------------------
// <copyright file="FiscalDocumentQueryAuditTddTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using Microsoft.AspNetCore.Http;
    using Xunit;

    /// <summary>
    /// Defines TDD contracts for tenant-safe fiscal-document consultation and operational audit evidence.
    /// </summary>
    public class FiscalDocumentQueryAuditTddTests
    {
        private const string AuditRequestTypeName = "BancoAlimentar.AlimentaEstaIdeia.Repository.FiscalAuditEventRequest";
        private const string AuditServiceInterfaceTypeName = "BancoAlimentar.AlimentaEstaIdeia.Repository.IFiscalAuditService";
        private const string AuditServiceTypeName = "BancoAlimentar.AlimentaEstaIdeia.Repository.FiscalAuditService";
        private const string QueryRequestTypeName = "BancoAlimentar.AlimentaEstaIdeia.Repository.FiscalDocumentQueryRequest";
        private const string QueryResultTypeName = "BancoAlimentar.AlimentaEstaIdeia.Repository.FiscalDocumentQueryResult";
        private const string QueryServiceInterfaceTypeName = "BancoAlimentar.AlimentaEstaIdeia.Repository.IFiscalDocumentQueryService";
        private const string QueryServiceTypeName = "BancoAlimentar.AlimentaEstaIdeia.Repository.FiscalDocumentQueryService";

        /// <summary>
        /// A fiscal-document query accepts only user-selectable filters. The current tenant is
        /// resolved from trusted context, and the selected issuer is verified inside that tenant.
        /// </summary>
        [Fact]
        public void FiscalDocumentQueryService_ExposesTrustedTenantAndIssuerBoundFilterContract()
        {
            Type requestType = GetRequiredRepositoryType(QueryRequestTypeName);
            Type resultType = GetRequiredRepositoryType(QueryResultTypeName);
            Type serviceInterfaceType = GetRequiredRepositoryType(QueryServiceInterfaceTypeName);
            Type serviceType = GetRequiredRepositoryType(QueryServiceTypeName);

            AssertRequiredProperties(
                requestType,
                "FiscalIssuerProfileId",
                "FiscalDocumentSeriesId",
                "DocumentNumber",
                "Atcud",
                "DonationId",
                "StartDate",
                "EndDate",
                "Status");
            Assert.Equal(typeof(int), requestType.GetProperty("FiscalIssuerProfileId")?.PropertyType);
            Assert.Equal(typeof(int?), requestType.GetProperty("FiscalDocumentSeriesId")?.PropertyType);
            Assert.Equal(typeof(int?), requestType.GetProperty("DonationId")?.PropertyType);
            Assert.Equal(typeof(DateOnly?), requestType.GetProperty("StartDate")?.PropertyType);
            Assert.Equal(typeof(DateOnly?), requestType.GetProperty("EndDate")?.PropertyType);
            Assert.Equal(typeof(FiscalDocumentStatus?), requestType.GetProperty("Status")?.PropertyType);
            Assert.Null(requestType.GetProperty("Tenant"));
            Assert.Null(requestType.GetProperty("TenantId"));
            Assert.Null(requestType.GetProperty("FoodBankId"));

            AssertRequiredProperties(
                resultType,
                "FiscalDocumentId",
                "FiscalIssuerProfileId",
                "FiscalDocumentSeriesId",
                "DocumentNumber",
                "Atcud",
                "DonationId",
                "InvoiceDate",
                "Status");
            Assert.Null(resultType.GetProperty("DonorName"));
            Assert.Null(resultType.GetProperty("DonorTaxRegistrationNumber"));

            Assert.Single(
                serviceInterfaceType.GetMethods(),
                method => method.Name == "Query" &&
                    method.ReturnType == typeof(IReadOnlyList<>).MakeGenericType(resultType) &&
                    method.GetParameters().Select(parameter => parameter.ParameterType)
                        .SequenceEqual(new[] { requestType }));
            Assert.True(serviceInterfaceType.IsAssignableFrom(serviceType));
            Assert.NotNull(serviceType.GetConstructor(new[] { typeof(ApplicationDbContext), typeof(IHttpContextAccessor) }));
        }

        /// <summary>
        /// Audit recording receives event identifiers only. The service derives tenant and actor
        /// from trusted context and never stores signing material, QR payloads, or personal data.
        /// </summary>
        [Fact]
        public void FiscalAuditService_ExposesTrustedAppendOnlyOperationalEventContract()
        {
            Type requestType = GetRequiredRepositoryType(AuditRequestTypeName);
            Type serviceInterfaceType = GetRequiredRepositoryType(AuditServiceInterfaceTypeName);
            Type serviceType = GetRequiredRepositoryType(AuditServiceTypeName);

            AssertRequiredProperties(
                requestType,
                "Action",
                "Result",
                "FiscalIssuerProfileId",
                "FiscalDocumentSeriesId",
                "FiscalDocumentId",
                "CorrelationId");
            Assert.Equal(typeof(string), requestType.GetProperty("Action")?.PropertyType);
            Assert.Equal(typeof(string), requestType.GetProperty("Result")?.PropertyType);
            Assert.Equal(typeof(int?), requestType.GetProperty("FiscalIssuerProfileId")?.PropertyType);
            Assert.Equal(typeof(int?), requestType.GetProperty("FiscalDocumentSeriesId")?.PropertyType);
            Assert.Equal(typeof(int?), requestType.GetProperty("FiscalDocumentId")?.PropertyType);
            Assert.Null(requestType.GetProperty("ActorId"));
            Assert.Null(requestType.GetProperty("Tenant"));
            Assert.Null(requestType.GetProperty("TenantId"));
            Assert.Null(requestType.GetProperty("PrivateKey"));
            Assert.Null(requestType.GetProperty("SigningKey"));
            Assert.Null(requestType.GetProperty("QrPayload"));
            Assert.Null(requestType.GetProperty("Payload"));
            Assert.Null(requestType.GetProperty("DonorName"));

            Assert.Single(
                serviceInterfaceType.GetMethods(),
                method => method.Name == "Record" &&
                    method.ReturnType == typeof(FiscalAuditEvent) &&
                    method.GetParameters().Select(parameter => parameter.ParameterType)
                        .SequenceEqual(new[] { requestType }));
            Assert.True(serviceInterfaceType.IsAssignableFrom(serviceType));
            Assert.NotNull(serviceType.GetConstructor(new[] { typeof(ApplicationDbContext), typeof(IHttpContextAccessor) }));
            Assert.DoesNotContain(
                serviceInterfaceType.GetMethods(),
                method => method.Name.Contains("Update", StringComparison.OrdinalIgnoreCase) ||
                    method.Name.Contains("Delete", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// The persisted audit record is a compact operational event. It contains no free-form
        /// payload column that could leak private key material or a complete donor snapshot.
        /// </summary>
        [Fact]
        public void FiscalAuditEvent_PersistsOnlyOperationalIdentifiersAndTrustedEvidence()
        {
            AssertRequiredProperties(
                typeof(FiscalAuditEvent),
                "TenantId",
                "FoodBankId",
                "FiscalIssuerProfileId",
                "FiscalDocumentSeriesId",
                "FiscalDocumentId",
                "Action",
                "Result",
                "ActorId",
                "OccurredAtUtc",
                "CorrelationId");

            foreach (string forbiddenProperty in new[]
            {
                "Payload",
                "Details",
                "PrivateKey",
                "SigningKey",
                "QrPayload",
                "DonorName",
                "DonorTaxRegistrationNumber",
            })
            {
                Assert.Null(typeof(FiscalAuditEvent).GetProperty(forbiddenProperty));
            }
        }

        private static void AssertRequiredProperties(Type type, params string[] propertyNames)
        {
            foreach (string propertyName in propertyNames)
            {
                Assert.NotNull(type.GetProperty(propertyName));
            }
        }

        private static Type GetRequiredRepositoryType(string typeName)
        {
            return Assert.IsAssignableFrom<Type>(typeof(IFiscalScopeResolver).Assembly.GetType(typeName));
        }
    }
}
