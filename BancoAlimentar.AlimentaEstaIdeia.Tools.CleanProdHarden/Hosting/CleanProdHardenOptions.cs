// -----------------------------------------------------------------------
// <copyright file="CleanProdHardenOptions.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Hosting
{
    using System;
    using System.IO;

    /// <summary>
    /// Tool settings, bound from the <c>CleanProdHarden</c> configuration section.
    /// </summary>
    public sealed class CleanProdHardenOptions
    {
        /// <summary>
        /// Configuration section name.
        /// </summary>
        public const string SectionName = "CleanProdHarden";

        /// <summary>
        /// Gets or sets the SQLite file that stores decisions. Defaults to the user's local application data folder.
        /// </summary>
        public string? DecisionStorePath { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether committing changes is allowed. Dry runs are always allowed.
        /// </summary>
        public bool AllowApply { get; set; }

        /// <summary>
        /// Resolves the decision store path.
        /// </summary>
        /// <returns>Absolute path.</returns>
        public string ResolveDecisionStorePath()
        {
            if (!string.IsNullOrWhiteSpace(this.DecisionStorePath))
            {
                return Path.GetFullPath(Environment.ExpandEnvironmentVariables(this.DecisionStorePath));
            }

            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CleanProdHarden", "decisions.db");
        }
    }
}
