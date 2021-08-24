// -----------------------------------------------------------------------
// <copyright file="Extensions.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Class for extensions methods.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Gets the Portugal date time, based on the <see cref="DateTime"/>.
        /// </summary>
        /// <param name="value">The <see cref="DateTime"/> to convert the date to Portugal datetime.</param>
        /// <returns>A instance of <see cref="DateTime"/> in Portugal datetime.</returns>
        public static DateTime GetPortugalDateTime(this DateTime value)
        {
            return TimeZoneInfo.ConvertTime(value, TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time"));
        }
    }
}
