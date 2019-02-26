using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Objects;
using System.Transactions;
using Link.BA.Donate.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Donation = Link.BA.Donate.Business.Donation;
using ProductCatalogue = Link.BA.Donate.Business.ProductCatalogue;

namespace Link.BA.Donate.Tests
{
    [TestClass]
    public class EfTests
    {
        [TestMethod]
        public void UpdateDonationStatusTests()
        {
            var donation = new Donation();
            Assert.IsTrue(donation.UpdateDonationStatus(4, (int?)DonationStatus.Status.Payed));
        }

        [TestMethod]
        public void InsertDonationTests()
        {
            IList<DonationItem> donationItems = new List<DonationItem>();

            donationItems.Add(new DonationItem
                                  {
                                      ProductCatalogueId = 5,
                                      Quantity = 10
                                  });
            donationItems.Add(new DonationItem
                                  {
                                      ProductCatalogueId = 6,
                                      Quantity = 1
                                  });
            var donation = new Donation();

            var donationEntity = new Models.Donation
                                     {
                                         Anonym = false,
                                         ServiceEntity = "12345",
                                         ServiceReference = "123456789",
                                         ServiceAmount = 12,
                                         DonationDate = DateTime.Now,
                                         DonationStatusId = (int) DonationStatus.Status.WaitingPayment,
                                         DonationStatusDate = DateTime.Now,
                                         Donor =
                                             {
                                                 Name = "UnitTest",
                                                 Email = "unittest@unittest.pt",
                                                 NIF = "123456789",
                                                 Picture = null,
                                                 Organization = false,
                                                 RegisterDate = DateTime.Now,
                                                 DonorAddress =
                                                     {
                                                         Address1 = "Morada",
                                                         Address2 = null,
                                                         City = "Lisboa",
                                                         PostalCode = "1300-100",
                                                         PhoneNumber = "213100107"
                                                     }
                                             },
                                         DonationItem = donationItems
                                     };

            bool donationInserted = donation.InsertDonation(donationEntity);

            Assert.IsTrue(donationInserted);
            Assert.IsTrue(donationEntity.DonationId!=0);
        }

        [TestMethod]
        public void GetTotalDonationsTests()
        {
            var donation = new Donation();

            foreach (TotalDonationsEntity totalDonation in donation.GetTotalDonations())
            {
                string dummy = totalDonation.Name;
                Assert.IsNotNull(dummy);
            }
        }

        [TestMethod]
        public void GetProductCatalogueTests()
        {
            var productCataloguenBll = new ProductCatalogue();

            foreach (ProductCatalogueEntity productCatalogue in productCataloguenBll.GetProductCatalogue())
            {
                string dummy = productCatalogue.Name;
                Assert.IsNotNull(dummy);
            }
        }

        [TestMethod]
        public void GetLastPayedDonationsTests()
        {
            var donationBll = new Donation();

            foreach (LastPayedDonationEntity donationEntity in donationBll.GetLastPayedDonations(null, null, null))
            {
                string dummy = donationEntity.Name;
                Assert.IsNotNull(dummy);
            }
        }

        [TestMethod]
        public void EfWithTransactionsTests()
        {
            // Define a transaction scope for the operations.
            using (var transaction = new TransactionScope())
            {
                try
                {
                    var donationBll = new Donation();

                    foreach (DonationEntity donationEntity in donationBll.GetDonationById(110))
                    {
                        string dummy = donationEntity.Address1;
                        Assert.IsNotNull(dummy);
                    }

                    // This is a dummy test just to see if the EF 4.1 is correctly working
                    using (var entities = new BancoAlimentarEntities())
                    {
                        ObjectResult<DonationEntity> donation = entities.GetDonationById(1);
                        foreach (DonationEntity donationEntity in donation)
                        {
                            string dummy = donationEntity.Address1;
                            Assert.IsNotNull(dummy);
                        }
                        // Output Value
                        var donationId = new ObjectParameter("DonationItemId", 0);
                        int insertDonationItem = entities.InsertDonationItem(donationId, 1, 3, 1);
                        Assert.IsNotNull(donationId.Value);
                    }
                    // commit
                    transaction.Complete();
                }
                catch (EntityException entityException)
                {
                    // Do Something
                    // Log Error Message
                    throw;
                }
                catch
                {
                    // Log Error Message
                    throw;
                }
            }
        }
    }
}