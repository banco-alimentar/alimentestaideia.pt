// -----------------------------------------------------------------------
// <copyright file="TenantData.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tenant
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Data about the tenant.
    /// </summary>
    public class TenantData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TenantData"/> class.
        /// </summary>
        /// <param name="name">Name of the tenant.</param>
        public TenantData(string name)
        {
            this.Name = name;
        }

        /// <summary>
        /// Gets or sets the tenant name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets when the tenant was created.
        /// </summary>
        public DateTime Created { get; set; }
    }
}
