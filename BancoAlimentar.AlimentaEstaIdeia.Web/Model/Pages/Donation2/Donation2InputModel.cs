// -----------------------------------------------------------------------
// <copyright file="Donation2InputModel.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Model.Pages.Donation2
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Models;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Validation;

    /// <summary>
    /// Represents the user-editable fields in the Donation2 form.
    /// </summary>
    public sealed class Donation2InputModel
    {
        /// <summary>
        /// Gets or sets the selected products.
        /// </summary>
        public List<Donation2ItemInputModel> Items { get; set; } = new List<Donation2ItemInputModel>();

        /// <summary>
        /// Gets or sets the destination food bank identifier.
        /// </summary>
        [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "FoodBankIdRequired")]
        public int? FoodBankId { get; set; }

        /// <summary>
        /// Gets or sets the donor name.
        /// </summary>
        [StringLength(256, ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "NameStringLength")]
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the donor company name.
        /// </summary>
        [StringLength(256, ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "NameStringLength")]
        public string? CompanyName { get; set; }

        /// <summary>
        /// Gets or sets the donor email address.
        /// </summary>
        [StringLength(256, ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "EmailStringLength")]
        [EmailAddress(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "EmailInvalid")]
        public string? Email { get; set; }

        /// <summary>
        /// Gets or sets the donor country.
        /// </summary>
        [StringLength(256, ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "CountryStringLength")]
        public string? Country { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a receipt is requested.
        /// </summary>
        public bool WantsReceipt { get; set; }

        /// <summary>
        /// Gets or sets the receipt address.
        /// </summary>
        [StringLength(256, ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "AddressStringLength")]
        public string? Address { get; set; }

        /// <summary>
        /// Gets or sets the receipt postal code.
        /// </summary>
        [StringLength(20, ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "PostalCodeStringLength")]
        public string? PostalCode { get; set; }

        /// <summary>
        /// Gets or sets the receipt tax identifier.
        /// </summary>
        [StringLength(20, ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "NifStringLength")]
        [RegularExpression("^[0-9 ]*$", ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "NifInvalid")]
        public string? Nif { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether recurring donation is requested.
        /// </summary>
        public bool IsSubscriptionEnabled { get; set; }

        /// <summary>
        /// Gets or sets the recurring donation frequency.
        /// </summary>
        public string? SubscriptionFrequency { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the privacy terms were accepted.
        /// </summary>
        [MustBeChecked(ErrorMessage = "Deve aceitar a Política de Privacidade.")]
        public bool AcceptsTerms { get; set; }
    }
}