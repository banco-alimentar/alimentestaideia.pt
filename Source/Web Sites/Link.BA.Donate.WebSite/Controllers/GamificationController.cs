using Acn.BA.Gamification.Business.Services;
using Acn.BA.Gamification.Models;
using Link.BA.Donate.WebSite.Models.Gamification;
using Microsoft.AppFabricCAT.Samples.Azure.TransientFaultHandling;
using Microsoft.AppFabricCAT.Samples.Azure.TransientFaultHandling.SqlAzure;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Resources;
using System.Security.Cryptography.X509Certificates;
using System.Web.Http;
using System.Web.UI.WebControls;

namespace Link.BA.Donate.WebSite.Controllers
{
    [RoutePrefix("api/gamification")]
    public class GamificationController : ApiController
    {
        const string USER_SESSION_COOKIE = "gmfcsess";
        private UserService _userService = null;
        private InvitesService _invitesService = null;
        private RetryPolicy _policy;

        public GamificationController() :
            this(
                new UserService(new Acn.BA.Gamification.Models.GamificationEntityModelContainer()),
                new InvitesService(new Acn.BA.Gamification.Models.GamificationEntityModelContainer(), new CustomerMessageService())
                )
        {
        }

        public GamificationController(UserService userService, InvitesService invitesService)
        {
            _userService = userService;
            _invitesService = invitesService;
            int maxRetries = Convert.ToInt32(ConfigurationManager.AppSettings["RetryPolicy.MaxRetries"]);
            int delayMs = Convert.ToInt32(ConfigurationManager.AppSettings["RetryPolicy.DelayMS"]);
            _policy = new RetryPolicy<SqlAzureTransientErrorDetectionStrategy>(maxRetries, TimeSpan.FromMilliseconds(delayMs));
        }

        [Route("user-session"), HttpGet]
        public HttpResponseMessage LoadUserSession(string userSessionCode)
        {
            HttpResponseMessage resp = null;
            _policy.ExecuteAction(() =>
            {
                var user = _userService.GetUserFromCode(userSessionCode);
                if (user != null)
                {
                    var cookie = new CookieHeaderValue(USER_SESSION_COOKIE, userSessionCode)
                    {
                        Expires = DateTime.UtcNow.AddYears(5),
                        HttpOnly = true,
                        Secure = true,
                    };
                    resp = new HttpResponseMessage(HttpStatusCode.OK);
                    resp.Headers.AddCookies(new CookieHeaderValue[] { cookie });
                }
            });
            return resp;
        }

        [Route("user-data"), HttpGet]
        public UserDataDto GetUserData()
        {
            var user = _userService.GetUserFromCode(GetUserSessionCodeFromCookie());
            if (user == null)
                throw new Exception("User not found");

            return UserDataDto.FromUser(user);
        }

        #region privates
        private string GetUserSessionCodeFromCookie()
        {
            var cookie = Request.Headers.GetCookies(USER_SESSION_COOKIE).FirstOrDefault();
            if (cookie != null && cookie.Cookies.Any(c => c.Value != null))
            {
                return cookie.Cookies.First(c => c.Value != null).Value;
            }
            else
            {
                throw new Exception("no session data available");
            }
        }
        #endregion
    }
}
