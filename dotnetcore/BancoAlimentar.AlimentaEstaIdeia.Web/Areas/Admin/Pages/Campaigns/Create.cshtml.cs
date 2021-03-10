namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages.Campaigns
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;

    public class CreateModel : PageModel
    {
        private readonly IUnitOfWork context;

        public CreateModel(IUnitOfWork context)
        {
            this.context = context;
        }

        public IActionResult OnGet()
        {
            return Page();
        }

        [BindProperty]
        public Campaign Campaign { get; set; }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            this.Campaign.ProductCatalogues = new List<ProductCatalogue>();

            Campaign currentCampaign = context.CampaignRepository.GetCurrentCampaign();
            foreach (var item in currentCampaign.ProductCatalogues)
            {
                this.Campaign.ProductCatalogues.Add(new ProductCatalogue()
                {
                    Cost = item.Cost,
                    Description = item.Description,
                    IconUrl = item.IconUrl,
                    Name = item.Name,
                    Quantity = item.Quantity,
                    UnitOfMeasure = item.UnitOfMeasure,
                    Campaign = this.Campaign,
                });
            }

            context.CampaignRepository.Add(Campaign);
            context.Complete();

            return RedirectToPage("./Index");
        }
    }
}
