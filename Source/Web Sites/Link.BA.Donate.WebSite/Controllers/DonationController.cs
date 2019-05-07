﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using System.Web.Services;
using CaptchaMVC.HtmlHelpers;
using CaptchaMVC.Models;
using Link.BA.Donate.Business;
using Link.BA.Donate.Models;
using Link.BA.Donate.WebSite.Models;
using System.Web.UI;
using System.Web.Script.Services;
using System.Data.Objects;
using System.Configuration;
using Donation = Link.BA.Donate.Models.Donation;
using System.Net;

namespace Link.BA.Donate.WebSite.Controllers
{
    public class DonationController : BaseController
    {
        private string pass = ConfigurationManager.AppSettings["CaptchaPass"];
        private string salt = ConfigurationManager.AppSettings["CaptchaSalt"];

        private static string ThankyouViewName = "Obrigado";
        private const string ProdutionEndDate = "Site.Prodution.End.Date";
        private const string ProdutionStartDate = "Site.Prodution.Start.Date";
        private const string DestinationTimeZoneId = "GMT Standard Time";
        private const string PortugalCulture = "pt-PT";

        private const string CurrencyCode = "978";
        private const string PaymentAction = "101";
        private const string PaymentMode = "CPT";
        private const string LanguageCode = "pt";
        private const string SecurityMode = "SSL";

        private const int MultibancoPaymentMode = 1;
        private const int RedunicrePaymentMode = 2;

        #region Remax

        [HandleError]
        public ActionResult Recruitment()
        {
            return View();
        }

        [HandleError]
        [ValidateAntiForgeryToken]
        [HttpPost]
        public ActionResult AddRecruit(RecruitmentViewModel recruitmentViewModel)
        {
            if (ModelState.IsValid)
            {
                RemaxContactEntity remaxContactEntity = new RemaxContactEntity();

                remaxContactEntity.Name = recruitmentViewModel.Name;
                remaxContactEntity.Email = recruitmentViewModel.Email;
                remaxContactEntity.PhoneNumber = recruitmentViewModel.Phone;
                remaxContactEntity.Reference = string.Empty;
                remaxContactEntity.Campaign = "Recruit";

                Business.Remax remax = new Business.Remax();

                // add recruit to the database
                if (!remax.InsertContact(remaxContactEntity))
                {
                    // show message recruited added with success
                    // on error: ModelState.AddModelError("Error", "O sistema está em manutenção, tente mais tarde.");
                }

            }
            return View("Recruitment");
        }

        [HandleError]
        public ActionResult Campaign500()
        {
            return View();
        }

        [HandleError]
        public ActionResult Campaign500Form()
        {
            return View();
        }

        [HandleError]
        public ActionResult Campaign500Confirm(RecruitmentViewModel recruitmentViewModel)
        {
            if (ModelState.IsValid)
            {
                RemaxContactEntity remaxContactEntity = new RemaxContactEntity();

                remaxContactEntity.Name = recruitmentViewModel.Name;
                remaxContactEntity.Email = recruitmentViewModel.Email;
                remaxContactEntity.PhoneNumber = recruitmentViewModel.Phone;
                remaxContactEntity.Reference = string.Empty;
                remaxContactEntity.Campaign = "500";

                Business.Remax remax = new Business.Remax();

                // add recruit to the database
                if (!remax.InsertContact(remaxContactEntity))
                {
                    // show message recruited added with success
                    // on error: ModelState.AddModelError("Error", "O sistema está em manutenção, tente mais tarde.");
                    return View();
                }
                // add register to the database
                return View("Campaign500Confirm");
            }
            return View();
        }

        #endregion Remax

        [HandleError]
        public ActionResult Obrigado()
        {
            ViewBag.HasReference = false;
            LoadBaseData("Obrigado");
            return View();
        }

        [HandleError]
        public ActionResult Countdown()
        {
            return View();
        }

        [HandleError]
        public ActionResult IndexIsabel()
        {
            ViewBag.IsPostBack = false;
            ViewBag.FromCaboVerde = ViewBag.FromAngola = false;


            if (!IsProductionDate())
            {
                return RedirectToActionPermanent(ThankyouViewName);
            }
            ViewBag.HasReference = false;
            LoadBaseData("Index");
            return View();
        }

