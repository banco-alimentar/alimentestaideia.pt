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
        private const int CaboVerdeFoodBank = 22;
        private const int AngolaFoodBank = 24;

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

            IList<LastPayedDonationEntity> lastPayedDonations = referenceView.Contains("CaboVerde")
                                                                    ? donation.GetLastPayedDonationsByFoodBank(
                                                                        totalPayedDonations, null, null, CaboVerdeFoodBank)
                                                                        : (referenceView.Contains("Angola") ? donation.GetLastPayedDonationsByFoodBank(
                                                                        totalPayedDonations, null, null, AngolaFoodBank) : donation.GetLastPayedDonations(
                                                                        totalPayedDonations, null, null));

            ViewBag.LastPayedDonations = lastPayedDonations;

            IList<TotalDonationsEntity> totalDonations = referenceView.Contains("CaboVerde")
                                                             ? donation.GetTotalDonationsByFoodBank(CaboVerdeFoodBank)
                                                             : (referenceView.Contains("Angola") ? donation.GetTotalDonationsByFoodBank(AngolaFoodBank) : donation.GetTotalDonations());

            ViewBag.TotalDonations = totalDonations;

            IList<FoodBankEntity> foodBanks = referenceView.Contains("CaboVerde")
                                                  ? donation.GetFoodBanksByFoodBank(CaboVerdeFoodBank)
                                                  : (referenceView.Contains("Angola") ? donation.GetFoodBanksByFoodBank(AngolaFoodBank) : donation.GetFoodBanks());

            foodBanks.Insert(0, null);

            ViewBag.FoodBanks = foodBanks;

            ViewBag.ProductCatalogue = donation.GetProductCatalogue();
        }
    }
}
