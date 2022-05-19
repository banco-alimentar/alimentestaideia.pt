// -----------------------------------------------------------------------
// <copyright file="MustBeCheckedAttribute.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Validation;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Make sure that the checked is true.
/// </summary>
public class MustBeCheckedAttribute : ValidationAttribute
{
    /// <summary>
    /// Checks if the validation attribute is valid.
    /// </summary>
    /// <param name="value">Object to validate.</param>
    /// <returns>True if the value is true, false otherwise.</returns>
    public override bool IsValid(object value)
    {
        return value != null && (bool)value;
    }
}
