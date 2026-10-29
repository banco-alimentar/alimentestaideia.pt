// -----------------------------------------------------------------------
// <copyright file="FiscalAdministrationTddTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.Tests
{
    using System;
    using System.Linq;
    using System.Reflection;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using Microsoft.AspNetCore.Http;
    using Xunit;

    /// <summary>
    /// Defines TDD contracts for tenant-bound administration of fiscal issuers and series.
    /// </summary>
    public class FiscalAdministrationTddTests
    {
        private const string ProfileRequestTypeName = "BancoAlimentar.AlimentaEstaIdeia.Repository.FiscalIssuerProfileAdministrationRequest";
        private const string SeriesRequestTypeName = "BancoAlimentar.AlimentaEstaIdeia.Repository.FiscalDocumentSeriesAdministrationRequest";
        private const string ServiceInterfaceTypeName = "BancoAlimentar.AlimentaEstaIdeia.Repository.IFiscalAdministrationService";
        private const string ServiceTypeName = "BancoAlimentar.AlimentaEstaIdeia.Repository.FiscalAdministrationService";

        /// <summary>
        /// Administration is anchored in the trusted current tenant. Profile inputs include a
        /// legal fiscal snapshot and non-secret certificate/key references; they never include
        /// secret signing material.
        /// </summary>
        [Fact]
        public void FiscalAdministrationService_ExposesTenantBoundIssuerProfileContract()
        {
            Type profileRequestType = GetRequiredRepositoryType(ProfileRequestTypeName);
            Type serviceInterfaceType = GetRequiredRepositoryType(ServiceInterfaceTypeName);
            Type serviceType = GetRequiredRepositoryType(ServiceTypeName);

            AssertRequiredProperties(
                profileRequestType,
                "Id",
                "ScopeKind",
                "FoodBankId",
                "LegalName",
                "LegalAddress",
                "TaxRegistrationNumber",
                "SaftDocumentType",
                "TaxRule",
                "SoftwareCertificateNumber",
                "SigningKeyIdentifier",
                "SigningKeyVersion",
                "ValidFromUtc",
                "ValidToUtc",
                "IsActive");
            Assert.Null(profileRequestType.GetProperty("SigningKey"));
            Assert.Null(profileRequestType.GetProperty("PrivateKey"));
            AssertMethod(serviceInterfaceType, "CreateIssuerProfile", profileRequestType, typeof(FiscalIssuerProfile));
            AssertMethod(serviceInterfaceType, "UpdateIssuerProfile", profileRequestType, typeof(FiscalIssuerProfile));
            Assert.True(serviceInterfaceType.IsAssignableFrom(serviceType));
            Assert.NotNull(serviceType.GetConstructor(new[] { typeof(ApplicationDbContext), typeof(IHttpContextAccessor) }));
        }

        /// <summary>
        /// An active series is legal only after AT communication evidence, a certificate-backed
        /// processing method and a validation code have been recorded. Closing is a lifecycle
        /// transition, not deletion of the historic series.
        /// </summary>
        [Fact]
        public void FiscalAdministrationService_RequiresCommunicatedSeriesAndSupportsClosing()
        {
            Type seriesRequestType = GetRequiredRepositoryType(SeriesRequestTypeName);
            Type serviceInterfaceType = GetRequiredRepositoryType(ServiceInterfaceTypeName);

            AssertRequiredProperties(
                seriesRequestType,
                "Id",
                "FiscalIssuerProfileId",
                "SeriesCode",
                "SaftDocumentType",
                "ProcessingMethod",
                "FirstSequence",
                "NextSequence",
                "LastSequence",
                "AtcudValidationCode",
                "AtCommunicationReference",
                "StartsAtUtc",
                "Status");
            AssertMethod(serviceInterfaceType, "ConfigureSeries", seriesRequestType, typeof(FiscalDocumentSeries));
            Assert.Single(
                serviceInterfaceType.GetMethods(),
                method => method.Name == "CloseSeries" &&
                    method.ReturnType == typeof(FiscalDocumentSeries) &&
                    method.GetParameters().Select(parameter => parameter.ParameterType)
                        .SequenceEqual(new[] { typeof(int) }));
        }

        /// <summary>
        /// Issued evidence fixes its issuer and series configuration. The administration service
        /// therefore has to reject profile/series edits and deletion-like replacement once an
        /// issued document references the series; only an explicit close remains permitted.
        /// </summary>
        [Fact]
        public void FiscalAdministrationService_PreventsMutatingSeriesUsedByIssuedFiscalDocument()
        {
            Type serviceType = GetRequiredRepositoryType(ServiceTypeName);
            Type seriesRequestType = GetRequiredRepositoryType(SeriesRequestTypeName);

            Assert.NotNull(serviceType.GetMethod("ConfigureSeries", new[] { seriesRequestType }));
            Assert.NotNull(serviceType.GetMethod("CloseSeries", new[] { typeof(int) }));
            Assert.DoesNotContain(
                serviceType.GetMethods(BindingFlags.Instance | BindingFlags.Public),
                method => method.Name.Contains("Delete", StringComparison.OrdinalIgnoreCase) &&
                    method.GetParameters().Any(parameter => parameter.ParameterType == typeof(int)));
        }

        /// <summary>
        /// The same trusted tenant boundary governs SAF-T selection. An administrator can select
        /// one issuer and an inclusive period, but cannot substitute a tenant in the export UI.
        /// </summary>
        [Fact]
        public void FiscalAdministrationService_ExposesIssuerAndPeriodSaftSelectionContract()
        {
            Type serviceInterfaceType = GetRequiredRepositoryType(ServiceInterfaceTypeName);

            Assert.Single(
                serviceInterfaceType.GetMethods(),
                method => method.Name == "ExportSaft" &&
                    method.ReturnType == typeof(string) &&
                    method.GetParameters().Select(parameter => parameter.ParameterType)
                        .SequenceEqual(new[] { typeof(int), typeof(DateOnly), typeof(DateOnly) }));
        }

        private static void AssertMethod(Type interfaceType, string name, Type parameterType, Type returnType)
        {
            Assert.Single(
                interfaceType.GetMethods(),
                method => method.Name == name &&
                    method.ReturnType == returnType &&
                    method.GetParameters().Select(parameter => parameter.ParameterType)
                        .SequenceEqual(new[] { parameterType }));
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
