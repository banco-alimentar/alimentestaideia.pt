// -----------------------------------------------------------------------
// <copyright file="AdminConfigurationRedactionTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaldeia.Web.IntegrationTests.IntegrationTests
{
    using System.Collections.Generic;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages;
    using Xunit;

    /// <summary>
    /// Tests that the admin configuration page never renders secret values.
    /// </summary>
    public class AdminConfigurationRedactionTests
    {
        /// <summary>
        /// Tenant Key Vault values and sensitive-named host values are redacted; other values remain visible.
        /// </summary>
        [Fact]
        public void TenantSecretsAndSensitiveKeysAreRedacted()
        {
            var values = new Dictionary<string, string>
            {
                { "PayPal:ClientId", "tenant-paypal-client" },
                { "ConnectionStrings:DefaultConnection", "Server=db;Password=test-only" },
                { "Authentication:Google:ClientSecret", "host-secret" },
                { "Storage:SasToken", "sv=test" },
                { "Logging:LogLevel:Default", "Information" },
                { "Admin:EmptyPassword", null },
            };

            Dictionary<string, string> result = ConfigurationModel.BuildRedactedConfiguration(values, new[] { "paypal:clientid" });

            Assert.Equal(ConfigurationModel.RedactedValue, result["PayPal:ClientId"]);
            Assert.Equal(ConfigurationModel.RedactedValue, result["ConnectionStrings:DefaultConnection"]);
            Assert.Equal(ConfigurationModel.RedactedValue, result["Authentication:Google:ClientSecret"]);
            Assert.Equal(ConfigurationModel.RedactedValue, result["Storage:SasToken"]);
            Assert.Equal("Information", result["Logging:LogLevel:Default"]);
            Assert.Null(result["Admin:EmptyPassword"]);
        }
    }
}
