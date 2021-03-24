namespace BancoAlimentar.AlimentaEstaIdeia.Web.Pages
{
    using System.Collections.Generic;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.ViewModel;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.Extensions.Logging;

    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> logger;
        private DonationRepository donationRepository;
        private ProductCatalogueRepository productCatalogueRepository;

        public IndexModel(ILogger<IndexModel> logger, DonationRepository donationRepository, ProductCatalogueRepository productCatalogueRepository)
        {
            this.logger = logger;
            this.donationRepository = donationRepository;
            this.productCatalogueRepository = productCatalogueRepository;
        }

        public List<TotalDonationsResult> TotalDonations { get; set; }

        public IReadOnlyList<ProductCatalogue> ProductCatalogue { get; set; }

        public void OnGet()
        {
            LoadData();
        }

        public void OnPost()
        {
            LoadData();
        }

        private void LoadData()
        {
            ProductCatalogue = this.productCatalogueRepository.GetCurrentProductCatalogue();
            TotalDonations = this.donationRepository.GetTotalDonations(this.ProductCatalogue);

            ViewData["IsPostBack"] = false;
            ViewData["HasReference"] = false;
            ViewData["IsMultibanco"] = false;
        }
    }
}
