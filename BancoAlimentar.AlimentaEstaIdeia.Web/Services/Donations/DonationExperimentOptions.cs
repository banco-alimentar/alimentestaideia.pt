// -----------------------------------------------------------------------
// <copyright file="DonationExperimentOptions.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Services.Donations
{
    /// <summary>
    /// Configuration for the Donation2 traffic experiment.
    /// </summary>
    public sealed class DonationExperimentOptions
    {
        /// <summary>
        /// Gets the configuration section name.
        /// </summary>
        public const string SectionName = "Donation2Experiment";

        /// <summary>
        /// Gets or sets the percentage of Donation GET requests assigned to Donation2.
        /// </summary>
        public int TrafficPercentage { get; set; }
    }
}