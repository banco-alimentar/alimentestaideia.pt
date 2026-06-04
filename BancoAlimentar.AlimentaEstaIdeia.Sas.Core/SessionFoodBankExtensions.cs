// -----------------------------------------------------------------------
// <copyright file="SessionFoodBankExtensions.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable disable

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core
{
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// Session helpers for payment processor configuration.
    /// </summary>
    public static class SessionFoodBankExtensions
    {
        /// <summary>
        /// Gets the current <see cref="FoodBank"/> id in session.
        /// </summary>
        /// <param name="session">A reference to the <see cref="ISession"/>.</param>
        /// <returns>The id for the current food bank.</returns>
        public static int? GetFoodBankId(this ISession session)
        {
            return session.GetInt32(typeof(FoodBank).Name);
        }
    }
}
