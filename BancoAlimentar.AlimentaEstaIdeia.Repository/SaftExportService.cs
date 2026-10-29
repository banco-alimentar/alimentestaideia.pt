// -----------------------------------------------------------------------
// <copyright file="SaftExportService.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Xml.Linq;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// Produces deterministic SAF-T (PT) XML from immutable fiscal document snapshots.
    /// </summary>
    public class SaftExportService : ISaftExportService
    {
        private const string SaftNamespaceUri = "urn:OECD:StandardAuditFile-Tax:PT_1.04_01";
        private const string SaftVersion = "1.04_01";
        private readonly ApplicationDbContext applicationDbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="SaftExportService"/> class.
        /// </summary>
        /// <param name="applicationDbContext">The tenant fiscal persistence context.</param>
        public SaftExportService(ApplicationDbContext applicationDbContext)
        {
            this.applicationDbContext = applicationDbContext ?? throw new ArgumentNullException(nameof(applicationDbContext));
        }

        /// <inheritdoc/>
        public string Export(SaftExportRequest request)
        {
            this.ValidateRequest(request);

            List<FiscalDocument> documents = this.applicationDbContext.FiscalDocuments
                .AsNoTracking()
                .Include(document => document.Lines)
                .Include(document => document.OriginalFiscalDocument)
                .Where(document =>
                    document.FiscalIssuerProfileId == request.FiscalIssuerProfileId &&
                    document.InvoicingStrategy == request.Tenant.InvoicingStrategy &&
                    document.InvoiceDate >= request.StartDate &&
                    document.InvoiceDate <= request.EndDate &&
                    document.Status != FiscalDocumentStatus.Draft)
                .OrderBy(document => document.InvoiceDate)
                .ThenBy(document => document.SeriesId)
                .ThenBy(document => document.Sequence)
                .ThenBy(document => document.Id)
                .ToList();

            XNamespace saft = SaftNamespaceUri;
            FiscalDocument issuerSnapshot = documents.FirstOrDefault();
            var auditFile = new XElement(
                saft + "AuditFile",
                this.CreateHeader(saft, request, issuerSnapshot),
                new XElement(
                    saft + "SourceDocuments",
                    this.CreateSalesInvoices(saft, documents)));
            var xml = new XDocument(new XDeclaration("1.0", "utf-8", null), auditFile);
            return xml.ToString(SaveOptions.DisableFormatting);
        }

        private XElement CreateHeader(XNamespace saft, SaftExportRequest request, FiscalDocument issuerSnapshot)
        {
            return new XElement(
                saft + "Header",
                new XElement(saft + "AuditFileVersion", SaftVersion),
                new XElement(saft + "CompanyID", issuerSnapshot?.IssuerTaxRegistrationNumber ?? string.Empty),
                new XElement(saft + "TaxRegistrationNumber", issuerSnapshot?.IssuerTaxRegistrationNumber ?? string.Empty),
                new XElement(saft + "CompanyName", issuerSnapshot?.IssuerLegalName ?? string.Empty),
                new XElement(
                    saft + "CompanyAddress",
                    new XElement(saft + "AddressDetail", issuerSnapshot?.IssuerLegalAddress ?? string.Empty),
                    new XElement(saft + "City", string.Empty),
                    new XElement(saft + "PostalCode", string.Empty),
                    new XElement(saft + "Country", "PT")),
                new XElement(saft + "FiscalYear", request.StartDate.Year.ToString(CultureInfo.InvariantCulture)),
                new XElement(saft + "StartDate", this.FormatDate(request.StartDate)),
                new XElement(saft + "EndDate", this.FormatDate(request.EndDate)),
                new XElement(saft + "CurrencyCode", "EUR"),
                new XElement(saft + "DateCreated", this.FormatDate(request.EndDate)),
                new XElement(saft + "TaxAccountingBasis", "I"),
                new XElement(saft + "ProductCompanyTaxID", issuerSnapshot?.IssuerTaxRegistrationNumber ?? string.Empty),
                new XElement(saft + "SoftwareCertificateNumber", issuerSnapshot?.SoftwareCertificateNumber ?? string.Empty));
        }

        private XElement CreateSalesInvoices(XNamespace saft, IReadOnlyCollection<FiscalDocument> documents)
        {
            decimal totalCredit = documents.Sum(document => document.GrossTotal);
            return new XElement(
                saft + "SalesInvoices",
                new XElement(saft + "NumberOfEntries", documents.Count.ToString(CultureInfo.InvariantCulture)),
                new XElement(saft + "TotalDebit", this.FormatAmount(0m)),
                new XElement(saft + "TotalCredit", this.FormatAmount(totalCredit)),
                documents.Select(document => this.CreateInvoice(saft, document)));
        }

        private XElement CreateInvoice(XNamespace saft, FiscalDocument document)
        {
            var invoice = new XElement(
                saft + "Invoice",
                new XElement(saft + "InvoiceNo", document.DocumentNumber),
                new XElement(saft + "ATCUD", document.Atcud),
                new XElement(saft + "InvoiceStatus", this.GetInvoiceStatus(document.Status)),
                new XElement(saft + "InvoiceStatusDate", this.FormatDateTime(document.SystemEntryDate)),
                new XElement(saft + "SourceID", "System"),
                new XElement(saft + "InvoiceType", this.GetInvoiceType(document)),
                new XElement(saft + "CustomerID", document.DonorTaxRegistrationNumber ?? string.Empty),
                new XElement(saft + "Hash", document.Hash),
                new XElement(saft + "HashControl", document.HashControl),
                new XElement(saft + "Period", document.InvoiceDate.Month.ToString(CultureInfo.InvariantCulture)),
                new XElement(saft + "InvoiceDate", this.FormatDate(document.InvoiceDate)),
                new XElement(saft + "SystemEntryDate", this.FormatDateTime(document.SystemEntryDate)),
                this.CreateOrderReferences(saft, document),
                this.CreateLines(saft, document),
                this.CreateDocumentTotals(saft, document));

            return invoice;
        }

        private XElement CreateOrderReferences(XNamespace saft, FiscalDocument document)
        {
            if (!document.OriginalFiscalDocumentId.HasValue)
            {
                return null;
            }

            if (document.OriginalFiscalDocument == null || string.IsNullOrWhiteSpace(document.OriginalFiscalDocument.DocumentNumber))
            {
                throw new InvalidOperationException("A fiscal correction or credit requires its persisted original document reference.");
            }

            return new XElement(
                saft + "OrderReferences",
                new XElement(saft + "OriginatingON", document.OriginalFiscalDocument.DocumentNumber));
        }

        private IEnumerable<XElement> CreateLines(XNamespace saft, FiscalDocument document)
        {
            return (document.Lines ?? Array.Empty<FiscalDocumentLine>())
                .OrderBy(line => line.LineNumber)
                .ThenBy(line => line.Id)
                .Select(line => new XElement(
                    saft + "Line",
                    new XElement(saft + "LineNumber", line.LineNumber.ToString(CultureInfo.InvariantCulture)),
                    new XElement(saft + "ProductCode", line.ProductCode),
                    new XElement(saft + "ProductDescription", line.Description),
                    new XElement(saft + "Quantity", this.FormatQuantity(line.Quantity)),
                    new XElement(saft + "UnitOfMeasure", "UN"),
                    new XElement(saft + "UnitPrice", this.FormatAmount(line.UnitPrice)),
                    new XElement(saft + "TaxPointDate", this.FormatDate(document.InvoiceDate)),
                    new XElement(saft + "Description", line.Description),
                    new XElement(saft + "CreditAmount", this.FormatAmount(line.NetTotal)),
                    this.CreateTax(saft, line),
                    this.CreateTaxExemptionReason(saft, line)));
        }

        private XElement CreateDocumentTotals(XNamespace saft, FiscalDocument document)
        {
            return new XElement(
                saft + "DocumentTotals",
                new XElement(saft + "TaxPayable", this.FormatAmount(document.TaxTotal)),
                new XElement(saft + "NetTotal", this.FormatAmount(document.NetTotal)),
                new XElement(saft + "GrossTotal", this.FormatAmount(document.GrossTotal)));
        }

        private XElement CreateTax(XNamespace saft, FiscalDocumentLine line)
        {
            return new XElement(
                saft + "Tax",
                new XElement(saft + "TaxType", "IVA"),
                new XElement(saft + "TaxCountryRegion", "PT"),
                new XElement(saft + "TaxCode", "NOR"),
                new XElement(saft + "TaxPercentage", this.FormatTaxRate(line.TaxRate)));
        }

        private XElement CreateTaxExemptionReason(XNamespace saft, FiscalDocumentLine line)
        {
            return string.IsNullOrWhiteSpace(line.TaxExemptionReason)
                ? null
                : new XElement(saft + "TaxExemptionReason", line.TaxExemptionReason);
        }

        private string GetInvoiceStatus(FiscalDocumentStatus status)
        {
            return status == FiscalDocumentStatus.Cancelled ? "A" : "N";
        }

        private string GetInvoiceType(FiscalDocument document)
        {
            if (document.Kind == FiscalDocumentKind.Correction || document.Kind == FiscalDocumentKind.Credit)
            {
                return "NC";
            }

            return document.DocumentNumber?.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "RC";
        }

        private void ValidateRequest(SaftExportRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (request.Tenant == null || request.Tenant.Id <= 0)
            {
                throw new InvalidOperationException("SAF-T export requires a trusted tenant with a positive identifier.");
            }

            if (request.FiscalIssuerProfileId <= 0)
            {
                throw new InvalidOperationException("SAF-T export requires a fiscal issuer profile identifier.");
            }

            if (request.StartDate == DateOnly.MinValue || request.EndDate == DateOnly.MinValue || request.StartDate > request.EndDate)
            {
                throw new InvalidOperationException("SAF-T export requires a valid inclusive invoice-date range.");
            }
        }

        private string FormatAmount(decimal amount)
        {
            return amount.ToString("0.00", CultureInfo.InvariantCulture);
        }

        private string FormatDate(DateOnly date)
        {
            return date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        private string FormatDateTime(DateTimeOffset dateTime)
        {
            return dateTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
        }

        private string FormatQuantity(decimal quantity)
        {
            return quantity.ToString("0.000", CultureInfo.InvariantCulture);
        }

        private string FormatTaxRate(decimal taxRate)
        {
            return taxRate.ToString("0.00", CultureInfo.InvariantCulture);
        }
    }
}
