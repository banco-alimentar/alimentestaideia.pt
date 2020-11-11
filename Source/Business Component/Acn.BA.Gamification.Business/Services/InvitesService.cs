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
        private GamificationEntityModelContainer _db;

        public InvitesService(GamificationEntityModelContainer db, CustomerMessageService customerMessageService)
        {
            _customerMessageService = customerMessageService;
            _db = db;
        }

        public void SendInvite(Invite invite)
        {
            _customerMessageService.SendInviteMail(invite);
        }

        public void Poke(User fromUser, User user)
        {
            var inv = _db.InviteSet
                .Where(x => x.InvitedBy == fromUser && x.Invited == user)
                .OrderByDescending(x => x.LastPokeTs)
                .FirstOrDefault();
            if (inv == null)
                throw new GamificationException("Cannot poke a non invited user", Messages.PokedNonInvitedUser);
            if (DateTime.Now.Subtract(inv.LastPokeTs).TotalDays < 10)
                throw new GamificationException("Can only poke once in every 10 days", Messages.PokedLessThan10Days);
            _customerMessageService.SendPokeMail(fromUser, user);
            inv.LastPokeTs = DateTime.Now;
            _db.SaveChanges();
        }
    }
}
