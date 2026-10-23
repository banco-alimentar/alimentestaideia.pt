// -----------------------------------------------------------------------
// <copyright file="FiscalIssuanceEntrypointTddTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaldeia.Web.IntegrationTests.IntegrationTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Reflection;
    using System.Runtime.ExceptionServices;
    using System.Text;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Web;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Pages.Account.Manage;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Extensions;
    using BancoAlimentar.AlimentaEstaIdeia.Web.TestHost;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Xunit;

    /// <summary>
    /// Defines fiscal-issuance integration requirements at payment, e-mail and download entry points.
    /// </summary>
    public class FiscalIssuanceEntrypointTddTests : IClassFixture<CustomWebApplicationFactory>
    {
        private const string JsonMediaType = "application/json";
        private readonly CustomWebApplicationFactory factory;

        /// <summary>
        /// Initializes a new instance of the <see cref="FiscalIssuanceEntrypointTddTests"/> class.
        /// </summary>
        /// <param name="factory">The in-memory web host.</param>
        public FiscalIssuanceEntrypointTddTests(CustomWebApplicationFactory factory)
        {
            this.factory = factory;
        }

        /// <summary>
        /// A verified payment webhook for an eligible paid donation invokes fiscal issuance once.
        /// A duplicate provider notification does not ask the entry point to issue again: it keeps
        /// the payment acknowledgement idempotent and the original fiscal document unchanged.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task EasyPayWebhook_IssuesExactlyOnceForEligiblePaidDonationAndIgnoresDuplicateTrigger()
        {
            var tracker = new TrackingFiscalDocumentIssuanceService();
            WebApplicationFactory<Program> webFactory = this.CreateFiscalWebhookFactory(tracker);
            Guid publicId = Guid.NewGuid();
            IntegrationTestDataSeeder.PendingDonationSeed seed;

            using (IServiceScope scope = webFactory.Services.CreateScope())
            {
                seed = await IntegrationTestDataSeeder.SeedPendingDonationWithCreditCardAsync(
                    scope.ServiceProvider,
                    publicId);
                seed.Donation.WantsReceipt = true;
                scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().SaveChanges();
            }

            HttpClient client = webFactory.CreateClient();
            string payload = EasyPayWebhookPayloadBuilder.BuildCreditCardPaymentNotification(
                publicId,
                seed.TransactionKey,
                seed.EasyPayId);

            HttpResponseMessage first = await this.PostJsonAsync(client, "/easypay/payment", payload);
            HttpResponseMessage duplicate = await this.PostJsonAsync(client, "/easypay/payment", payload);

            Assert.Equal(HttpStatusCode.OK, first.StatusCode);
            Assert.Equal(HttpStatusCode.OK, duplicate.StatusCode);
            IssuanceCall call = Assert.Single(tracker.Calls);
            Assert.Equal(seed.Donation.Id, call.DonationId);
            Assert.Equal(1, call.TenantId);
            Assert.Equal(tracker.OriginalDocument.Id, call.ReturnedDocumentId);
        }

        /// <summary>
        /// A fiscal-issuance failure must not be acknowledged as a successful webhook. The
        /// payment provider can then retry and obtain the same original after the operational
        /// signing/configuration fault has been fixed.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task EasyPayWebhook_DoesNotAcknowledgeWhenFiscalIssuanceFails()
        {
            var tracker = new TrackingFiscalDocumentIssuanceService
            {
                ExceptionToThrow = new InvalidOperationException("Fiscal signing is unavailable."),
            };
            WebApplicationFactory<Program> webFactory = this.CreateFiscalWebhookFactory(tracker);
            Guid publicId = Guid.NewGuid();
            IntegrationTestDataSeeder.PendingDonationSeed seed;

            using (IServiceScope scope = webFactory.Services.CreateScope())
            {
                seed = await IntegrationTestDataSeeder.SeedPendingDonationWithCreditCardAsync(
                    scope.ServiceProvider,
                    publicId);
                seed.Donation.WantsReceipt = true;
                scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().SaveChanges();
            }

            HttpClient client = webFactory.CreateClient();
            string payload = EasyPayWebhookPayloadBuilder.BuildCreditCardPaymentNotification(
                publicId,
                seed.TransactionKey,
                seed.EasyPayId);

            HttpResponseMessage response = await this.PostJsonAsync(client, "/easypay/payment", payload);

            Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.Single(tracker.Calls);
        }

        /// <summary>
        /// Thanks, subscription thanks, claim-invoice and the webhook already route receipt
        /// e-mail through <see cref="IMail"/>. That shared boundary receives fiscal issuance;
        /// the later download page receives it directly. This leaves legacy Invoice generation
        /// available for documents that were never activated as fiscal documents.
        /// </summary>
        [Fact]
        public void ReceiptMailAndLaterDownload_ExposeFiscalIssuanceWithoutReplacingLegacyInvoiceContract()
        {
            Assert.Contains(
                typeof(Mail).GetConstructors().SelectMany(constructor => constructor.GetParameters()),
                parameter => parameter.ParameterType == typeof(IFiscalDocumentIssuanceService));
            Assert.Contains(
                typeof(GenerateInvoiceModel).GetConstructors().SelectMany(constructor => constructor.GetParameters()),
                parameter => parameter.ParameterType == typeof(IFiscalDocumentIssuanceService));

            string mailSource = File.ReadAllText(Path.Combine(
                GetRepositoryRoot(),
                "BancoAlimentar.AlimentaEstaIdeia.Web",
                "Extensions",
                "Mail.cs"));
            string downloadSource = File.ReadAllText(Path.Combine(
                GetRepositoryRoot(),
                "BancoAlimentar.AlimentaEstaIdeia.Web",
                "Areas",
                "Identity",
                "Pages",
                "Account",
                "Manage",
                "GenerateInvoice.cshtml.cs"));
            Assert.Contains("fiscalDocumentIssuanceService.IssueOriginal", mailSource, StringComparison.Ordinal);
            Assert.Contains("fiscalDocumentIssuanceService.IssueOriginal", downloadSource, StringComparison.Ordinal);

            MethodInfo legacyInvoice = Assert.Single(
                typeof(GenerateInvoiceModel).GetMethods(BindingFlags.Instance | BindingFlags.Public),
                method => method.Name == "GenerateInvoiceInternalAsync" &&
                    method.GetParameters().Select(parameter => parameter.ParameterType)
                        .SequenceEqual(new[] { typeof(string), typeof(Tenant), typeof(bool) }));
            Assert.DoesNotContain(
                legacyInvoice.GetParameters(),
                parameter => parameter.ParameterType == typeof(FiscalDocument));
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

        private WebApplicationFactory<Program> CreateFiscalWebhookFactory(TrackingFiscalDocumentIssuanceService tracker)
        {
            return this.factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll(typeof(IFiscalDocumentIssuanceService));
                    services.AddScoped<IFiscalDocumentIssuanceService>(_ => tracker);
                });
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string>
                    {
                        ["IsEmailEnabled"] = bool.FalseString,
                    });
                });
            });
        }

        private async Task<HttpResponseMessage> PostJsonAsync(HttpClient client, string path, string payload)
        {
            using var content = new StringContent(payload, Encoding.UTF8, JsonMediaType);
            return await client.PostAsync(path, content);
        }

        private sealed class TrackingFiscalDocumentIssuanceService : IFiscalDocumentIssuanceService
        {
            public ICollection<IssuanceCall> Calls { get; } = new List<IssuanceCall>();

            public Exception ExceptionToThrow { get; set; }

            public FiscalDocument OriginalDocument { get; } = new FiscalDocument
            {
                Id = 734,
                DocumentNumber = "RC 2026/734",
                Atcud = "VALIDATION-734",
                Status = FiscalDocumentStatus.Issued,
            };

            public FiscalDocument IssueOriginal(Tenant tenant, Donation donation)
            {
                this.Calls.Add(new IssuanceCall
                {
                    DonationId = donation.Id,
                    ReturnedDocumentId = this.OriginalDocument.Id,
                    TenantId = tenant.Id,
                });
                if (this.ExceptionToThrow != null)
                {
                    ExceptionDispatchInfo.Capture(this.ExceptionToThrow).Throw();
                }

                return this.OriginalDocument;
            }
        }

        private sealed class IssuanceCall
        {
            public int DonationId { get; init; }

            public int ReturnedDocumentId { get; init; }

            public int TenantId { get; init; }
        }
    }
}
