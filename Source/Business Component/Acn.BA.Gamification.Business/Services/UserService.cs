using Acn.BA.Gamification.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Acn.BA.Gamification.Business.Services
{
    public class UserService 
    {
        private GamificationDbContext _db;
        private CustomerMessageService _messageService;
        public UserService(GamificationDbContext db, CustomerMessageService messageService)
        {
            _db = db;
            _messageService = messageService;
        }

        /// <summary>
        /// Get user from sessionCode
        /// </summary>
        /// <param name="sessionCode"></param>
        /// <returns></returns>
        public User GetUserFromCode(string sessionCode)
        {
            User user = _db.User.Where(u => u.SessionCode == sessionCode).FirstOrDefault();
            if (user == null)
                throw new GamificationException("The user code provided does not exist", Messages.SessionCodeNotFound);
            else
                return user;
        }

        /// <summary>
        /// Get user from userId
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public User GetUserFromId(int userId)
        {
            User user = _db.User.Where(u => u.Id == userId).FirstOrDefault();
            if (user == null)
                throw new GamificationException("The user code provided dows not exist", Messages.SessionCodeNotFound);
            else
                return user;
        }

        /// <summary>
        /// Recursively updates the current user and is chain metrics "upwards"
        /// </summary>
        /// <param name="user"></param>
        public void UpdateUser(User user, decimal donatedAmount, int inviteCnt, int donationCnt)
        {
            user.DonatedAmount += donatedAmount;
            user.InvitedCount += inviteCnt;
            user.DonationCount += donationCnt;

            // don't know if we eventually can loose the contition to some badge
            // is safer just to add the new ones and never replace them
            var newBadges = GenerateUserBadges(user).Except(user.Badges);
            var userBadges = new List<Badge>(user.Badges);
            userBadges.AddRange(newBadges);
            user.Badges = userBadges;
            _messageService.SendBadgeEmail(user, newBadges.ToList());

            GetUpwardsDistinctUsers(user)
                .ForEach(u => UpdateUserNetworkStats(u, donatedAmount, inviteCnt, donationCnt));
        }
        
        private List<User> GetUpwardsDistinctUsers(User user)
        {
            var invitedByUsers = user.InvitedBy.Select(i => i.UserFrom).Distinct().ToList();
            if (invitedByUsers.Count > 0)
            {
                var result = invitedByUsers.SelectMany(u => GetUpwardsDistinctUsers(u)).ToList();
                result.AddRange(invitedByUsers);
                return result.Distinct().ToList();
            }
            else
                return invitedByUsers;
        }

        private void UpdateUserNetworkStats(User user, decimal donatedAmount, int inviteCnt, int donationCnt)
        {
            user.NetworkDonatedAmount += donatedAmount;
            user.NetworkDonationsCount += donationCnt;
            user.NetworkInvitedCount += inviteCnt;
        }

        private List<Badge> GenerateUserBadges(User user)
        {
            var badges = new List<Badge>();
            var totalAmt = user.NetworkDonatedAmount + user.DonatedAmount;

            if (user.InvitedCount >= 3)
                badges.Add(Badge.InternautaSocial);

            if (user.NetworkDonationsCount >= 3)
                badges.Add(Badge.InfluencerSocial);

            if(totalAmt >= 30)
                badges.Add(Badge.Maratonista);

            if (totalAmt >= 91)
                badges.Add(Badge.SurfistaSocial);

            if (totalAmt >= 125)
                badges.Add(Badge.MichaelPhelps);

            if (totalAmt >= 250)
                badges.Add(Badge.ExcelenciaBa);

            return badges;
        }
    }
}
