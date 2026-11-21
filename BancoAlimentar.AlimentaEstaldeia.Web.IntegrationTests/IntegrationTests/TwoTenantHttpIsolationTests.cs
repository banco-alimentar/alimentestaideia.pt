// -----------------------------------------------------------------------
// <copyright file="TwoTenantHttpIsolationTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
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
    using System.Net.Http.Json;
    using System.Security.Claims;
    using System.Threading;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration.Options;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.StaticFileProvider;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Web;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Services;
    using BancoAlimentar.AlimentaEstaIdeia.Web.TestHost;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authentication.Google;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.AspNetCore.TestHost;
    using Microsoft.AspNetCore.WebUtilities;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Diagnostics;
    using Microsoft.EntityFrameworkCore.Storage;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Microsoft.Extensions.FileProviders;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Options;
    using Xunit;

    /// <summary>
    /// Exercises tenant boundaries through the production HTTP middleware pipeline.
    /// </summary>
    public class TwoTenantHttpIsolationTests
    {
        private const string ProbePath = "/__two-tenant-probe";
        private const string CssPath = "/css/two-tenant-isolation.css";
        private static readonly string[] Domains = { "tenant-alpha.invalid", "tenant-beta.invalid" };

        /// <summary>
        /// Alternating concurrent hosts retain their configuration, database writes, and cached assets.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task ConcurrentHosts_IsolateConfigurationDatabaseWritesAndCachedCss()
        {
            using IsolationHost host = new IsolationHost();
            using HttpClient client = host.CreateClient();

            await Task.WhenAll(Enumerable.Range(0, 12).Select(async index =>
            {
                int tenantIndex = index % 2;
                string marker = $"tenant-{tenantIndex}-write-{index}";
                using HttpResponseMessage write = await client.PostAsync(
                    Address(tenantIndex, $"{ProbePath}/configuration?write={marker}"),
                    null);
                Assert.Equal(HttpStatusCode.OK, write.StatusCode);
                ProbeResult snapshot = await ReadProbeAsync(write);
                AssertConfiguration(snapshot, tenantIndex, host.DatabaseNames[tenantIndex]);

                using HttpResponseMessage css = await client.GetAsync(Address(tenantIndex, CssPath));
                Assert.Equal(HttpStatusCode.OK, css.StatusCode);
                Assert.Equal("text/css", css.Content.Headers.ContentType?.MediaType);
                Assert.Equal(host.CssContents[tenantIndex], await css.Content.ReadAsStringAsync());
            }));

            await Task.WhenAll(Enumerable.Range(0, 8).Select(async index =>
            {
                int tenantIndex = index % 2;
                using HttpResponseMessage response = await client.GetAsync(Address(tenantIndex, $"{ProbePath}/configuration"));
                ProbeResult snapshot = await ReadProbeAsync(response);
                AssertConfiguration(snapshot, tenantIndex, host.DatabaseNames[tenantIndex]);
                Assert.Equal(
                    Enumerable.Range(0, 12)
                        .Where(value => value % 2 == tenantIndex)
                        .Select(value => $"tenant-{tenantIndex}-write-{value}")
                        .OrderBy(value => value, StringComparer.Ordinal),
                    snapshot.Writes.OrderBy(value => value, StringComparer.Ordinal));
            }));
        }

        /// <summary>
        /// Real Identity application cookies authenticate only on their issuing tenant.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task ApplicationCookies_AreAcceptedOnOriginAndRejectedOnOtherTenant()
        {
            using IsolationHost host = new IsolationHost();
            using HttpClient client = host.CreateClient();
            string[] cookies = await Task.WhenAll(Enumerable.Range(0, 2).Select(async tenantIndex =>
            {
                using HttpResponseMessage response = await client.PostAsync(Address(tenantIndex, $"{ProbePath}/sign-in"), null);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                return GetCookies(response);
            }));

            await Task.WhenAll(Enumerable.Range(0, 12).Select(async index =>
            {
                int origin = index % 2;
                int destination = (index / 2) % 2;
                using HttpRequestMessage request = new HttpRequestMessage(
                    HttpMethod.Get,
                    Address(destination, $"{ProbePath}/principal"));
                request.Headers.Add("Cookie", cookies[origin]);
                using HttpResponseMessage response = await client.SendAsync(request);
                ProbeResult snapshot = await ReadProbeAsync(response);

                Assert.Equal(destination + 1, snapshot.TenantId);
                Assert.Equal(origin == destination, snapshot.Authenticated);
                Assert.Equal(origin == destination ? (origin + 1).ToString() : null, snapshot.CookieTenant);
            }));
        }

        /// <summary>
        /// OAuth callback state and its correlation cookie cannot be replayed on the other host.
        /// </summary>
        /// <returns>A task representing the asynchronous test.</returns>
        [Fact]
        public async Task OAuthCallbacks_RejectCrossTenantStateWithoutCallingExternalProvider()
        {
            using IsolationHost host = new IsolationHost();
            using HttpClient client = host.CreateClient();
            OAuthChallenge[] challenges = await Task.WhenAll(Enumerable.Range(0, 2).Select(async tenantIndex =>
            {
                using HttpResponseMessage response = await client.GetAsync(Address(tenantIndex, $"{ProbePath}/challenge"));
                Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
                Assert.NotNull(response.Headers.Location);
                var query = QueryHelpers.ParseQuery(response.Headers.Location.Query);
                Assert.Equal($"tenant-{tenantIndex}-google-id", query["client_id"].ToString());
                Assert.Equal(Address(tenantIndex, "/signin-google").AbsoluteUri, query["redirect_uri"].ToString());
                Assert.False(string.IsNullOrEmpty(query["state"].ToString()));
                return new OAuthChallenge(query["state"].ToString(), GetCookies(response));
            }));

            Assert.NotEqual(challenges[0].State, challenges[1].State);
            await Task.WhenAll(Enumerable.Range(0, 4).Select(async index =>
            {
                int origin = index % 2;
                int destination = index / 2;
                string callback = QueryHelpers.AddQueryString(
                    "/signin-google",
                    new Dictionary<string, string>
                    {
                        ["state"] = challenges[origin].State,
                        ["error"] = "access_denied",
                    });
                using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, Address(destination, callback));
                request.Headers.Add("Cookie", challenges[origin].Cookies);
                using HttpResponseMessage response = await client.SendAsync(request);
                ProbeResult result = await response.Content.ReadFromJsonAsync<ProbeResult>();
                Assert.NotNull(result);
                Assert.Equal(destination + 1, result.TenantId);
                if (origin == destination)
                {
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    Assert.True(result.CallbackStateAccepted);
                    Assert.Null(result.Error);
                }
                else
                {
                    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
                    Assert.False(result.CallbackStateAccepted);
                    Assert.Contains("state", result.Error, StringComparison.OrdinalIgnoreCase);
                }
            }));

            Assert.Equal(0, host.BackchannelCalls);
        }

        private static Uri Address(int tenantIndex, string path)
        {
            return new Uri($"https://{Domains[tenantIndex]}{path}");
        }

        private static string GetCookies(HttpResponseMessage response)
        {
            Assert.True(response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string> values));
            string cookies = string.Join("; ", values.Select(value => value.Split(';')[0]));
            Assert.False(string.IsNullOrEmpty(cookies));
            return cookies;
        }

        private static async Task<ProbeResult> ReadProbeAsync(HttpResponseMessage response)
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            ProbeResult result = await response.Content.ReadFromJsonAsync<ProbeResult>();
            Assert.NotNull(result);
            return result;
        }

        private static void AssertConfiguration(ProbeResult result, int tenantIndex, string databaseName)
        {
            Assert.Equal(tenantIndex + 1, result.TenantId);
            Assert.Equal(Domains[tenantIndex], result.Domain);
            Assert.Equal($"tenant-{tenantIndex}-secret", result.Scalar);
            Assert.Equal($"tenant-{tenantIndex}-section", result.SectionValue);
            Assert.Equal($"tenant-{tenantIndex}-database", result.DatabaseValue);
            Assert.Equal("host-fallback", result.HostFallback);
            Assert.Equal(databaseName, result.ConnectionString);
            Assert.Equal($"tenant-{tenantIndex}-google-id", result.GoogleClientId);
        }

        private sealed class IsolationHost : IDisposable
        {
            private readonly string fixtureRoot;
            private readonly InMemoryDatabaseRoot databaseRoot = new InMemoryDatabaseRoot();
            private readonly CustomWebApplicationFactory baseFactory;
            private readonly WebApplicationFactory<Program> factory;
            private readonly PhysicalFileProvider[] caches;
            private readonly RejectingBackchannel backchannelHandler = new RejectingBackchannel();
            private readonly HttpClient backchannel;

            public IsolationHost()
            {
                string fixtureId = Guid.NewGuid().ToString("N");
                this.fixtureRoot = Path.GetFullPath(Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "two-tenant-http-fixtures",
                    fixtureId));
                this.DatabaseNames = new[] { $"{fixtureId}-alpha", $"{fixtureId}-beta" };
                this.CssContents = new[] { "body { --tenant: alpha; }", "body { --tenant: beta; }" };
                Tenant[] tenants = Enumerable.Range(0, 2).Select(index => new Tenant
                {
                    Id = index + 1,
                    Name = Domains[index],
                    PublicId = Guid.NewGuid(),
                    Created = DateTime.UtcNow,
                    Domains = new List<DomainIdentifier>
                    {
                        new DomainIdentifier
                        {
                            DomainName = Domains[index],
                            Environment = Environments.Development,
                            Created = DateTime.UtcNow,
                        },
                    },
                }).ToArray();
                this.caches = Enumerable.Range(0, 2).Select(index =>
                {
                    // Production's cache root is process-owned and outside the checkout. Supply
                    // both local fixture caches; the real static provider must select the tenant.
                    Assert.False(Directory.Exists(StaticFileConfigurationManager.GetTenantLocalCacheRootPath(tenants[index].PublicId)));
                    string cacheRoot = Path.Combine(this.fixtureRoot, index.ToString());
                    string cssDirectory = Path.Combine(cacheRoot, "wwwroot", "css");
                    Directory.CreateDirectory(cssDirectory);
                    File.WriteAllText(Path.Combine(cssDirectory, "two-tenant-isolation.css"), this.CssContents[index]);
                    return new PhysicalFileProvider(cacheRoot);
                }).ToArray();
                this.backchannel = new HttpClient(this.backchannelHandler);
                var infrastructureOptions = new DbContextOptionsBuilder<InfrastructureDbContext>()
                    .UseInMemoryDatabase($"{fixtureId}-registry", this.databaseRoot).Options;
                using (InfrastructureDbContext registry = new InfrastructureDbContext(infrastructureOptions))
                {
                    registry.Database.EnsureCreated();
                    registry.Tenants.AddRange(tenants);
                    registry.SaveChanges();
                }

                var secrets = Enumerable.Range(0, 2).ToDictionary(index => index + 1, index => new Dictionary<string, string>
                {
                    ["ConnectionStrings:DefaultConnection"] = this.DatabaseNames[index],
                    ["Isolation:Scalar"] = $"tenant-{index}-secret",
                    ["Isolation:Nested:Value"] = $"tenant-{index}-section",
                    ["Isolation:Database"] = "secret-overridden-by-database",
                    ["Authentication:Google:ClientId"] = $"tenant-{index}-google-id",
                    ["Authentication:Google:ClientSecret"] = $"tenant-{index}-synthetic-secret",
                });
                for (int index = 0; index < 2; index++)
                {
                    using ApplicationDbContext context = this.CreateDatabase(this.DatabaseNames[index]);
                    context.Database.EnsureCreated();
                    context.Configurations.Add(new Configuration
                    {
                        Name = "Isolation:Database",
                        Value = $"tenant-{index}-database",
                        Created = DateTime.UtcNow,
                    });
                    context.SaveChanges();
                }

                this.baseFactory = new CustomWebApplicationFactory();
                this.factory = this.baseFactory.WithWebHostBuilder(builder =>
                {
                    builder.ConfigureAppConfiguration((_, configuration) =>
                        configuration.AddInMemoryCollection(new Dictionary<string, string>
                        {
                            ["Isolation:Scalar"] = "host-scalar",
                            ["Isolation:Nested:Value"] = "host-section",
                            ["Isolation:HostFallback"] = "host-fallback",
                            ["Authentication:Google:ClientId"] = "host-wide-synthetic-google-id",
                            ["Authentication:Google:ClientSecret"] = "host-wide-synthetic-google-secret",
                            ["AzureStorage:ConnectionString"] = "UseDevelopmentStorage=true",
                            ["AzureStorage:FoodBankSourceBlobName"] = string.Empty,
                            ["AzureStorage:FoodBankSourceContainerName"] = string.Empty,
                            ["LocalEmailCapture:Enabled"] = bool.FalseString,
                        }));
                    builder.ConfigureTestServices(services =>
                    {
                        services.RemoveAll<InfrastructureDbContext>();
                        services.AddScoped(_ => new InfrastructureDbContext(infrastructureOptions));
                        services.RemoveAll<ApplicationDbContext>();
                        services.AddScoped(provider => this.CreateDatabase(
                            provider.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection")));
                        services.RemoveAll<IKeyVaultConfigurationManager>();
                        services.AddSingleton<IKeyVaultConfigurationManager>(new SyntheticConfigurationManager(secrets));
                        foreach (ServiceDescriptor descriptor in services.Where(descriptor =>
                            descriptor.ServiceType == typeof(IHostedService)
                            && descriptor.ImplementationType == typeof(RuntimeCacheInvalidationHostedService)).ToArray())
                        {
                            services.Remove(descriptor);
                        }

                        services.AddSingleton<IStartupFilter>(new ProbeStartupFilter(tenants, this.caches));
                        services.PostConfigure<GoogleOptions>(GoogleDefaults.AuthenticationScheme, options =>
                        {
                            options.Backchannel = this.backchannel;
                            options.Events.OnAccessDenied = async context =>
                            {
                                context.HandleResponse();
                                await context.Response.WriteAsJsonAsync(new ProbeResult
                                {
                                    TenantId = context.HttpContext.GetTenant().Id,
                                    CallbackStateAccepted = true,
                                });
                            };
                            options.Events.OnRemoteFailure = async context =>
                            {
                                context.HandleResponse();
                                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                                await context.Response.WriteAsJsonAsync(new ProbeResult
                                {
                                    TenantId = context.HttpContext.GetTenant().Id,
                                    Error = context.Failure?.Message,
                                });
                            };
                        });
                    });
                });
            }

            public string[] DatabaseNames { get; }

            public string[] CssContents { get; }

            public int BackchannelCalls => this.backchannelHandler.Calls;

            public HttpClient CreateClient()
            {
                return this.factory.CreateClient(new WebApplicationFactoryClientOptions
                {
                    AllowAutoRedirect = false,
                    HandleCookies = false,
                    BaseAddress = Address(0, "/"),
                });
            }

            public void Dispose()
            {
                this.factory.Dispose();
                this.baseFactory.Dispose();
                this.backchannel.Dispose();
                foreach (PhysicalFileProvider cache in this.caches)
                {
                    cache.Dispose();
                }

                Directory.Delete(this.fixtureRoot, recursive: true);
            }

            private ApplicationDbContext CreateDatabase(string databaseName)
            {
                Assert.Contains(databaseName, this.DatabaseNames);
                return new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseInMemoryDatabase(databaseName, this.databaseRoot)
                    .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                    .Options);
            }
        }

        private sealed class ProbeStartupFilter : IStartupFilter
        {
            private readonly Tenant[] tenants;
            private readonly PhysicalFileProvider[] caches;

            public ProbeStartupFilter(Tenant[] tenants, PhysicalFileProvider[] caches)
            {
                this.tenants = tenants;
                this.caches = caches;
            }

            public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
            {
                return app =>
                {
                    app.Use(async (context, continuation) =>
                    {
                        for (int index = 0; index < this.tenants.Length; index++)
                        {
                            string cacheKey = $"__blob_client_static_file_provider-file.provider-{this.tenants[index].NormalizedName}";
                            context.Items[cacheKey] = this.caches[index];
                        }

                        await continuation();
                    });
                    next(app);
                    app.Run(ProbeAsync);
                };
            }

            private static async Task ProbeAsync(HttpContext context)
            {
                if (context.Request.Path == $"{ProbePath}/sign-in")
                {
                    ClaimsPrincipal principal = new ClaimsPrincipal(new ClaimsIdentity(
                        new[] { new Claim(ClaimTypes.NameIdentifier, "same-synthetic-user") },
                        IdentityConstants.ApplicationScheme));
                    await context.SignInAsync(IdentityConstants.ApplicationScheme, principal);
                    await context.Response.WriteAsJsonAsync(new ProbeResult { TenantId = context.GetTenant().Id });
                }
                else if (context.Request.Path == $"{ProbePath}/principal")
                {
                    await context.Response.WriteAsJsonAsync(new ProbeResult
                    {
                        TenantId = context.GetTenant().Id,
                        Authenticated = context.User.Identity?.IsAuthenticated == true,
                        CookieTenant = context.User.FindFirst(TenantCookieEvents.TenantClaimType)?.Value,
                    });
                }
                else if (context.Request.Path == $"{ProbePath}/challenge")
                {
                    await context.ChallengeAsync(GoogleDefaults.AuthenticationScheme, new AuthenticationProperties
                    {
                        RedirectUri = $"{ProbePath}/principal",
                    });
                }
                else if (context.Request.Path == $"{ProbePath}/configuration")
                {
                    ApplicationDbContext database = context.RequestServices.GetRequiredService<ApplicationDbContext>();
                    if (HttpMethods.IsPost(context.Request.Method))
                    {
                        string marker = context.Request.Query["write"].ToString();
                        database.Configurations.Add(new Configuration
                        {
                            Name = $"Isolation:Write:{marker}",
                            Value = marker,
                            Created = DateTime.UtcNow,
                        });
                        await database.SaveChangesAsync(context.RequestAborted);
                    }

                    // Yield before reading scoped configuration to expose accidental host-wide state.
                    await Task.Yield();
                    IConfiguration configuration = context.RequestServices.GetRequiredService<IConfiguration>();
                    GoogleOptions google = context.RequestServices.GetRequiredService<IOptionsMonitor<GoogleOptions>>()
                        .Get(GoogleDefaults.AuthenticationScheme);
                    await context.Response.WriteAsJsonAsync(new ProbeResult
                    {
                        TenantId = context.GetTenant().Id,
                        Domain = context.GetTenant().CurrentDomain.DomainName,
                        Scalar = configuration["Isolation:Scalar"],
                        SectionValue = configuration.GetSection("Isolation:Nested")["Value"],
                        DatabaseValue = configuration["Isolation:Database"],
                        HostFallback = configuration["Isolation:HostFallback"],
                        ConnectionString = configuration.GetConnectionString("DefaultConnection"),
                        GoogleClientId = google.ClientId,
                        Writes = await database.Configurations
                            .Where(item => item.Name.StartsWith("Isolation:Write:"))
                            .Select(item => item.Value)
                            .ToArrayAsync(context.RequestAborted),
                    });
                }
                else
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                }
            }
        }

        private sealed class SyntheticConfigurationManager : IKeyVaultConfigurationManager
        {
            private readonly Dictionary<int, Dictionary<string, string>> configurations;

            public SyntheticConfigurationManager(Dictionary<int, Dictionary<string, string>> configurations)
            {
                this.configurations = configurations;
            }

            public bool LoadTenantConfiguration()
            {
                return true;
            }

            public Task<bool> LoadTenantConfigurationAsync(CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Task.FromResult(true);
            }

            public Task<bool> EnsureTenantConfigurationLoaded(int tenantId, TenantDevelopmentOptions developmentOptions)
            {
                return this.EnsureTenantConfigurationLoaded(tenantId, developmentOptions, CancellationToken.None);
            }

            public Task<bool> EnsureTenantConfigurationLoaded(
                int tenantId,
                TenantDevelopmentOptions developmentOptions,
                CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Assert.True(this.configurations.ContainsKey(tenantId));
                return Task.FromResult(false);
            }

            public Dictionary<string, string> GetTenantConfiguration(int tenantId)
            {
                return this.configurations.TryGetValue(tenantId, out Dictionary<string, string> configuration)
                    ? configuration
                    : null;
            }

            public KeyVaultConfigurationLoadDiagnostics GetLastLoadDiagnostics()
            {
                return null;
            }
        }

        private sealed class RejectingBackchannel : HttpMessageHandler
        {
            private int calls;

            public int Calls => Volatile.Read(ref this.calls);

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Interlocked.Increment(ref this.calls);
                throw new InvalidOperationException("The tenant isolation tests must not contact an external authentication provider.");
            }
        }

        private sealed class OAuthChallenge
        {
            public OAuthChallenge(string state, string cookies)
            {
                this.State = state;
                this.Cookies = cookies;
            }

            public string State { get; }

            public string Cookies { get; }
        }

        private sealed class ProbeResult
        {
            public int TenantId { get; set; }

            public string Domain { get; set; }

            public string Scalar { get; set; }

            public string SectionValue { get; set; }

            public string DatabaseValue { get; set; }

            public string HostFallback { get; set; }

            public string ConnectionString { get; set; }

            public string GoogleClientId { get; set; }

            public string[] Writes { get; set; } = Array.Empty<string>();

            public bool Authenticated { get; set; }

            public string CookieTenant { get; set; }

            public bool CallbackStateAccepted { get; set; }

            public string Error { get; set; }
        }
    }
}
