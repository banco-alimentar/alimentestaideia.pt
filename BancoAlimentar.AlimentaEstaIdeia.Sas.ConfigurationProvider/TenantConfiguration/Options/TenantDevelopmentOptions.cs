// -----------------------------------------------------------------------
// <copyright file="TenantDevelopmentOptions.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration.Options
{
    /// <summary>
    /// Tenant development configuration options.
    /// </summary>
    public class TenantDevelopmentOptions
    {
        /// <summary>
        /// Gets the Tenant Development Option tenant override configuration section.
        /// </summary>
        public const string Section = "Tenant-Override";

        private static TenantDevelopmentOptions production = new TenantDevelopmentOptions();

        /// <summary>
        /// Gets empty tenant development options.
        /// </summary>
        public static TenantDevelopmentOptions ProductionOptions
        {
            get
            {
                return production;
            }
        }

        /// <summary>
        /// Gets or sets the tenant name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Domain identifier.
        /// </summary>
        public string DomainIdentifier { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Invoicing Startegy.
        /// </summary>
        public string InvoicingStrategy { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Payment Strategy.
        /// </summary>
        public string PaymentStrategy { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether if secrets should be used.
        /// </summary>
        public bool UseSecrets { get; set; } = true;
    }
}
