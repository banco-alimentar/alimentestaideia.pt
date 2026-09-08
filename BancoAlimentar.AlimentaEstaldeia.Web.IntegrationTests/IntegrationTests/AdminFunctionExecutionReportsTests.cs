// -----------------------------------------------------------------------
// <copyright file="AdminFunctionExecutionReportsTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaldeia.Web.IntegrationTests.IntegrationTests
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.FunctionExecutionReports;
    using BancoAlimentar.AlimentaEstaIdeia.Testing.Common;
    using BancoAlimentar.AlimentaEstaIdeia.Web;
    using BancoAlimentar.AlimentaEstaIdeia.Web.TestHost;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Xunit;

    /// <summary>
    /// Integration tests for the Admin function execution report overview and viewer.
    /// </summary>
    public class AdminFunctionExecutionReportsTests : IClassFixture<CustomWebApplicationFactory>
    {
        private const string AdminEmail = "function-reports-admin@test.com";
        private const string UserEmail = "function-reports-user@test.com";
        private const string Password = IntegrationTestCredentials.DefaultPassword;
        private readonly CustomWebApplicationFactory factory;

        /// <summary>
        /// Initializes a new instance of the <see cref="AdminFunctionExecutionReportsTests"/> class.
        /// </summary>
        /// <param name="factory">Web application factory.</param>
        public AdminFunctionExecutionReportsTests(CustomWebApplicationFactory factory)
        {
            this.factory = factory;
        }

        /// <summary>
        /// Anonymous users cannot access the function execution report overview.
        /// </summary>
        [Fact]
        public async Task Overview_RedirectsToLogin_WhenAnonymous()
        {
            HttpClient client = this.factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });

            HttpResponseMessage response = await client.GetAsync("/Admin/FunctionExecutionReports");

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        }

        /// <summary>
        /// An ordinary authenticated user is redirected to the access-denied page.
        /// </summary>
        [Fact]
        public async Task Overview_RedirectsOrdinaryUserToAccessDenied()
        {
            using (var scope = this.factory.Services.CreateScope())
            {
                await IntegrationTestDataSeeder.EnsureUserAsync(scope.ServiceProvider, UserEmail, Password);
            }

            HttpClient client = await WebTestAuthHelper.CreateAuthenticatedClientAsync(
                this.factory,
                UserEmail,
                Password,
                allowAutoRedirect: false);
            HttpResponseMessage response = await client.GetAsync("/Admin/FunctionExecutionReports");

            Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        }

        /// <summary>
        /// The overview renders all five catalog functions even when no reports exist.
        /// </summary>
        [Fact]
        public async Task Overview_RendersAllFunctionsAndNoReportState()
        {
            using (var scope = this.factory.Services.CreateScope())
            {
                await IntegrationTestDataSeeder.EnsureSuperAdminUserAsync(scope.ServiceProvider, AdminEmail, Password);
            }

            HttpClient client = await WebTestAuthHelper.CreateAuthenticatedClientAsync(this.factory, AdminEmail, Password);
            HttpResponseMessage response = await client.GetAsync("/Admin/FunctionExecutionReports");
            string html = await response.Content.ReadAsStringAsync();

            response.EnsureSuccessStatusCode();
            Assert.Contains("GenerateDonationReportFunction", html, StringComparison.Ordinal);
            Assert.Contains("GenerateSiteHealthReportFunction", html, StringComparison.Ordinal);
            Assert.Contains("DeleteOldSubscriptionFunction", html, StringComparison.Ordinal);
            Assert.Contains("MultiBancoPaymentNotificationFunction", html, StringComparison.Ordinal);
            Assert.Contains("UpdateSubscriptions", html, StringComparison.Ordinal);
            Assert.Contains("No report", html, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Latest report metadata and links are rendered using safe catalog identifiers.
        /// </summary>
        [Fact]
        public async Task Overview_RendersLatestStatusDurationAndDetailsLink()
        {
            var webFactory = this.CreateFactory(new FakeFunctionExecutionReportReader(new Dictionary<string, FunctionExecutionReportStorageResult>
            {
                ["DeleteOldSubscriptionFunction"] = FakeFunctionExecutionReportReader.Success(
                    "execution-42",
                    FunctionExecutionReportOutcome.Partial,
                    "Some tenants failed.",
                    new DateTime(2026, 9, 7, 7, 0, 0, DateTimeKind.Utc),
                    1250),
            }));

            using (var scope = webFactory.Services.CreateScope())
            {
                await IntegrationTestDataSeeder.EnsureSuperAdminUserAsync(scope.ServiceProvider, AdminEmail, Password);
            }

            HttpClient client = await WebTestAuthHelper.CreateAuthenticatedClientAsync(webFactory, AdminEmail, Password);
            string html = await (await client.GetAsync("/Admin/FunctionExecutionReports")).Content.ReadAsStringAsync();

            Assert.Contains("Partial", html, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("execution-42", html, StringComparison.Ordinal);
            Assert.Contains("1250 ms", html, StringComparison.Ordinal);
            Assert.Contains("Some tenants failed.", html, StringComparison.Ordinal);
            Assert.Contains("/Admin/FunctionExecutionReports/Details?functionKey=DeleteOldSubscriptionFunction&amp;executionId=execution-42", html, StringComparison.Ordinal);
        }

        /// <summary>
        /// Tenant rows expose global infrastructure status without loading another tenant's report.
        /// </summary>
        [Fact]
        public async Task Overview_RendersGlobalInfrastructureStatusAsMetadataOnly()
        {
            var reader = new FakeFunctionExecutionReportReader(new Dictionary<string, FunctionExecutionReportStorageResult>
            {
                ["DeleteOldSubscriptionFunction"] = FakeFunctionExecutionReportReader.Success(
                    "tenant-execution",
                    FunctionExecutionReportOutcome.Succeeded,
                    "Tenant completed.",
                    DateTime.UtcNow),
                ["global/DeleteOldSubscriptionFunction"] = FakeFunctionExecutionReportReader.Success(
                    "global-execution",
                    FunctionExecutionReportOutcome.Partial,
                    "Tenant processing completed with 1 tenant failure(s).",
                    DateTime.UtcNow),
            });
            var webFactory = this.CreateFactory(reader);

            using (var scope = webFactory.Services.CreateScope())
            {
                await IntegrationTestDataSeeder.EnsureSuperAdminUserAsync(scope.ServiceProvider, AdminEmail, Password);
            }

            HttpClient client = await WebTestAuthHelper.CreateAuthenticatedClientAsync(webFactory, AdminEmail, Password);
            string html = await (await client.GetAsync("/Admin/FunctionExecutionReports")).Content.ReadAsStringAsync();

            Assert.Contains("Global infrastructure status", html, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Partial", html, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("global-execution", html, StringComparison.Ordinal);
        }

        /// <summary>
        /// The report viewer renders activities, warnings, errors, and summary status.
        /// </summary>
        [Fact]
        public async Task Details_RendersReportContent()
        {
            var reader = new FakeFunctionExecutionReportReader(new Dictionary<string, FunctionExecutionReportStorageResult>
            {
                ["DeleteOldSubscriptionFunction/execution-42"] = FakeFunctionExecutionReportReader.Success(
                    "execution-42",
                    FunctionExecutionReportOutcome.Failed,
                    "Database operation failed.",
                    DateTime.UtcNow),
            });
            reader.AddReport("DeleteOldSubscriptionFunction/execution-42", new FunctionExecutionReport
            {
                FunctionKey = "DeleteOldSubscriptionFunction",
                FunctionName = "DeleteOldSubscriptionFunction",
                ExecutionId = "execution-42",
                InvocationId = "invocation-42",
                CorrelationId = "correlation-42",
                TriggerType = "Timer",
                Environment = "PreProd",
                SlotKey = "production",
                ScopeKey = "alimentestaideia",
                Outcome = FunctionExecutionReportOutcome.Failed,
                BusinessDataChanged = true,
                Activities = new List<FunctionExecutionReportActivity>
                {
                    new FunctionExecutionReportActivity
                    {
                        ActivityKey = "transaction",
                        Message = "Transaction rolled back.",
                        Severity = FunctionExecutionReportActivitySeverity.Error,
                    },
                },
                Warnings = new List<string> { "One tenant was skipped." },
                Errors = new List<string> { "Database operation failed." },
                Summary = new FunctionExecutionReportSummary
                {
                    RecordsChanged = 0,
                    Message = "Database operation failed.",
                    Counters = new Dictionary<string, long> { ["subscriptionsDeleted"] = 3 },
                },
            });
            var webFactory = this.CreateFactory(reader);

            using (var scope = webFactory.Services.CreateScope())
            {
                await IntegrationTestDataSeeder.EnsureSuperAdminUserAsync(scope.ServiceProvider, AdminEmail, Password);
            }

            HttpClient client = await WebTestAuthHelper.CreateAuthenticatedClientAsync(webFactory, AdminEmail, Password);
            string html = await (await client.GetAsync(
                "/Admin/FunctionExecutionReports/Details?functionKey=DeleteOldSubscriptionFunction&executionId=execution-42"))
                .Content.ReadAsStringAsync();

            Assert.Contains("Transaction rolled back.", html, StringComparison.Ordinal);
            Assert.Contains("One tenant was skipped.", html, StringComparison.Ordinal);
            Assert.Contains("Database operation failed.", html, StringComparison.Ordinal);
            Assert.Contains("Failed", html, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("subscriptionsDeleted", html, StringComparison.Ordinal);
            Assert.Contains("Database operation failed.", html, StringComparison.Ordinal);
            Assert.Contains("PreProd", html, StringComparison.Ordinal);
            Assert.Contains("Timer", html, StringComparison.Ordinal);
            Assert.Contains("invocation-42", html, StringComparison.Ordinal);
            Assert.Contains("correlation-42", html, StringComparison.Ordinal);
            Assert.Contains("Business data changed", html, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Yes", html, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Optional metadata uses a localized safe placeholder when it is absent.
        /// </summary>
        [Fact]
        public async Task Details_RendersSafeEmptyMetadataValues()
        {
            var reader = new FakeFunctionExecutionReportReader(new Dictionary<string, FunctionExecutionReportStorageResult>());
            reader.AddReport("DeleteOldSubscriptionFunction/execution-empty", new FunctionExecutionReport
            {
                FunctionKey = "DeleteOldSubscriptionFunction",
                ExecutionId = "execution-empty",
                InvocationId = null,
                CorrelationId = null,
                TriggerType = null,
                Environment = null,
                Outcome = FunctionExecutionReportOutcome.Succeeded,
                BusinessDataChanged = false,
            });
            var webFactory = this.CreateFactory(reader);

            using (var scope = webFactory.Services.CreateScope())
            {
                await IntegrationTestDataSeeder.EnsureSuperAdminUserAsync(scope.ServiceProvider, AdminEmail, Password);
            }

            HttpClient client = await WebTestAuthHelper.CreateAuthenticatedClientAsync(webFactory, AdminEmail, Password);
            string html = await (await client.GetAsync(
                "/Admin/FunctionExecutionReports/Details?functionKey=DeleteOldSubscriptionFunction&executionId=execution-empty"))
                .Content.ReadAsStringAsync();

            Assert.Contains("Not available", html, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("No", html, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Missing, corrupt, expired, and unavailable reports are shown explicitly.
        /// </summary>
        [Theory]
        [InlineData(FunctionExecutionReportStorageState.Missing, "No report")]
        [InlineData(FunctionExecutionReportStorageState.Corrupt, "corrupt")]
        [InlineData(FunctionExecutionReportStorageState.Expired, "expired")]
        [InlineData(FunctionExecutionReportStorageState.Unavailable, "unavailable")]
        public async Task Details_ShowsUnavailableState(
            FunctionExecutionReportStorageState state,
            string expectedText)
        {
            var webFactory = this.CreateFactory(new FakeFunctionExecutionReportReader(new Dictionary<string, FunctionExecutionReportStorageResult>
            {
                ["DeleteOldSubscriptionFunction/execution-42"] = new FunctionExecutionReportStorageResult { State = state },
            }));

            using (var scope = webFactory.Services.CreateScope())
            {
                await IntegrationTestDataSeeder.EnsureSuperAdminUserAsync(scope.ServiceProvider, AdminEmail, Password);
            }

            HttpClient client = await WebTestAuthHelper.CreateAuthenticatedClientAsync(webFactory, AdminEmail, Password);
            string html = await (await client.GetAsync(
                "/Admin/FunctionExecutionReports/Details?functionKey=DeleteOldSubscriptionFunction&executionId=execution-42"))
                .Content.ReadAsStringAsync();

            Assert.Contains(expectedText, html, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Invalid catalog keys and traversal-like execution IDs cannot select arbitrary storage paths.
        /// </summary>
        [Theory]
        [InlineData("NotAFunction", "execution-42")]
        [InlineData("DeleteOldSubscriptionFunction", "../../other-tenant")]
        public async Task Details_RejectsOutOfScopeIdentifiers(string functionKey, string executionId)
        {
            using (var scope = this.factory.Services.CreateScope())
            {
                await IntegrationTestDataSeeder.EnsureSuperAdminUserAsync(scope.ServiceProvider, AdminEmail, Password);
            }

            HttpClient client = await WebTestAuthHelper.CreateAuthenticatedClientAsync(
                this.factory,
                AdminEmail,
                Password,
                allowAutoRedirect: false);
            HttpResponseMessage response = await client.GetAsync(
                $"/Admin/FunctionExecutionReports/Details?functionKey={Uri.EscapeDataString(functionKey)}&executionId={Uri.EscapeDataString(executionId)}");

            Assert.True(
                response.StatusCode == HttpStatusCode.NotFound
                    || response.StatusCode == HttpStatusCode.BadRequest,
                $"Unexpected status: {response.StatusCode}");
        }

        /// <summary>
        /// Portuguese resources are used when the request culture is Portuguese.
        /// </summary>
        [Fact]
        public async Task Overview_RendersPortugueseLabels_WhenPortugueseCultureIsSelected()
        {
            using (var scope = this.factory.Services.CreateScope())
            {
                await IntegrationTestDataSeeder.EnsureSuperAdminUserAsync(scope.ServiceProvider, AdminEmail, Password);
            }

            HttpClient client = await WebTestAuthHelper.CreateAuthenticatedClientAsync(this.factory, AdminEmail, Password);
            client.DefaultRequestHeaders.Add("Cookie", ".AspNetCore.Culture=c=pt|uic=pt");
            client.DefaultRequestHeaders.Add("Accept-Language", "pt");
            var document = await HtmlHelpers.GetDocumentAsync(
                await client.GetAsync("/Admin/FunctionExecutionReports"));
            string text = document.DocumentElement.TextContent;

            Assert.Contains("Execuções das funções", text, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Última execução", text, StringComparison.OrdinalIgnoreCase);
        }

        private WebApplicationFactory<Program> CreateFactory(IFunctionExecutionReportReader reader)
        {
            return this.factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            {
                services.RemoveAll<IFunctionExecutionReportReader>();
                services.AddSingleton(reader);
            }));
        }

        private sealed class FakeFunctionExecutionReportReader : IFunctionExecutionReportReader
        {
            private readonly Dictionary<string, FunctionExecutionReportStorageResult> results;
            private readonly Dictionary<string, FunctionExecutionReport> reports = new Dictionary<string, FunctionExecutionReport>();

            public FakeFunctionExecutionReportReader(Dictionary<string, FunctionExecutionReportStorageResult> results)
            {
                this.results = results;
            }

            public static FunctionExecutionReportStorageResult Success(
                string executionId,
                FunctionExecutionReportOutcome outcome,
                string summary,
                DateTime completedAtUtc,
                long durationMilliseconds = 0)
            {
                return new FunctionExecutionReportStorageResult
                {
                    State = FunctionExecutionReportStorageState.Available,
                    LatestPointer = new FunctionExecutionReportLatestPointer
                    {
                        ExecutionId = executionId,
                        Outcome = outcome,
                        CompletedAtUtc = completedAtUtc,
                        DurationMilliseconds = durationMilliseconds,
                        Summary = summary,
                    },
                };
            }

            public void AddReport(string key, FunctionExecutionReport report) => this.reports[key] = report;

            public Task<FunctionExecutionReportStorageResult> GetLatestAsync(
                FunctionExecutionReportScope scope,
                string functionKey,
                System.Threading.CancellationToken cancellationToken = default)
            {
                this.results.TryGetValue(
                    scope.ScopeKey == "global" ? $"global/{functionKey}" : functionKey,
                    out FunctionExecutionReportStorageResult result);
                return Task.FromResult(result ?? new FunctionExecutionReportStorageResult
                {
                    State = FunctionExecutionReportStorageState.Missing,
                });
            }

            public Task<FunctionExecutionReportStorageResult> GetExecutionAsync(
                FunctionExecutionReportScope scope,
                string functionKey,
                string executionId,
                System.Threading.CancellationToken cancellationToken = default)
            {
                string key = $"{functionKey}/{executionId}";
                if (this.reports.TryGetValue(key, out FunctionExecutionReport report))
                {
                    return Task.FromResult(new FunctionExecutionReportStorageResult
                    {
                        State = FunctionExecutionReportStorageState.Available,
                        Report = report,
                    });
                }

                this.results.TryGetValue(key, out FunctionExecutionReportStorageResult result);
                return Task.FromResult(result ?? new FunctionExecutionReportStorageResult
                {
                    State = FunctionExecutionReportStorageState.Missing,
                });
            }
        }
    }
}
