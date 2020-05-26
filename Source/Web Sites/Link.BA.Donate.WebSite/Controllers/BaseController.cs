using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
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
                IList<SumTotalPayedDonationEntity> payedDonationEntities = donation.GetSumTotalPayedDonation();

                ViewBag.SumTotalDonations = payedDonationEntities[0].SumTotalPayedDonation;
            }

            IList<LastPayedDonationEntity> lastPayedDonations = donation.GetLastPayedDonations(totalPayedDonations, null, null);

            ViewBag.LastPayedDonations = lastPayedDonations;

            IList<TotalDonationsEntity> totalDonations = donation.GetTotalDonations();

            ViewBag.TotalDonations = totalDonations;

            IList<FoodBankEntity> foodBanks = donation.GetFoodBanks();

            foodBanks.Insert(0, null);

            ViewBag.FoodBanks = foodBanks;

            ViewBag.ProductCatalogue = donation.GetProductCatalogue();
        }
    }
}
