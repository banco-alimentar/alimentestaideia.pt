// -----------------------------------------------------------------------
// <copyright file="UserDataDonationFlowModel.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Model
{
    using BancoAlimentar.AlimentaEstaIdeia.Model;

    /// <summary>
    /// Represent the user for the anonymous user during the donation flow.
    /// We only commit this information at the end of the payment process.
    /// </summary>
    public class UserDataDonationFlowModel
    {
        /// <summary>
        /// Gets or sets the email address.
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets the company name.
        /// </summary>
        public string CompanyName { get; set; }

        /// <summary>
        /// Gets or sets the user's NIF.
        /// </summary>
        public string Nif { get; set; }

        /// <summary>
        /// Gets or sets the user address.
        /// </summary>
        public DonorAddress Address { get; set; }

        /// <summary>
        /// Gets or sets user full name.
        /// </summary>
        public string FullName { get; set; }
    }
}
