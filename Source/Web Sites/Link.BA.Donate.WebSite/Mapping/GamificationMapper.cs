using Acn.BA.Gamification.Models;
using Link.BA.Donate.WebSite.Models.Gamification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Link.BA.Donate.WebSite.Mapping
{
    public class GamificationMapper
    {
        public UserDataDto MapUserDataDto(User user)
        {
            var result = new UserDataDto()
            {
                Name = user.Name,
                Id = user.Id,
                DonatedAmount = user.Donation.Sum(d => d.Amount),
                Invited = user.Invited.Select<Invite,UserDataDto>( x => MapUserDataDtoFromInvite(x)) 
            };

            return result;
        }

        private UserDataDto MapUserDataDtoFromInvite(Invite invite)
        {
            return new UserDataDto()
            {
                Id = invite.ToUserId,
                Name = invite.Nickname,
                DonatedAmount = invite.UserTo.Donation.Where(d => d.CreatedTs >= invite.CreatedTs).Sum(d => d.Amount)
            };
        }
    }
}