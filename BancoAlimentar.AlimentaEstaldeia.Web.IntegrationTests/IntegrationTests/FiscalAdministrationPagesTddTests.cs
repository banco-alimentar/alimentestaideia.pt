// -----------------------------------------------------------------------
// <copyright file="FiscalAdministrationPagesTddTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaldeia.Web.IntegrationTests.IntegrationTests
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Reflection;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Web;
    using BancoAlimentar.AlimentaEstaIdeia.Web.TestHost;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Xunit;

    /// <summary>
    /// Defines integration entrypoint contracts for fiscal administration pages.
    /// </summary>
    public sealed class FiscalAdministrationPagesTddTests : IClassFixture<CustomWebApplicationFactory>
    {
        private const string IssuerProfilesPageTypeName = "BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages.Fiscal.IssuerProfilesModel";
        private const string SaftExportPageTypeName = "BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages.Fiscal.SaftExportModel";
        private const string SeriesPageTypeName = "BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages.Fiscal.SeriesModel";
        private readonly CustomWebApplicationFactory factory;

        /// <summary>
        /// Initializes a new instance of the <see cref="FiscalAdministrationPagesTddTests"/> class.
        /// </summary>
        /// <param name="factory">The shared integration host.</param>
        public FiscalAdministrationPagesTddTests(CustomWebApplicationFactory factory)
        {
            this.factory = factory;
        }

        /// <summary>
        /// Fiscal configuration is an Admin-only surface. Anonymous callers cannot create an
        /// issuer profile or communicate/close a fiscal series.
        /// </summary>
        [Theory]
        [InlineData("/Admin/Fiscal/IssuerProfiles?handler=Create")]
        [InlineData("/Admin/Fiscal/Series?handler=Configure")]
        [InlineData("/Admin/Fiscal/Series?handler=Close")]
        [InlineData("/Admin/Fiscal/SaftExport?handler=Export")]
        public async Task FiscalAdministrationPosts_RedirectAnonymousCallersToLogin(string path)
        {
            HttpClient client = this.factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });

            HttpResponseMessage response = await client.PostAsync(path, new FormUrlEncodedContent(new Dictionary<string, string>()));

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        }

        /// <summary>
        /// The fiscal configuration pages use the Admin role policy before exposing any handler.
        /// This keeps a signed-in non-admin outside tenant and food-bank fiscal configuration.
        /// </summary>
        [Fact]
        public void FiscalAdministrationPages_RequireRoleAreaPolicy()
        {
            foreach (Type pageType in new[]
            {
                GetRequiredWebType(IssuerProfilesPageTypeName),
                GetRequiredWebType(SeriesPageTypeName),
                GetRequiredWebType(SaftExportPageTypeName),
            })
            {
                Assert.Contains(
                    pageType.GetCustomAttributes<AuthorizeAttribute>(),
                    attribute => string.Equals(attribute.Policy, "RoleArea", StringComparison.Ordinal));
            }
        }

        /// <summary>
        /// An authorized issuer-profile page binds only non-secret administration input and sends
        /// create and update operations through the fiscal administration service.
        /// </summary>
        [Fact]
        public async Task IssuerProfilesPage_CreatesAndUpdatesThroughFiscalAdministrationService()
        {
            RecordingFiscalAdministrationService recorder = CreateService(out object serviceProxy);
            object page = CreatePage(IssuerProfilesPageTypeName, serviceProxy);
            var request = new FiscalIssuerProfileAdministrationRequest
            {
                LegalName = "Issuer snapshot",
                LegalAddress = "Rua fiscal, 1",
                TaxRegistrationNumber = "501234567",
                SaftDocumentType = "RC",
                TaxRule = "NOR",
                SoftwareCertificateNumber = "1234",
                SigningKeyIdentifier = "key-reference",
                SigningKeyVersion = "3",
                IsActive = true,
            };

            Assert.Null(page.GetType().GetProperty("PrivateKey"));
            Assert.Null(page.GetType().GetProperty("SigningKey"));
            SetRequiredProperty(page, "IssuerProfile", request);
            await InvokeHandlerAsync(page, "OnPostCreateAsync");

            Assert.Same(request, Assert.Single(recorder.CreateRequests));
            request.Id = 17;
            await InvokeHandlerAsync(page, "OnPostUpdateAsync");

            Assert.Same(request, Assert.Single(recorder.UpdateRequests));
        }

        /// <summary>
        /// An authorized series page posts AT communication evidence when configuring a series,
        /// and invokes the explicit close operation rather than an edit/delete shortcut.
        /// </summary>
        [Fact]
        public async Task SeriesPage_ConfiguresAndClosesThroughFiscalAdministrationService()
        {
            RecordingFiscalAdministrationService recorder = CreateService(out object serviceProxy);
            object page = CreatePage(SeriesPageTypeName, serviceProxy);
            var request = new FiscalDocumentSeriesAdministrationRequest
            {
                FiscalIssuerProfileId = 17,
                SeriesCode = "RC 2026",
                SaftDocumentType = "RC",
                ProcessingMethod = FiscalProcessingMethod.CertifiedSoftware,
                FirstSequence = 1,
                NextSequence = 1,
                AtcudValidationCode = "VALIDATION",
                AtCommunicationReference = "AT-communication-42",
                StartsAtUtc = DateTimeOffset.UtcNow,
                Status = FiscalSeriesStatus.Active,
            };

            SetRequiredProperty(page, "Series", request);
            await InvokeHandlerAsync(page, "OnPostConfigureAsync");

            Assert.Same(request, Assert.Single(recorder.SeriesRequests));
            SetRequiredProperty(page, "FiscalDocumentSeriesId", 29);
            await InvokeHandlerAsync(page, "OnPostCloseAsync");

            Assert.Equal(29, Assert.Single(recorder.ClosedSeriesIds));
        }

        /// <summary>
        /// SAF-T page binding chooses issuer and period only. Its handler delegates to the
        /// trusted service and does not accept a caller-supplied tenant identifier.
        /// </summary>
        [Fact]
        public async Task SaftExportPage_SelectsIssuerAndPeriodThroughFiscalAdministrationService()
        {
            RecordingFiscalAdministrationService recorder = CreateService(out object serviceProxy);
            object page = CreatePage(SaftExportPageTypeName, serviceProxy);
            DateOnly startDate = new DateOnly(2026, 1, 1);
            DateOnly endDate = new DateOnly(2026, 1, 31);

            Assert.Null(page.GetType().GetProperty("TenantId"));
            SetRequiredProperty(page, "FiscalIssuerProfileId", 17);
            SetRequiredProperty(page, "StartDate", startDate);
            SetRequiredProperty(page, "EndDate", endDate);
            await InvokeHandlerAsync(page, "OnPostExportAsync");

            Assert.Equal((17, startDate, endDate), Assert.Single(recorder.SaftRequests));
        }

        private static object CreatePage(string typeName, object service)
        {
            Type pageType = GetRequiredWebType(typeName);
            ConstructorInfo constructor = pageType.GetConstructor(new[] { typeof(IFiscalAdministrationService) });
            Assert.NotNull(constructor);
            object page = constructor.Invoke(new[] { service });
            Assert.IsAssignableFrom<PageModel>(page).PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext(),
            };
            return page;
        }

        private static RecordingFiscalAdministrationService CreateService(out object proxy)
        {
            proxy = DispatchProxy.Create<IFiscalAdministrationService, RecordingFiscalAdministrationService>();
            return Assert.IsAssignableFrom<RecordingFiscalAdministrationService>(proxy);
        }

        private static Type GetRequiredWebType(string typeName)
        {
            return Assert.IsAssignableFrom<Type>(typeof(Program).Assembly.GetType(typeName));
        }

        private static async Task InvokeHandlerAsync(object page, string handlerName)
        {
            MethodInfo handler = page.GetType().GetMethod(handlerName, Type.EmptyTypes);
            Assert.NotNull(handler);
            object result = handler.Invoke(page, null);
            if (result is Task task)
            {
                await task;
            }
        }

        private static void SetRequiredProperty(object instance, string propertyName, object value)
        {
            PropertyInfo property = instance.GetType().GetProperty(propertyName);
            Assert.NotNull(property);
            property.SetValue(instance, value);
        }

        private class RecordingFiscalAdministrationService : DispatchProxy
        {
            public IList<FiscalIssuerProfileAdministrationRequest> CreateRequests { get; } = new List<FiscalIssuerProfileAdministrationRequest>();

            public IList<int> ClosedSeriesIds { get; } = new List<int>();

            public IList<(int FiscalIssuerProfileId, DateOnly StartDate, DateOnly EndDate)> SaftRequests { get; } = new List<(int FiscalIssuerProfileId, DateOnly StartDate, DateOnly EndDate)>();

            public IList<FiscalDocumentSeriesAdministrationRequest> SeriesRequests { get; } = new List<FiscalDocumentSeriesAdministrationRequest>();

            public IList<FiscalIssuerProfileAdministrationRequest> UpdateRequests { get; } = new List<FiscalIssuerProfileAdministrationRequest>();

            protected override object Invoke(MethodInfo targetMethod, object[] args)
            {
                if (targetMethod.Name == "CreateIssuerProfile")
                {
                    FiscalIssuerProfileAdministrationRequest request = Assert.IsType<FiscalIssuerProfileAdministrationRequest>(Assert.Single(args));
                    this.CreateRequests.Add(request);
                    return new FiscalIssuerProfile { Id = 17 };
                }

                if (targetMethod.Name == "UpdateIssuerProfile")
                {
                    FiscalIssuerProfileAdministrationRequest request = Assert.IsType<FiscalIssuerProfileAdministrationRequest>(Assert.Single(args));
                    this.UpdateRequests.Add(request);
                    return new FiscalIssuerProfile { Id = request.Id };
                }

                if (targetMethod.Name == "ConfigureSeries")
                {
                    FiscalDocumentSeriesAdministrationRequest request = Assert.IsType<FiscalDocumentSeriesAdministrationRequest>(Assert.Single(args));
                    this.SeriesRequests.Add(request);
                    return new FiscalDocumentSeries { Id = 29 };
                }

                if (targetMethod.Name == "CloseSeries")
                {
                    int id = Assert.IsType<int>(Assert.Single(args));
                    this.ClosedSeriesIds.Add(id);
                    return new FiscalDocumentSeries { Id = id, Status = FiscalSeriesStatus.Closed };
                }

                if (targetMethod.Name == "ExportSaft")
                {
                    this.SaftRequests.Add((
                        Assert.IsType<int>(args[0]),
                        Assert.IsType<DateOnly>(args[1]),
                        Assert.IsType<DateOnly>(args[2])));
                    return "<AuditFile />";
                }

                throw new NotSupportedException($"Unexpected fiscal administration method: {targetMethod.Name}.");
            }
        }
    }
}
