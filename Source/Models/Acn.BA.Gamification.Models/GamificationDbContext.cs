using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Data.Entity.SqlServer;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Acn.BA.Gamification.Models
{
    public class GamificationDbContext : GamificationEntityModelContainer
    {
        public GamificationDbContext()
            :base()
        { }
    }

    public class GamificationDbConfiguration : DbConfiguration
    {
        private int _maxRetries = Convert.ToInt32(ConfigurationManager.AppSettings["RetryPolicy.MaxRetries"]);
        private int _delayMs = Convert.ToInt32(ConfigurationManager.AppSettings["RetryPolicy.DelayMS"]);

        public GamificationDbConfiguration()
        {
            this.SetExecutionStrategy(
                    "System.Data.SqlClient", () => new SqlAzureExecutionStrategy(_maxRetries, TimeSpan.FromMilliseconds(_delayMs)));
        }
    }
}
