using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Threading;
using System.Globalization;
using Link.BA.Donate.Business;
using Link.BA.Donate.Models;
using Donation = Link.BA.Donate.Business.Donation;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using System.Web.Http.Controllers;
using System.Web.Routing;

namespace Link.BA.Donate.WebSite.Controllers
{
    public class MyAuthorizeAttribute : AuthorizeAttribute
    {
        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            //http://www.prideparrot.com/blog/archive/2012/6/customizing_authorize_attribute
            if (!filterContext.HttpContext.User.Identity.IsAuthenticated)
            {
                base.HandleUnauthorizedRequest(filterContext);
            }
            else
            {
                filterContext.Result = new RedirectToRouteResult(new
                RouteValueDictionary(new { controller = "Report", action = "NotAuthorized" }));
            }
        }
    }

    //https://docs.microsoft.com/en-us/learn/modules/identity-users-groups-approles/4-security-groups
    [MyAuthorizeAttribute(Roles = "31e7f7da-86a6-4b85-ab23-c1e7c59ae907")]
    public class ReportController : Controller
    {
        private const string HeaderLine = "0";
        private const string AepsFileType = "AEPS";
        private const string OriginInstitution = "90262884";
        private const string DestinationInstitution = "50000000";
        private readonly string _entity = ConfigurationManager.AppSettings["Sibs.Entity"];
        private const string CurrencyCode = "978";
        private const string OperationLine = "1";
        private const string InsertionOperation = "80";
        private const string FooterLine = "9";

        private const int MultibancoPaymentMode = 1;
        private const int RedunicrePaymentMode = 2;
        private const int PayPalPaymentMode = 3;
        private const int MBWayPaymentMode = 4;

        
        public ActionResult Index()
        {
            if (Request.IsAuthenticated)
            {
                var userClaims = User.Identity as System.Security.Claims.ClaimsIdentity;

                //You get the user’s first and last name below:
                ViewBag.Name = userClaims?.FindFirst("name")?.Value;
            }
            else {
                return RedirectToAction("Login");
            }

            return View();
        }

        [AllowAnonymous]
        public ActionResult Login()
        {

            return View();
        }

        [AllowAnonymous]
        public ActionResult Logout()
        {
            SignOut();
            return View();
        }

        [AllowAnonymous]
        public ActionResult NotAuthorized(string message)
        {


            if (Request.IsAuthenticated)
            {
                var userClaims = User.Identity as System.Security.Claims.ClaimsIdentity;

                //You get the user’s first and last name below:
                ViewBag.Name = userClaims?.FindFirst("name")?.Value;
            }

            return View();
        }


        public FileContentResult GetAllDonors()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("pt-PT");

            var donation = new Donation();
            var donors = donation.GetAllDonors();
            var csv =
                "Número;Nome;Email;NIF;Empresa;Data;Morada;País;Código Postal;Referência;Valor;Valor Por Extenso;Banco Alimentar;Nome da empresa\n";
            for (int index = 0; index < donors.Count; index++)
            {
                AllDonorsEntity donor = donors[index];
                csv = csv +
                      string.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};{10};{11};{12};{13};\n",
                                    string.Format("B2012-{0}", index + 2581), donor.Name.Replace(";", " "), donor.Email,
                                    donor.NIF.Replace(";", " "), donor.Organization, donor.RegisterDate,
                                    donor.Address1.Replace(";", " "), donor.Address2.Replace(";", " "), donor.PostalCode, donor.ServiceReference, donor.ServiceAmount,
                                   Converstion.MoneyToString(donor.ServiceAmount.ToString()), donor.FoodBank, donor.CompnayName);
            }

            var csvBytes = System.Text.Encoding.Default.GetBytes(csv);

            return File(csvBytes, System.Net.Mime.MediaTypeNames.Text.Plain, "ListaDeDonativos.csv");
        }

        public FileContentResult GetTotalDonationValue()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("pt-PT");

            var donation = new Donation();
            var values = donation.GetTotalDonationValue();
            var csv = "Banco Alimentar;Total;Contagem\n";

            foreach (var value in values) csv += string.Format("{0};{1};{2}\n", value.Name, value.Total, value.Contagem);

            var csvBytes = System.Text.Encoding.Default.GetBytes(csv);

            return File(csvBytes, System.Net.Mime.MediaTypeNames.Text.Plain, "TotalPorBancoAlimentar.csv");
        }

        public FileContentResult GetQuantitiesByProduct()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("pt-PT");

            var donation = new Donation();
            var quantities = donation.GetQuantitiesByProduct();
            var csv = "Nome;Quantidade;Unidade;Valor\n";

            foreach (var quantity in quantities)
                csv += string.Format("{0};{1};{2};{3}\n", quantity.Name, quantity.Quantity, quantity.UnitOfMeasure,
                                     quantity.Total);

            var csvBytes = System.Text.Encoding.Default.GetBytes(csv);

            return File(csvBytes, System.Net.Mime.MediaTypeNames.Text.Plain, "QuantidadePorProduto.csv");
        }

        public FileContentResult GetAepsFile()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("pt-PT");

            var currentDate = DateTime.Now;
            var oneYearAfter = currentDate.AddYears(1);

            var donation = new Donation();
            var notPaidDonations = donation.GetNotPaidDonations();

            if (notPaidDonations.Any())
            {
                var lastAepsFile = donation.GetLastAepsFile();
                var previousFileId = lastAepsFile.FileId;
                var fileId = SibsHelper.GetFileId(previousFileId);

                var text = string.Format("{0}{1}{2}{3}{4}{5}{6}{7}\n", HeaderLine, AepsFileType, OriginInstitution,
                                         DestinationInstitution, fileId, previousFileId, _entity, CurrencyCode);
                text = notPaidDonations.Aggregate(text,
                                                  (current, paidDonation) =>
                                                  current +
                                                  string.Format("{0}{1}{2}{3}{4}{5}{4}\n", OperationLine,
                                                                InsertionOperation,
                                                                paidDonation.ServiceReference.Replace(" ", string.Empty),
                                                                oneYearAfter.ToString("yyyyMMdd"),
                                                                paidDonation.ServiceAmount.GetValueOrDefault().ToString(
                                                                    "00000000.##").Replace(",", string.Empty),
                                                                currentDate.ToString("yyyyMMdd")));
                text += string.Format("{0}{1}\n", FooterLine, notPaidDonations.Count().ToString("00000000"));

                var textBytes = System.Text.Encoding.Default.GetBytes(text);

                donation.InsertAepsFile(fileId, currentDate, text);

                return File(textBytes, System.Net.Mime.MediaTypeNames.Text.Plain, string.Format("{0}.txt", fileId));
            }
            return new FileContentResult(null, System.Net.Mime.MediaTypeNames.Text.Plain);
        }

        [HttpGet]
        public ActionResult ValidateFile()
        {
            return View();
        }

        [HttpPost]
        public ActionResult ProcessFile(string file)
        {
            string report = string.Empty;

            try
            {

                var mailMessagePath = new MailMessagePath
                {
                    ReferenceToDonorPath =
                                                  Server.MapPath(
                                                      ConfigurationManager.AppSettings[
                                                          "Email.ReferenceToDonor.Body.Path"
                                                          ]),
                    PaymentToDonorPath =
                                                  Server.MapPath(
                                                      ConfigurationManager.AppSettings[
                                                          "Email.PaymentToDonor.Body.Path"
                                                          ]),
                    PaymentToBancoAlimentarPath =
                                                  Server.MapPath(
                                                      ConfigurationManager.AppSettings[
                                                          "Email.PaymentToBancoAlimentar.Body.Path"
                                                          ]),
                    ReceiptToDonorPath = Server.MapPath(ConfigurationManager.AppSettings["Email.ReceiptToDonor.Body.Path"]),
                    ReceiptTemplatePath = Server.MapPath(ConfigurationManager.AppSettings["Email.ReceiptTemplate.Path"])
                };

                var donation = new Donation(mailMessagePath);

                var bytes = System.Text.Encoding.Default.GetBytes(file);
                var memory = new MemoryStream(bytes);

                using (var reader = new StreamReader(memory))
                {
                    var text = reader.ReadLine();
                    if (text != null)
                    {
                        while (!string.IsNullOrEmpty(text = reader.ReadLine()))
                        {
                            if (text.StartsWith("9"))
                            {
                                break;
                            }

                            var reference = string.Format("{0} {1} {2}",
                                                          text.Substring(74, 3),
                                                          text.Substring(77, 3),
                                                          text.Substring(80, 3));

                            var donationEntity = donation.GetDonationByReference(reference);

                            // Para não enviar emails repetidos se já tiver sido pago
                            if (donationEntity[0].DonationStatusId !=
                                (int?) DonationStatus.Status.Payed)
                            {
                                donation.UpdateDonationStatusByReference(reference,
                                                                         (int?)
                                                                         DonationStatus.
                                                                             Status.Payed,
                                                                         MultibancoPaymentMode);
                            }
                        }

                        report = "Ficheiro processado com sucesso";
                    }
                }
            }
            catch (Exception)
            {
                report = "Erro a processar o ficheiro";
            }

            ViewBag.Report = report;
            return View("ValidateFile");
        }

        [HttpPost]
        public ActionResult ValidateFile(HttpPostedFileBase file)
        {
            if (file == null)
            {
                return View();
            }

            var bytes = new byte[file.ContentLength];
            file.InputStream.Read(bytes, 0, file.ContentLength);
            var memory = new MemoryStream(bytes);

            ViewBag.File = System.Text.Encoding.Default.GetString(bytes);

            using (var reader = new StreamReader(memory))
            {
                var text = reader.ReadLine();
                if (text != null)
                {
                    var fileType = text.Substring(1, 4);
                    var fileId = text.Substring(21, 9);
                    var donation = new Donation();

                    ViewBag.Report = string.Format("Ficheiro do tipo {0} com o ID {1}", fileType, fileId);
                    
                            donation.ValidateMepsFile(fileId, ViewBag.File);
                }
            }

            return View("ProcessFile");
        }

        public FileContentResult GetQuantitiesByFoodBankAndProduct()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("pt-PT");

            var donation = new Donation();
            var quantities = donation.GetQuantitiesByFoodBankAndProduct();
            var csv =
                "Banco Alimentar;Produto;Quantidade;Unidade\n";
            for (int index = 0; index < quantities.Count; index++)
            {
                QuantitiesByFoodBankAndProductEntity quantity = quantities[index];
                csv = csv +
                      string.Format("{0};{1};{2};{3}\n", quantity.foodbank, quantity.product, quantity.quantity, quantity.unitofmeasure);
            }

            var csvBytes = System.Text.Encoding.Default.GetBytes(csv);

            return File(csvBytes, System.Net.Mime.MediaTypeNames.Text.Plain, "QuantidadePorBancoAlimentarEProduto.csv");
        }

        public FileContentResult GetQuantitiesByDonor()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("pt-PT");

            var donation = new Donation();
            var quantities = donation.GetQuantitiesByDonor();
            var csv =
                "Nome;NIF;Empresa;Referência;Quantidade;Quantidade;Unidade;Valor;Produto\n";
            for (int index = 0; index < quantities.Count; index++)
            {
                GetQuantitiesByDonor_Result quantity = quantities[index];
                csv = csv +
                      string.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8}\n", quantity.Name, quantity.NIF, quantity.CompanyName, quantity.ServiceReference, quantity.Quantity, quantity.ProductQuantity, quantity.UnitOfMeasure, quantity.Cost, quantity.ProductName);
            }

            var csvBytes = System.Text.Encoding.Default.GetBytes(csv);

            return File(csvBytes, System.Net.Mime.MediaTypeNames.Text.Plain, "QuantidadePorDoador.csv");
        }


        /// <summary>
        /// Send an OpenID Connect sign-in request.
        /// Alternatively, you can just decorate the SignIn method with the [Authorize] attribute
        /// </summary>
        [Authorize(Roles = "Alimentestaideia.Backoffice")]
        public void SignIn()
        {
            if (!Request.IsAuthenticated)
            {
                HttpContext.GetOwinContext().Authentication.Challenge(
                    new AuthenticationProperties { RedirectUri = "/Report/Index" },
                    OpenIdConnectAuthenticationDefaults.AuthenticationType);
            }
        }
        /// <summary>
        /// Send an OpenID Connect sign-out request.
        /// </summary>
        public void SignOut()
        {
            HttpContext.GetOwinContext().Authentication.SignOut(
                    OpenIdConnectAuthenticationDefaults.AuthenticationType,
                    CookieAuthenticationDefaults.AuthenticationType);
        }
    }
}