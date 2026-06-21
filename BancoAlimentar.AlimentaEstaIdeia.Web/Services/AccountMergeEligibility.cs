// -----------------------------------------------------------------------
// <copyright file="AccountMergeEligibility.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Services
{
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;

    /// <summary>
    /// Result of evaluating whether two accounts may be merged.
    /// </summary>
    public class AccountMergeEligibility
    {
        /// <summary>
        /// Gets or sets a value indicating whether merge is allowed.
        /// </summary>
        public bool CanMerge { get; set; }

        /// <summary>
        /// Gets or sets the account that would be merged into the signed-in account.
        /// </summary>
        public WebUser SourceUser { get; set; }

        /// <summary>
        /// Gets or sets the reason merge was blocked.
        /// </summary>
        public AccountMergeBlockReason BlockReason { get; set; }

        /// <summary>
        /// Gets or sets the provider display name.
        /// </summary>
        public string ProviderDisplayName { get; set; }

        /// <summary>
        /// Gets or sets the masked email of the conflicting account.
        /// </summary>
        public string MaskedSourceEmail { get; set; }

        /// <summary>
        /// Gets or sets the external provider email.
        /// </summary>
        public string MaskedExternalEmail { get; set; }

        /// <summary>
        /// Creates an allowed merge result.
        /// </summary>
        /// <param name="sourceUser">Source user.</param>
        /// <param name="providerDisplayName">Provider display name.</param>
        /// <param name="maskedSourceEmail">Masked source email.</param>
        /// <returns>Eligibility result.</returns>
        public static AccountMergeEligibility Allowed(
            WebUser sourceUser,
            string providerDisplayName,
            string maskedSourceEmail)
        {
            return new AccountMergeEligibility
            {
                CanMerge = true,
                SourceUser = sourceUser,
                ProviderDisplayName = providerDisplayName,
                MaskedSourceEmail = maskedSourceEmail,
            };
        }

        /// <summary>
        /// Creates a blocked merge result.
        /// </summary>
        /// <param name="reason">Block reason.</param>
        /// <param name="providerDisplayName">Provider display name.</param>
        /// <param name="maskedExternalEmail">Masked external email.</param>
        /// <param name="maskedSourceEmail">Masked source email.</param>
        /// <returns>Eligibility result.</returns>
        public static AccountMergeEligibility Blocked(
            AccountMergeBlockReason reason,
            string providerDisplayName = null,
            string maskedExternalEmail = null,
            string maskedSourceEmail = null)
        {
            return new AccountMergeEligibility
            {
                CanMerge = false,
                BlockReason = reason,
                ProviderDisplayName = providerDisplayName,
                MaskedExternalEmail = maskedExternalEmail,
                MaskedSourceEmail = maskedSourceEmail,
            };
        }
    }
}
