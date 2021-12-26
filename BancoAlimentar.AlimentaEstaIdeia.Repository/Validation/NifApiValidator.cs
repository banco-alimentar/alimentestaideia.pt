// -----------------------------------------------------------------------
// <copyright file="NifApiValidator.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.Validation
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Configuration;
    using Newtonsoft.Json.Linq;

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
}
