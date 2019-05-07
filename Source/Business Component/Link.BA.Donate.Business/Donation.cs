using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.Entity.Infrastructure;
using System.Data.Objects;
using System.Diagnostics;
using Link.BA.Donate.Business.HimediaService;
using Link.BA.Donate.Models;
using Microsoft.AppFabricCAT.Samples.Azure.TransientFaultHandling;
using Microsoft.AppFabricCAT.Samples.Azure.TransientFaultHandling.SqlAzure;

namespace Link.BA.Donate.Business
{
    public class Donation
    {
        private int _maxRetries = Convert.ToInt32(ConfigurationManager.AppSettings["RetryPolicy.MaxRetries"]);
        private int _delayMs = Convert.ToInt32(ConfigurationManager.AppSettings["RetryPolicy.DelayMS"]);
        private bool m_sendMail = Convert.ToBoolean(ConfigurationManager.AppSettings["Email.Send"]);
        private MailMessagePath _mailMessagePath;

        private readonly string _entity = ConfigurationManager.AppSettings["Sibs.Entity"];

        public Donation()
        {
        }

        public Donation(MailMessagePath mailMessagePath)
        {
            _mailMessagePath = mailMessagePath;
        }

        public bool SendReportImage(int donorId, string requestUrl)
        {
            IList<DonorsPictureEntity> donationList = GetDonorsPictureById(donorId);
            if (donationList == null || donationList.Count == 0) return false;
            try
            {
                Mail.SendReportImage(donationList[0], requestUrl);
            }
            catch (Exception exp)
            {
                BusinessException.WriteExceptionToTrace(exp);
                return false;
            }

            return true;
        }

        public bool UpdateDonationStatus(int? donationId, int? donationStatusId)
        {
            try
            {

                RetryPolicy policy = new RetryPolicy<SqlAzureTransientErrorDetectionStrategy>(_maxRetries,
                                                                                              TimeSpan.FromMilliseconds(
                                                                                                  _delayMs));

                policy.ExecuteAction(() =>
                                         {
                                             using (var entities = new BancoAlimentarEntities())
                                             {
                                                 int updated = 0;
                                                 if (
                                                     ((IObjectContextAdapter) entities).ObjectContext.Connection.State !=
                                                     ConnectionState.Open)
                                                 {
                                                     ((IObjectContextAdapter) entities).ObjectContext.Connection.Open();
                                                 }

                                                 using (
                                                     DbTransaction transaction =
                                                         ((IObjectContextAdapter) entities).ObjectContext.Connection.
                                                             BeginTransaction(IsolationLevel.ReadCommitted))
                                                 {
                                                     updated = entities.UpdateDonationStatus(donationId,
                                                                                             donationStatusId);

                                                     if (m_sendMail)
                                                     {
                                                         throw new NotImplementedException();
                                                     }

                                                     transaction.Commit();
                                                 }

                                                 return updated > 0;
                                             }
                                         });
            }
            catch (Exception exp)
            {
                BusinessException.WriteExceptionToTrace(exp);
                return false;
            }
            return false;
        }



