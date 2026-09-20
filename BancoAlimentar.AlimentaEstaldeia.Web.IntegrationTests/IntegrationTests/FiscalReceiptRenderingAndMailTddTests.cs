// -----------------------------------------------------------------------
// <copyright file="FiscalReceiptRenderingAndMailTddTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaldeia.Web.IntegrationTests.IntegrationTests
{
    using System;
    using System.IO;
    using System.Linq;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Extensions;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Services.Invoices;
    using Xunit;

    /// <summary>
    /// Defines the browser-rendering and mail-delivery contracts for immutable fiscal receipts.
    /// </summary>
    public class FiscalReceiptRenderingAndMailTddTests
    {
        /// <summary>
        /// The fiscal Razor receipt must expose every legally relevant persisted value, including
        /// a self-contained QR PNG. The renderer runs outside a browser and cannot resolve a
        /// relative QR route after the immutable PDF has been created.
        /// </summary>
        [Fact]
        public void FiscalReceiptRazor_RendersPersistedFiscalFieldsAndEmbeddedQrPng()
        {
            string razor = ReadWebFile("Areas", "Admin", "Pages", "Fiscal", "FiscalReceipt.cshtml");

            Assert.Contains("Convert.ToBase64String(Model.Receipt.QrPng)", razor, StringComparison.Ordinal);
            Assert.Contains("data:image/png;base64", razor, StringComparison.Ordinal);
            AssertContainsFiscalFields(
                razor,
                "@Model.Receipt.",
                "IssuerName",
                "IssuerTaxRegistrationNumber",
                "IssuerLegalAddress",
                "BuyerName",
                "BuyerTaxRegistrationNumber",
                "DocumentIdentifier",
                "Atcud",
                "SoftwareCertificateNumber",
                "HashControl",
                "NetTotal",
                "TaxTotal",
                "GrossTotal",
                "IsCancelled",
                "CancellationReason",
                "IsCorrection",
                "OriginalDocumentIdentifier");
            AssertContainsFiscalFields(
                razor,
                "@line.",
                "LineNumber",
                "ProductCode",
                "Description",
                "Quantity",
                "UnitPrice",
                "TaxRate",
                "TaxExemptionReason",
                "NetTotal",
                "TaxTotal",
                "GrossTotal");
        }

        /// <summary>
        /// An eligible fiscal receipt email must route the issued document through the immutable
        /// fiscal delivery service and return from that path. It must never invoke the legacy
        /// invoice generator, which reads mutable donation and tenant data and can produce a
        /// different artifact from the one that was signed and issued.
        /// </summary>
        [Fact]
        public void FiscalReceiptEmail_DeliversImmutableArtifactAndDoesNotInvokeLegacyGenerator()
        {
            Assert.Contains(
                typeof(Mail).GetConstructors().SelectMany(constructor => constructor.GetParameters()),
                parameter => parameter.ParameterType == typeof(IFiscalReceiptDeliveryService));

            string mailSource = ReadWebFile("Extensions", "Mail.cs");
            string fiscalBranch = GetEligibleFiscalIssuanceBranch(mailSource);

            Assert.Contains("fiscalDocumentIssuanceService.IssueOriginal", fiscalBranch, StringComparison.Ordinal);
            Assert.Contains("fiscalReceiptDeliveryService.GetOrCreate", fiscalBranch, StringComparison.Ordinal);
            Assert.Contains("FiscalReceiptPdfArtifact", fiscalBranch, StringComparison.Ordinal);
            Assert.Contains(".Content", fiscalBranch, StringComparison.Ordinal);
            Assert.Contains("new MemoryStream", fiscalBranch, StringComparison.Ordinal);
            Assert.Contains("SendConfirmedPaymentMailToDonor", fiscalBranch, StringComparison.Ordinal);
            Assert.Contains("return", fiscalBranch, StringComparison.Ordinal);
            Assert.DoesNotContain("GenerateInvoice(", fiscalBranch, StringComparison.Ordinal);
        }

        private static void AssertContainsFiscalFields(string razor, string expressionPrefix, params string[] fieldNames)
        {
            foreach (string fieldName in fieldNames)
            {
                Assert.Contains($"{expressionPrefix}{fieldName}", razor, StringComparison.Ordinal);
            }
        }

        private static string GetEligibleFiscalIssuanceBranch(string mailSource)
        {
            const string branchStart = "if (this.IsEligibleForFiscalIssuance(donation))";
            int start = mailSource.IndexOf(branchStart, StringComparison.Ordinal);
            Assert.True(start >= 0, "The receipt-email flow must contain the fiscal eligibility branch.");

            int openBrace = mailSource.IndexOf('{', start + branchStart.Length);
            Assert.True(openBrace >= 0, "The fiscal eligibility branch must have a body.");
            int closeBrace = FindMatchingBrace(mailSource, openBrace);
            return mailSource.Substring(start, closeBrace - start + 1);
        }

        private static int FindMatchingBrace(string source, int openBrace)
        {
            int depth = 0;
            for (int index = openBrace; index < source.Length; index++)
            {
                if (source[index] == '{')
                {
                    depth++;
                }
                else if (source[index] == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        return index;
                    }
                }
            }

            throw new InvalidOperationException("The fiscal eligibility branch is not syntactically balanced.");
        }

        private static string ReadWebFile(params string[] pathSegments)
        {
            string repositoryRoot = GetRepositoryRoot();
            string path = Path.Combine(new[] { repositoryRoot, "BancoAlimentar.AlimentaEstaIdeia.Web" }.Concat(pathSegments).ToArray());
            return File.ReadAllText(path);
        }

        private static string GetRepositoryRoot()
        {
            DirectoryInfo directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, ".git")))
            {
                directory = directory.Parent;
            }

            return Assert.IsType<string>(directory?.FullName);
        }
    }
}
