namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages
{
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using Microsoft.AspNetCore.Mvc.RazorPages;

    public class DeleteUserModel : PageModel
    {
        private readonly IUnitOfWork context;

        public DeleteUserModel(IUnitOfWork context)
        {
            this.context = context;
        }

        public void OnGet()
        {
          // this.context.User.DeleteAllUsers();
        }
    }
}