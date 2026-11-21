// -----------------------------------------------------------------------
// <copyright file="Configuration.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Show configuration model.
    /// </summary>
    public class ConfigurationModel : PageModel
    {
        /// <summary>
        /// Value displayed instead of a secret.
        /// </summary>
        public const string RedactedValue = "[redacted]";

        private static readonly string[] SensitiveKeyFragments =
        {
            "secret",
            "password",
            "pwd",
            "connectionstring",
            "key",
            "token",
            "credential",
            "signature",
            "sas",
        };

        private readonly IConfiguration configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationModel"/> class.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        public ConfigurationModel(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        /// <summary>
        /// Gets or sets the actual configuration in the system.
        /// </summary>
        [BindProperty]
        public Dictionary<string, string> ActualConfiguration { get; set; }

        /// <summary>
        /// Builds the displayed configuration, hiding every tenant Key Vault value and any sensitive-named host value.
        /// </summary>
        /// <param name="values">Effective configuration values.</param>
        /// <param name="tenantSecretKeys">Keys loaded from the tenant Key Vault.</param>
        /// <returns>Configuration safe to display.</returns>
        public static Dictionary<string, string> BuildRedactedConfiguration(
            IEnumerable<KeyValuePair<string, string>> values,
            IEnumerable<string> tenantSecretKeys)
        {
            var tenantKeys = new HashSet<string>(tenantSecretKeys, StringComparer.OrdinalIgnoreCase);
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, string> item in values)
            {
                bool redact = item.Value != null && (tenantKeys.Contains(item.Key) || IsSensitiveKey(item.Key));
                result[item.Key] = redact ? RedactedValue : item.Value;
            }

            return result;
        }

        /// <summary>
        /// Execute get operation.
        /// </summary>
        public void OnGet()
        {
            IEnumerable<string> tenantKeys = this.HttpContext.GetTenantSpecificConfiguration()?.Keys ?? Enumerable.Empty<string>();
            ActualConfiguration = BuildRedactedConfiguration(this.configuration.AsEnumerable(), tenantKeys);
        }

        private static bool IsSensitiveKey(string key)
        {
            return SensitiveKeyFragments.Any(fragment => key.Contains(fragment, StringComparison.OrdinalIgnoreCase));
        }
    }
}