        public bool UpdateDonationStatusByReference(string serviceReference, int? donationStatusId, int? donationMode)
        {
            try
            {

                RetryPolicy policy = new RetryPolicy<SqlAzureTransientErrorDetectionStrategy>(_maxRetries,
                                                                                              TimeSpan.FromMilliseconds(
                                                                                                  _delayMs));

                policy.ExecuteAction(() =>
                                         {
                                             using (var entities = new BancoAlimentarEntities())
                                             {
                                                 int updated = 0;
                                                 if (
                                                     ((IObjectContextAdapter) entities).ObjectContext.Connection.State !=
                                                     ConnectionState.Open)
                                                 {
                                                     ((IObjectContextAdapter) entities).ObjectContext.Connection.Open();
                                                 }

                                                 using (
                                                     DbTransaction transaction =
                                                         ((IObjectContextAdapter) entities).ObjectContext.Connection.
                                                             BeginTransaction(IsolationLevel.ReadCommitted))
                                                 {
                                                     IList<DonationByReferenceEntity> donationEntity =
                                                         GetDonationByReference(serviceReference);

                                                     IList<DonationItemsEntity> donationItemsByDonationId =
                                                         GetDonationItemsByDonationId(donationEntity[0].DonationId);

                                                     updated = entities.UpdateDonationStatusByReference(
                                                         serviceReference, donationStatusId, donationMode);

                                                     if (m_sendMail)
                                                     {

                                                         Mail.SendPaymentMailToDonor(donationEntity[0],
                                                                                     donationItemsByDonationId,
                                                                                     _mailMessagePath.PaymentToDonorPath);
                                                         Mail.SendPaymentMailToBancoAlimentar(donationEntity[0],
                                                                                              donationItemsByDonationId,
                                                                                              _mailMessagePath.
                                                                                                  PaymentToBancoAlimentarPath);

                                                         if (donationEntity[0].WantsReceipt != false)
                                                         {
                                                             Mail.SendReceiptMailToDonor(donationEntity[0],
                                                                                         donationItemsByDonationId,
                                                                                         _mailMessagePath.ReceiptToDonorPath, _mailMessagePath.ReceiptTemplatePath);
                                                         }
                                                     }

                                                     transaction.Commit();
                                                 }

                                                 return updated > 0;
                                             }
                                         });
            }
            catch (Exception exp)
            {
                BusinessException.WriteExceptionToTrace(exp);
                return false;
            }
            return false;
        }

        public bool UpdateDonationStatusByRefAndNif(string nif, string serviceReference, int? donationStatusId,
                                                    int? donationMode)
        {
            try
            {
                int updated = 0;
                RetryPolicy policy = new RetryPolicy<SqlAzureTransientErrorDetectionStrategy>(_maxRetries,
                                                                                              TimeSpan.FromMilliseconds(
                                                                                                  _delayMs));

                policy.ExecuteAction(() =>
                                         {
                                             using (var entities = new BancoAlimentarEntities())
                                             {

                                                 if (
                                                     ((IObjectContextAdapter) entities).ObjectContext.Connection.State !=
                                                     ConnectionState.Open)
                                                 {
                                                     ((IObjectContextAdapter) entities).ObjectContext.Connection.Open();
                                                 }

                                                 using (
                                                     DbTransaction transaction =
                                                         ((IObjectContextAdapter) entities).ObjectContext.Connection.
                                                             BeginTransaction(IsolationLevel.ReadCommitted))
                                                 {
                                                     IList<DonationByReferenceEntity> donationEntity =
                                                         GetDonationByReference(serviceReference);

                                                     IList<DonationItemsEntity> donationItemsByDonationId =
                                                         GetDonationItemsByDonationId(donationEntity[0].DonationId);

                                                     updated = entities.UpdateDonationStatusByRefAndNif(
                                                         serviceReference, nif,
                                                         donationStatusId, donationMode);

                                                     if (m_sendMail)
                                                     {

                                                         Mail.SendPaymentMailToDonor(donationEntity[0],
                                                                                     donationItemsByDonationId,
                                                                                     _mailMessagePath.PaymentToDonorPath);
                                                         Mail.SendPaymentMailToBancoAlimentar(donationEntity[0],
                                                                                              donationItemsByDonationId,
                                                                                              _mailMessagePath.
                                                                                                  PaymentToBancoAlimentarPath);

                                                         if (donationEntity[0].WantsReceipt != false)
                                                         {
                                                             Mail.SendReceiptMailToDonor(donationEntity[0],
                                                                                         donationItemsByDonationId,
                                                                                         _mailMessagePath.ReceiptToDonorPath, _mailMessagePath.ReceiptTemplatePath);
                                                         }
                                                     }

                                                     transaction.Commit();
                                                 }


                                             }
                                         });
                return updated > 0;
            }
            catch (Exception exp)
            {
                BusinessException.WriteExceptionToTrace(exp);
                return false;
            }
        }

