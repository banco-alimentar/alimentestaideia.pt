// -----------------------------------------------------------------------
// <copyright file="DonationReportGenerationResult.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.Reporting
{
    using System;

    /// <summary>
    /// Outcome of a donation report generation run.
    /// </summary>
    public class DonationReportGenerationResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether generation completed successfully.
        /// </summary>
        public bool Succeeded { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the run was skipped (e.g. disabled in configuration).
        /// </summary>
        public bool Skipped { get; set; }

        /// <summary>
        /// Gets or sets a human-readable status message.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the number of pages uploaded to blob storage.
        /// </summary>
        public int PagesUploaded { get; set; }

        /// <summary>
        /// Gets or sets the number of pages written to local disk.
        /// </summary>
        public int PagesWrittenLocally { get; set; }

        /// <summary>
        /// Gets or sets total paid amount in the snapshot.
        /// </summary>
        public double TotalPaidAmount { get; set; }

        /// <summary>
        /// Gets or sets paid donation count in the snapshot.
        /// </summary>
        public int PaidDonationCount { get; set; }

        /// <summary>
        /// Gets or sets when the report was generated (UTC).
        /// </summary>
        public DateTime GeneratedAtUtc { get; set; }
    }
}
