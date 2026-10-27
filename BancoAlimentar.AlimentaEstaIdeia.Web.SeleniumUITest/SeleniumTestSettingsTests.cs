// -----------------------------------------------------------------------
// <copyright file="SeleniumTestSettingsTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.SeleniumUITest
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Extensions.Configuration;
    using Xunit;

    [Trait("Category", "Offline")]
    public sealed class SeleniumTestSettingsTests
    {
        private const string PasswordMarker = "synthetic-settings-password";

        [Fact]
        public void DefaultTarget_UsesDeveloperAndLegacyConnection()
        {
            IConfiguration configuration = Configure();
            configuration["SeleniumTest:Target"] = null;
            configuration["SeleniumTest:Developer:VerificationConnectionString"] = null;
            configuration["SeleniumTest:VerificationConnectionString"] = Connection("alimentestaideia.core_staging");

            SeleniumTestSettings settings = SeleniumTestSettings.FromConfiguration(configuration);

            Assert.False(settings.IsLocal);
            Assert.False(settings.IsSelfContained);
            Assert.Equal("Developer", settings.Target);
            Assert.Equal(new Uri("https://dev.alimentestaideia.pt"), settings.BaseUrl);
            Assert.Equal("alimentestaideia.core_staging", settings.ExpectedDatabase);
            Assert.Equal(configuration["SeleniumTest:VerificationConnectionString"], settings.VerificationConnectionString);
            Assert.Null(settings.MailpitBaseUrl);
        }

        [Fact]
        public void Developer_FallsBackToDefaultConnection()
        {
            IConfiguration configuration = Configure();
            configuration["SeleniumTest:Developer:VerificationConnectionString"] = null;
            configuration["ConnectionStrings:DefaultConnection"] = Connection("alimentestaideia.core_staging");

            SeleniumTestSettings settings = SeleniumTestSettings.FromConfiguration(configuration);

            Assert.Equal(configuration["ConnectionStrings:DefaultConnection"], settings.VerificationConnectionString);
        }

        [Fact]
        public void Developer_ScopedConnectionTakesPrecedenceOverLegacy()
        {
            IConfiguration configuration = Configure();
            configuration["SeleniumTest:VerificationConnectionString"] = Connection("production");
            configuration["ConnectionStrings:DefaultConnection"] = Connection("production");

            SeleniumTestSettings settings = SeleniumTestSettings.FromConfiguration(configuration);

            Assert.Equal(configuration["SeleniumTest:Developer:VerificationConnectionString"], settings.VerificationConnectionString);
        }

        [Fact]
        public void Local_UsesDefaultsAndNeverReadsLegacyBaseUrl()
        {
            IConfiguration configuration = Configure("local");
            configuration["SeleniumTest:BaseUrl"] = "https://dev.alimentestaideia.pt";

            SeleniumTestSettings settings = SeleniumTestSettings.FromConfiguration(configuration);

            Assert.True(settings.IsLocal);
            Assert.Equal("Local", settings.Target);
            Assert.Equal(new Uri("https://localhost:5051"), settings.BaseUrl);
            Assert.Equal(new Uri("http://127.0.0.1:8025"), settings.MailpitBaseUrl);
            Assert.Equal("app_selenium", settings.ExpectedDatabase);
            Assert.Equal(configuration["SeleniumTest:Local:VerificationConnectionString"], settings.VerificationConnectionString);
        }

        [Theory]
        [InlineData("VerificationConnectionString")]
        [InlineData("ExpectedVerificationDatabase")]
        public void Local_RequiresScopedDatabaseSettingsDespiteLegacyFallbacks(string missingKey)
        {
            IConfiguration configuration = Configure("Local");
            configuration["SeleniumTest:Local:" + missingKey] = null;
            configuration["SeleniumTest:VerificationConnectionString"] = Connection("app_selenium");
            configuration["ConnectionStrings:DefaultConnection"] = Connection("app_selenium");
            configuration["SeleniumTest:ExpectedVerificationDatabase"] = "app_selenium";

            AssertRejected(() => SeleniumTestSettings.FromConfiguration(configuration));
        }

        [Theory]
        [InlineData("Production")]
        public void UnknownTarget_IsRejected(string target)
        {
            AssertRejected(() => SeleniumTestSettings.FromConfiguration(Configure(target)));
        }

        [Theory]
        [InlineData("Local", "https://dev.alimentestaideia.pt")]
        [InlineData("Local", "http://user:synthetic-settings-password@localhost:5051")]
        [InlineData("Local", "http://localhost:5051/path")]
        [InlineData("Local", "http://localhost:5051/?secret=synthetic-settings-password")]
        [InlineData("Local", "http://localhost:5051/#fragment")]
        [InlineData("Developer", "http://dev.alimentestaideia.pt")]
        [InlineData("Developer", "https://alimentestaideia.pt")]
        [InlineData("Developer", "https://dev.alimentestaideia.pt.evil.invalid")]
        public void UnsafeTargetUrl_IsRejectedWithoutSecrets(string target, string url)
        {
            IConfiguration configuration = Configure(target);
            configuration["SeleniumTest:" + target + ":BaseUrl"] = url;

            AssertRejected(() => SeleniumTestSettings.FromConfiguration(configuration));
        }

        [Theory]
        [InlineData("https://mailpit.example.invalid")]
        public void UnsafeMailpitUrl_IsRejected(string url)
        {
            IConfiguration configuration = Configure("Local");
            configuration["SeleniumTest:Local:MailpitBaseUrl"] = url;

            AssertRejected(() => SeleniumTestSettings.FromConfiguration(configuration));
        }

        [Theory]
        [InlineData("Local", "alimentestaideia.core_staging", "alimentestaideia.core_staging")]
        [InlineData("Local", "production", "production")]
        [InlineData("Local", "other_selenium", "app_selenium")]
        public void UnsafeOrMismatchedDatabase_IsRejected(string target, string actual, string expected)
        {
            IConfiguration configuration = Configure(target);
            configuration["SeleniumTest:" + target + ":VerificationConnectionString"] = Connection(actual);
            configuration["SeleniumTest:" + target + ":ExpectedVerificationDatabase"] = expected;

            AssertRejected(() => SeleniumTestSettings.FromConfiguration(configuration));
        }

        [Theory]
        [InlineData("not a connection string synthetic-settings-password")]
        [InlineData("Server=#{Server}#;Database=app_selenium;Password=synthetic-settings-password")]
        public void InvalidConnection_IsRejectedWithoutSecrets(string connection)
        {
            IConfiguration configuration = Configure("Local");
            configuration["SeleniumTest:Local:VerificationConnectionString"] = connection;

            AssertRejected(() => SeleniumTestSettings.FromConfiguration(configuration));
        }

        [Fact]
        public void AssertDatabase_RevalidatesEffectiveCatalogWithoutConnecting()
        {
            SeleniumTestSettings settings = SeleniumTestSettings.FromConfiguration(Configure("Local"));

            settings.AssertDatabase(Connection("APP_SELENIUM"));
            AssertRejected(() => settings.AssertDatabase(Connection("alimentestaideia.core_staging")));
        }

        [Fact]
        public void Local_ReadSettingDoesNotInheritLegacyCredentials()
        {
            IConfiguration configuration = Configure("Local");
            configuration["SeleniumTest:Username"] = "legacy-user@example.test";
            configuration["SeleniumTest:Password"] = PasswordMarker;
            configuration["SeleniumTest:Developer:Password"] = PasswordMarker;
            SeleniumTestSettings settings = SeleniumTestSettings.FromConfiguration(configuration);

            Assert.Null(settings.ReadSetting(configuration, "Username"));
            Assert.Null(settings.ReadSetting(configuration, "Password"));
        }

        [Fact]
        public void Developer_ReadSettingFallsBackToLegacyCredentials()
        {
            IConfiguration configuration = Configure();
            configuration["SeleniumTest:Username"] = "legacy-user@example.test";
            configuration["SeleniumTest:Password"] = PasswordMarker;
            SeleniumTestSettings settings = SeleniumTestSettings.FromConfiguration(configuration);

            Assert.Equal("legacy-user@example.test", settings.ReadSetting(configuration, "Username"));
            Assert.Equal(PasswordMarker, settings.ReadSetting(configuration, "Password"));
        }

        [Fact]
        public void ReadSetting_PrefersSelectedProfileCredentials()
        {
            foreach (string target in new[] { "Local", "Developer" })
            {
                IConfiguration configuration = Configure(target);
                configuration["SeleniumTest:Password"] = "legacy-password";
                configuration["SeleniumTest:" + target + ":Password"] = PasswordMarker;
                SeleniumTestSettings settings = SeleniumTestSettings.FromConfiguration(configuration);

                Assert.Equal(PasswordMarker, settings.ReadSetting(configuration, "Password"));
            }
        }

        [Fact]
        public void Local_DefaultsToSelfContainedWithoutSqlOrCredentials()
        {
            IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
            configuration["SeleniumTest:Target"] = "Local";
            configuration["SeleniumTest:SelfContained"] = "false";

            SeleniumTestSettings settings = SeleniumTestSettings.FromConfiguration(configuration);

            Assert.True(settings.IsSelfContained);
            Assert.True(settings.IsLocal);
            Assert.True(settings.BaseUrl.IsLoopback);
            Assert.Empty(settings.VerificationConnectionString);
            Assert.Empty(settings.ExpectedDatabase);
            Assert.Equal(new Uri("http://127.0.0.1:8025"), settings.MailpitBaseUrl);
            Assert.Equal(1025, settings.MailpitSmtpPort);
            Assert.Null(settings.ReadSetting(configuration, "Site:Username"));
            AssertRejected(() => settings.AssertDatabase(Connection("app_selenium")));
        }

        [Fact]
        public void SelfContained_IgnoresInheritedUrlsSqlAndCredentialsUntilHostBinding()
        {
            IConfiguration configuration = Configure("Local");
            configuration["SeleniumTest:Local:SelfContained"] = "true";
            configuration["SeleniumTest:BaseUrl"] = "https://dev.alimentestaideia.pt";
            configuration["SeleniumTest:Local:BaseUrl"] = "https://production.invalid";
            configuration["SeleniumTest:Local:VerificationConnectionString"] = "invalid " + PasswordMarker;
            configuration["ConnectionStrings:DefaultConnection"] = Connection("production");
            configuration["SeleniumTest:Local:ExpectedVerificationDatabase"] = "production";
            foreach (string key in new[] { "Site:Username", "Site:Password", "Admin:Username", "Admin:Password", "ReferralMetricsCode" })
            {
                configuration["SeleniumTest:Local:" + key] = PasswordMarker;
                configuration["SeleniumTest:" + key] = PasswordMarker;
            }

            SeleniumTestSettings settings = SeleniumTestSettings.FromConfiguration(configuration);

            Assert.True(settings.BaseUrl.IsLoopback);
            Assert.Empty(settings.VerificationConnectionString);
            Assert.Empty(settings.ExpectedDatabase);
            Assert.Null(settings.ReadSetting(configuration, "Site:Username"));
            Assert.Null(settings.ReadSetting(configuration, "Site:Password"));

            IConfiguration seeded = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SeleniumTest:Local:Site:Username"] = "generated@example.test",
                ["SeleniumTest:Local:Site:Password"] = "generated-password",
                ["SeleniumTest:Local:Admin:Username"] = "admin-generated@example.test",
                ["SeleniumTest:Local:Admin:Password"] = "generated-admin-password",
                ["SeleniumTest:Local:ReferralMetricsCode"] = "generated-code",
            }).Build();
            SeleniumTestSettings effective = settings.BindHost(new Uri("http://127.0.0.1:41234"), seeded);

            Assert.Equal(new Uri("http://127.0.0.1:41234"), effective.BaseUrl);
            Assert.True(effective.IsSelfContained);
            Assert.Equal(settings.MailpitBaseUrl, effective.MailpitBaseUrl);
            Assert.Equal("generated@example.test", effective.ReadSetting(configuration, "Site:Username"));
            Assert.Equal("generated-password", effective.ReadSetting(configuration, "Site:Password"));
            Assert.Equal("admin-generated@example.test", effective.ReadSetting(configuration, "Admin:Username"));
            Assert.Equal("generated-admin-password", effective.ReadSetting(configuration, "Admin:Password"));
            Assert.Equal("generated-code", effective.ReadSetting(configuration, "ReferralMetricsCode"));
            Assert.Null(effective.ReadSetting(configuration, "MissingSeed"));
            seeded["SeleniumTest:Local:Site:Password"] = null;
            Assert.Null(effective.ReadSetting(configuration, "Site:Password"));
        }

        [Theory]
        [InlineData("")]
        [InlineData("invalid")]
        [InlineData("synthetic-settings-password")]
        public void Local_RejectsInvalidSelfContainedSelection(string value)
        {
            IConfiguration configuration = Configure("Local");
            configuration["SeleniumTest:Local:SelfContained"] = value;

            AssertRejected(() => SeleniumTestSettings.FromConfiguration(configuration));
        }

        [Theory]
        [InlineData("https://production.invalid")]
        [InlineData("http://localhost:41234/path")]
        [InlineData("http://user:synthetic-settings-password@localhost:41234")]
        public void SelfContained_RejectsUnsafeBoundHostOrigin(string origin)
        {
            IConfiguration configuration = Configure("Local");
            configuration["SeleniumTest:Local:SelfContained"] = "true";
            SeleniumTestSettings settings = SeleniumTestSettings.FromConfiguration(configuration);

            AssertRejected(() => settings.BindHost(new Uri(origin), configuration));
        }

        [Theory]
        [InlineData("Local")]
        [InlineData("Developer")]
        public void ExternalTargets_CannotBindAnOwnedHost(string target)
        {
            IConfiguration configuration = Configure(target);
            SeleniumTestSettings settings = SeleniumTestSettings.FromConfiguration(configuration);

            Assert.False(settings.IsSelfContained);
            AssertRejected(() => settings.BindHost(new Uri("http://127.0.0.1:41234"), configuration));
        }

        [Fact]
        public void Developer_IgnoresLocalSelfContainedAndMailpitSettings()
        {
            IConfiguration configuration = Configure();
            configuration["SeleniumTest:Local:SelfContained"] = "true";
            configuration["SeleniumTest:Local:MailpitBaseUrl"] = "https://production.invalid";
            configuration["SeleniumTest:Local:MailpitSmtpPort"] = "invalid";

            SeleniumTestSettings settings = SeleniumTestSettings.FromConfiguration(configuration);

            Assert.False(settings.IsSelfContained);
            Assert.Null(settings.MailpitBaseUrl);
            Assert.Equal(new Uri("https://dev.alimentestaideia.pt"), settings.BaseUrl);
            Assert.Equal("alimentestaideia.core_staging", settings.ExpectedDatabase);
        }

        [Theory]
        [InlineData("http://127.0.0.1:8026", "2525")]
        [InlineData("http://localhost:8025", "1025")]
        public void SelfContained_AcceptsLoopbackMailpitOverrides(string origin, string port)
        {
            IConfiguration configuration = Configure("Local");
            configuration["SeleniumTest:Local:SelfContained"] = "true";
            configuration["SeleniumTest:Local:MailpitBaseUrl"] = origin;
            configuration["SeleniumTest:Local:MailpitSmtpPort"] = port;

            SeleniumTestSettings settings = SeleniumTestSettings.FromConfiguration(configuration);

            Assert.Equal(new Uri(origin), settings.MailpitBaseUrl);
            Assert.Equal(int.Parse(port), settings.MailpitSmtpPort);
        }

        [Theory]
        [InlineData("https://mailpit.example.invalid", "1025")]
        [InlineData("http://user:synthetic-settings-password@localhost:8025", "1025")]
        [InlineData("http://localhost:8025/path", "1025")]
        [InlineData("http://localhost:8025", "0")]
        [InlineData("http://localhost:8025", "65536")]
        [InlineData("http://localhost:8025", "synthetic-settings-password")]
        public void SelfContained_RejectsUnsafeMailpitSettings(string origin, string port)
        {
            IConfiguration configuration = Configure("Local");
            configuration["SeleniumTest:Local:SelfContained"] = "true";
            configuration["SeleniumTest:Local:MailpitBaseUrl"] = origin;
            configuration["SeleniumTest:Local:MailpitSmtpPort"] = port;

            AssertRejected(() => SeleniumTestSettings.FromConfiguration(configuration));
        }

        private static IConfiguration Configure(string target = "Developer")
        {
            return new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SeleniumTest:Target"] = target,
                ["SeleniumTest:Developer:VerificationConnectionString"] = Connection("alimentestaideia.core_staging"),
                ["SeleniumTest:Local:VerificationConnectionString"] = Connection("app_selenium"),
                ["SeleniumTest:Local:ExpectedVerificationDatabase"] = "app_selenium",
                ["SeleniumTest:Local:SelfContained"] = "false",
            }).Build();
        }

        private static string Connection(string catalog)
        {
            return "Server=127.0.0.1,1;Database=" + catalog + ";User ID=selenium-test;Password=" + PasswordMarker;
        }

        private static void AssertRejected(Action action)
        {
            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(action);
            Assert.Null(exception.InnerException);
            Assert.DoesNotContain(PasswordMarker, exception.ToString());
            Assert.DoesNotContain("User ID=selenium-test", exception.ToString());
        }
    }
}