        public bool InsertDonation(Models.Donation donation)
        {
            if (donation == null)
            {
                Trace.Write("Donation não pode estar a null.");
                return false;
            }

            if (donation.ServiceAmount == 0)
            {
                Trace.Write("donation.ServiceAmount não pode estar a null.");
                return false;
            }

            try
            {
                RetryPolicy policy = new RetryPolicy<SqlAzureTransientErrorDetectionStrategy>(_maxRetries,
                                                                                              TimeSpan.FromMilliseconds(
                                                                                                  _delayMs));

                policy.ExecuteAction(() =>
                                         {
                                             using (var entities = new BancoAlimentarEntities())
                                             {
                                                 if (
                                                     ((IObjectContextAdapter) entities).ObjectContext.Connection.State !=
                                                     ConnectionState.Open)
                                                 {
                                                     ((IObjectContextAdapter) entities).ObjectContext.Connection.Open();
                                                 }

                                                 using (
                                                     DbTransaction transaction =
                                                         ((IObjectContextAdapter) entities).ObjectContext.Connection.
                                                             BeginTransaction(IsolationLevel.ReadCommitted))
                                                 {

                                                     var reference = new ObjectParameter("Reference", 0);
                                                     entities.InsertServiceReference(reference);
                                                     var referenceString = SibsHelper.GenerateReference(_entity,
                                                                                                        (int)
                                                                                                        reference.Value,
                                                                                                        donation.
                                                                                                            ServiceAmount
                                                                                                            .
                                                                                                            GetValueOrDefault
                                                                                                            ());

                                                     var donationId = new ObjectParameter("donationId", 0);
                                                     entities.InsertDonation(donationId, donation.Anonym, _entity,
                                                                             referenceString, donation.ServiceAmount,
                                                                             donation.DonationDate,
                                                                             donation.DonationStatusId,
                                                                             donation.DonationStatusDate,
                                                                             donation.Donor.Name, donation.Donor.Email,
                                                                             donation.Donor.NIF, donation.Donor.Picture,
                                                                             donation.Donor.PictureContentType,
                                                                             donation.Donor.Organization,
                                                                             donation.Donor.RegisterDate,
                                                                             donation.Donor.DonorAddress.Address1,
                                                                             donation.Donor.DonorAddress.Address2,
                                                                             donation.Donor.DonorAddress.City,
                                                                             donation.Donor.DonorAddress.PostalCode,
                                                                             donation.Donor.DonorAddress.PhoneNumber,
                                                                             donation.FoodBankId, donation.WantsReceipt, donation.Donor.CompanyName);
                                                     donation.DonationId = (int) donationId.Value;
                                                     donation.ServiceEntity = _entity;
                                                     donation.ServiceReference = referenceString;

                                                     // insert donated items
                                                     foreach (DonationItem donationItem in donation.DonationItem)
                                                     {
                                                         var donationItemId = new ObjectParameter("donationItemId", 0);

                                                         entities.InsertDonationItem(donationItemId,
                                                                                     (int?) donationId.Value,
                                                                                     donationItem.ProductCatalogueId,
                                                                                     donationItem.Quantity);
                                                     }

                                                     if (m_sendMail)
                                                     {
                                                         Mail.SendReferenceMailToDonor(donation,
                                                                                       _mailMessagePath.
                                                                                           ReferenceToDonorPath);
                                                     }

                                                     transaction.Commit();
                                                 }
                                             }
                                         });
            }
            catch (Exception exp)
            {
                BusinessException.WriteExceptionToTrace(exp);
                return false;
            }

            return true;
        }

