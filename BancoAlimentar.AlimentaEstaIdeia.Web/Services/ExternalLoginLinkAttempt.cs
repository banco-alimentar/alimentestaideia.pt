// -----------------------------------------------------------------------
// <copyright file="ExternalLoginLinkAttempt.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Services
{
    using Microsoft.AspNetCore.Identity;

    /// <summary>
    /// Outcome of an attempt to link an external provider to a local account.
    /// </summary>
    public class ExternalLoginLinkAttempt
    {
        /// <summary>
        /// Gets the link attempt status.
        /// </summary>
        public ExternalLoginLinkStatus Status { get; init; }

        /// <summary>
        /// Gets conflict details when the provider cannot be linked automatically.
        /// </summary>
        public AccountMergeEligibility Conflict { get; init; }

        /// <summary>
        /// Gets the identity error when the link or merge operation failed unexpectedly.
        /// </summary>
        public IdentityResult Error { get; init; }

        /// <summary>
        /// Gets a value indicating whether the provider is linked to the account.
        /// </summary>
        public bool Succeeded =>
            this.Status == ExternalLoginLinkStatus.Linked
            || this.Status == ExternalLoginLinkStatus.Merged;

        /// <summary>
        /// Creates a successful link result.
        /// </summary>
        /// <returns>A linked attempt result.</returns>
        public static ExternalLoginLinkAttempt Linked()
        {
            return new ExternalLoginLinkAttempt
            {
                Status = ExternalLoginLinkStatus.Linked,
            };
        }

        /// <summary>
        /// Creates a successful merge result.
        /// </summary>
        /// <returns>A merged attempt result.</returns>
        public static ExternalLoginLinkAttempt Merged()
        {
            return new ExternalLoginLinkAttempt
            {
                Status = ExternalLoginLinkStatus.Merged,
            };
        }

        /// <summary>
        /// Creates a conflict result.
        /// </summary>
        /// <param name="eligibility">Conflict details.</param>
        /// <param name="error">Optional identity error.</param>
        /// <returns>A conflict attempt result.</returns>
        public static ExternalLoginLinkAttempt Blocked(
            AccountMergeEligibility eligibility,
            IdentityResult error = null)
        {
            return new ExternalLoginLinkAttempt
            {
                Status = ExternalLoginLinkStatus.Conflict,
                Conflict = eligibility,
                Error = error,
            };
        }
    }
}
