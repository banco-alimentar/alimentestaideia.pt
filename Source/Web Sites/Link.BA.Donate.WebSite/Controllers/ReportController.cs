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

namespace Link.BA.Donate.WebSite.Controllers
{
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

        [Authorize]
        public ActionResult Index()
        {
            return View();
        }

        [Authorize]
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

        [Authorize]
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

        [Authorize]
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

        [Authorize]
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
                                                          ])
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

        [Authorize]
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
    }
}