        public IList<DonorsPictureEntity> GetDonorsPictureById(int donorId)
        {
            IList<DonorsPictureEntity> donationList = new List<DonorsPictureEntity>();

            RetryPolicy policy = new RetryPolicy<SqlAzureTransientErrorDetectionStrategy>(_maxRetries,
                                                                                          TimeSpan.FromMilliseconds(
                                                                                              _delayMs));

            policy.ExecuteAction(() =>
                                     {
                                         using (var entities = new BancoAlimentarEntities())
                                         {
                                             ObjectResult<DonorsPictureEntity> donation =
                                                 entities.GetDonorsPictureById(donorId);
                                             foreach (DonorsPictureEntity donationEntity in donation)
                                             {
                                                 donationList.Add(donationEntity);
                                             }
                                         }
                                     });

            return donationList;
        }

        public IList<DonationEntity> GetDonationById(int donationId)
        {
            IList<DonationEntity> donationList = new List<DonationEntity>();

            RetryPolicy policy = new RetryPolicy<SqlAzureTransientErrorDetectionStrategy>(_maxRetries,
                                                                                          TimeSpan.FromMilliseconds(
                                                                                              _delayMs));

            policy.ExecuteAction(() =>
                                     {
                                         using (var entities = new BancoAlimentarEntities())
                                         {
                                             ObjectResult<DonationEntity> donation =
                                                 entities.GetDonationById(donationId);
                                             foreach (DonationEntity donationEntity in donation)
                                             {
                                                 donationList.Add(donationEntity);
                                             }
                                         }
                                     });

            return donationList;
        }

        public IList<TotalDonationsEntity> GetTotalDonations()
        {
            IList<TotalDonationsEntity> donationList = new List<TotalDonationsEntity>();

            RetryPolicy policy = new RetryPolicy<SqlAzureTransientErrorDetectionStrategy>(_maxRetries,
                                                                                          TimeSpan.FromMilliseconds(
                                                                                              _delayMs));

            policy.ExecuteAction(() =>
                                     {
                                         using (var entities = new BancoAlimentarEntities())
                                         {
                                             ObjectResult<TotalDonationsEntity> totalDonations =
                                                 entities.GetTotalDonations();
                                             foreach (TotalDonationsEntity totalDonationEntity in totalDonations)
                                             {
                                                 donationList.Add(totalDonationEntity);
                                             }
                                         }
                                     });

            return donationList;
        }

        public IList<LastPayedDonationEntity> GetLastPayedDonations(int? totalDonations, int? pageNumber, int? pageSize)
        {
            IList<LastPayedDonationEntity> lastPayedDonationEntities = new List<LastPayedDonationEntity>();

            RetryPolicy policy = new RetryPolicy<SqlAzureTransientErrorDetectionStrategy>(_maxRetries,
                                                                                          TimeSpan.FromMilliseconds(
                                                                                              _delayMs));

            policy.ExecuteAction(() =>
                                     {
                                         using (var entities = new BancoAlimentarEntities())
                                         {
                                             ObjectResult<LastPayedDonationEntity> donation =
                                                 entities.GetLastPayedDonations(totalDonations,
                                                                                pageNumber, pageSize);
                                             foreach (LastPayedDonationEntity lastPayedDonationEntity in donation)
                                             {
                                                 lastPayedDonationEntities.Add(lastPayedDonationEntity);
                                             }
                                         }
                                     });

            return lastPayedDonationEntities;
        }

        public IList<DonationItemsEntity> GetDonationItemsByDonationId(int donationId)
        {
            IList<DonationItemsEntity> donationItemEntities = new List<DonationItemsEntity>();

            RetryPolicy policy = new RetryPolicy<SqlAzureTransientErrorDetectionStrategy>(_maxRetries,
                                                                                          TimeSpan.FromMilliseconds(
                                                                                              _delayMs));

            policy.ExecuteAction(() =>
                                     {
                                         using (var entities = new BancoAlimentarEntities())
                                         {
                                             ObjectResult<DonationItemsEntity> donation =
                                                 entities.GetDonationItemsByDonationId(donationId);

                                             foreach (DonationItemsEntity donationEntity in donation)
                                             {
                                                 donationItemEntities.Add(donationEntity);
                                             }
                                         }
                                     });

            return donationItemEntities;
        }

