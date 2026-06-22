// -----------------------------------------------------------------------
// <copyright file="SiteHealthQueryRowParser.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.SiteHealth
{
    using System.Collections.Generic;

    /// <summary>
    /// Parses Log Analytics query row dictionaries.
    /// </summary>
    internal static class SiteHealthQueryRowParser
    {
        /// <summary>
        /// Reads a string column from a query row.
        /// </summary>
        /// <param name="row">Query row.</param>
        /// <param name="column">Column name.</param>
        /// <returns>Column value or empty string.</returns>
        internal static string GetString(IReadOnlyDictionary<string, object> row, string column)
        {
            if (row.TryGetValue(column, out object value) && value != null)
            {
                return value.ToString();
            }

            return string.Empty;
        }

        /// <summary>
        /// Reads a numeric column from a query row.
        /// </summary>
        /// <param name="row">Query row.</param>
        /// <param name="column">Column name.</param>
        /// <returns>Column value or zero.</returns>
        internal static long GetLong(IReadOnlyDictionary<string, object> row, string column)
        {
            if (!row.TryGetValue(column, out object value) || value == null)
            {
                return 0;
            }

            return value switch
            {
                long longValue => longValue,
                int intValue => intValue,
                double doubleValue => (long)doubleValue,
                _ when long.TryParse(value.ToString(), out long parsed) => parsed,
                _ => 0,
            };
        }
    }
}
