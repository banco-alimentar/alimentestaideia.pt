using Acn.BA.Gamification.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;

namespace Acn.BA.Gamification.Business.Services
{
    public class StatisticsService
    {
        private const string TOTAL_WEIGHT_KEY = "total-weight";
        private static MemoryCache _cache = new MemoryCache(nameof(StatisticsService));
        private GamificationDbContext _db;

        public StatisticsService(GamificationDbContext db) 
        {
            this._db = db;
        }

        /// <summary>
        /// Returns the total weight donated
        /// the value is cached and only updated every 15m, we have no high precision requirements on this
        /// </summary>
        /// <returns></returns>
        public decimal GetTotalWeight() 
        {
            if (_cache.Contains(TOTAL_WEIGHT_KEY))
                return (decimal)_cache[TOTAL_WEIGHT_KEY];

            decimal totalWeightDb = _db.Donation.Sum(d => d.Weight);
            _cache.Add(TOTAL_WEIGHT_KEY, totalWeightDb, new CacheItemPolicy() { SlidingExpiration = new TimeSpan(0, 15, 0) });
            return totalWeightDb;
        }
    }
}
