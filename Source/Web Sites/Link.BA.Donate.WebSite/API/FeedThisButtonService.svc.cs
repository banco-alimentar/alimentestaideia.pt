using Link.BA.Donate.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace Link.BA.Donate.WebSite.API
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "FeedThisButtonService" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select FeedThisButtonService.svc or FeedThisButtonService.svc.cs at the Solution Explorer and start debugging.
    public class FeedThisButtonService : IFeedThisButtonService
    {
        public DonateResponse Donate(int bancoAlimentar, bool empresa, string nome, string nomeEmpresa, string email, string pais, bool recibo, string morada, string codigoPostal, string nif, string itens, decimal valor, string apiKey)
        {
            var mailMessagePath = new MailMessagePath();

            mailMessagePath.ReferenceToDonorPath =
                ConfigurationManager.AppSettings["Email.ReferenceToDonor.Body.Path"];
            mailMessagePath.PaymentToDonorPath =
                ConfigurationManager.AppSettings["Email.PaymentToDonor.Body.Path"];
            mailMessagePath.ReceiptToDonorPath =
                ConfigurationManager.AppSettings["Email.ReceiptToDonor.Body.Path"];
            mailMessagePath.ReceiptTemplatePath =
                ConfigurationManager.AppSettings["Email.ReceiptTemplate.Path"];
            mailMessagePath.PaymentToBancoAlimentarPath =
                ConfigurationManager.AppSettings["Email.PaymentToBancoAlimentar.Body.Path"];

            var donation = new Business.Donation(mailMessagePath);

            if(!donation.ValidateApiKey(apiKey))
            {
                throw new InvalidOperationException();
            }

            var donationEntity = new Donation
            {
                Anonym = false,
                DonationDate = DateTime.Now,
                DonationStatusId = (int)DonationStatus.Status.WaitingPayment,
                DonationStatusDate = DateTime.Now,
                ServiceAmount = valor,
                FoodBankId = bancoAlimentar,
                Donor = new Donor
                {
                    Name = nome,
                    NIF = nif,
                    Email = email,
                    Organization = !empresa,
                    RegisterDate = DateTime.Now,
                    DonorAddress =
                            new DonorAddress
                            {
                                Address1 = morada,
                                Address2 = pais,
                                PostalCode = codigoPostal,
                                City = string.Empty
                            },
                    CompanyName = nomeEmpresa
                },
                WantsReceipt = recibo
            };

            string productCatalogueId = null, quantity = null;
            int counter = 0;
            foreach (string idAndAmount in itens.Split(new[] { ';' }))
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

            var donationDone = donation.InsertDonation(donationEntity);

            var donationEntities = donation.GetDonationById(donationEntity.DonationId);

            return new DonateResponse
            {
                ServiceEntity = donationEntities[0].ServiceEntity,
                ServiceReference = donationEntities[0].ServiceReference,
                ServiceAmount = donationEntities[0].ServiceAmount
            };
        }
    }

    public class DonateResponse
    {
        public string ServiceEntity { get; set; }
        public string ServiceReference { get; set; }
        public decimal? ServiceAmount { get; set; }
    }
}
