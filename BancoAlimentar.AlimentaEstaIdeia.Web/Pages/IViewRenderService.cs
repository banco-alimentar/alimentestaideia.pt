namespace BancoAlimentar.AlimentaEstaIdeia.Web.Pages
{
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc.RazorPages;

    public interface IViewRenderService
    {
        Task<string> RenderToStringAsync<T>(string viewName, string area, T model) where T : PageModel;
    }
}
