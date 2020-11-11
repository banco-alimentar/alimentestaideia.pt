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
        private DonationLoadService _donationLoadService = null;
        private RetryPolicy _policy;

        public GamificationController() :
            this(new GamificationDbContext())
        {
        }

        public GamificationController(GamificationDbContext db):
            this(
                new UserService(db),
                new InvitesService(db, new CustomerMessageService()),
                new DonationLoadService(db)
                )
        {

        }

        public GamificationController(UserService userService, InvitesService invitesService, DonationLoadService donationLoadService)
        {
            _userService = userService;
            _invitesService = invitesService;
            _donationLoadService = donationLoadService;
            int maxRetries = Convert.ToInt32(ConfigurationManager.AppSettings["RetryPolicy.MaxRetries"]);
            int delayMs = Convert.ToInt32(ConfigurationManager.AppSettings["RetryPolicy.DelayMS"]);
            _policy = new RetryPolicy<SqlAzureTransientErrorDetectionStrategy>(maxRetries, TimeSpan.FromMilliseconds(delayMs));
        }

        /// <summary>
        /// Fetches the user session from the sessionCode and creates the session cookie
        /// </summary>
        /// <param name="userSessionCode"></param>
        /// <returns></returns>
        [Route("user-session"), HttpGet]
        public HttpResponseMessage LoadUserSession(string userSessionCode)
        {
            HttpResponseMessage resp = null;
            _policy.ExecuteAction(() =>
            {
                var user = _userService.GetUserFromCode(userSessionCode);
                var cookie = new CookieHeaderValue(USER_SESSION_COOKIE, userSessionCode)
                {
                    Expires = DateTime.UtcNow.AddYears(5),
#if DEBUG
                    Secure = false,
#else
                    Secure = true,
#endif
                    HttpOnly = true,
                    Path = "/",
                };
                resp = new HttpResponseMessage(HttpStatusCode.OK);
                resp.Headers.AddCookies(new CookieHeaderValue[] { cookie });
            });
            return resp;
        }

        /// <summary>
        /// Gets user data for the user dashboard
        /// </summary>
        /// <returns></returns>
        [Route("user-data"), HttpGet]
        public UserDataDto GetUserData()
        {
            UserDataDto result = null;
            _policy.ExecuteAction(() =>
            {
                var user = _userService.GetUserFromCode(GetUserSessionCodeFromCookie());
                result = UserDataDto.FromUser(user);
            });
            return result;
        }

        /// <summary>
        /// Pokes a previously invited user
        /// </summary>
        /// <param name="inviteId">the id from the invite to poke</param>
        /// <returns></returns>
        [Route("poke/{userId}"), HttpPost]
        public IHttpActionResult Poke([FromUri]int inviteId)
        {
            _policy.ExecuteAction(() =>
            {
                var fromUser = _userService.GetUserFromCode(GetUserSessionCodeFromCookie());

                _invitesService.Poke(fromUser, inviteId);
            });
            return Ok();
        }


        [Route("load-batch"), HttpPost]
        public IHttpActionResult LoadBatch()
        {
            _donationLoadService.LoadPendingDonations();
            return Ok();
        }

#region debug
#if DEBUG
        [Route("create-donation"), HttpPost]
        public IHttpActionResult CreateDonation(int qt = 1)
        {
            var rng = new Random();
            for (int idx = 0; idx < qt; idx++)
            {
                string user = Guid.NewGuid().ToString(),
                   user1 = Guid.NewGuid().ToString(),
                   user2 = Guid.NewGuid().ToString(),
                   user3 = Guid.NewGuid().ToString();
                var donation = new CompletedDonation()
                {
                    Amount = Convert.ToDecimal(rng.NextDouble()) * 50,
                    Email = user.Replace("-", "").Substring(0, 10),
                    Id = rng.Next(),
                    Name = user,
                    User1Email = user1.Replace("-", "").Substring(0, 10),
                    User1Name = user1,
                    User2Email = user2.Replace("-", "").Substring(0, 10),
                    User2Name = user2,
                };
                _donationLoadService.AddCompletedDonation(donation);
            }
            return Ok();
        }
#endif
#endregion debug

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
