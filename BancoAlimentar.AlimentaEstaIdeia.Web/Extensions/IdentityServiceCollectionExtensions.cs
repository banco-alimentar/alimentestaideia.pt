// -----------------------------------------------------------------------
// <copyright file="IdentityServiceCollectionExtensions.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Extensions
{
    using System;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration.Authentication;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Middleware;
    using BancoAlimentar.AlimentaEstaIdeia.Web.JsonConverter;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Services;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authentication.Cookies;
    using Microsoft.AspNetCore.Authentication.Facebook;
    using Microsoft.AspNetCore.Authentication.Google;
    using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
    using Microsoft.AspNetCore.Authentication.Twitter;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Identity.UI.Services;
    using Microsoft.AspNetCore.Mvc.ApplicationModels;
    using Microsoft.AspNetCore.Mvc.Authorization;
    using Microsoft.AspNetCore.Mvc.Razor;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Registers identity, authentication, and authorization services.
    /// </summary>
    public static class IdentityServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the web application's identity and authorization services.
        /// </summary>
        /// <param name="services">The collection that receives identity registrations.</param>
        /// <param name="configuration">The host configuration.</param>
        /// <returns>The configured identity service collection.</returns>
        public static IServiceCollection AddWebIdentity(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            AddExternalAuthentication(services, configuration);
            services.AddControllersWithViews().AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.Converters.Add(new GuidConverter());
            });
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddDatabaseDeveloperPageExceptionFilter();
            services.AddDefaultIdentity<WebUser>(options =>
            {
                options.SignIn.RequireConfirmedAccount = true;
                options.User.RequireUniqueEmail = true;
            })
                .AddRoles<ApplicationRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();
            services.ConfigureApplicationCookie(options =>
            {
                TenantCookieEvents tenantCookieEvents = new TenantCookieEvents();
                Func<CookieSigningInContext, Task> existingSigningIn = options.Events.OnSigningIn;
                Func<CookieValidatePrincipalContext, Task> existingValidatePrincipal = options.Events.OnValidatePrincipal;

                options.Events.OnSigningIn = async context =>
                {
                    await tenantCookieEvents.SigningIn(context);
                    if (existingSigningIn != null)
                    {
                        await existingSigningIn(context);
                    }
                };

                options.Events.OnValidatePrincipal = async context =>
                {
                    if (existingValidatePrincipal != null)
                    {
                        await existingValidatePrincipal(context);
                    }

                    if (context.Principal != null)
                    {
                        await tenantCookieEvents.ValidatePrincipal(context);
                    }
                };
            });
            services.AddTransient<IEmailSender, EmailSender>();
            services.AddScoped<ReferralImageService>();
            services.AddScoped<ReferralQrCodeService>();
            services.AddScoped<UserLoginReportRepository>();
            services.AddScoped<UserLoginTrackingService>();
            services.AddScoped<AccountMergeService>();
            services.AddRazorPages(options =>
            {
                options.Conventions.AuthorizeAreaFolder("Admin", "/", "AdminArea");
                options.Conventions.AuthorizeAreaFolder("RoleManagement", "/", "RoleArea");
                ConfigureSuperAdminOnlyAdminPages(options.Conventions);
            }).AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix).AddDataAnnotationsLocalization();

            services.AddAuthorization(options =>
            {
                string[] authenticationSchemes =
                {
                    "Identity.Application",
                    "Identity.External",
                    "Identity.TwoFactorUserId",
                    "Google",
                    "Microsoft",
                };
                options.AddPolicy(
                    "AdminArea",
                    new AuthorizationPolicyBuilder(authenticationSchemes)
                        .RequireAuthenticatedUser()
                        .RequireRole("Admin", "Manager")
                        .Build());
                options.AddPolicy(
                    "RoleArea",
                    new AuthorizationPolicyBuilder(authenticationSchemes)
                        .RequireAuthenticatedUser()
                        .RequireRole("SuperAdmin")
                        .Build());
            });
            return services;
        }

        private static void AddExternalAuthentication(
            IServiceCollection services,
            IConfiguration configuration)
        {
            AuthenticationBuilder authenticationBuilder = services.AddAuthentication();
            if (!string.IsNullOrEmpty(configuration["Authentication:Google:ClientId"]) &&
                !string.IsNullOrEmpty(configuration["Authentication:Google:ClientSecret"]))
            {
                authenticationBuilder.AddGoogle();
                services.AddScoped<IPostConfigureOptions<GoogleOptions>, GooglePostConfigureOptions>();
            }

            if (!string.IsNullOrEmpty(configuration["Authentication:Facebook:AppId"]) &&
                !string.IsNullOrEmpty(configuration["Authentication:Facebook:AppSecret"]))
            {
                authenticationBuilder.AddFacebook();
                services.AddScoped<IPostConfigureOptions<FacebookOptions>, FacebookPostConfigureOptions>();
            }

            if (!string.IsNullOrEmpty(configuration["Authentication:Microsoft:ClientId"]) &&
                !string.IsNullOrEmpty(configuration["Authentication:Microsoft:ClientSecret"]))
            {
                authenticationBuilder.AddMicrosoftAccount();
                services.AddScoped<IPostConfigureOptions<MicrosoftAccountOptions>, MicrosoftAccountPostConfigureOptions>();
            }

            if (!string.IsNullOrEmpty(configuration["Authentication:Twitter:ConsumerAPIKey"]) &&
                !string.IsNullOrEmpty(configuration["Authentication:Twitter:ConsumerSecret"]))
            {
                authenticationBuilder.AddTwitter();
                services.AddScoped<IPostConfigureOptions<TwitterOptions>,
                    Sas.ConfigurationProvider.TenantConfiguration.Authentication.TwitterPostConfigureOptions>();
            }
        }

        private static void ConfigureSuperAdminOnlyAdminPages(PageConventionCollection conventions)
        {
            string[] superAdminPages =
            {
                "/ReloadSettings",
                "/ClearTenantStaticCache",
                "/SiteHealthReport",
                "/GenerateDonationReport",
                "/EmailTest",
            };
            foreach (string page in superAdminPages)
            {
                ApplyRoleAreaOnlyPage(conventions, page);
            }

            ApplyRoleAreaOnlyFolder(conventions, "/FoodBanks");
            ApplyRoleAreaOnlyFolder(conventions, "/FunctionExecutionReports");
            ApplyRoleAreaOnlyFolder(conventions, "/Errors");
        }

        private static void ApplyRoleAreaOnlyPage(PageConventionCollection conventions, string page)
        {
            conventions.AddAreaPageApplicationModelConvention("Admin", page, RemoveAuthorizeFilters);
            conventions.AuthorizeAreaPage("Admin", page, "RoleArea");
        }

        private static void ApplyRoleAreaOnlyFolder(PageConventionCollection conventions, string folder)
        {
            conventions.AddAreaFolderApplicationModelConvention("Admin", folder, RemoveAuthorizeFilters);
            conventions.AuthorizeAreaFolder("Admin", folder, "RoleArea");
        }

        private static void RemoveAuthorizeFilters(PageApplicationModel model)
        {
            for (int i = model.Filters.Count - 1; i >= 0; i--)
            {
                if (model.Filters[i] is AuthorizeFilter)
                {
                    model.Filters.RemoveAt(i);
                }
            }
        }
    }
}