        public IList<DonationByReferenceEntity> GetDonationByReference(string serviceReference)
        {
            IList<DonationByReferenceEntity> donationItemEntities = new List<DonationByReferenceEntity>();

            RetryPolicy policy = new RetryPolicy<SqlAzureTransientErrorDetectionStrategy>(_maxRetries,
                                                                                          TimeSpan.FromMilliseconds(
                                                                                              _delayMs));

            policy.ExecuteAction(() =>
                                     {
                                         using (var entities = new BancoAlimentarEntities())
                                         {
                                             ObjectResult<DonationByReferenceEntity> donation =
                                                 entities.GetDonationByReference(serviceReference);

                                             foreach (DonationByReferenceEntity donationEntity in donation)
                                             {
                                                 donationItemEntities.Add(donationEntity);
                                             }
                                         }
                                     });

            return donationItemEntities;
        }

        // GetSumTotalPayedDonation()
        public IList<SumTotalPayedDonationEntity> GetSumTotalPayedDonation()
        {
            IList<SumTotalPayedDonationEntity> donationItemEntities = new List<SumTotalPayedDonationEntity>();

            RetryPolicy policy = new RetryPolicy<SqlAzureTransientErrorDetectionStrategy>(_maxRetries,
                                                                                          TimeSpan.FromMilliseconds(
                                                                                              _delayMs));

            policy.ExecuteAction(() =>
                                     {
                                         using (var entities = new BancoAlimentarEntities())
                                         {
                                             ObjectResult<SumTotalPayedDonationEntity> donation =
                                                 entities.GetSumTotalPayedDonation();

                                             foreach (SumTotalPayedDonationEntity donationEntity in donation)
                                             {
                                                 donationItemEntities.Add(donationEntity);
                                             }
                                         }
                                     });

            return donationItemEntities;
        }

        public IList<AllDonorsEntity> GetAllDonors()
        {
            IList<AllDonorsEntity> donors = new List<AllDonorsEntity>();

            RetryPolicy policy = new RetryPolicy<SqlAzureTransientErrorDetectionStrategy>(_maxRetries,
                                                                                          TimeSpan.FromMilliseconds(
                                                                                              _delayMs));

            policy.ExecuteAction(() =>
                                     {
                                         using (var entities = new BancoAlimentarEntities())
                                         {
                                             ObjectResult<AllDonorsEntity> donation = entities.GetAllDonors();

                                             foreach (AllDonorsEntity donor in donation)
                                             {
                                                 donors.Add(donor);
                                             }
                                         }
                                     });

            return donors;
        }

        public IList<TotalDonationValueEntity> GetTotalDonationValue()
        {
            IList<TotalDonationValueEntity> values = new List<TotalDonationValueEntity>();

            RetryPolicy policy = new RetryPolicy<SqlAzureTransientErrorDetectionStrategy>(_maxRetries,
                                                                                          TimeSpan.FromMilliseconds(
                                                                                              _delayMs));

            policy.ExecuteAction(() =>
                                     {
                                         using (var entities = new BancoAlimentarEntities())
                                         {
                                             ObjectResult<TotalDonationValueEntity> donation =
                                                 entities.GetTotalDonationValue();

                                             foreach (TotalDonationValueEntity value in donation)
                                             {
                                                 values.Add(value);
                                             }
                                         }
                                     });

            return values;
        }

