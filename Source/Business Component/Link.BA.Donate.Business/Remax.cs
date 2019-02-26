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
    public class Remax
    {
        private int _maxRetries = Convert.ToInt32(ConfigurationManager.AppSettings["RetryPolicy.MaxRetries"]);
        private int _delayMs = Convert.ToInt32(ConfigurationManager.AppSettings["RetryPolicy.DelayMS"]);
        private bool m_sendMail = Convert.ToBoolean(ConfigurationManager.AppSettings["Email.Send"]);

        public Remax()
        {
        }

        public bool InsertContact(RemaxContactEntity remaxContactEntity)
        {
            IList<DonorsPictureEntity> donationList = new List<DonorsPictureEntity>();

            RetryPolicy policy = new RetryPolicy<SqlAzureTransientErrorDetectionStrategy>(_maxRetries,
                                                                                          TimeSpan.FromMilliseconds(
                                                                                              _delayMs));
            try
            {
                policy.ExecuteAction(() =>
                {
                    using (var entities = new BancoAlimentarEntities())
                    {

                        var remaxContactId = new ObjectParameter("RemaxContactId", 0);
                        entities.InsertRemaxContact(remaxContactId, remaxContactEntity.Name, remaxContactEntity.Email, remaxContactEntity.PhoneNumber, remaxContactEntity.Reference, remaxContactEntity.Campaign);
                        remaxContactEntity.RemaxContactId = (int)remaxContactId.Value;

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
    }
}
