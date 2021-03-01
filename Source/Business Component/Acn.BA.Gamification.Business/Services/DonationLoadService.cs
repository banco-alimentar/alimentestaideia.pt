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
        private GamificationDbContext _db;
        private UserService _userService;
        public DonationLoadService(GamificationDbContext dbContext, UserService userService)
        {
            _db = dbContext;
            _userService = userService;
        }
        private int _batchSize = 20;

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
            foreach (var completedDonation in _db.CompletedDonation
                .SqlQuery($"select top {_batchSize} * from CompletedDonation  WITH (ROWLOCK, READPAST)")
                .ToList<CompletedDonation>())
            {
                processedCount++;
                ProcessDonation(_db, completedDonation);
            }
            return processedCount;
        }

        public void AddCompletedDonation(CompletedDonation donation)
        {
            _db.CompletedDonation.Add(donation);
            _db.SaveChanges();
        }

        private void ProcessDonation(GamificationDbContext db, CompletedDonation completedDonation)
        {
            try
            {
                var fromUser = GetOrCreateUser(completedDonation.Email, completedDonation.Name);
                fromUser.Name = completedDonation.Name;

                var donation = GetOrCreateDonation(completedDonation.Id, completedDonation.Amount, completedDonation.Weight);
                donation.User = fromUser;

                if (!string.IsNullOrEmpty(completedDonation.User1Email) &&
                    !string.IsNullOrEmpty(completedDonation.User1Name))
                {
                    var invUser = GetOrCreateUser(completedDonation.User1Email, completedDonation.User1Name);
                    var invite = GetOrCreateInvite(donation, fromUser, invUser, completedDonation.User1Name);
                    if (invite.Id == 0)
                        completedDonation.InviteCount++;
                }
                if (!string.IsNullOrEmpty(completedDonation.User2Email) &&
                    !string.IsNullOrEmpty(completedDonation.User2Name))
                {
                    var invUser = GetOrCreateUser(completedDonation.User2Email, completedDonation.User2Name);
                    var invite = GetOrCreateInvite(donation, fromUser, invUser, completedDonation.User2Name);
                    if (invite.Id == 0)
                        completedDonation.InviteCount++;
                }
                if (!string.IsNullOrEmpty(completedDonation.User3Email) &&
                    !string.IsNullOrEmpty(completedDonation.User3Name))
                {
                    var invUser = GetOrCreateUser(completedDonation.User3Email, completedDonation.User3Name);
                    var invite = GetOrCreateInvite(donation, fromUser, invUser, completedDonation.User3Name);
                    if (invite.Id == 0)
                        completedDonation.InviteCount++;
                }

                foreach(var inv in _db.Invite.Where(i => i.ToUserId == fromUser.Id && i.IsOpen == true))
                    inv.IsOpen = false;

                _userService.UpdateUser(fromUser, completedDonation.Amount, completedDonation.Weight, completedDonation.InviteCount, 1);
                db.CompletedDonation.Remove(completedDonation);
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                db.ChangeTracker.Entries().ToList()
                    .ForEach(e => {
                        if (!(e.Entity is CompletedDonation))
                            db.Entry(e).State = System.Data.Entity.EntityState.Detached;
                        });
                completedDonation.LoadError = ex.ToString();
                db.SaveChanges();
            }
        }

        #region upsert methods
        public User GetOrCreateUser(string email, string name)
        {
            var normalizedEmail = CleanEmail(email);
            var user = _db.User.Where(u => u.Email == email).SingleOrDefault();

            if (user != null)
            {
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
                _db.User.Add(user);
                return user;
            }
        }
        private Donation GetOrCreateDonation(int  id, decimal amount, decimal weight)
        {
            var donation = _db.Donation.Where(d => d.Id == id).SingleOrDefault();

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
                    Weight = weight,
                    CreatedTs = DateTime.UtcNow,
                };
                _db.Donation.Add(donation);
                return donation;
            }
        }
        private Invite GetOrCreateInvite(Donation donation, User fromUser, User toUser, string nickname)
        {
            var invite = _db.Invite.Where(i => i.FromUserId == fromUser.Id && i.ToUserId == toUser.Id).SingleOrDefault();
            if (invite != null)
                return invite;
            else
            {
                invite = new Invite()
                {
                    Donation = donation,
                    UserFrom = fromUser,
                    UserTo = toUser,
                    Nickname = nickname,
                    CreatedTs = DateTime.UtcNow,
                    LastPokeTs = DateTime.UtcNow
                };
                _db.Invite.Add(invite);
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
