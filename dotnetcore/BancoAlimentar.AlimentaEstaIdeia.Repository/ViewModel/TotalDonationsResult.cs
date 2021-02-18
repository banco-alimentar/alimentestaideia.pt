namespace BancoAlimentar.AlimentaEstaIdeia.Repository.ViewModel
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class TotalDonationsResult
    {
        public double Total { get; set; }

        public double TotalCost { get; set; }

        public int ProductCatalogueId { get; set; }

        public string Name { get; set; }

        public string IconUrl { get; set; }

        public string UnitOfMeasure { get; set; }

        public string Description { get; set; }

        public double Cost { get; set; }

        public double? Quantity { get; set; }
    }
}
