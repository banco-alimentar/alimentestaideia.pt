using Acn.BA.Gamification.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Acn.BA.Gamification.Business.Services
{
    public class InvitesService
    {
        private CustomerMessageService _customerMessageService;
        private GamificationDbContext _db;

        public InvitesService(GamificationDbContext db, CustomerMessageService customerMessageService)
        {
            _customerMessageService = customerMessageService;
            _db = db;
        }

        public void CreateInvite(User fromUser, User toUser, string nickName, string message)
        {
            if (fromUser.AvailableInvites <= 0)
                throw new GamificationException("No available invites", Messages.NoAvailableInvites);

            if (fromUser.Invited.Any(i => i.ToUserId == toUser.Id))
                //throw new GamificationException("Already invited user", Messages.AlreadyInvitedUser);
                return;

            var invite = new Invite()
            {
                UserFrom = fromUser,
                UserTo = toUser,
                CreatedTs = DateTime.UtcNow,
                Nickname = nickName,
                LastPokeTs = DateTime.UtcNow,
                Message = message,
            };
            _db.Invite.Add(invite);
            _customerMessageService.SendInviteMail(invite);
            _db.SaveChanges();
        }

        public void SendInviteMessage(Invite invite)
        {
            _customerMessageService.SendInviteMail(invite);
        }

        public void Poke(User fromUser, int invitedId)
        {
            var inv = fromUser.Invited.Where(i => i.IsOpen && i.ToUserId == invitedId).FirstOrDefault();
            if (inv == null)
                throw new GamificationException("Cannot poke a non invited user", Messages.PokedNonInvitedUser);
            if (DateTime.Now.Subtract(inv.LastPokeTs).TotalDays < 10)
                throw new GamificationException("Can only poke once in every 10 days", Messages.PokedLessThan10Days);
            _customerMessageService.SendPokeMail(fromUser, inv.UserTo);
            inv.LastPokeTs = DateTime.Now;
            _db.SaveChanges();
        }
    }
}
