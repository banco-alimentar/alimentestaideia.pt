using System.Collections.Generic;
using System.Data.Objects;
using Link.BA.Donate.Models;

namespace Link.BA.Donate.Business
{
    public class ProductCatalogue
    {
        public IList<ProductCatalogueEntity> GetProductCatalogue()
        {
            IList<ProductCatalogueEntity> donationList = new List<ProductCatalogueEntity>();

            using (var entities = new BancoAlimentarEntities())
            {
                ObjectResult<ProductCatalogueEntity> productCatalogueEntities = entities.GetProductCatalogue();
                foreach (ProductCatalogueEntity productCatalogueEntity in productCatalogueEntities)
                {
                    donationList.Add(productCatalogueEntity);
                }
            }

            return donationList;
        }
    }
}