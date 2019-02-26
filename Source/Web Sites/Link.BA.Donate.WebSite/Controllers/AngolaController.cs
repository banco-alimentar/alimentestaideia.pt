using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Link.BA.Donate.WebSite.Controllers
{
    public class AngolaController : BaseController
    {
        [HandleError]
        public ActionResult Index()
        {
            return RedirectToAction("Angola", "Donation");
        }

    }
}
