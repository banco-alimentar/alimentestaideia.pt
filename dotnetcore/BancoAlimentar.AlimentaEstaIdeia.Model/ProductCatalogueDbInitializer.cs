namespace BancoAlimentar.AlimentaEstaIdeia.Model
{
    using System.Linq;

    /// <summary>
    /// Initialize the product catalogue table when migration.
    /// </summary>
    public static class ProductCatalogueDbInitializer
    {
        public static void Initialize(ApplicationDbContext context)
        {
            context.Database.EnsureCreated();

            if (context.ProductCatalogues.Any())
            {
                return;
            }

            context.ProductCatalogues.Add(new ProductCatalogue()
            {
                Name = "Azeite",
                Description = "Azeite",
                UnitOfMeasure = "L",
                Cost = 2.3d,
                IconUrl = "azeite.png",
                Quantity = 1,
            });
            context.ProductCatalogues.Add(new ProductCatalogue()
            {
                Name = "Óleo",
                Description = "Óleo",
                UnitOfMeasure = "L",
                Cost = 1d,
                IconUrl = "oleo.png",
                Quantity = 1,
            });
            context.ProductCatalogues.Add(new ProductCatalogue()
            {
                Name = "Leite",
                Description = "Leite",
                UnitOfMeasure = "L",
                Cost = 0.4d,
                IconUrl = "leite.png",
                Quantity = 1,
            });
            context.ProductCatalogues.Add(new ProductCatalogue()
            {
                Name = "Atum",
                Description = "Atum",
                UnitOfMeasure = "kg",
                Cost = 0.7d,
                IconUrl = "atum.png",
                Quantity = 0.120,
            });
            context.ProductCatalogues.Add(new ProductCatalogue()
            {
                Name = "Salsichas",
                Description = "Salsichas",
                UnitOfMeasure = "kg",
                Cost = 0.4d,
                IconUrl = "salsichas.png",
                Quantity = 0.430,
            });
            context.ProductCatalogues.Add(new ProductCatalogue()
            {
                Name = "Arroz",
                Description = "Arroz",
                UnitOfMeasure = "kg",
                Cost = 0.6d,
                IconUrl = "acucar.png",
                Quantity = 1,
            });

            context.SaveChanges();
        }
    }
}
