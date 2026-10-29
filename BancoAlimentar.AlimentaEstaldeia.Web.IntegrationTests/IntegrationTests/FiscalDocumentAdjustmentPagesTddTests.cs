// -----------------------------------------------------------------------
// <copyright file="FiscalDocumentAdjustmentPagesTddTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaldeia.Web.IntegrationTests.IntegrationTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Reflection;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Testing.Common;
    using BancoAlimentar.AlimentaEstaIdeia.Web;
    using BancoAlimentar.AlimentaEstaIdeia.Web.TestHost;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;

    /// <summary>
    /// Defines the authorized browser contracts for fiscal document cancellation and correction.
    /// </summary>
    public sealed class FiscalDocumentAdjustmentPagesTddTests : IClassFixture<CustomWebApplicationFactory>
    {
        private const string AdminEmail = "fiscal-adjustment-admin@integration.test";
        private const string NonAdminEmail = "fiscal-adjustment-user@integration.test";
        private const string Password = IntegrationTestCredentials.DefaultPassword;
        private const string CancellationPageTypeName = "BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages.Fiscal.CancellationModel";
        private const string CorrectionPageTypeName = "BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages.Fiscal.CorrectionModel";
        private readonly CustomWebApplicationFactory factory;

        /// <summary>
        /// Initializes a new instance of the <see cref="FiscalDocumentAdjustmentPagesTddTests"/> class.
        /// </summary>
        /// <param name="factory">The shared web application factory.</param>
        public FiscalDocumentAdjustmentPagesTddTests(CustomWebApplicationFactory factory)
        {
            this.factory = factory;
        }

        /// <summary>
        /// Anonymous callers cannot submit a fiscal cancellation or correction request.
        /// </summary>
        /// <param name="path">The protected fiscal adjustment endpoint.</param>
        /// <returns>A task representing the assertion.</returns>
        [Theory]
        [InlineData("/Admin/Fiscal/Cancellation?handler=Cancel")]
        [InlineData("/Admin/Fiscal/Correction?handler=Correct")]
        public async Task FiscalAdjustmentPosts_RedirectAnonymousCallersToLogin(string path)
        {
            HttpClient client = this.factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });

            HttpResponseMessage response = await client.PostAsync(path, new FormUrlEncodedContent(new Dictionary<string, string>()));

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        }

        /// <summary>
        /// A signed-in user without the Admin-area role cannot access either adjustment page.
        /// The RoleArea policy, rather than request data, controls this boundary.
        /// </summary>
        /// <param name="path">The protected fiscal adjustment endpoint.</param>
        /// <returns>A task representing the assertion.</returns>
        [Theory]
        [InlineData("/Admin/Fiscal/Cancellation?handler=Cancel")]
        [InlineData("/Admin/Fiscal/Correction?handler=Correct")]
        public async Task FiscalAdjustmentPosts_RejectNonAdminCallersThroughRoleAreaPolicy(string path)
        {
            using (IServiceScope scope = this.factory.Services.CreateScope())
            {
                await IntegrationTestDataSeeder.EnsureUserAsync(scope.ServiceProvider, NonAdminEmail, Password);
            }

            HttpClient client = await WebTestAuthHelper.CreateAuthenticatedClientAsync(
                this.factory,
                NonAdminEmail,
                Password,
                allowAutoRedirect: false);

            HttpResponseMessage response = await client.PostAsync(path, new FormUrlEncodedContent(new Dictionary<string, string>()));

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Contains("AccessDenied", response.Headers.Location?.OriginalString, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Both fiscal adjustment pages declare the Admin-area policy before exposing a handler.
        /// </summary>
        [Fact]
        public void FiscalAdjustmentPages_RequireRoleAreaPolicy()
        {
            foreach (Type pageType in new[]
            {
                GetRequiredWebType(CancellationPageTypeName),
                GetRequiredWebType(CorrectionPageTypeName),
            })
            {
                Assert.Contains(
                    pageType.GetCustomAttributes<AuthorizeAttribute>(),
                    attribute => string.Equals(attribute.Policy, "RoleArea", StringComparison.Ordinal));
            }
        }

        /// <summary>
        /// Cancellation binds only a document identifier and statutory motive, then delegates to
        /// the trusted cancellation service. Tenant, issuer, scope, and actor come from services.
        /// </summary>
        [Fact]
        public async Task CancellationPage_DelegatesOnlySafeRequestDataThroughTrustedService()
        {
            RecordingCancellationService recorder = CreateCancellationService(out object serviceProxy);
            object page = CreatePage(CancellationPageTypeName, typeof(IFiscalDocumentCancellationService), serviceProxy);
            var request = new FiscalDocumentCancellationRequest
            {
                FiscalDocumentId = 41,
                Motive = "Duplicated payment",
            };

            AssertRequestProperties(request, "FiscalDocumentId", "Motive");
            AssertNoCallerControlledScope(page);
            SetRequiredProperty(page, "Cancellation", request);

            IActionResult result = Assert.IsAssignableFrom<IActionResult>(await InvokeHandlerAsync(page, "OnPostCancelAsync"));

            Assert.Same(request, Assert.Single(recorder.Requests));
            Assert.IsType<RedirectToPageResult>(result);
        }

        /// <summary>
        /// Correction binds only the original document identifier, statutory motive, and durable
        /// idempotency key. It delegates scope and actor resolution to the trusted service and
        /// exposes the returned correction for the authorized page to display.
        /// </summary>
        [Fact]
        public async Task CorrectionPage_DelegatesSafeRequestAndDisplaysCreatedCorrection()
        {
            var expected = new FiscalDocument
            {
                Id = 42,
                DocumentNumber = "RC 2026/42",
                Atcud = "VALIDATION-42",
                Status = FiscalDocumentStatus.Issued,
            };
            RecordingCorrectionService recorder = CreateCorrectionService(expected, out object serviceProxy);
            object page = CreatePage(CorrectionPageTypeName, typeof(IFiscalDocumentCorrectionService), serviceProxy);
            var request = new FiscalDocumentCorrectionRequest
            {
                OriginalFiscalDocumentId = 41,
                IdempotencyKey = "fiscal-correction-41",
                Motive = "Correct buyer tax number",
            };

            AssertRequestProperties(request, "OriginalFiscalDocumentId", "IdempotencyKey", "Motive");
            AssertNoCallerControlledScope(page);
            SetRequiredProperty(page, "Correction", request);

            IActionResult result = Assert.IsAssignableFrom<IActionResult>(await InvokeHandlerAsync(page, "OnPostCorrectAsync"));

            Assert.Same(request, Assert.Single(recorder.Requests));
            Assert.Same(expected, GetRequiredProperty(page, "CorrectionDocument"));
            Assert.IsType<PageResult>(result);
        }

        private static void AssertNoCallerControlledScope(object page)
        {
            foreach (string propertyName in new[]
            {
                "TenantId",
                "FiscalIssuerProfileId",
                "FiscalDocumentSeriesId",
                "SeriesId",
                "FoodBankId",
                "FiscalScope",
                "ActorId",
                "UserId",
            })
            {
                Assert.Null(page.GetType().GetProperty(propertyName));
            }
        }

        private static void AssertRequestProperties(object request, params string[] expectedPropertyNames)
        {
            string[] actualPropertyNames = request.GetType().GetProperties()
                .Select(property => property.Name)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();
            string[] expected = expectedPropertyNames.OrderBy(name => name, StringComparer.Ordinal).ToArray();

            Assert.Equal(expected, actualPropertyNames);
        }

        private static object CreatePage(string pageTypeName, Type serviceType, object service)
        {
            Type pageType = GetRequiredWebType(pageTypeName);
            ConstructorInfo constructor = pageType.GetConstructor(new[] { serviceType });
            Assert.NotNull(constructor);
            object page = constructor.Invoke(new[] { service });
            Assert.IsAssignableFrom<PageModel>(page).PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext(),
            };
            return page;
        }

        private static RecordingCancellationService CreateCancellationService(out object proxy)
        {
            proxy = DispatchProxy.Create<IFiscalDocumentCancellationService, RecordingCancellationService>();
            return Assert.IsAssignableFrom<RecordingCancellationService>(proxy);
        }

        private static RecordingCorrectionService CreateCorrectionService(FiscalDocument correction, out object proxy)
        {
            proxy = DispatchProxy.Create<IFiscalDocumentCorrectionService, RecordingCorrectionService>();
            RecordingCorrectionService recorder = Assert.IsAssignableFrom<RecordingCorrectionService>(proxy);
            recorder.Correction = correction;
            return recorder;
        }

        private static Type GetRequiredWebType(string typeName)
        {
            return Assert.IsAssignableFrom<Type>(typeof(Program).Assembly.GetType(typeName));
        }

        private static object GetRequiredProperty(object instance, string propertyName)
        {
            PropertyInfo property = instance.GetType().GetProperty(propertyName);
            Assert.NotNull(property);
            return property.GetValue(instance);
        }

        private static async Task<object> InvokeHandlerAsync(object page, string handlerName)
        {
            MethodInfo handler = page.GetType().GetMethod(handlerName, Type.EmptyTypes);
            Assert.NotNull(handler);
            object invocationResult = handler.Invoke(page, null);
            if (invocationResult is not Task task)
            {
                return invocationResult;
            }

            await task;
            return task.GetType().GetProperty("Result")?.GetValue(task);
        }

        private static void SetRequiredProperty(object instance, string propertyName, object value)
        {
            PropertyInfo property = instance.GetType().GetProperty(propertyName);
            Assert.NotNull(property);
            property.SetValue(instance, value);
        }

        private class RecordingCancellationService : DispatchProxy
        {
            public IList<FiscalDocumentCancellationRequest> Requests { get; } = new List<FiscalDocumentCancellationRequest>();

            protected override object Invoke(MethodInfo targetMethod, object[] args)
            {
                Assert.Equal("Cancel", targetMethod.Name);
                FiscalDocumentCancellationRequest request = Assert.IsType<FiscalDocumentCancellationRequest>(Assert.Single(args));
                this.Requests.Add(request);
                return new FiscalDocument { Id = request.FiscalDocumentId, Status = FiscalDocumentStatus.Cancelled };
            }
        }

        private class RecordingCorrectionService : DispatchProxy
        {
            public FiscalDocument Correction { get; set; }

            public IList<FiscalDocumentCorrectionRequest> Requests { get; } = new List<FiscalDocumentCorrectionRequest>();

            protected override object Invoke(MethodInfo targetMethod, object[] args)
            {
                Assert.Equal("CreateCorrection", targetMethod.Name);
                FiscalDocumentCorrectionRequest request = Assert.IsType<FiscalDocumentCorrectionRequest>(Assert.Single(args));
                this.Requests.Add(request);
                return this.Correction;
            }
        }
    }
}