        public IList<QuantitiesByProductEntity> GetQuantitiesByProduct()
        {
            IList<QuantitiesByProductEntity> quantities = new List<QuantitiesByProductEntity>();

            RetryPolicy policy = new RetryPolicy<SqlAzureTransientErrorDetectionStrategy>(_maxRetries,
                                                                                          TimeSpan.FromMilliseconds(
                                                                                              _delayMs));

            policy.ExecuteAction(() =>
                                     {
                                         using (var entities = new BancoAlimentarEntities())
                                         {
                                             ObjectResult<QuantitiesByProductEntity> donation =
                                                 entities.GetQuantitiesByProduct();

                                             foreach (QuantitiesByProductEntity quantity in donation)
                                             {
                                                 quantities.Add(quantity);
                                             }
                                         }
                                     });

            return quantities;
        }

        public IList<FoodBankEntity> GetFoodBanks()
        {
            IList<FoodBankEntity> foodBanks = new List<FoodBankEntity>();

            RetryPolicy policy = new RetryPolicy<SqlAzureTransientErrorDetectionStrategy>(_maxRetries,
                                                                                          TimeSpan.FromMilliseconds(
                                                                                              _delayMs));

            policy.ExecuteAction(() =>
                                     {
                                         using (var entities = new BancoAlimentarEntities())
                                         {
                                             ObjectResult<FoodBankEntity> donation = entities.GetFoodBanks();

                                             foreach (FoodBankEntity foodBank in donation)
                                             {
                                                 foodBanks.Add(foodBank);
                                             }
                                         }
                                     });

            return foodBanks;
        }

        public GetLastAepsFile_Result GetLastAepsFile()
        {
            var result = new GetLastAepsFile_Result();

            var policy = new RetryPolicy<SqlAzureTransientErrorDetectionStrategy>(_maxRetries,
                                                                                  TimeSpan.FromMilliseconds(
                                                                                      _delayMs));

            policy.ExecuteAction(() =>
                                     {
                                         using (var entities = new BancoAlimentarEntities())
                                         {
                                             var lastAepsFile = entities.GetLastAepsFile();
                                             foreach (var item in lastAepsFile)
                                             {
                                                 result = item;
                                                 break;
                                             }
                                         }
                                     });

            return result;
        }

        public IList<GetNotPaidDonations_Result> GetNotPaidDonations()
        {
            var result = new List<GetNotPaidDonations_Result>();

            var policy = new RetryPolicy<SqlAzureTransientErrorDetectionStrategy>(_maxRetries,
                                                                                  TimeSpan.FromMilliseconds(
                                                                                      _delayMs));

            policy.ExecuteAction(() =>
                                     {
                                         using (var entities = new BancoAlimentarEntities())
                                         {
                                             var notPaidDonations = entities.GetNotPaidDonations();
                                             result.AddRange(notPaidDonations);
                                         }
                                     });

            return result;
        }

        public void InsertAepsFile(string fileId, DateTime date, string fileContent)
        {
            var policy = new RetryPolicy<SqlAzureTransientErrorDetectionStrategy>(_maxRetries,
                                                                                  TimeSpan.FromMilliseconds(
                                                                                      _delayMs));

            policy.ExecuteAction(() =>
                                     {
                                         using (var entities = new BancoAlimentarEntities())
                                         {
                                             entities.InsertAepsFile(fileId, date, fileContent);
                                         }
                                     });
        }

        public void UpdateAepsFile(string fileId, string mepsFileContent, string aepeFileContent, string aeprFileContent)
        {
            var policy = new RetryPolicy<SqlAzureTransientErrorDetectionStrategy>(_maxRetries,
                                                                                  TimeSpan.FromMilliseconds(
                                                                                      _delayMs));

            policy.ExecuteAction(() =>
                                     {
                                         using (var entities = new BancoAlimentarEntities())
                                         {
                                             entities.UpdateAepsFile(fileId, mepsFileContent, aepeFileContent,
                                                                     aeprFileContent);
                                         }
                                     });
        }

