// -----------------------------------------------------------------------
// <copyright file="ExtensionMethods.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Common
{
    using System;
    using System.Globalization;

    /// <summary>
    /// Extension methods.
    /// </summary>
    public static class ExtensionMethods
    {
        /// <summary>
        /// Gets the datetime in the format that EasyPay required.
        /// </summary>
        /// <param name="value">A reference to the <see cref="DateTime"/>.</param>
        /// <returns>A string representation of the datetime with the format, yyyy-MM-dd HH:mm.</returns>
        public static string GetEasyPayDateTimeString(this DateTime value)
        {
            return value.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Parse the easypay datetime format to a <see cref="DateTime"/> object.
        /// </summary>
        /// <param name="value">String representation of the datetime in easypya format. </param>
        /// <returns>The instance of the <see cref="DateTime"/>.</returns>
        public static DateTime FromEasyPayDateTimeString(this string value)
        {
            return DateTime.Parse(value);
        }
    }
}
