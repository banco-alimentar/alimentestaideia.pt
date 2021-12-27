// -----------------------------------------------------------------------
// <copyright file="INamingStrategy.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Tenant.Naming
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Naming strategy pattert to get the name of the tenant.
    /// </summary>
    public interface INamingStrategy
    {
        /// <summary>
        /// Gets the name of the tentant.
        /// </summary>
        /// <returns>The name of the tenant.</returns>
        TenantData GetTenantName();
    }
}
