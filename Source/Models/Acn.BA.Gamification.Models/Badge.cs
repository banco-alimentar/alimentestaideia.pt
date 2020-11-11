using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace Acn.BA.Gamification.Models
{
    public class Badge
    {
        public static Badge Beginner = new Badge(1, "Beginner", "");


        public Badge(int id, string name, string imageUrl)
        {
            if (name == null || imageUrl == null)
                throw new Exception("EnumBadge parameters cannot be null");

            Id = id;
            Name = name;
            ImageUrl = imageUrl;
        }

        public int Id { get; private set; }

        public string Name { get; private set; }

        public string ImageUrl { get; private set; }

        private static List<Badge> _valuesCache;
        public static List<Badge> Values {
            get {
                if (_valuesCache == null)
                {
                    _valuesCache = typeof(Badge)
                        .GetFields(BindingFlags.Public | BindingFlags.Static)
                        .Select(f => (Badge)f.GetValue(null))
                        .ToList();
                }
                return _valuesCache;
            }
        }

        public static Badge Parse(int id)
        {
            return Badge.Values.First(x => x.Id == id);
        }

        #region equality
        public override bool Equals(object obj)
        {
            return obj is Badge badge &&
                   Id == badge.Id;
        }

        public override int GetHashCode()
        {
            return 2108858624 + Id.GetHashCode();
        }

        public static bool operator ==(Badge left, Badge right)
        {
            return EqualityComparer<Badge>.Default.Equals(left, right);
        }

        public static bool operator !=(Badge left, Badge right)
        {
            return !(left == right);
        }
        #endregion

        public override string ToString()
        {
            return Id.ToString();
        }
    }
}
