// -----------------------------------------------------------------------
// <copyright file="FoodBankDbInitializer.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Model.Initializer
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Resources;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Initialize the food bank table when migration.
    /// </summary>
    public static class FoodBankDbInitializer
    {
        /// <summary>
        /// Initialize the database.
        /// </summary>
        /// <param name="context">A reference to the <see cref="ApplicationDbContext"/>.</param>
        public static void Initialize(ApplicationDbContext context)
        {
            context.Database.EnsureCreated();

            if (context.FoodBanks.Any())
            {
                return;
            }

            string[] foodBanks = GetFoodBankFromResources();
            foreach (var item in foodBanks)
            {
                FoodBank foodBank = new FoodBank()
                {
                    Name = item,
                };

                context.FoodBanks.Add(foodBank);
            }

            context.SaveChanges();
        }

        /// <summary>
        /// Gets the food banks for the embedded resourde FoodBankList.txt.
        /// </summary>
        /// <returns>An array of the food bank to be inserted in the database.</returns>
        private static string[] GetFoodBankFromResources()
        {
            string[] result = null;

            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("BancoAlimentar.AlimentaEstaIdeia.Model.Initializer.FoodBankList.txt"))
            {
                using (StreamReader streamReader = new StreamReader(stream, true))
                {
                    string value = streamReader.ReadToEnd();
                    result = value.Split(Environment.NewLine);
                    result = result.Select(p => p.TrimEnd('\r')).ToArray();
                }
            }

            return result;
        }
    }
}