        [HandleError]
        public ActionResult Index()
        {
            ViewBag.IsPostBack = false;
            ViewBag.FromCaboVerde = ViewBag.FromAngola = false;

            // EXAMPLE: detect we are on Azure. RoleEnvironment.IsAvailable

            //if (!IpCanDonate())
            //{                 
            //    CultureInfo culture = new CultureInfo("pt-PT");
            //    DateTime productionDate = Convert.ToDateTime(ConfigurationManager.AppSettings["Site.Prodution.End.Date"], culture);
            //    if (DateTime.Now.CompareTo(productionDate) < 0)
            //    {
            //        return View("Countdown");
            //    }                
            //}


            if (!IsProductionDate())
            {
                return RedirectToActionPermanent(ThankyouViewName);
            }
            ViewBag.HasReference = false;
            LoadBaseData("Index");
            return View();
        }

        /*
        [HandleError]
        public ActionResult Dummy()
        {
            if (!IsProductionDate())
            {
                return RedirectToActionPermanent(ThankyouViewName);
            }
            ViewBag.HasReference = false;
            LoadBaseData("Index");
            return View();
        }
        */

        [HandleError]
        public ActionResult IndexFB()
        {
            ViewBag.FromCaboVerde = ViewBag.FromAngola = false;

            if (!IsProductionDate())
            {
                ViewBag.InProduction = false;
                return RedirectToActionPermanent("IndexFBTab");
            }
            ViewBag.HasReference = false;
            LoadBaseData("IndexFB");
            return View();
        }

        public ActionResult IndexFBTab()
        {
            ViewBag.FromCaboVerde = ViewBag.FromAngola = false;

            ViewBag.InProduction = true;
            if (!IsProductionDate())
            {
                ViewBag.InProduction = false;
                //return RedirectToActionPermanent(ThankyouViewName);
            }
            return View();
        }

        [HandleError]
        public ActionResult IndexMSN()
        {
            ViewBag.FromCaboVerde = ViewBag.FromAngola = false;

            if (!IsProductionDate())
            {
                return RedirectToActionPermanent(ThankyouViewName);
            }
            return View();
        }

        [HandleError]
        public ActionResult IndexHack()
        {
            ViewBag.FromCaboVerde = ViewBag.FromAngola = false;

            if (!IsProductionDate())
            {
                return RedirectToActionPermanent(ThankyouViewName);
            }
            ViewBag.HasReference = false;

            ViewBag.RefreshTime = ConfigurationManager.AppSettings["Mobile.Refresh"];

            LoadBaseData("IndexFB");
            return View("IndexLink");
        }

        [HandleError]
        public ActionResult IndexMobile()
        {
            ViewBag.FromCaboVerde = ViewBag.FromAngola = false;

            if (!IsProductionDate())
            {
                return RedirectToActionPermanent(ThankyouViewName);
            }
            ViewBag.HasReference = false;
            ViewBag.RefreshTime = ConfigurationManager.AppSettings["Mobile.Refresh"];
            LoadBaseData("IndexMobile");
            return View("IndexMobile");
        }

