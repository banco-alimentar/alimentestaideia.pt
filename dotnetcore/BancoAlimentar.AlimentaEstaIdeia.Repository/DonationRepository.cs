namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System.Collections.Generic;
    using System.Linq;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.ViewModel;

    /// <summary>
    /// Default implementation for the <see cref="Donation"/> repository patter.
    /// </summary>
    public class DonationRepository : GenericRepository<Donation>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DonationRepository"/> class.
        /// </summary>
        /// <param name="context"><see cref="ApplicationDbContext"/> instance.</param>
        public DonationRepository(ApplicationDbContext context)
            : base(context)
        {
        }

        /// <summary>
        /// Gets total donations sum for all the elements in the product catalogues.
        /// </summary>
        /// <returns>Return a <see cref="TotalDonationsResult"/> list.</returns>
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