        public IList<LastPayedDonationEntity> GetLastPayedDonationsByFoodBank(int? totalDonations, int? pageNumber,
                                                                              int? pageSize, int foodBank)
        {
            IList<LastPayedDonationEntity> lastPayedDonationEntities = new List<LastPayedDonationEntity>();

            RetryPolicy policy = new RetryPolicy<SqlAzureTransientErrorDetectionStrategy>(_maxRetries,
                                                                                          TimeSpan.FromMilliseconds(
                                                                                              _delayMs));

            policy.ExecuteAction(() =>
                                     {
                                         using (var entities = new BancoAlimentarEntities())
                                         {
                                             ObjectResult<LastPayedDonationEntity> donation =
                                                 entities.GetLastPayedDonationsByFoodBank(totalDonations,
                                                                                          pageNumber, pageSize, foodBank);
                                             foreach (LastPayedDonationEntity lastPayedDonationEntity in donation)
                                             {
                                                 lastPayedDonationEntities.Add(lastPayedDonationEntity);
                                             }
                                         }
                                     });

            return lastPayedDonationEntities;
        }

        public IList<TotalDonationsEntity> GetTotalDonationsByFoodBank(int foodBank)
        {
            IList<TotalDonationsEntity> donationList = new List<TotalDonationsEntity>();

            RetryPolicy policy = new RetryPolicy<SqlAzureTransientErrorDetectionStrategy>(_maxRetries,
                                                                                          TimeSpan.FromMilliseconds(
                                                                                              _delayMs));

            policy.ExecuteAction(() =>
                                     {
                                         using (var entities = new BancoAlimentarEntities())
                                         {
                                             ObjectResult<TotalDonationsEntity> totalDonations =
                                                 entities.GetTotalDonationsByFoodBank(foodBank);
                                             foreach (TotalDonationsEntity totalDonationEntity in totalDonations)
                                             {
                                                 donationList.Add(totalDonationEntity);
                                             }
                                         }
                                     });

            return donationList;
        }

        public IList<FoodBankEntity> GetFoodBanksByFoodBank(int foodBank)
        {
            IList<FoodBankEntity> foodBanks = new List<FoodBankEntity>();

            RetryPolicy policy = new RetryPolicy<SqlAzureTransientErrorDetectionStrategy>(_maxRetries,
                                                                                          TimeSpan.FromMilliseconds(
                                                                                              _delayMs));

            policy.ExecuteAction(() =>
                                     {
                                         using (var entities = new BancoAlimentarEntities())
                                         {
                                             ObjectResult<FoodBankEntity> donation =
                                                 entities.GetFoodBanksByFoodBank(foodBank);

                                             foreach (FoodBankEntity entity in donation)
                                             {
                                                 foodBanks.Add(entity);
                                             }
                                         }
                                     });

            return foodBanks;
        }

        public void ValidateMepsFile(string fileId, string mepsFileContent)
        {
            var policy = new RetryPolicy<SqlAzureTransientErrorDetectionStrategy>(_maxRetries,
                                                                                  TimeSpan.FromMilliseconds(
                                                                                      _delayMs));

            policy.ExecuteAction(() =>
            {
                using (var entities = new BancoAlimentarEntities())
                {
                    entities.ValidateMepsFile(fileId, mepsFileContent);
                }
            });
        }

        public IList<QuantitiesByFoodBankAndProductEntity> GetQuantitiesByFoodBankAndProduct()
        {
            IList<QuantitiesByFoodBankAndProductEntity> quantities = new List<QuantitiesByFoodBankAndProductEntity>();

            RetryPolicy policy = new RetryPolicy<SqlAzureTransientErrorDetectionStrategy>(_maxRetries,
                                                                                          TimeSpan.FromMilliseconds(
                                                                                              _delayMs));

            policy.ExecuteAction(() =>
            {
                using (var entities = new BancoAlimentarEntities())
                {
                    ObjectResult<QuantitiesByFoodBankAndProductEntity> rows = entities.GetQuantitiesByFoodBankAndProduct();

                    foreach (QuantitiesByFoodBankAndProductEntity row in rows)
                    {
                        quantities.Add(row);
                    }
                }
            });

            return quantities;
        }
    }
}