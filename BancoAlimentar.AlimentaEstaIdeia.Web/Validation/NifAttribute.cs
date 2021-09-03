// -----------------------------------------------------------------------
// <copyright file="NifAttribute.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Validation
{
    using System.ComponentModel.DataAnnotations;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.Validation;

    /// <summary>
    /// Nif validation attribute.
    /// </summary>
    public class NifAttribute : ValidationAttribute
    {
        /// <summary>
        /// Check if the value is valid.
        /// </summary>
        /// <param name="value">Value to check.</param>
        /// <returns>True if is valid, false otherwise.</returns>
        public override bool IsValid(object value)
        {
            return NifValidation.ValidateNif((string)value);
        }
    }
}
