// -----------------------------------------------------------------------
// <copyright file="AccountMergeBlockReason.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Services
{
    /// <summary>
    /// Reasons account merge may be blocked.
    /// </summary>
    public enum AccountMergeBlockReason
    {
        /// <summary>
        /// Merge is allowed.
        /// </summary>
        None,

        /// <summary>
        /// Required information was missing.
        /// </summary>
        MissingInformation,

        /// <summary>
        /// The external login is not linked to another account.
        /// </summary>
        SourceAccountNotFound,

        /// <summary>
        /// The external login is already on the signed-in account.
        /// </summary>
        SameAccount,

        /// <summary>
        /// The provider did not return an email address.
        /// </summary>
        MissingExternalEmail,

        /// <summary>
        /// Emails do not match across accounts and the provider.
        /// </summary>
        EmailMismatch,

        /// <summary>
        /// One of the accounts has an administrative role.
        /// </summary>
        ProtectedAccount,
    }
}
