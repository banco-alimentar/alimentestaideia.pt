namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System;
    using System.Linq;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using Microsoft.EntityFrameworkCore;

    /// <summary>
    /// Default implementation for the <see cref="WebUser"/> repository patter.
    /// </summary>
    public class UserRepository : GenericRepository<WebUser>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UserRepository"/> class.
        /// </summary>
        /// <param name="context"><see cref="ApplicationDbContext"/> instance.</param>
        public UserRepository(ApplicationDbContext context)
            : base(context)
        {
        }

        /// <summary>
        /// Gets the Anonymous user.
        /// </summary>
        /// <returns>A reference to the <see cref="WebUser"/>.</returns>
        public WebUser GetAnonymousUser()
        {
            string zeroId = new Guid().ToString();
            return this.DbContext.WebUser.Where(p => p.Id == zeroId).FirstOrDefault();
        }

        /// <summary>
        /// Find the <see cref="WebUser"/> by the ID.
        /// </summary>
        /// <param name="id">A <see cref="string"/> with the user id.</param>
        /// <returns>A reference to the <see cref="WebUser"/>.</returns>
        public WebUser FindUserById(string id)
        {
            WebUser result = null;

            if (!string.IsNullOrEmpty(id))
            {
                result = this.DbContext.WebUser
                    .Include(p => p.Address)
                    .Where(p => p.Id == id)
                    .FirstOrDefault();
            }

            return result;
        }

        public void DeleteUserAndDonations(string id)
        {
            if (!string.IsNullOrEmpty(id))
            {
                WebUser user = this.DbContext.WebUser.Where(p => p.Id == id).FirstOrDefault();

                var invoices = this.DbContext.Invoices.Where(p => p.User == user).ToList();
                this.DbContext.Invoices.RemoveRange(invoices);
                this.DbContext.SaveChanges();

                var paypalPayments = this.DbContext.PayPalPayments.Where(p => p.Donation.User.Id == id).ToList();
                this.DbContext.PayPalPayments.RemoveRange(paypalPayments);
                this.DbContext.SaveChanges();

                var donations = this.DbContext.Donations.Include(p => p.DonationItems).Where(p => p.User == user).ToList();
                this.DbContext.Donations.RemoveRange(donations);
                this.DbContext.SaveChanges();

                //this.DbContext.WebUser.Remove(user);
                //this.DbContext.SaveChanges();
            }
        }
    }
}
