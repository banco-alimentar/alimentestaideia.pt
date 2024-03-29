﻿// <copyright file="ProductCatalogueDbInitializer.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>

namespace BancoAlimentar.AlimentaEstaIdeia.Model.Initializer
{
    using System;
    using System.Linq;

    /// <summary>
    /// Initialize the product catalogue table when migration.
    /// </summary>
    public static class ProductCatalogueDbInitializer
    {
        /// <summary>
        /// Initialize the database.
        /// </summary>
        /// <param name="context">A reference to the <see cref="ApplicationDbContext"/>.</param>
        public static void Initialize(ApplicationDbContext context)
        {
            context.Database.EnsureCreated();

            if (!context.ProductCatalogues.Any())
            {
                Campaign campaign = new Campaign()
                {
                    Start = DateTime.UtcNow,
                    End = DateTime.UtcNow.AddYears(100),
                    IsDefaultCampaign = true,
                    Number = "default",
                };

                context.Campaigns.Add(campaign);

                context.ProductCatalogues.Add(new ProductCatalogue()
                {
                    Name = "Azeite",
                    Description = "Azeite",
                    UnitOfMeasure = "L",
                    Cost = 2.5d,
                    IconUrl = "azeite.png",
                    Quantity = 1,
                    Campaign = campaign,
                });
                context.ProductCatalogues.Add(new ProductCatalogue()
                {
                    Name = "Óleo",
                    Description = "Óleo",
                    UnitOfMeasure = "L",
                    Cost = 1d,
                    IconUrl = "oleo.png",
                    Quantity = 1,
                    Campaign = campaign,
                });
                context.ProductCatalogues.Add(new ProductCatalogue()
                {
                    Name = "Leite",
                    Description = "Leite",
                    UnitOfMeasure = "L",
                    Cost = 0.45d,
                    IconUrl = "leite.png",
                    Quantity = 1,
                    Campaign = campaign,
                });
                context.ProductCatalogues.Add(new ProductCatalogue()
                {
                    Name = "Atum",
                    Description = "Atum",
                    UnitOfMeasure = "kg",
                    Cost = 0.88d,
                    IconUrl = "atum.png",
                    Quantity = 0.120,
                    Campaign = campaign,
                });
                context.ProductCatalogues.Add(new ProductCatalogue()
                {
                    Name = "Salsichas",
                    Description = "Salsichas",
                    UnitOfMeasure = "kg",
                    Cost = 0.4d,
                    IconUrl = "salsichas.png",
                    Quantity = 0.430,
                    Campaign = campaign,
                });
                context.ProductCatalogues.Add(new ProductCatalogue()
                {
                    Name = "Arroz",
                    Description = "Arroz",
                    UnitOfMeasure = "kg",
                    Cost = 0.7d,
                    IconUrl = "acucar.png",
                    Quantity = 1,
                    Campaign = campaign,
                });

                context.SaveChanges();
            }

            ProductCatalogue cashProductCatalogue = context.ProductCatalogues
                .Where(p => p.Name == ProductCatalogue.CashProductCatalogName)
                .FirstOrDefault();

            if (cashProductCatalogue == null)
            {
                cashProductCatalogue = new ProductCatalogue()
                {
                    Name = ProductCatalogue.CashProductCatalogName,
                    IconUrl = "cash.png",
                    UnitOfMeasure = "€",
                    Quantity = 1,
                };

                context.ProductCatalogues.Add(cashProductCatalogue);
                context.SaveChanges();
            }
        }
    }
}
