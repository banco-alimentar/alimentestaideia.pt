// -----------------------------------------------------------------------
// <copyright file="IInvoiceDownloadTokenService.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Services.Invoices
{
    using System;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// Creates and validates short-lived signed invoice download tokens.
    /// </summary>
    public interface IInvoiceDownloadTokenService
    {
        /// <summary>
        /// Creates a signed token for the given donation public id.
        /// </summary>
        /// <param name="publicDonationId">Donation public identifier.</param>
        /// <returns>URL-safe signed token.</returns>
        string CreateToken(Guid publicDonationId);

        /// <summary>
        /// Validates a signed token and returns the donation public id when valid.
        /// </summary>
        /// <param name="token">Signed token from an invoice link.</param>
        /// <param name="publicDonationId">Resolved donation public identifier.</param>
        /// <returns><see langword="true"/> when the token is valid and not expired.</returns>
        bool TryValidateToken(string token, out Guid publicDonationId);

        /// <summary>
        /// Builds an absolute invoice download URL with a signed token.
        /// </summary>
        /// <param name="request">Current HTTP request.</param>
        /// <param name="publicDonationId">Donation public identifier.</param>
        /// <returns>Absolute invoice download URL.</returns>
        string BuildDownloadUrl(HttpRequest request, Guid publicDonationId);

        /// <summary>
        /// Builds a relative invoice download URL with a signed token.
        /// </summary>
        /// <param name="publicDonationId">Donation public identifier.</param>
        /// <returns>Relative invoice download URL.</returns>
        string BuildRelativeDownloadUrl(Guid publicDonationId);
    }
}
