using System;
using System.Configuration;
using System.Globalization;
using System.Threading;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;
using System.Web.Routing;

namespace Link.BA.Donate.WebSite
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class MvcApplication : System.Web.HttpApplication
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }

        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            routes.IgnoreRoute("favicon.ico");
            routes.IgnoreRoute("{*apple}", new { apple = @"(.*/)?apple-touch-icon.*\.png(/.*)?" });

            routes.MapRoute(
                "Default", // Route name
                "{controller}/{action}/{id}", // URL with parameters
                new { controller = "Donation", action = "Index", id = UrlParameter.Optional } // Parameter defaults
                );
        }

        protected void Application_Start()
        {
#if RELEASE
            foreach (ConnectionStringSettings item in WebConfigurationManager.ConnectionStrings)
            {
                if (item.Name == "BancoAlimentarEntities")
                {
                    item.ProviderName = "System.Data.EntityClient";
                }
            }
#endif
            AreaRegistration.RegisterAllAreas();

            RegisterGlobalFilters(GlobalFilters.Filters);
            RegisterRoutes(RouteTable.Routes);
        }

        protected void Application_AcquireRequestState(object sender, EventArgs e)
        {
            //It's important to check whether session object is ready
            if (HttpContext.Current.Session != null)
            {
                var ci = (CultureInfo)Session["Culture"];
                //Checking first if there is no value in session 
                //and set default language 
                //this can happen for first user's request
                if (ci == null)
                {
                    //Sets default culture to portuguese invariant
                    string langName = "pt";

                    //Try to get values from Accept lang HTTP header
                    if (HttpContext.Current.Request.UserLanguages != null &&
                        HttpContext.Current.Request.UserLanguages.Length != 0)
                    {
                        //Gets accepted list 
                        langName = HttpContext.Current.Request.UserLanguages[0].Substring(0, 2);
                    }
                    ci = new CultureInfo(langName);
                    this.Session["Culture"] = ci;
                }
                //Finally setting culture for each request
                Thread.CurrentThread.CurrentUICulture = ci;
                Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture(ci.Name);
                Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator = ".";
            }
        }
    }
}