// -----------------------------------------------------------------------
// <copyright file="FiscalDocumentQueryPageTddTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
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
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Web;
    using BancoAlimentar.AlimentaEstaIdeia.Web.TestHost;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Xunit;

    /// <summary>
    /// Defines TDD contracts for the authorized fiscal-document administration list.
    /// </summary>
    public sealed class FiscalDocumentQueryPageTddTests : IClassFixture<CustomWebApplicationFactory>
    {
        private const string DocumentsPageTypeName = "BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages.Fiscal.DocumentsModel";
        private const string QueryRequestTypeName = "BancoAlimentar.AlimentaEstaIdeia.Repository.FiscalDocumentQueryRequest";
        private const string QueryServiceTypeName = "BancoAlimentar.AlimentaEstaIdeia.Repository.IFiscalDocumentQueryService";
        private readonly CustomWebApplicationFactory factory;

        /// <summary>
        /// Initializes a new instance of the <see cref="FiscalDocumentQueryPageTddTests"/> class.
        /// </summary>
        /// <param name="factory">The shared integration host.</param>
        public FiscalDocumentQueryPageTddTests(CustomWebApplicationFactory factory)
        {
            this.factory = factory;
        }

        /// <summary>
        /// The fiscal document list is an Admin-only read surface. Anonymous callers cannot use
        /// a document number, ATCUD, or donation id to enumerate fiscal evidence.
        /// </summary>
        [Fact]
        public async Task FiscalDocumentsPage_RedirectsAnonymousCallersToLogin()
        {
            HttpClient client = this.factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });

            HttpResponseMessage response = await client.GetAsync("/Admin/Fiscal/Documents?Query.FiscalIssuerProfileId=17");

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        }

        /// <summary>
        /// The page requires the Admin role policy before it accepts any fiscal search filters.
        /// </summary>
        [Fact]
        public void FiscalDocumentsPage_RequiresRoleAreaPolicy()
        {
            Type pageType = GetRequiredWebType(DocumentsPageTypeName);

            Assert.Contains(
                pageType.GetCustomAttributes<AuthorizeAttribute>(),
                attribute => string.Equals(attribute.Policy, "RoleArea", StringComparison.Ordinal));
        }

        /// <summary>
        /// The page delegates the complete, non-tenant filter object to the trusted query service.
        /// It never binds a caller-selected tenant or food-bank boundary.
        /// </summary>
        [Fact]
        public async Task FiscalDocumentsPage_DelegatesFiltersThroughTrustedQueryService()
        {
            Type queryServiceType = GetRequiredRepositoryType(QueryServiceTypeName);
            Type requestType = GetRequiredRepositoryType(QueryRequestTypeName);
            RecordingFiscalDocumentQueryService recorder = CreateService(queryServiceType, out object serviceProxy);
            object page = CreatePage(serviceProxy, queryServiceType);
            object request = Activator.CreateInstance(requestType);

            SetRequiredProperty(request, "FiscalIssuerProfileId", 17);
            SetRequiredProperty(request, "FiscalDocumentSeriesId", 29);
            SetRequiredProperty(request, "DocumentNumber", "RC 2026/42");
            SetRequiredProperty(request, "Atcud", "VALIDATION-42");
            SetRequiredProperty(request, "DonationId", 301);
            SetRequiredProperty(request, "StartDate", new DateOnly(2026, 1, 1));
            SetRequiredProperty(request, "EndDate", new DateOnly(2026, 1, 31));
            Assert.Null(request.GetType().GetProperty("TenantId"));
            Assert.Null(request.GetType().GetProperty("FoodBankId"));
            SetRequiredProperty(page, "Query", request);

            await InvokeHandlerAsync(page, "OnGetAsync");

            Assert.Same(request, Assert.Single(recorder.Requests));
        }

        private static RecordingFiscalDocumentQueryService CreateService(Type queryServiceType, out object proxy)
        {
            proxy = DispatchProxy.Create(queryServiceType, typeof(RecordingFiscalDocumentQueryService));
            return Assert.IsAssignableFrom<RecordingFiscalDocumentQueryService>(proxy);
        }

        private static object CreatePage(object queryService, Type queryServiceType)
        {
            Type pageType = GetRequiredWebType(DocumentsPageTypeName);
            ConstructorInfo constructor = pageType.GetConstructor(new[] { queryServiceType });
            Assert.NotNull(constructor);
            object page = constructor.Invoke(new[] { queryService });
            Assert.IsAssignableFrom<PageModel>(page).PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext(),
            };
            return page;
        }

        private static Type GetRequiredRepositoryType(string typeName)
        {
            return Assert.IsAssignableFrom<Type>(typeof(IFiscalScopeResolver).Assembly.GetType(typeName));
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

        private sealed class RecordingFiscalDocumentQueryService : DispatchProxy
        {
            /// <summary>Gets the received filter requests.</summary>
            public IList<object> Requests { get; } = new List<object>();

            /// <inheritdoc/>
            protected override object Invoke(MethodInfo targetMethod, object[] args)
            {
                if (targetMethod.Name != "Query")
                {
                    throw new NotSupportedException($"Unexpected fiscal document query method: {targetMethod.Name}.");
                }

                this.Requests.Add(Assert.Single(args));
                Type resultType = targetMethod.ReturnType.GetGenericArguments().Single();
                Type listType = typeof(List<>).MakeGenericType(resultType);
                return Activator.CreateInstance(listType);
            }
        }
    }
}
