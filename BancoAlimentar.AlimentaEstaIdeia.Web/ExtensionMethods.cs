// -----------------------------------------------------------------------
// <copyright file="ExtensionMethods.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web;

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
}
