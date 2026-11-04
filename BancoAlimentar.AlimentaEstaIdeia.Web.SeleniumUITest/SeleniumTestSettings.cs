// -----------------------------------------------------------------------
// <copyright file="SeleniumTestSettings.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.SeleniumUITest
{
    using System;
    using Microsoft.Data.SqlClient;
    using Microsoft.Extensions.Configuration;

    internal sealed class SeleniumTestSettings
    {
        private const string StagingDatabase = "alimentestaideia.core_staging";
        private readonly IConfiguration? hostConfiguration;

        private SeleniumTestSettings(
            string target, Uri baseUrl, Uri? mailpitBaseUrl, string connectionString, string expectedDatabase,
            bool selfContained = false, int mailpitSmtpPort = 1025, IConfiguration? hostConfiguration = null)
        {
            Target = target;
            BaseUrl = baseUrl;
            MailpitBaseUrl = mailpitBaseUrl;
            VerificationConnectionString = connectionString;
            ExpectedDatabase = expectedDatabase;
            IsSelfContained = selfContained;
            MailpitSmtpPort = mailpitSmtpPort;
            this.hostConfiguration = hostConfiguration;
        }

        public string Target { get; }

        public bool IsLocal => Target == "Local";

        public bool IsSelfContained { get; }

        public Uri BaseUrl { get; }

        public Uri? MailpitBaseUrl { get; }

        public int MailpitSmtpPort { get; }

        public string VerificationConnectionString { get; }

        public string ExpectedDatabase { get; }

        public static SeleniumTestSettings FromConfiguration(IConfiguration configuration)
        {
            string target = configuration["SeleniumTest:Target"] ?? "Developer";
            target = target.Equals("Local", StringComparison.OrdinalIgnoreCase) ? "Local"
                : target.Equals("Developer", StringComparison.OrdinalIgnoreCase) ? "Developer"
                : throw new InvalidOperationException("SeleniumTest:Target must be Local or Developer.");
            bool local = target == "Local";
            string prefix = "SeleniumTest:" + target + ":";
            bool selfContained = false;
            Uri? mailpit = null;
            int smtpPort = 1025;
            if (local)
            {
                string? configuredSelfContained = configuration[prefix + "SelfContained"];
                if (configuredSelfContained == null)
                {
                    selfContained = true;
                }
                else if (!bool.TryParse(configuredSelfContained, out selfContained))
                {
                    throw new InvalidOperationException(prefix + "SelfContained must be true or false.");
                }

                mailpit = ParseOrigin(configuration[prefix + "MailpitBaseUrl"] ?? "http://127.0.0.1:8025", "Mailpit base URL");
                if (!mailpit.IsLoopback)
                {
                    throw new InvalidOperationException("Local Mailpit must use a loopback URL.");
                }

                string? configuredPort = configuration[prefix + "MailpitSmtpPort"];
                if (configuredPort != null && (!int.TryParse(configuredPort, out smtpPort) || smtpPort < 1 || smtpPort > 65535))
                {
                    throw new InvalidOperationException(prefix + "MailpitSmtpPort must be between 1 and 65535.");
                }
            }

            if (selfContained)
            {
                // The bound host supplies its ephemeral origin and generated credentials after startup.
                // Never parse inherited URLs, SQL connections, or accounts for this mode.
                return new SeleniumTestSettings(target, new Uri("http://127.0.0.1"), mailpit, string.Empty, string.Empty, true, smtpPort);
            }

            string configuredUrl = configuration[prefix + "BaseUrl"]
                ?? (!local ? configuration["SeleniumTest:BaseUrl"] : null)
                ?? (local ? "https://localhost:5051" : "https://dev.alimentestaideia.pt");
            Uri baseUrl = ParseOrigin(configuredUrl, "Selenium base URL");
            if (local ? !baseUrl.IsLoopback
                : baseUrl.Scheme != Uri.UriSchemeHttps || baseUrl.Port != 443
                    || !baseUrl.Host.Equals("dev.alimentestaideia.pt", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Local Selenium requires a loopback URL; Developer requires https://dev.alimentestaideia.pt.");
            }

            string? connection = configuration[prefix + "VerificationConnectionString"];
            if (!local && string.IsNullOrWhiteSpace(connection))
            {
                connection = configuration["SeleniumTest:VerificationConnectionString"]
                    ?? configuration["ConnectionStrings:DefaultConnection"];
            }

            string? expected = configuration[prefix + "ExpectedVerificationDatabase"]
                ?? (!local ? configuration["SeleniumTest:ExpectedVerificationDatabase"] ?? StagingDatabase : null);
            if (string.IsNullOrWhiteSpace(connection) || string.IsNullOrWhiteSpace(expected))
            {
                throw new InvalidOperationException(prefix + "VerificationConnectionString and ExpectedVerificationDatabase are required. Local never falls back to shared settings.");
            }

            if (local ? !expected.EndsWith("_selenium", StringComparison.OrdinalIgnoreCase) || expected.Length <= "_selenium".Length
                : !expected.Equals(StagingDatabase, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Local requires a dedicated database ending in _selenium; Developer requires alimentestaideia.core_staging.");
            }

            var settings = new SeleniumTestSettings(target, baseUrl, mailpit, connection, expected, mailpitSmtpPort: smtpPort);
            settings.AssertDatabase(connection);
            return settings;
        }

        public void AssertDatabase(string connectionString)
        {
            if (IsSelfContained)
            {
                throw new InvalidOperationException("Self-contained verification requires BrowserTestApplication.AssertOwnsDatabase on the actual context.");
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
                // Connection strings may contain passwords; never include them or parser exceptions.
                throw new InvalidOperationException("Configure a valid verification SQL connection string for the selected Selenium target.");
            }

            if (!catalog.Equals(ExpectedDatabase, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("The verification database does not match the explicitly selected Selenium target/database.");
            }
        }

        public string? ReadSetting(IConfiguration configuration, string key)
        {
            if (IsSelfContained)
            {
                return hostConfiguration?["SeleniumTest:Local:" + key];
            }

            return configuration["SeleniumTest:" + Target + ":" + key]
                ?? (!IsLocal ? configuration["SeleniumTest:" + key] : null);
        }

        public SeleniumTestSettings BindHost(Uri baseAddress, IConfiguration configuration)
        {
            if (!IsSelfContained)
            {
                throw new InvalidOperationException("Only self-contained Local settings can bind an owned browser host.");
            }

            Uri origin = ParseOrigin(baseAddress.AbsoluteUri, "Browser host base address");
            if (!origin.IsLoopback)
            {
                throw new InvalidOperationException("The owned browser host must use a loopback origin.");
            }

            return new SeleniumTestSettings(Target, origin, MailpitBaseUrl, string.Empty, string.Empty, true, MailpitSmtpPort, configuration);
        }

        private static Uri ParseOrigin(string value, string setting)
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out Uri? uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                || uri.UserInfo.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length != 0
                || uri.AbsolutePath != "/")
            {
                throw new InvalidOperationException(setting + " must be an HTTP(S) origin without credentials, path, query, or fragment.");
            }

            return uri;
        }
    }
}