        [HandleError]
        [ValidateAntiForgeryToken]
        [HttpPost]
        public ActionResult Donate(CaptchaModel captchaModel, DonateViewModel donateViewModel)
        {
            ViewBag.IsPostBack = true;

            ViewBag.HasReference = false;

            // Facebook, MSN & Mobile App Support
            string referenceView = "Index";
            referenceView = SetReferenceView(donateViewModel);

            if (ModelState.IsValid)
            {
                if (!CaptchaHelper.Verify(captchaModel))
                {
                    ModelState.AddModelError("Error", "Palavra inválida.");
                    ViewBag.HasReference = false;
                    LoadBaseData(referenceView);

                    return View(referenceView);
                }

                var donationEntity = new Donation
                {
                    Anonym = false,
                    DonationDate = DateTime.Now,
                    DonationStatusId = (int)DonationStatus.Status.WaitingPayment,
                    DonationStatusDate = DateTime.Now,
                    ServiceAmount = (decimal?)donateViewModel.Amount,
                    FoodBankId = donateViewModel.FoodBankId,
                    Donor = new Donor
                    {
                        Name = CleanInput(donateViewModel.Name, 256),
                        NIF = CleanInput(donateViewModel.Nif, 20),
                        Email = CleanInput(donateViewModel.Email, 128),
                        Organization = !donateViewModel.Private,
                        RegisterDate = DateTime.Now,
                        DonorAddress =
                            new DonorAddress
                            {
                                Address1 =
                                    CleanInput(donateViewModel.Address, 256),
                                Address2 =
                                    CleanInput(donateViewModel.Country, 256),
                                PostalCode =
                                    CleanInput(donateViewModel.PostalCode, 20),
                                City = CleanInput(donateViewModel.City, 256)
                            },
                        CompanyName = donateViewModel.CompanyName
                    },
                    DonationItem = new List<DonationItem>(),
                    WantsReceipt = donateViewModel.WantsReceipt
                };

                string productCatalogueId = null, quantity = null;
                int counter = 0;
                foreach (string idAndAmount in donateViewModel.DonatedItems.Split(new[] { ';' }))
                {
                    if (string.IsNullOrEmpty(idAndAmount)) continue;
                    counter = 0;
                    foreach (string item in idAndAmount.Split(new[] { ':' }))
                    {
                        if (counter == 0)
                        {
                            productCatalogueId = item;
                            counter++;
                        }
                        else
                        {
                            quantity = item;
                        }
                    }
                    donationEntity.DonationItem.Add(new DonationItem
                    {
                        ProductCatalogueId = Convert.ToInt32(productCatalogueId),
                        Quantity = Convert.ToInt32(quantity)
                    });
                }

                if (donateViewModel.Picture != null && donateViewModel.Picture.ContentLength > 0)
                {
                    // TODO: implement this correctly
                    switch (donateViewModel.Picture.ContentType)
                    {
                        case "image/pjpeg":
                        case "image/x-png":
                        case "image/png":
                        case "image/gif":
                        case "image/jpeg":
                            break;
                        default:
                            ModelState.AddModelError("Error", "Só são aceites ficheiros jpg, gif e png.");
                            ViewBag.HasReference = false;
                            LoadBaseData(referenceView);
                            return View(referenceView);
                    }
                    if (donateViewModel.Picture.ContentLength >= 307200)
                    {
                        ModelState.AddModelError("Error", "O tamanho máximo da foto é 300 KB.");
                        ViewBag.HasReference = false;
                        LoadBaseData(referenceView);
                        return View(referenceView);
                    }

                    // TODO: validate if it's an image with max size of 300 Kb
                    var file = new byte[donateViewModel.Picture.ContentLength];
                    Stream stream = donateViewModel.Picture.InputStream;
                    stream.Read(file, 0, donateViewModel.Picture.ContentLength);

                    donationEntity.Anonym = false;
                    donationEntity.Donor.Picture = file;
                    donationEntity.Donor.PictureContentType = donateViewModel.Picture.ContentType;
                }

                var mailMessagePath = new MailMessagePath();

                mailMessagePath.ReferenceToDonorPath =
                    Server.MapPath(ConfigurationManager.AppSettings["Email.ReferenceToDonor.Body.Path"]);
                mailMessagePath.PaymentToDonorPath =
                    Server.MapPath(ConfigurationManager.AppSettings["Email.PaymentToDonor.Body.Path"]);
                mailMessagePath.ReceiptToDonorPath =
                    Server.MapPath(ConfigurationManager.AppSettings["Email.ReceiptToDonor.Body.Path"]);
                mailMessagePath.ReceiptTemplatePath =
                    Server.MapPath(ConfigurationManager.AppSettings["Email.ReceiptTemplate.Path"]);
                mailMessagePath.PaymentToBancoAlimentarPath =
                    Server.MapPath(ConfigurationManager.AppSettings["Email.PaymentToBancoAlimentar.Body.Path"]);

                var donation = new Business.Donation(mailMessagePath);

                bool donationDone = false;
                try
                {
                    donationDone = donation.InsertDonation(donationEntity);
                    if (!donationDone)
                    {
                        ModelState.AddModelError("Error", "De momento não é possível doar. Tente mais tarde.");
                    }
                    else
                    {
                        IList<DonationEntity> donationEntities = donation.GetDonationById(donationEntity.DonationId);
                        if (donationEntities != null && donationEntities.Count > 0)
                        {
                            ViewBag.DonationEntity = donationEntities;
                            ViewBag.DonationEntityItems =
                                donation.GetDonationItemsByDonationId(donationEntity.DonationId);
                        }
                        ViewBag.HasReference = true;

                        string encryptedUrl =
                            Encryption.Encrypt(string.Format("{0}:{1}", donationEntity.DonationId, referenceView), pass,
                                               Convert.FromBase64String(salt));

                        return RedirectToAction("Reference", "Donation", new { n = encryptedUrl });
                    }
                }
                catch (BusinessException bexp)
                {
                    ModelState.AddModelError("Error", bexp.Message.ToString());
                }
                catch
                {
                    ModelState.AddModelError("Error", "De momento não é possível doar. Tente mais tarde.");
                }
            }

            LoadBaseData(referenceView);

            return View(referenceView);
        }

