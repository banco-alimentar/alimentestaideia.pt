// -----------------------------------------------------------------------
// <copyright file="SaftExportTddTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.ExceptionServices;
    using System.Xml.Linq;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model.Strategy;
    using Microsoft.EntityFrameworkCore;
    using Xunit;

    /// <summary>
    /// Defines TDD contracts for a Portuguese SAF-T (PT) sales-invoice export.
    /// </summary>
    public class SaftExportTddTests
    {
        private const string ExportRequestTypeName = "BancoAlimentar.AlimentaEstaIdeia.Repository.SaftExportRequest";
        private const string ExportServiceInterfaceTypeName = "BancoAlimentar.AlimentaEstaIdeia.Repository.ISaftExportService";
        private const string ExportServiceTypeName = "BancoAlimentar.AlimentaEstaIdeia.Repository.SaftExportService";
        private const string SaftNamespace = "urn:OECD:StandardAuditFile-Tax:PT_1.04_01";

        /// <summary>
        /// The trusted tenant and issuer are mandatory export boundaries. An export service is
        /// deliberately given only fiscal persistence, so legacy invoice and live donation,
        /// identity, food-bank or configuration data cannot become certified fiscal evidence.
        /// </summary>
        [Fact]
        public void SaftExportService_ExposesTrustedFiscalSnapshotOnlyContract()
        {
            Type requestType = GetRequiredRepositoryType(ExportRequestTypeName);
            Type serviceInterfaceType = GetRequiredRepositoryType(ExportServiceInterfaceTypeName);
            Type serviceType = GetRequiredRepositoryType(ExportServiceTypeName);

            AssertRequiredProperties(
                requestType,
                "Tenant",
                "FiscalIssuerProfileId",
                "StartDate",
                "EndDate");
            Assert.Equal(typeof(Tenant), requestType.GetProperty("Tenant")?.PropertyType);
            Assert.Equal(typeof(int), requestType.GetProperty("FiscalIssuerProfileId")?.PropertyType);
            Assert.Equal(typeof(DateOnly), requestType.GetProperty("StartDate")?.PropertyType);
            Assert.Equal(typeof(DateOnly), requestType.GetProperty("EndDate")?.PropertyType);

            MethodInfo export = Assert.Single(
                serviceInterfaceType.GetMethods(),
                method => method.Name == "Export" &&
                    method.ReturnType == typeof(string) &&
                    method.GetParameters().Select(parameter => parameter.ParameterType)
                        .SequenceEqual(new[] { requestType }));

            Assert.True(serviceInterfaceType.IsAssignableFrom(serviceType));
            Assert.NotNull(serviceType.GetConstructor(new[] { typeof(ApplicationDbContext) }));
            Assert.DoesNotContain(
                serviceType.GetConstructors().SelectMany(constructor => constructor.GetParameters()),
                parameter => IsLiveOrLegacyEvidenceType(parameter.ParameterType));
            Assert.DoesNotContain(
                serviceType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public),
                field => IsLiveOrLegacyEvidenceType(field.FieldType));
            Assert.Equal("Export", export.Name);
        }

        /// <summary>
        /// Official version source: AT SAF-T (PT) version 1.04_01 page,
        /// https://info.portaldasfinancas.gov.pt/pt/apoio_ao_contribuinte/Negocios/Faturacao/SAF_T_PT/SAF_T_PT_Versao_PT/Paginas/default.aspx.
        /// The statutory version and namespace are asserted here; this repository has no local
        /// official XSD to validate against.
        /// </summary>
        [Fact]
        public void Export_WritesWellFormedVersionedDeterministicSalesInvoicesFromFiscalSnapshots()
        {
            using var context = CreateContext();
            Tenant tenant = CreateTenant();
            PersistFiscalDocuments(context);
            object service = CreateExportService(context);
            object request = CreateExportRequest(tenant, fiscalIssuerProfileId: 10, new DateOnly(2026, 9, 18), new DateOnly(2026, 9, 20));

            string firstExport = Export(service, request);
            string secondExport = Export(service, request);
            XDocument document = XDocument.Parse(firstExport, LoadOptions.PreserveWhitespace);
            XNamespace saft = SaftNamespace;
            XElement salesInvoices = document.Root?.Element(saft + "SourceDocuments")?.Element(saft + "SalesInvoices");
            List<XElement> invoices = salesInvoices?.Elements(saft + "Invoice").ToList();

            Assert.Equal(firstExport, secondExport);
            Assert.NotNull(document.Root);
            Assert.Equal(saft + "AuditFile", document.Root.Name);
            Assert.Equal("1.04_01", document.Root.Element(saft + "Header")?.Element(saft + "AuditFileVersion")?.Value);
            Assert.NotNull(invoices);
            Assert.Collection(
                invoices,
                invoice => Assert.Equal("RC 2026/42", invoice.Element(saft + "InvoiceNo")?.Value),
                invoice => Assert.Equal("RC 2026/43", invoice.Element(saft + "InvoiceNo")?.Value),
                invoice => Assert.Equal("NC 2026/44", invoice.Element(saft + "InvoiceNo")?.Value));

            XElement original = invoices[0];
            Assert.Equal("VALIDATION-42", original.Element(saft + "ATCUD")?.Value);
            Assert.Equal("signature-42", original.Element(saft + "Hash")?.Value);
            Assert.Equal("3", original.Element(saft + "HashControl")?.Value);
            Assert.Equal("2026-09-18", original.Element(saft + "InvoiceDate")?.Value);
            Assert.Equal("25.50", original.Element(saft + "DocumentTotals")?.Element(saft + "NetTotal")?.Value);
            Assert.Equal("5.87", original.Element(saft + "DocumentTotals")?.Element(saft + "TaxPayable")?.Value);
            Assert.Equal("31.37", original.Element(saft + "DocumentTotals")?.Element(saft + "GrossTotal")?.Value);
            Assert.Collection(
                original.Elements(saft + "Line"),
                line => Assert.Equal("1", line.Element(saft + "LineNumber")?.Value),
                line => Assert.Equal("2", line.Element(saft + "LineNumber")?.Value));
        }

        /// <summary>
        /// A cancelled document remains in the file with the statutory cancelled status. A
        /// correction is separately represented and names the persisted original document,
        /// rather than reconstructing either record from a current donation or user.
        /// </summary>
        [Fact]
        public void Export_RepresentsCancelledAndCorrectedFiscalDocumentsUsingPersistedState()
        {
            using var context = CreateContext();
            Tenant tenant = CreateTenant();
            PersistFiscalDocuments(context);
            object service = CreateExportService(context);
            string xml = Export(
                service,
                CreateExportRequest(tenant, fiscalIssuerProfileId: 10, new DateOnly(2026, 9, 18), new DateOnly(2026, 9, 20)));
            XNamespace saft = SaftNamespace;
            XElement salesInvoices = XDocument.Parse(xml).Root?.Element(saft + "SourceDocuments")?.Element(saft + "SalesInvoices");
            XElement cancelled = salesInvoices?.Elements(saft + "Invoice")
                .Single(invoice => invoice.Element(saft + "InvoiceNo")?.Value == "RC 2026/43");
            XElement correction = salesInvoices?.Elements(saft + "Invoice")
                .Single(invoice => invoice.Element(saft + "InvoiceNo")?.Value == "NC 2026/44");

            Assert.Equal("A", cancelled?.Element(saft + "InvoiceStatus")?.Value);
            Assert.Equal("RC 2026/42", correction?.Element(saft + "OrderReferences")?.Element(saft + "OriginatingON")?.Value);
            Assert.Equal("signature-44", correction?.Element(saft + "Hash")?.Value);
            Assert.Equal("3", correction?.Element(saft + "HashControl")?.Value);
        }

        /// <summary>
        /// Export boundaries are the request's trusted tenant, issuer id and invoice-date range.
        /// No legacy Invoice may enter the SAF-T fiscal source, and this fixture deliberately
        /// persists no Donation, WebUser, FoodBank or live issuer profile for the included rows.
        /// </summary>
        [Fact]
        public void Export_ExcludesOtherIssuerAndLegacyInvoiceWithoutLookingUpLiveEntities()
        {
            using var context = CreateContext();
            Tenant tenant = CreateTenant();
            PersistFiscalDocuments(context);
            context.Invoices.Add(new Invoice
            {
                Id = 900,
                Number = "LEGACY-INVOICE-MUST-NOT-BE-CERTIFIED",
                Created = new DateTime(2026, 9, 19),
                Year = 2026,
                Sequence = 900,
            });
            context.SaveChanges();
            object service = CreateExportService(context);

            string xml = Export(
                service,
                CreateExportRequest(tenant, fiscalIssuerProfileId: 10, new DateOnly(2026, 9, 18), new DateOnly(2026, 9, 20)));

            Assert.DoesNotContain("OTHER-ISSUER", xml, StringComparison.Ordinal);
            Assert.DoesNotContain("OUTSIDE-DATE-RANGE", xml, StringComparison.Ordinal);
            Assert.DoesNotContain("LEGACY-INVOICE-MUST-NOT-BE-CERTIFIED", xml, StringComparison.Ordinal);
            Assert.Contains("Emitente fiscal snapshot", xml, StringComparison.Ordinal);
            Assert.DoesNotContain("Current live donor", xml, StringComparison.Ordinal);
        }

        private static bool IsLiveOrLegacyEvidenceType(Type type)
        {
            return type == typeof(Donation) ||
                type == typeof(WebUser) ||
                type == typeof(FoodBank) ||
                type == typeof(Invoice) ||
                type.FullName == "Microsoft.Extensions.Configuration.IConfiguration";
        }

        private static void AssertRequiredProperties(Type type, params string[] propertyNames)
        {
            foreach (string propertyName in propertyNames)
            {
                Assert.NotNull(type.GetProperty(propertyName));
            }
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

        private static object CreateExportRequest(Tenant tenant, int fiscalIssuerProfileId, DateOnly startDate, DateOnly endDate)
        {
            Type requestType = GetRequiredRepositoryType(ExportRequestTypeName);
            object request = Activator.CreateInstance(requestType);
            SetRequiredProperty(request, "Tenant", tenant);
            SetRequiredProperty(request, "FiscalIssuerProfileId", fiscalIssuerProfileId);
            SetRequiredProperty(request, "StartDate", startDate);
            SetRequiredProperty(request, "EndDate", endDate);
            return request;
        }

        private static object CreateExportService(ApplicationDbContext context)
        {
            Type serviceType = GetRequiredRepositoryType(ExportServiceTypeName);
            ConstructorInfo constructor = serviceType.GetConstructor(new[] { typeof(ApplicationDbContext) });
            Assert.NotNull(constructor);
            return constructor.Invoke(new object[] { context });
        }

        private static Tenant CreateTenant()
        {
            return new Tenant
            {
                Id = 7,
                InvoicingStrategy = InvoicingStrategy.SingleInvoiceTable,
                Name = "Fiscal tenant",
            };
        }

        private static string Export(object service, object request)
        {
            Type requestType = GetRequiredRepositoryType(ExportRequestTypeName);
            MethodInfo method = service.GetType().GetMethod("Export", new[] { requestType });
            Assert.NotNull(method);

            try
            {
                return Assert.IsType<string>(method.Invoke(service, new[] { request }));
            }
            catch (TargetInvocationException exception) when (exception.InnerException is not null)
            {
                ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
                throw;
            }
        }

        private static Type GetRequiredRepositoryType(string typeName)
        {
            return Assert.IsAssignableFrom<Type>(typeof(IFiscalScopeResolver).Assembly.GetType(typeName));
        }

        private static FiscalDocument CreateFiscalDocument(
            int id,
            int donationId,
            int fiscalIssuerProfileId,
            int sequence,
            DateOnly invoiceDate,
            string documentNumber,
            FiscalDocumentKind kind,
            FiscalDocumentStatus status)
        {
            return new FiscalDocument
            {
                Id = id,
                DonationId = donationId,
                SeriesId = 100,
                FiscalIssuerProfileId = fiscalIssuerProfileId,
                InvoicingStrategy = InvoicingStrategy.SingleInvoiceTable,
                Kind = kind,
                Status = status,
                Sequence = sequence,
                DocumentNumber = documentNumber,
                Atcud = $"VALIDATION-{sequence}",
                IssuedAtUtc = new DateTimeOffset(invoiceDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero),
                InvoiceDate = invoiceDate,
                SystemEntryDate = new DateTimeOffset(invoiceDate.ToDateTime(new TimeOnly(10, 30)), TimeSpan.Zero),
                Hash = $"signature-{sequence}",
                HashControl = "3",
                PreviousHash = sequence == 42 ? string.Empty : "signature-42",
                SigningKeyIdentifier = "fiscal-key",
                SigningKeyVersion = "3",
                SigningAlgorithm = "RSA",
                QrPayload = "A:501234567",
                QrPng = new byte[] { 1 },
                SoftwareCertificateNumber = "1234",
                IssuerLegalName = "Emitente fiscal snapshot",
                IssuerTaxRegistrationNumber = "501234567",
                IssuerLegalAddress = "Rua fiscal, 1",
                DonorName = "Fiscal donor snapshot",
                DonorTaxRegistrationNumber = "123456789",
                TaxRule = "NOR",
                NetTotal = 25.50m,
                TaxTotal = 5.87m,
                GrossTotal = 31.37m,
            };
        }

        private static void PersistFiscalDocuments(ApplicationDbContext context)
        {
            FiscalDocument original = CreateFiscalDocument(
                id: 42,
                donationId: 501,
                fiscalIssuerProfileId: 10,
                sequence: 42,
                invoiceDate: new DateOnly(2026, 9, 18),
                documentNumber: "RC 2026/42",
                kind: FiscalDocumentKind.Original,
                status: FiscalDocumentStatus.Issued);
            FiscalDocument cancelled = CreateFiscalDocument(
                id: 43,
                donationId: 502,
                fiscalIssuerProfileId: 10,
                sequence: 43,
                invoiceDate: new DateOnly(2026, 9, 19),
                documentNumber: "RC 2026/43",
                kind: FiscalDocumentKind.Original,
                status: FiscalDocumentStatus.Cancelled);
            FiscalDocument otherIssuer = CreateFiscalDocument(
                id: 45,
                donationId: 503,
                fiscalIssuerProfileId: 11,
                sequence: 45,
                invoiceDate: new DateOnly(2026, 9, 19),
                documentNumber: "OTHER-ISSUER",
                kind: FiscalDocumentKind.Original,
                status: FiscalDocumentStatus.Issued);
            FiscalDocument outsideDateRange = CreateFiscalDocument(
                id: 46,
                donationId: 504,
                fiscalIssuerProfileId: 10,
                sequence: 46,
                invoiceDate: new DateOnly(2026, 9, 21),
                documentNumber: "OUTSIDE-DATE-RANGE",
                kind: FiscalDocumentKind.Original,
                status: FiscalDocumentStatus.Issued);
            context.FiscalDocuments.AddRange(original, cancelled, otherIssuer, outsideDateRange);
            context.SaveChanges();

            FiscalDocument correction = CreateFiscalDocument(
                id: 44,
                donationId: 501,
                fiscalIssuerProfileId: 10,
                sequence: 44,
                invoiceDate: new DateOnly(2026, 9, 20),
                documentNumber: "NC 2026/44",
                kind: FiscalDocumentKind.Correction,
                status: FiscalDocumentStatus.Issued);
            correction.OriginalFiscalDocumentId = original.Id;
            context.FiscalDocuments.Add(correction);
            context.FiscalDocumentLines.AddRange(
                new FiscalDocumentLine
                {
                    Id = 2,
                    FiscalDocumentId = original.Id,
                    LineNumber = 2,
                    ProductCode = "DON-2",
                    Description = "Second fiscal line",
                    Quantity = 1m,
                    UnitPrice = 10.50m,
                    TaxRate = 23m,
                    NetTotal = 10.50m,
                    TaxTotal = 2.42m,
                    GrossTotal = 12.92m,
                },
                new FiscalDocumentLine
                {
                    Id = 1,
                    FiscalDocumentId = original.Id,
                    LineNumber = 1,
                    ProductCode = "DON-1",
                    Description = "First fiscal line",
                    Quantity = 1m,
                    UnitPrice = 15m,
                    TaxRate = 23m,
                    NetTotal = 15m,
                    TaxTotal = 3.45m,
                    GrossTotal = 18.45m,
                });
            context.FiscalDocumentEvents.Add(new FiscalDocumentEvent
            {
                Id = 1,
                FiscalDocumentId = cancelled.Id,
                EventType = "Cancelled",
                OccurredAtUtc = new DateTimeOffset(2026, 9, 19, 12, 0, 0, TimeSpan.Zero),
                Reason = "Payment reversal",
            });
            context.SaveChanges();
        }

        private static void SetRequiredProperty(object instance, string propertyName, object value)
        {
            PropertyInfo property = instance.GetType().GetProperty(propertyName);
            Assert.NotNull(property);
            property.SetValue(instance, value);
        }
    }
}
