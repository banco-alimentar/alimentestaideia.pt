// -----------------------------------------------------------------------
// <copyright file="LocalEmailCapture.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Services
{
    using System;
    using System.Net.Mail;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.Data.SqlClient;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Opt-in SMTP capture for an isolated, loopback-only local test application.
    /// </summary>
    public static class LocalEmailCapture
    {
        /// <summary>Checks the process-level capture switch, not tenant SMTP overrides.</summary>
        /// <param name="configuration">Application configuration.</param>
        /// <returns>Whether local capture was explicitly enabled.</returns>
        public static bool IsEnabled(IConfiguration configuration)
        {
            // TenantConfigurationRoot.GetSection returns process configuration. A tenant must not
            // disable the local safety switch or replace its expected database names.
            return configuration.GetSection("LocalEmailCapture").GetValue<bool>("Enabled");
        }

        /// <summary>Checks the process-level choice of temporary Development infrastructure.</summary>
        /// <param name="configuration">Application configuration.</param>
        /// <returns>Whether local capture requests the existing in-memory infrastructure database.</returns>
        public static bool UsesInMemoryInfrastructure(IConfiguration configuration)
        {
            return IsEnabled(configuration)
                && configuration.GetSection("LocalEmailCapture").GetValue<bool>("UseInMemoryInfrastructure");
        }

        /// <summary>Suppresses inherited telemetry before any token-bearing browser requests run.</summary>
        /// <param name="configuration">Application configuration.</param>
        /// <param name="telemetry">The actual application telemetry singleton.</param>
        public static void ConfigureTelemetry(IConfiguration configuration, TelemetryConfiguration telemetry)
        {
            if (IsEnabled(configuration))
            {
                telemetry.DisableTelemetry = true;
            }
        }

        /// <summary>Rejects unsafe capture configuration before the application starts.</summary>
        /// <param name="configuration">Application configuration.</param>
        /// <param name="environmentName">Hosting environment.</param>
        /// <param name="urls">Explicit HTTP(S) listening URLs.</param>
        public static void ValidateStartup(IConfiguration configuration, string environmentName, string urls)
        {
            if (!IsEnabled(configuration))
            {
                return;
            }

            ValidateEnvironmentAndDatabases(configuration, environmentName);
            if (string.IsNullOrWhiteSpace(urls))
            {
                throw new InvalidOperationException("Local email capture requires explicit loopback listening URLs.");
            }

            foreach (string address in urls.Split(';'))
            {
                ValidateListeningUrl(address);
            }

            // Explicit Kestrel endpoints take precedence over ASPNETCORE_URLS.
            foreach (IConfigurationSection endpoint in configuration.GetSection("Kestrel:Endpoints").GetChildren())
            {
                ValidateListeningUrl(endpoint["Url"]);
            }
        }

        /// <summary>Checks effective tenant settings before opening any SQL connection.</summary>
        /// <param name="configuration">Effective application/tenant configuration.</param>
        /// <param name="environmentName">Hosting environment.</param>
        public static void ValidateDataAccess(IConfiguration configuration, string environmentName)
        {
            if (IsEnabled(configuration))
            {
                ValidateEnvironmentAndDatabases(configuration, environmentName);
            }
        }

        /// <summary>Creates the fixed local capture transport, ignoring tenant SMTP settings.</summary>
        /// <param name="configuration">Effective application/tenant configuration.</param>
        /// <param name="environmentName">Hosting environment.</param>
        /// <returns>The local SMTP client, or null when capture is disabled.</returns>
        public static SmtpClient CreateClient(IConfiguration configuration, string environmentName)
        {
            if (!IsEnabled(configuration))
            {
                return null;
            }

            ValidateEnvironmentAndDatabases(configuration, environmentName);
            int port = configuration.GetSection("LocalEmailCapture").GetValue<int?>("SmtpPort") ?? 1025;
            if (port < 1 || port > 65535)
            {
                throw new InvalidOperationException("LocalEmailCapture:SmtpPort must be between 1 and 65535.");
            }

            return new SmtpClient("127.0.0.1", port)
            {
                EnableSsl = false,
                UseDefaultCredentials = false,
                Credentials = null,
                DeliveryMethod = SmtpDeliveryMethod.Network,
            };
        }

        private static void ValidateEnvironmentAndDatabases(IConfiguration configuration, string environmentName)
        {
            if (!string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Local email capture is only permitted in Development, never in an Azure slot.");
            }

            IConfigurationSection capture = configuration.GetSection("LocalEmailCapture");
            ValidateDatabase(configuration["ConnectionStrings:DefaultConnection"], capture["ExpectedDatabase"]);
            if (!UsesInMemoryInfrastructure(configuration))
            {
                ValidateDatabase(configuration["ConnectionStrings:Infrastructure"], capture["ExpectedInfrastructureDatabase"]);
            }
        }

        private static void ValidateListeningUrl(string address)
        {
            if (!Uri.TryCreate(address, UriKind.Absolute, out Uri uri)
                || !uri.IsLoopback || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                || uri.UserInfo.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length != 0
                || uri.AbsolutePath != "/")
            {
                throw new InvalidOperationException("Local email capture can only be used with loopback HTTP(S) listening URLs.");
            }
        }

        private static void ValidateDatabase(string connectionString, string expectedDatabase)
        {
            if (string.IsNullOrWhiteSpace(expectedDatabase)
                || !expectedDatabase.EndsWith("_selenium", StringComparison.OrdinalIgnoreCase)
                || expectedDatabase.Length <= "_selenium".Length)
            {
                throw new InvalidOperationException("Local email capture requires explicit isolated database names ending in _selenium.");
            }

            string catalog;
            try
            {
                if (string.IsNullOrWhiteSpace(connectionString) || connectionString.Contains("#{", StringComparison.Ordinal))
                {
                    throw new ArgumentException();
                }

                catalog = new SqlConnectionStringBuilder(connectionString).InitialCatalog;
            }
            catch (ArgumentException)
            {
                throw new InvalidOperationException("Local email capture requires valid isolated SQL connection strings.");
            }

            if (!catalog.Equals(expectedDatabase, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Local email capture refuses a database that differs from its explicit isolated test database.");
            }
        }
    }
}
