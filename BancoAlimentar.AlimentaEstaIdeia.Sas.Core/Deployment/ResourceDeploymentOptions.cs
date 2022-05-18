// -----------------------------------------------------------------------
// <copyright file="ResourceDeploymentOptions.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Deployment
{
    /// <summary>
    /// The configuration setting needed for the <see cref="ResourceDeploymentService"/> service.
    /// </summary>
    public class ResourceDeploymentOptions
    {
        /// <summary>Gets or sets the Azure AD tenant identifier.</summary>
        public string? TenantId { get; set; }

        /// <summary>Gets or sets the service principal client id.</summary>
        public string? ClientId { get; set; }

        /// <summary>Sets the service principal client secret.</summary>
        public string? ClientSecret { internal get; set; }
    }
}
