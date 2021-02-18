namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.ViewModel;

    public class DonationRepository : GenericRepository<Donation>
    {
        public DonationRepository(ApplicationDbContext context)
            : base(context)
        {
        }

        public List<TotalDonationsResult> GetTotalDonations()
        {
            List<TotalDonationsResult> result = new List<TotalDonationsResult>();

            foreach (var product in this.DbContext.ProductCatalogues.ToList())
            {
                int sum = this.DbContext.DonationItems.Where(p => p.ProductCatalogue == product).Sum(p => p.Quantity);
                double total = product.Quantity.Value * sum;
                result.Add(new TotalDonationsResult()
                {
                    Cost = product.Cost,
                    Description = product.Description,
                    IconUrl = product.IconUrl,
                    Name = product.Name,
                    Quantity = product.Quantity,
                    Total = sum,
                    TotalCost = total,
                    UnitOfMeasure = product.UnitOfMeasure,
                });
            }

            return result;
        }
    }
}
