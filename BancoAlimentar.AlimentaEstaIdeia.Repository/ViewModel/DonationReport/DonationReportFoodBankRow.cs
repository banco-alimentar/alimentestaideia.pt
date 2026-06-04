// -----------------------------------------------------------------------
// <copyright file="DonationReportFoodBankRow.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.ViewModel.DonationReport
{
    /// <summary>
    /// Food bank aggregation row.
    /// </summary>
    public class DonationReportFoodBankRow
    {
        /// <summary>
        /// Gets or sets food bank identifier.
        /// </summary>
        public int FoodBankId { get; set; }

        /// <summary>
        /// Gets or sets food bank name.
        /// </summary>
        public string FoodBankName { get; set; }

        /// <summary>
        /// Gets or sets paid amount routed to this bank.
        /// </summary>
        public double PaidAmount { get; set; }

        /// <summary>
        /// Gets or sets paid donation count.
        /// </summary>
        public int PaidCount { get; set; }

        /// <summary>
        /// Gets or sets product units allocated.
        /// </summary>
        public long ProductUnits { get; set; }

        /// <summary>
        /// Gets or sets share of total paid amount (%).
        /// </summary>
        public double SharePercent { get; set; }
    }
}
