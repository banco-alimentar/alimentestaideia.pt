// -----------------------------------------------------------------------
// <copyright file="ParameterValue.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Deployment
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents a value for an ARM template parameter.
    /// </summary>
    public class ParameterValue
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterValue"/> class.
        /// </summary>
        /// <param name="value">The parameter value.</param>
        public ParameterValue(string value)
        {
            this.Value = value;
        }

        /// <summary>
        /// Gets the deploymentParameters schema.
        /// </summary>
        [JsonPropertyName("value")]
        public string Value { get; }
    }
}
