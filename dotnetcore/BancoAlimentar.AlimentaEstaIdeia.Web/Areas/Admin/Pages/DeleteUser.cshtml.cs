using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
using BancoAlimentar.AlimentaEstaIdeia.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages
{
    public class DeleteUserModel : PageModel
    {
        private readonly IUnitOfWork context;

        public DeleteUserModel(IUnitOfWork context)
        {
            this.context = context;
        }

        public void OnGet()
        {
            //this.context.User.DeleteUserAndDonations("7cc175df-b94b-418d-b5d0-2ebb028b1890");
        }
    }
}
