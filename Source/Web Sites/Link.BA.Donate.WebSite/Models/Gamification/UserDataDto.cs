using Acn.BA.Gamification.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Link.BA.Donate.WebSite.Models.Gamification
{
    public class UserDataDto
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public decimal DonatedAmount { get; set; }

        public IEnumerable<BadgeDto> Badges { get; set; }

        public IEnumerable<UserDataDto> Invited { get; set; }

        public bool CanPoke { get; set; }

        public static UserDataDto FromUser(User user, bool includeInvitedUsers = true)
        {
            return new UserDataDto
            {
                Id = user.Id,
                Name = user.Name,
                Badges = null,
                CanPoke = false,
                DonatedAmount = user.Donation.Sum(d => d.Amount),
                Invited = user.CreatedInvites.Select(x => UserDataDto.FromUser(x.Invited, includeInvitedUsers: false)),
            };
        }

    }


}