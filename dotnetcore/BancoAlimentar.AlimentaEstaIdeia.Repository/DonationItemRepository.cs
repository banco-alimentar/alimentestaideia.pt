namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System.Collections.Generic;
    using System.Linq;
    using BancoAlimentar.AlimentaEstaIdeia.Model;

    /// <summary>
    /// Default implementation for the <see cref="DonationItem"/> repository pattern.
    /// </summary>
    public class DonationItemRepository : GenericRepository<DonationItem>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DonationItemRepository"/> class.
        /// </summary>
        /// <param name="context"><see cref="ApplicationDbContext"/> instance.</param>
        public DonationItemRepository(ApplicationDbContext context)
            : base(context)
        {
        }

        /// <summary>
        /// Gets the Donations items from the string representation.
        /// </summary>
        /// <param name="value">String with the donation items.</param>
        /// <returns>A reference to <see cref="ICollection{DonationItem}"/> with all the donation items.</returns>
        public List<DonationItem> GetDonationItems(string value)
        {
            List<DonationItem> result = new List<DonationItem>();

            if (!string.IsNullOrEmpty(value))
            {
                foreach (string pair in value.Split(new[] { ';' }))
                {
                    string[] values = pair.Split(":");
                    int id;
                    int quantity;
                    if (values.Length == 2 && int.TryParse(values[0], out id) && int.TryParse(values[1], out quantity))
                    {
                        ProductCatalogue product = this.DbContext.ProductCatalogues.Where(p => p.Id == id).First();

                        DonationItem donationItem = new DonationItem()
                        {
                            Quantity = quantity,
                            ProductCatalogue = product,
                            Price = product.Cost,
                        };

                        result.Add(donationItem);
                    }
                }
            }

            return result;
        }
    }
}
