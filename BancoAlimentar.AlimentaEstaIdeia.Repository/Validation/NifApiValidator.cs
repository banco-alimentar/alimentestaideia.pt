// -----------------------------------------------------------------------
// <copyright file="NifApiValidator.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.Validation;

/// <summary>
/// Validate if a Nif is valid or not.
/// </summary>
public class NifApiValidator
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NifApiValidator"/> class.
    /// </summary>
    public NifApiValidator()
    {
    }

    /// <summary>
    /// Checks if the nif is valid or not.
    /// </summary>
    /// <param name="value">The nif to validate.</param>
    /// <returns>True if the nif is valid, false otherwise.</returns>
    public bool IsValidNif(string value)
    {
        return NifValidation.ValidateNif(value);
    }
}
