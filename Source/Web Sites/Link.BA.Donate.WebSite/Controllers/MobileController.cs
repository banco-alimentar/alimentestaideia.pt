using System.Web.Mvc;

namespace Link.BA.Donate.WebSite.Controllers
{
    public class MobileController : Controller
    {
        public ActionResult Index()
        {
            return RedirectToAction("Index", "Donation");
        }
    }
}