        [HandleError]
        [HttpGet]
        public ActionResult Reference(string n)
        {
            /*if (!IsProductionDate())
            {
                return RedirectToActionPermanent(ThankyouViewName);
            }*/
            try
            {
                var decriptedUrl = Encryption.Decrypt(n, pass, Convert.FromBase64String(salt));

                string[] parms = null;

                if (decriptedUrl != null)
                {
                    parms = decriptedUrl.Split(new[] { ':' });
                    if (parms.Count() == 2)
                    {
                        int id = Convert.ToInt32(parms[0]);
                        string rederenceView = parms[1];

                        var donation = new Business.Donation();
                        IList<DonationEntity> donationEntities = donation.GetDonationById(id);
                        if (donationEntities != null && donationEntities.Count > 0)
                        {
                            ViewBag.DonationEntity = donationEntities;
                            ViewBag.DonationEntityItems = donation.GetDonationItemsByDonationId(Convert.ToInt32(id));
                        }
                        ViewBag.HasReference = true;

                        LoadBaseData(rederenceView);

                        return View(rederenceView);
                    }
                }
            }
            catch (Exception exp)
            {
                BusinessException.WriteExceptionToTrace(exp);
            }
            // Someone tempered with the URL, redirect to error page with that information
            return RedirectToAction("Index"); // TEDIM dixit
        }

        private string SetReferenceView(DonateViewModel dnv)
        {
            if (dnv.Hidden != null) return dnv.Hidden;
            string referenceView = "Index";
            if (Request.ServerVariables["http_referer"].Contains("IndexFB"))
            {
                referenceView = "IndexFB";
            }
            else if (Request.ServerVariables["http_referer"].Contains("IndexMSN"))
            {
                referenceView = "IndexMSN";
            }
            else if (Request.ServerVariables["http_referer"].Contains("IndexMobile"))
            {
                referenceView = "IndexMobile";
            }
            return referenceView;
        }

        [HandleError]
        [HttpGet]
        public ActionResult ReferencePayed(string id, string Ref, string paycount, int? donationMode)
        {

            var mailMessagePath = new MailMessagePath();

            mailMessagePath.ReferenceToDonorPath =
                Server.MapPath(ConfigurationManager.AppSettings["Email.ReferenceToDonor.Body.Path"]);
            mailMessagePath.PaymentToDonorPath =
                Server.MapPath(ConfigurationManager.AppSettings["Email.PaymentToDonor.Body.Path"]);
            mailMessagePath.PaymentToBancoAlimentarPath =
                Server.MapPath(ConfigurationManager.AppSettings["Email.PaymentToBancoAlimentar.Body.Path"]);

            var donation = new Business.Donation(mailMessagePath);

            // TODO: update by reference and id (NIF)
            bool updated = donation.UpdateDonationStatusByRefAndNif(id, Ref, (int?)DonationStatus.Status.Payed,
                                                                    donationMode ?? MultibancoPaymentMode);

            if (!updated)
            {
                return new HttpStatusCodeResult(400);
            }

            return new EmptyResult();
        }

