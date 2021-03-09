namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages.ProductsCatalogues
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.EntityFrameworkCore;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;

    public class IndexModel : PageModel
    {
        private readonly IUnitOfWork context;

        public IndexModel(IUnitOfWork context)
        {
            this.context = context;
        }

        public IList<ProductCatalogue> ProductCatalogue { get; set; }

        public async Task OnGetAsync()
        {
            ProductCatalogue = this.context.ProductCatalogue.GetAllWithCampaign();
        }
    }
}
