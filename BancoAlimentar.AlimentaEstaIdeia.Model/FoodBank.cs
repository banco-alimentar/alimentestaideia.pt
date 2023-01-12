// <copyright file="FoodBank.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>

namespace BancoAlimentar.AlimentaEstaIdeia.Model
{
    using Microsoft.AspNetCore.Identity;

    /// <summary>
    /// Represent a food bank.
    /// </summary>
    public class FoodBank
    {
        /// <summary>
        /// Gets or sets the unique id.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the food bank.
        /// </summary>
        [PersonalData]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the name of the FoodBank receipt in the invoice for cashdonation.
        /// </summary>
        public string ReceiptName { get; set; }

        /// <summary>
        /// Gets or sets the path for the image that contains the signature.
        /// </summary>
        public string ReceiptSignatureImg { get; set; }

        /// <summary>
        /// Gets or sets or set the footer place in the invoice.
        /// </summary>
        public string ReceiptPlace { get; set; }

        /// <summary>
        /// Gets or sets the html header part of the invoice.
        /// </summary>
        public string ReceiptHeader { get; set; }

        /// <summary>
        /// Gets or sets the Portugal Tax Registration Number for the Tenant.
        /// </summary>
        public string TaxRegistrationNumber { get; set; }

        /// <summary>
        /// Gets or sets the Portugal hash cyber for the Tenant.
        /// </summary>
        public string HashCypher { get; set; }

        /// <summary>
        /// Gets or sets the Software Certificate Number for the Tenant.
        /// </summary>
        public string SoftwareCertificateNumber { get; set; }

        /// <summary>
        /// Gets or sets the Portugal ATCUD for the Tenant.
        /// </summary>
        public string ATCUD { get; set; }
    }
}
