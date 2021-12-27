// -----------------------------------------------------------------------
// <copyright file="KeyVaultConfiguration.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Model
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Define the Key Vault configuration.
    /// </summary>
    public class KeyVaultConfiguration
    {
        /// <summary>
        /// Gets or sets the unique id.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets Azure Key Vault url.
        /// </summary>
        public Uri Vault { get; set; }

        /// <summary>
        /// Gets or sets the environment.
        /// </summary>
        public string Environment { get; set; }

        /// <summary>
        /// Gets or sets when the Configuration was created.
        /// </summary>
        public DateTime Created { get; set; }
    }
}
