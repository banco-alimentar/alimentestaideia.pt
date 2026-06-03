// -----------------------------------------------------------------------
// <copyright file="DonationReportPaths.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.Reporting
{
    /// <summary>
    /// Paths used when publishing donation reports to tenant static storage.
    /// </summary>
    public static class DonationReportPaths
    {
        /// <summary>
        /// Blob prefix under the tenant storage container (maps to <c>/report/</c> on the site).
        /// </summary>
        public const string BlobPrefix = "wwwroot/report/";

        /// <summary>
        /// Public URL path on the main site.
        /// </summary>
        public const string PublicPath = "/report/";
    }
}
