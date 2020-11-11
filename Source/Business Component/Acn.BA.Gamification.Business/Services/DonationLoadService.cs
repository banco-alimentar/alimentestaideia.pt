using Acn.BA.Gamification.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Acn.BA.Gamification.Business.Services
{
    public class DonationLoadService
    {
        GamificationEntityModelContainer _db;
        public DonationLoadService(GamificationEntityModelContainer dbContext)
        {
            _db = dbContext;
        }
        private InvitesService _invitesService;
        private int _batchSize = 20;

        public DonationLoadService(InvitesService invitesService)
        {
            _invitesService = invitesService;
        }

        public void LoadPendingDonations()
        {
            int processed = 0;
            do
            {
                processed = ProcessBatchChunk();
            } while (processed > 0);
        }

        /// <summary>
        /// transforms comleted donations into the final data model
        /// </summary>
        /// <returns>the number of processed records</returns>
        private int ProcessBatchChunk()
        {
            int processedCount = 0;
            foreach (var completedDonation in _db.CompletedDonationSet
                .SqlQuery($"select top {_batchSize} * from CompletedDonationSet  WITH (ROWLOCK, READPAST)")
                .ToList<CompletedDonation>())
            {
                processedCount++;
                ProcessDonation(_db, completedDonation);
            }
            return processedCount;
        }

        public void AddCompletedDonation(CompletedDonation donation)
        {
            _db.CompletedDonationSet.Add(donation);
            _db.SaveChanges();
        }

        private void ProcessDonation(GamificationEntityModelContainer db, CompletedDonation completedDonation)
        {
            try
            {
                var fromUser = GetOrCreateUser(completedDonation.Email, completedDonation.Name);
                var donation = GetOrCreateDonation(completedDonation.Id, completedDonation.Amount);
                donation.User = fromUser;

                if (!string.IsNullOrEmpty(completedDonation.User1Email) &&
                    !string.IsNullOrEmpty(completedDonation.User1Name))
                {
                    var invUser = GetOrCreateUser(completedDonation.User1Email, completedDonation.User1Name);
                    var invite = GetOrCreateInvite(donation, fromUser, invUser, completedDonation.User1Name);
                }
                if (!string.IsNullOrEmpty(completedDonation.User2Email) &&
                    !string.IsNullOrEmpty(completedDonation.User2Name))
                {
                    var invUser = GetOrCreateUser(completedDonation.User2Email, completedDonation.User2Name);
                    var invite = GetOrCreateInvite(donation, fromUser, invUser, completedDonation.User2Name);
                }
                if (!string.IsNullOrEmpty(completedDonation.User3Email) &&
                    !string.IsNullOrEmpty(completedDonation.User3Name))
                {
                    var invUser = GetOrCreateUser(completedDonation.User3Email, completedDonation.User3Name);
                    var invite = GetOrCreateInvite(donation, fromUser, invUser, completedDonation.User3Name);
                }
                db.CompletedDonationSet.Remove(completedDonation);
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                completedDonation.LoadError = ex.ToString();
                db.SaveChanges();
            }
        }

        #region upsert methods
        private User GetOrCreateUser(string email, string name)
        {
            var normalizedEmail = CleanEmail(email);
            var user = _db.UserSet.Where(u => u.Email == email).SingleOrDefault();

            if (user != null)
            {
                user.Name = name;
                return user;
            }
            else
            {
                user = new User()
                {
                    Email = normalizedEmail,
                    Name = name,
                    SessionCode = CreateSessionCode(),
                    CreatedTs = DateTime.UtcNow,
                };
                _db.UserSet.Add(user);
                return user;
            }
        }
        private Donation GetOrCreateDonation(int  id, decimal amount)
        {
            var donation = _db.DonationSet.Where(d => d.Id == id).SingleOrDefault();

            if (donation != null)
            {
                return donation;
            }
            else
            {
                donation = new Donation()
                {
                    Id = id,
                    Amount = amount,
                    CreatedTs = DateTime.UtcNow,
                };
                _db.DonationSet.Add(donation);
                return donation;
            }
        }
        private Invite GetOrCreateInvite(Donation donation, User fromUser, User toUser, string nickname)
        {
            var invite = _db.InviteSet.Where(i => i.DonationId == donation.Id && i.ToUserId == toUser.Id).SingleOrDefault();
            if (invite != null)
                return invite;
            else
            {
                invite = new Invite()
                {
                    Donation = donation,
                    InvitedBy = fromUser,
                    Invited = toUser,
                    Nickname = nickname,
                    CreatedTs = DateTime.UtcNow,
                    LastPokeTs = DateTime.UtcNow
                };
                _db.InviteSet.Add(invite);
                return invite;
            }
        }
        #endregion

        private static string CleanEmail(string email)
        {
            return email.ToLowerInvariant().Trim();
        }

        private static string CreateSessionCode()
        {
            return Guid.NewGuid().ToString().Replace("-", "");
        }
    }
}
