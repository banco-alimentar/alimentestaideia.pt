// -----------------------------------------------------------------------
// <copyright file="ITenantLayout.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Layout
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Represent the ASP.NET Core Layout properties for the Tenant.
    /// </summary>
    public interface ITenantLayout
    {
        /// <summary>
        /// Gets the Tenant Layout.
        /// </summary>
        string Layout { get; }

        /// <summary>
        /// Gets the Tenant Layout.
        /// </summary>
        string AdminLayout { get; }
    }
}
