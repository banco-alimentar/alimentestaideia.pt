// -----------------------------------------------------------------------
// <copyright file="Configuration.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Model
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Define custom configuration for the Tenant.
    /// </summary>
    /// <remarks>
    /// This class hold specific configuration for the Tenant that was
    /// stored in the appSettings.json file. Now this configuration is
    /// Tenant depends and it doesn't make sense to have it in a single
    /// file.
    /// </remarks>
    public class Configuration
    {
        /// <summary>
        /// Gets or sets the Configuration Id.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets when the configuration was created.
        /// </summary>
        public DateTime Created { get; set; }

        /// <summary>
        /// Gets or sets the configuration name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the configuration value.
        /// </summary>
        public string Value { get; set; }
    }
}
