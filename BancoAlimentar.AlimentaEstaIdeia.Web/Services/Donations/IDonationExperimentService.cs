// -----------------------------------------------------------------------
// <copyright file="IDonationExperimentService.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Services.Donations
{
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// Assigns visitors to a donation-page experiment variant.
    /// </summary>
    public interface IDonationExperimentService
    {
        /// <summary>
        /// Gets a value indicating whether the visitor should use Donation2.
        /// </summary>
        /// <param name="httpContext">Current HTTP context.</param>
        /// <returns><see langword="true"/> for Donation2; otherwise, <see langword="false"/>.</returns>
        bool ShouldUseDonation2(HttpContext httpContext);
    }
}