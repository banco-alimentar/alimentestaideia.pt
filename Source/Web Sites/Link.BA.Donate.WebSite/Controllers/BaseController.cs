using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Caching;
using System.Web.Mvc;
using Link.BA.Donate.Models;

namespace Link.BA.Donate.WebSite.Controllers
{
    public class BaseController : Controller
    {
        protected void LoadBaseData(string referenceView)
        {
            var donation = new Business.Donation();
            int totalPayedDonations = 40;
            if (referenceView.Contains("IndexFB"))
            {
                totalPayedDonations = 52;
            }
            else if (referenceView.Contains("Obrigado"))
            {
                IList<SumTotalPayedDonationEntity> payedDonationEntities = GetCachedElement("GetSumTotalPayedDonation", () => { return donation.GetSumTotalPayedDonation(); });

                ViewBag.SumTotalDonations = payedDonationEntities[0].SumTotalPayedDonation;
            }

            IList<LastPayedDonationEntity> lastPayedDonations = GetCachedElement($"GetLastPayedDonations{totalPayedDonations}", () => { return donation.GetLastPayedDonations(totalPayedDonations, null, null); });

            ViewBag.LastPayedDonations = lastPayedDonations;

            IList<TotalDonationsEntity> totalDonations = GetCachedElement("GetTotalDonations", () => { return donation.GetTotalDonations(); });

            ViewBag.TotalDonations = totalDonations;

            IList<FoodBankEntity> foodBanks = GetCachedElement("GetFoodBanks", () => { return donation.GetFoodBanks(); });

            if (foodBanks.ElementAt(0) != null)
            {
                foodBanks.Insert(0, null);
            }

            ViewBag.FoodBanks = foodBanks;

            ViewBag.ProductCatalogue = GetCachedElement("GetProductCatalogue", () => { return donation.GetProductCatalogue(); });
        }

        private T GetCachedElement<T>(string key, Func<T> builder) where T : class
        {
            T result = default(T);

            Cache cache = HttpContext.Cache;
            object target = cache.Get(key);
            if (target != null)
            {
                if (typeof(T).IsAssignableFrom(target.GetType()))
                {
                    result = (T)target;
                }
            }
            else
            {
                result = builder();
                cache.Insert(key, result, null, Cache.NoAbsoluteExpiration, TimeSpan.FromMinutes(5));
            }

            return result;
        }
    }
}
