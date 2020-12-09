using Acn.BA.Gamification.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Web;

namespace Link.BA.Donate.WebSite.Models.Gamification
{
    public class UserDataDto
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public decimal DonatedAmount { get; set; }

        public int DonationCount { get; set; }

        public int InvitedCount { get; set; }

        public int AvailableInvites { get; set; }

        public IEnumerable<BadgeDto> Badges { get; set; }

        public IEnumerable<UserDataDto> Invited { get; set; }

        public bool CanPoke { get; set; }

        public static UserDataDto FromUser(User user, bool includeInvitedUsers = true, bool canPoke = false)
        {
            return new UserDataDto
            {
                Id = user.Id,
                Name = user.Name,
                Badges = user.Badges == null
                        ? new List<BadgeDto>()
                        : user.Badges.Select(b => new BadgeDto(b.Id, GetString(b.Name), GetString(b.Description), $"/Content/gmf/badge-{b.Id}.png")),
                CanPoke = canPoke,
                DonatedAmount = user.Donation.Sum(d => d.Amount),
                DonationCount = user.DonationCount,
                InvitedCount = user.InvitedCount,
                Invited = includeInvitedUsers 
                            ? user.Invited.Where(i => i.IsOpen).Select(x => 
                                UserDataDto.FromUser(
                                    x.UserTo, 
                                    includeInvitedUsers: false, 
                                    canPoke: x.LastPokeTs <= DateTime.UtcNow.Subtract(new TimeSpan(10,0,0,0))
                                    )
                                )
                            : null,
                AvailableInvites = user.AvailableInvites,
            };
        }

        private static string GetString(string name)
        {
            return Gamification.ResourceManager.GetString(name);
        }

    }


}