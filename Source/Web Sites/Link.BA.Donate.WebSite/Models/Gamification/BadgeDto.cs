using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Link.BA.Donate.WebSite.Models.Gamification
{
    public class BadgeDto
    {
        public BadgeDto(int id, string name, string description, string imageUrl)
        {
            Id = id;
            Name = name;
            Description = description;
            ImageUrl = imageUrl;
        }

        public int Id { get; private set; }

        public string Name { get; private set; }

        public string Description { get; private set; }

        public string ImageUrl { get; private set; }
    }
}