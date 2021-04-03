namespace BancoAlimentar.AlimentaEstaIdeia.Web.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Models;
    using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class MinimumValueAttribute : ValidationAttribute, IClientModelValidator
    {
        private readonly double minimumValue;

        public MinimumValueAttribute(double minimumValue)
        {
            this.minimumValue = minimumValue;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (Convert.ToDouble(value) * 0.1< minimumValue)
            {
                return new ValidationResult(ValidationMessages.MinAmount);
            }

            return ValidationResult.Success;
        }

        public void AddValidation(ClientModelValidationContext context)
        {
            MergeAttribute(context.Attributes, "data-val", "true");
            var errorMessage = FormatErrorMessage(ValidationMessages.MinAmount);
            MergeAttribute(context.Attributes, "data-val-minvalue", errorMessage);
            var minimumValue = this.minimumValue.ToString(CultureInfo.InvariantCulture);
            MergeAttribute(context.Attributes, "data-val-minvalue-minvalue", minimumValue);
        }

        private bool MergeAttribute(
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
