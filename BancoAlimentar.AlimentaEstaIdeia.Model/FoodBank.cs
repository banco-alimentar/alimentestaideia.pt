// <copyright file="FoodBank.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>

namespace BancoAlimentar.AlimentaEstaIdeia.Model
{
    using Microsoft.AspNetCore.Identity;

    /// <summary>
    /// Represent a food bank.
    /// </summary>
    public class FoodBank
    {
        /// <summary>
        /// Gets or sets the unique id.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the food bank.
        /// </summary>
        [PersonalData]
        public string Name { get; set; }
    }
}
