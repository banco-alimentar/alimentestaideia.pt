// -----------------------------------------------------------------------
// <copyright file="MinimumValueAttribute.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Globalization;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Models;
    using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

    /// <summary>
    /// Minimum value attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class MinimumValueAttribute : ValidationAttribute, IClientModelValidator
    {
        private readonly double minimumValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="MinimumValueAttribute"/> class.
        /// </summary>
        /// <param name="minimumValue">Minimum value to check.</param>
        public MinimumValueAttribute(double minimumValue)
        {
            this.minimumValue = minimumValue;
        }

        /// <summary>
        /// Initialize the validation.
        /// </summary>
        /// <param name="context">Client model validation context.</param>
        public void AddValidation(ClientModelValidationContext context)
        {
            MergeAttribute(context.Attributes, "data-val", "true");
            var errorMessage = FormatErrorMessage(ValidationMessages.MinAmount);
            MergeAttribute(context.Attributes, "data-val-minvalue", errorMessage);
            var minimumValue = this.minimumValue.ToString(CultureInfo.InvariantCulture);
            MergeAttribute(context.Attributes, "data-val-minvalue-minvalue", minimumValue);
        }

        /// <summary>
        /// Check the validator is valid.
        /// </summary>
        /// <param name="value">Object to validate.</param>
        /// <param name="validationContext">Validation context.</param>
        /// <returns>A reference to the <see cref="ValidationResult"/>.</returns>
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (Convert.ToDouble(value) < minimumValue)
            {
                return new ValidationResult(ValidationMessages.MinAmount);
            }

            return ValidationResult.Success;
        }

        private static bool MergeAttribute(
            IDictionary<string, string> attributes,
            string key,
            string value)
        {
            if (attributes.ContainsKey(key))
            {
                return false;
            }

            attributes.Add(key, value);
            return true;
        }
    }
}
