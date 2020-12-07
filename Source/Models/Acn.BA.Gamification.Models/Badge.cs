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
        public static Badge InternautaSocial = new Badge(1, Messages.InternautaSocialName, Messages.InternautaSocialDesc);
        public static Badge InfluencerSocial = new Badge(2, Messages.InfluencerSocialName, Messages.InfluencerSocialDesc);
        public static Badge Maratonista = new Badge(3, Messages.MaratonistaName, Messages.MaratonistaDesc);
        public static Badge SurfistaSocial = new Badge(4, Messages.SurfistaSocialName, Messages.SurfistaSocialDesc);
        public static Badge MichaelPhelps = new Badge(5, Messages.MichaelPhelpsName, Messages.MichaelPhelpsDesc);
        public static Badge ExcelenciaBa = new Badge(6, Messages.ExcelenciaBaName, Messages.ExcelenciaBaDesc);

        public Badge(int id, string name, string description)
        {
            if (name == null || description == null)
                throw new Exception("EnumBadge parameters cannot be null");

            Id = id;
            Name = name;
            Description = description;
        }

        public int Id { get; private set; }

        public string Name { get; private set; }

        public string Description { get; private set; }

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
