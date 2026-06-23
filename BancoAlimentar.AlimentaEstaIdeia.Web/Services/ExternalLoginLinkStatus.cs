// -----------------------------------------------------------------------
// <copyright file="ExternalLoginLinkStatus.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Services
{
    /// <summary>
    /// Result status when linking an external login to a local account.
    /// </summary>
    public enum ExternalLoginLinkStatus
    {
        /// <summary>
        /// The provider was linked to the account.
        /// </summary>
        Linked,

        /// <summary>
        /// A duplicate account was merged and the provider was linked.
        /// </summary>
        Merged,

        /// <summary>
        /// The provider could not be linked because of a conflict.
        /// </summary>
        Conflict,
    }
}
