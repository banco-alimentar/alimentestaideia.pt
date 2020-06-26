using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
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
using System.Configuration;
using Donation = Link.BA.Donate.Models.Donation;
using System.Net;
using PayPal.Api;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.DataContracts;
using System.Web.Configuration;

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
        private const int PayPalPaymentMode = 3;
        private const int MBWayPaymentMode = 4;

        private TelemetryClient telemetryClient = new TelemetryClient(TelemetryConfiguration.Active);

        [HandleError]
        public ActionResult Obrigado()
        {
            telemetryClient.TrackEvent("Obrigado");
            ViewBag.HasReference = false;
            ViewBag.IsMultibanco = false;
            LoadBaseData("Obrigado");
            return View();
        }

        [HandleError]
        public ActionResult Countdown()
        {
            telemetryClient.TrackEvent("Countdown");
            return View();
        }

        [HandleError]
        public ActionResult Index()
        {
            ViewBag.IsPostBack = false;
            telemetryClient.TrackEvent("Index");
            if (!IsProductionDate())
            {
                return RedirectToActionPermanent(ThankyouViewName);
            }
            ViewBag.HasReference = false;
            ViewBag.IsMultibanco = false;
            LoadBaseData("Index");
            return View();
        }

        [HandleError]
        [ValidateAntiForgeryToken]
        [HttpPost]
        public ActionResult Donate(CaptchaModel captchaModel, DonateViewModel donateViewModel)
        {
            ViewBag.IsPostBack = true;

            ViewBag.HasReference = false;
            ViewBag.IsMultibanco = false;

            // Facebook, MSN & Mobile App Support
            string referenceView = "Index";
            referenceView = SetReferenceView(donateViewModel);

            if (ModelState.IsValid)
            {
                if (!CaptchaHelper.Verify(captchaModel))
                {
                    ModelState.AddModelError("Error", "Código errado.");
                    ViewBag.HasReference = false;
                    ViewBag.IsMultibanco = false;
                    LoadBaseData(referenceView);

                    telemetryClient.TrackEvent("Donate.WrongCaptcha");
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
                            ViewBag.IsMultibanco = false;
                            LoadBaseData(referenceView);
                            return View(referenceView);
                    }
                    if (donateViewModel.Picture.ContentLength >= 307200)
                    {
                        ModelState.AddModelError("Error", "O tamanho máximo da foto é 300 KB.");
                        ViewBag.HasReference = false;
                        ViewBag.IsMultibanco = false;
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
                        telemetryClient.TrackEvent("Donate.InsertDonationError");
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
                        ViewBag.IsMultibanco = false;

                        string encryptedUrl =
                            Encryption.Encrypt(string.Format("{0}:{1}", donationEntity.DonationId, referenceView), pass,
                                               Convert.FromBase64String(salt));

                        telemetryClient.TrackEvent("Donate");
                        return RedirectToAction("Reference", "Donation", new { n = encryptedUrl });
                    }
                }
                catch (BusinessException bexp)
                {
                    telemetryClient.TrackException(bexp);
                    ModelState.AddModelError("Error", bexp.Message.ToString());
                }
                catch (Exception exp)
                {
                    telemetryClient.TrackException(exp);
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
                        ViewBag.IsMultibanco = false;

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

            var mailMessagePath = new MailMessagePath
            {
                ReferenceToDonorPath =
                Server.MapPath(ConfigurationManager.AppSettings["Email.ReferenceToDonor.Body.Path"]),
                PaymentToDonorPath =
                Server.MapPath(ConfigurationManager.AppSettings["Email.PaymentToDonor.Body.Path"]),
                PaymentToBancoAlimentarPath =
                Server.MapPath(ConfigurationManager.AppSettings["Email.PaymentToBancoAlimentar.Body.Path"]),
                ReceiptToDonorPath = Server.MapPath(ConfigurationManager.AppSettings["Email.ReceiptToDonor.Body.Path"]),
                ReceiptTemplatePath = Server.MapPath(ConfigurationManager.AppSettings["Email.ReceiptTemplate.Path"])
            };

            var donation = new Business.Donation(mailMessagePath);

            // TODO: update by reference and id (NIF)
            bool updated = donation.UpdateDonationStatusByRefAndNif(id, Ref, (int?)DonationStatus.Status.Payed, donationMode);

            telemetryClient.TrackEvent("ReferencePayed", new Dictionary<string, string>() {
                                { "id", id }
                            });

            if (!updated)
            {
                telemetryClient.TrackEvent("ReferencePayedNotUPdated", new Dictionary<string, string>() {
                                { "id", id }
                            });
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
                var returnUrl = string.Format("{0}://{1}{2}?id={3}&Ref={4}&paycount={5}", Request.Url.Scheme, Request.Url.Authority, Url.Content(ConfigurationManager.AppSettings["Unicre.ReturnUrl"]), nif, HttpUtility.UrlEncode(reference), 1);
                var cancelUrl = string.Format("{0}://{1}{2}", Request.Url.Scheme, Request.Url.Authority, ConfigurationManager.AppSettings["Unicre.CancelUrl"]);

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
                        new NetworkCredential(
                        ConfigurationManager.AppSettings["Unicre.MerchantId"],
                        ConfigurationManager.AppSettings["Unicre.AccessKey"])
                };

                if (ConfigurationManager.AppSettings["Unicre.Production"].Equals("true"))
                {
                    ws.Url = ConfigurationManager.AppSettings["Unicre.ProductionUrl"];
                }

                // ¯\_(ツ)_/¯
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

                string stepCode, reqCode, method;

                var result = ws.doWebPayment(null, payment, returnUrl, cancelUrl, order, null, null, null, null, LanguageCode, null, null, null, SecurityMode, null, null, null, out token, out redirectUrl, out stepCode, out reqCode, out method);

                var business = new Business.Donation();
                business.UpdateDonationTokenByRefAndNif(nif, reference, token);

                telemetryClient.TrackEvent("PayWithUnicre", new Dictionary<string, string>() {
                                { "reference", reference },
                                { "token", token },
                                { "ammout", order.amount}
                            });
                telemetryClient.TrackMetric("PayWithUnicre", double.Parse(order.amount));

                if (!string.IsNullOrEmpty(redirectUrl) && int.Parse(result.code) == 0)
                {
                    Response.Redirect(redirectUrl);
                }
            }
            catch (Exception exp)
            {
                telemetryClient.TrackException(new Exception("PayWithUnicre", exp));
                BusinessException.WriteExceptionToTrace(exp);
            }

            return null;
        }

        private Dictionary<string, string> GetPayPalConfiguration()
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            result.Add("mode", WebConfigurationManager.AppSettings["PayPal.mode"]);
            result.Add("clientId", WebConfigurationManager.AppSettings["PayPal.clientId"]);
            result.Add("clientSecret", WebConfigurationManager.AppSettings["PayPal.clientSecret"]);
            return result;
        }

        [HandleError]
        [ValidateAntiForgeryToken]
        [HttpPost]
        public ActionResult PayWithPayPal(Donation donation)
        {
            ActionResult result = null;

            var config = GetPayPalConfiguration();
            var accessToken = new OAuthTokenCredential(config).GetAccessToken();
            var apiContext = new APIContext(accessToken);


            var payer = new Payer() { payment_method = "paypal" };

            var redirUrls = new RedirectUrls
            {
                cancel_url = string.Format("{0}://{1}{2}", Request.Url.Scheme, Request.Url.Authority, ConfigurationManager.AppSettings["PayPal.CancelUrl"]),
                return_url = string.Format("{0}://{1}{2}", Request.Url.Scheme, Request.Url.Authority, ConfigurationManager.AppSettings["PayPal.ReturnUrl"])
            };

            var itemList = new ItemList
            {
                items = new List<Item>()
            };

            itemList.items.Add(new Item
            {
                name = "Donativo Banco Alimentar",
                currency = "EUR",
                price = Convert.ToString(donation.ServiceAmount),
                quantity = "1",
                sku = donation.ServiceReference
            });

            var details = new Details
            {
                tax = "0",
                shipping = "0",
                subtotal = Convert.ToString(donation.ServiceAmount)
            };

            var amount = new Amount
            {
                currency = "EUR",
                total = Convert.ToString(donation.ServiceAmount),
                details = details
            };

            var transactionList = new List<Transaction>();

            transactionList.Add(new Transaction
            {
                description = "Donativo Banco Alimentar",
                amount = amount,
                item_list = itemList
            });

            var payment = new Payment
            {
                intent = "sale",
                payer = payer,
                redirect_urls = redirUrls,
                transactions = transactionList
            };

            var createdPayment = payment.Create(apiContext);

            var business = new Business.Donation();
            var splitReference = donation.ServiceReference.Split('|');
            var nif = splitReference[0];
            var reference = splitReference[1];
            business.UpdateDonationTokenByRefAndNif(nif, reference, createdPayment.id);



            var link = createdPayment.links
                .Where(p => p.rel.ToLowerInvariant() == "approval_url")
                .FirstOrDefault();

            if (link != null)
            {
                result = Redirect(link.href);
            }

            return result;
        }

        [HandleError]
        [ValidateAntiForgeryToken]
        [HttpPost]
        public ActionResult PayWithMultibanco(Donation donation)
        {
            var business = new Business.Donation();
            var donationByReference = business.GetDonationByReference(donation.ServiceReference);

            donation.Donor = new Donor
            {
                Email = donationByReference[0].Email
            };

            if (Convert.ToBoolean(ConfigurationManager.AppSettings["Email.Send"]))
            {
                Mail.SendReferenceMailToDonor(donation, Server.MapPath(ConfigurationManager.AppSettings["Email.ReferenceToDonor.Body.Path"]));
            }

            ViewBag.HasReference = true;
            ViewBag.IsMultibanco = true;
            ViewBag.ServiceEntity = donation.ServiceEntity;
            ViewBag.ServiceReference = donation.ServiceReference;
            ViewBag.ServiceAmount = donation.ServiceAmount;

            LoadBaseData("Index");
            return View("Index");
        }

        [HandleError]
        [HttpGet]
        public ActionResult ReferencePayedViaUnicre(string id, string Ref, string paycount)
        {
            ActionResult actionResult = null;
            try
            {
                var ws = new WebPaymentAPI
                {
                    Credentials =
                        new NetworkCredential(
                        ConfigurationManager.AppSettings["Unicre.MerchantId"],
                        ConfigurationManager.AppSettings["Unicre.AccessKey"])
                };

                if (ConfigurationManager.AppSettings["Unicre.Production"].Equals("true"))
                {
                    ws.Url = ConfigurationManager.AppSettings["Unicre.ProductionUrl"];
                }

                // ¯\_(ツ)_/¯
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

                payment payment;
                transaction transaction;
                authorization authorization;
                privateData[] privateDataList;
                billingRecord[] billingRecordList;
                authentication3DSecure authentication3DSecure;
                string paymentRecordId, media, numberOfAttempt, contractNumber;
                cardOut card;
                extendedCardType extendedCard;
                order order;
                paymentAdditional[] paymentAdditionalList;
                wallet wallet;
                string[] contractNumberWalletList;
                bankAccountData bankAccountData;

                var business = new Business.Donation();
                var donation = business.GetDonationByReference(Ref);

                var details = ws.getWebPaymentDetails(null, donation[0].Token, out transaction, out payment, out authorization, out privateDataList, out paymentRecordId, out billingRecordList, out authentication3DSecure, out card, out extendedCard, out order, out paymentAdditionalList, out media, out numberOfAttempt, out wallet, out contractNumberWalletList, out contractNumber, out bankAccountData);

                int statusCode = -1;

                if (int.TryParse(details.code, out statusCode))
                {
                    if (statusCode == 0)
                    {
                        var result = ReferencePayed(id, Ref, paycount, RedunicrePaymentMode);

                        // Empty request is because in the previous method the information for the user wasn't updated in the database
                        // and something happen. We would like to find why.
                        if (result is EmptyResult)
                        {
                            telemetryClient.TrackEvent("ReferencePayedEmtpy", new Dictionary<string, string>() {
                                { "id", id },
                                { "Ref", Ref },
                                { "paycount", paycount },
                                { "payment-details-logmessage", details.longMessage },
                                { "payment-details-shortmessage", details.shortMessage }
                            });
                            actionResult = Redirect("~/");
                        }
                        telemetryClient.TrackMetric("ReferencePayedViaUnicre", (Double)donation.FirstOrDefault().ServiceAmount);
                    }
                }
                else
                {
                    telemetryClient.TrackEvent("Details-Code-NonIntegrer", new Dictionary<string, string>() { { "code", details.code } });
                }

                actionResult = Redirect("~/");
            }
            catch (Exception exp)
            {
                telemetryClient.TrackException(new Exception("ReferencePayedViaUnicre", exp));
                BusinessException.WriteExceptionToTrace(exp);
            }

            return actionResult;
        }

        [HandleError]
        [HttpGet]
        public ActionResult ReferencePayedViaPayPal(string paymentId, string token, string PayerID)
        {
            try
            {
                var config = GetPayPalConfiguration();
                var accessToken = new OAuthTokenCredential(config).GetAccessToken();
                var apiContext = new APIContext(accessToken);

                var paymentExecution = new PaymentExecution { payer_id = PayerID };
                var payment = new Payment { id = paymentId };

                var executedPayment = payment.Execute(apiContext, paymentExecution);

                if (executedPayment.state.Equals("approved"))
                {
                    var business = new Business.Donation();
                    var donation = business.GetDonationByToken(paymentId);

                    var result = ReferencePayed(donation[0].NIF, donation[0].ServiceReference, "1", PayPalPaymentMode);

                    if (result is EmptyResult)
                    {
                        Response.Redirect(
                            Url.Content(string.Format("{0}://{1}{2}", Request.Url.Scheme, Request.Url.Authority, ConfigurationManager.AppSettings["PayPal.CancelUrl"])));
                    }
                }

                return Redirect("~/");
            }
            catch (Exception exp)
            {
                BusinessException.WriteExceptionToTrace(exp);
            }

            return null;
        }

        public ActionResult ChangeCulture(string lang, string returnUrl)
        {
            Session["Culture"] = new CultureInfo(lang);

            //Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator = ".";
            telemetryClient.TrackEvent("ChangeCulture", new Dictionary<string, string>() {
                                { "lang", lang }
                            });

            return Redirect(returnUrl);
        }
    }
}