namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages.Invoices
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.EntityFrameworkCore;

    public class IndexModel : PageModel
    {
        private readonly BancoAlimentar.AlimentaEstaIdeia.Model.ApplicationDbContext context;

        public IndexModel(BancoAlimentar.AlimentaEstaIdeia.Model.ApplicationDbContext context)
        {
            this.context = context;
        }

        public IList<Invoice> Invoice { get;set; }

        public async Task OnGetAsync()
        {
            Invoice = await context.Invoices.ToListAsync();
        }
    }
}
