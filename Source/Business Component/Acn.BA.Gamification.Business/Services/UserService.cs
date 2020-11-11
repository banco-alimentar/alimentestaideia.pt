using Acn.BA.Gamification.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Acn.BA.Gamification.Business.Services
{
    public class UserService 
    {
        private GamificationDbContext _db;

        public UserService(GamificationDbContext db)
        {
            _db = db;
        }

        public User GetUserFromCode(string sessionCode)
        {
            User user = _db.User.Where(u => u.SessionCode == sessionCode).FirstOrDefault();
            if (user == null)
                throw new GamificationException("The user code provided dows not exist", Messages.SessionCodeNotFound);
            else
                return user;
        }

        public User GetUserFromId(int userId)
        {
            User user = _db.User.Where(u => u.Id == userId).FirstOrDefault();
            if (user == null)
                throw new GamificationException("The user code provided dows not exist", Messages.SessionCodeNotFound);
            else
                return user;
        }
    }
}
