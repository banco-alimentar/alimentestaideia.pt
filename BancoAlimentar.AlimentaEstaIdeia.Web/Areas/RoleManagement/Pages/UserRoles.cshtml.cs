namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.RoleManagement.Pages
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Areas.RoleManagement.Model;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.EntityFrameworkCore;

    public class UserRolesModel : PageModel
    {
        private readonly UserManager<WebUser> userManager;
        private readonly RoleManager<ApplicationRole> roleManager;

        public UserRolesModel(UserManager<WebUser> userManager, RoleManager<ApplicationRole> roleManager)
        {
            this.roleManager = roleManager;
            this.userManager = userManager;
        }

        public List<UserRolesViewModel> UserRolesViewModel { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var users = await userManager.Users.ToListAsync();
            UserRolesViewModel = new List<UserRolesViewModel>();
            foreach (WebUser user in users)
            {
                var thisViewModel = new UserRolesViewModel();
                thisViewModel.UserId = user.Id;
                thisViewModel.Email = user.Email;
                thisViewModel.FirstName = user.FullName;
                thisViewModel.Roles = await GetUserRoles(user);
                UserRolesViewModel.Add(thisViewModel);
            }

            return Page();
        }

        private async Task<List<string>> GetUserRoles(WebUser user)
        {
            return new List<string>(await userManager.GetRolesAsync(user));
        }
    }
}
