// -----------------------------------------------------------------------
// <copyright file="TemplateStorageOptions.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Deployment
{
    /// <summary>
    /// Configuration options defining the storage location of ARM templates.
    /// </summary>
    public class TemplateStorageOptions
    {
        /// <summary>
        /// Gets or sets the full URL of the blob file for the ARM template.
        /// </summary>
        public string? BlobUrl { get; set; }
    }
}