        [HandleError]
        [HttpGet]
        [OutputCache(Duration = 900, Location = OutputCacheLocation.ServerAndClient, VaryByParam = "*")]
        public FileResult DisplayImage(int donorId)
        {
            var donation = new Business.Donation();

            IList<DonorsPictureEntity> donationEntity = donation.GetDonorsPictureById(donorId);
            if (donationEntity != null && donationEntity[0].Picture != null && donationEntity[0].Picture.Length > 0 &&
                donationEntity[0].PictureContentType != null)
            {
                return File(donationEntity[0].Picture, donationEntity[0].PictureContentType);
            }
            return File(Url.Content("~/Content/images/donor.jpg"), "image/pjpeg");
        }



        private String CleanInput(string input, int maxLength)
        {
            if (string.IsNullOrEmpty(input) || input.Length < maxLength) return input;
            //TODO: replace invalid character with empty string (other than alpanumeric,.,@ and -)
            return input.Substring(0, maxLength);
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public JsonResult GetLastPaidDonations()
        {
            /*if (!IsProductionDate())
            {
                return null;
            }*/
            return Json(new BancoAlimentarEntities().GetLastPayedDonations(52, null, null), JsonRequestBehavior.AllowGet);
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public JsonResult GetTotalDonations()
        {
            /*if (!IsProductionDate())
            {
                return null;
            }*/
            return Json(new BancoAlimentarEntities().GetTotalDonations(), JsonRequestBehavior.AllowGet);
        }

        [HandleError]
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        [HttpPost]
        public int ReportBadPicture(int id)
        {
            /*if (!IsProductionDate())
            {
                return 0;
            }*/
            var donation = new Business.Donation();

            string baseUrl = string.Format("{0}://{1}{2}", Request.Url.Scheme,
                                           Request.Url.Authority,
                                           Request.ApplicationPath);

            return (donation.SendReportImage(id, baseUrl) ? 1 : 0);
        }


        private static bool IsProductionDate()
        {
            // ThankyouViewName
            var culture = new CultureInfo(PortugalCulture);
            DateTime endProductionDate;
            DateTime startProductionDate;


            endProductionDate = Convert.ToDateTime(ConfigurationManager.AppSettings[ProdutionEndDate], culture);
            startProductionDate = Convert.ToDateTime(ConfigurationManager.AppSettings[ProdutionStartDate], culture);

            DateTime timeInWesternEurope = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.Now,
                                                                                      DestinationTimeZoneId);

            if (timeInWesternEurope.CompareTo(endProductionDate) > 0)
            {
                return false;
            }

            if (timeInWesternEurope.CompareTo(startProductionDate) < 0)
            {
                ThankyouViewName = "Countdown";
                return false;
            }

            return true;
        }

        private bool IpCanDonate()
        {
            string ip = String.Empty;
            if (Request.ServerVariables != null)
            {
                ip = Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
                if (!string.IsNullOrEmpty(ip))
                {
                    string[] ipRange = ip.Split(',');
                    int le = ipRange.Length - 1;
                    string trueIp = ipRange[le];
                    ip = trueIp;
                }
                else
                {
                    ip = Request.ServerVariables["REMOTE_ADDR"];
                }
            }
            string validIps = ConfigurationManager.AppSettings["Presidency.Valid.Ips"];

            return validIps.Contains(ip);
        }

        [HandleError]
        [ValidateAntiForgeryToken]
        [HttpPost]
        public ActionResult PayWithUnicre(Donation donation)
        {
            try
            {
                var amount = ((int)(donation.ServiceAmount * 100)).ToString();
                var splitReference = donation.ServiceReference.Split('|');
                var nif = splitReference[0];
                var reference = splitReference[1];
                var returnUrl = string.Format("{0}?id={1}&Ref={2}&paycount={3}",
                                              Url.Content(ConfigurationManager.AppSettings["Unicre.ReturnUrl"]), nif,
                                              reference, 1);

                string token, redirectUrl;

                var payment = new payment
                {
                    amount = amount,
                    currency = CurrencyCode,
                    action = PaymentAction,
                    mode = PaymentMode,
                    contractNumber = ConfigurationManager.AppSettings["Unicre.ContractNumber"]
                };


                var order = new order
                {
                    @ref = donation.ServiceReference,
                    amount = amount,
                    currency = CurrencyCode,
                    date = DateTime.Now.ToString("dd/MM/yyyy HH:mm")
                };

                var ws = new WebPaymentAPI
                {
                    Credentials =
                        new System.Net.NetworkCredential(
                        ConfigurationManager.AppSettings["Unicre.MerchantId"],
                        ConfigurationManager.AppSettings["Unicre.AccessKey"])
                };

                if (ConfigurationManager.AppSettings["Unicre.Production"].Equals("true"))
                {
                    ws.Url = ConfigurationManager.AppSettings["Unicre.ProductionUrl"];
                }

                // ¯\_(ツ)_/¯
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

                var result = ws.doWebPayment(payment, returnUrl,
                                             Url.Content(string.Format("{0}{1}",
                                                                       ConfigurationManager.AppSettings[
                                                                           "Unicre.CancelUrl"],
                                                                       ViewBag.FromCaboVerde != null &&
                                                                       ViewBag.FromCaboVerde
                                                                           ? "CaboVerde"
                                                                           : (ViewBag.FromAngola != null &&
                                                                       ViewBag.FromAngola ? "Angola" : string.Empty))), order, null
                    /* notificationUrl */, null /* selectedCrontractList */, null
                    /* privateDataList */, LanguageCode, null /* customPaymentPageCode */, null
                    /* buyer */, SecurityMode, null /* recurring */, null
                    /* customPaymentTemplateUrl */, out token, out redirectUrl);

                if (!string.IsNullOrEmpty(redirectUrl))
                {
                    Response.Redirect(redirectUrl);
                }
            }
            catch (Exception exp)
            {
                BusinessException.WriteExceptionToTrace(exp);
            }

            return null;
        }

        [HandleError]
        [HttpGet]
        public ActionResult ReferencePayedViaUnicre(string id, string Ref, string paycount)
        {
            try
            {
                var result = ReferencePayed(id, Ref, paycount, RedunicrePaymentMode);

                if (result is EmptyResult)
                {
                    Response.Redirect(
                        Url.Content(string.Format("{0}{1}", ConfigurationManager.AppSettings["Unicre.CancelUrl"],
                                                  ViewBag.FromCaboVerde != null && ViewBag.FromCaboVerde
                                                      ? "CaboVerde"
                                                      : (ViewBag.FromAngola != null && ViewBag.FromAngola ? "Angola" : string.Empty))));
                }
            }
            catch (Exception exp)
            {
                BusinessException.WriteExceptionToTrace(exp);
            }

            return null;
        }

        [HandleError]
        public ActionResult CaboVerde()
        {
            if (!IsProductionDate())
            {
                return RedirectToActionPermanent(ThankyouViewName);
            }

            ViewBag.FromCaboVerde = true;
            ViewBag.FromAngola = false;

            ViewBag.HasReference = false;
            LoadBaseData("CaboVerde");
            return View("Index");
        }

        [HandleError]
        public ActionResult Angola()
        {
            if (!IsProductionDate())
            {
                return RedirectToActionPermanent(ThankyouViewName);
            }

            ViewBag.FromAngola = true;
            ViewBag.FromCaboVerde = false;

            ViewBag.HasReference = false;
            LoadBaseData("Angola");
            return View("Index");
        }

        public ActionResult ChangeCulture(string lang, string returnUrl)
        {
            Session["Culture"] = new CultureInfo(lang);

            //Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator = ".";

            return Redirect(returnUrl);
        }
    }
}