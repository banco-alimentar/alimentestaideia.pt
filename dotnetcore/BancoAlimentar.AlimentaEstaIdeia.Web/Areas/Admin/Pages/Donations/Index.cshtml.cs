namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages.Donations
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

        public IList<Donation> Donation { get;set; }

        public async Task OnGetAsync()
        {
            Donation = await this.context.Donation.GetAll().ToListAsync();
        }
    }
}
