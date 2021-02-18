namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class ProductCatalogueRepository : GenericRepository<ProductCatalogue>
    {
        public ProductCatalogueRepository(ApplicationDbContext context)
            : base(context)
        {
        }
    }
}
