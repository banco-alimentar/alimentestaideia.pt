using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;

namespace Link.BA.Donate.WebSite.Controllers
{
    public class ErrorController : Controller
    {
        private TelemetryClient telemetryClient = new TelemetryClient(TelemetryConfiguration.Active);

        public ActionResult NotFound()
        {

            telemetryClient.TrackEvent("NotFound");
            Response.StatusCode = (int)HttpStatusCode.NotFound;
            return View();
        }

        public ActionResult ServerError()
        {
            telemetryClient.TrackEvent("ServerError");
            Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            return View();
        }

        public ActionResult ThrowError()
        {
            throw new NotImplementedException("Pew ^ Pew");
        }
    }
}