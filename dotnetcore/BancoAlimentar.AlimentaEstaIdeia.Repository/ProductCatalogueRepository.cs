﻿namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using BancoAlimentar.AlimentaEstaIdeia.Model;

    /// <summary>
    /// Default implementation for the <see cref="ProductCatalogue"/> repository patter.
    /// </summary>
    public class ProductCatalogueRepository : GenericRepository<ProductCatalogue>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProductCatalogueRepository"/> class.
        /// </summary>
        /// <param name="context"><see cref="ApplicationDbContext"/> instance.</param>
        public ProductCatalogueRepository(ApplicationDbContext context)
            : base(context)
        {
        }
    }
}
