namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using Microsoft.EntityFrameworkCore;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Default implementation for the <see cref="Invoice"/> repository pattern.
    /// </summary>
    public class InvoiceRepository : GenericRepository<Invoice>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InvoiceRepository"/> class.
        /// </summary>
        /// <param name="context"><see cref="ApplicationDbContext"/> instance.</param>
        public InvoiceRepository(ApplicationDbContext context)
            : base(context)
        {
        }

        public Invoice FindInvoiceByPublicId(string publicId)
        {
            Invoice result = null;
            if (!string.IsNullOrEmpty(publicId))
            {
                Guid targetPublicId;
                if (Guid.TryParse(publicId, out targetPublicId))
                {
                    Donation donation = this.DbContext.Donations
                        .Include(p => p.User)
                        .Where(p => p.PublicId == targetPublicId)
                        .FirstOrDefault();

                    result = this.FindInvoiceByDonation(donation.Id, donation.User);
                }
            }

            return result;
        }

        /// <summary>
        /// Gets the invoice for the specified donation and for the user.
        /// </summary>
        /// <param name="donationId">Donation id.</param>
        /// <param name="user"><see cref="WebUser"/>.</param>
        /// <returns>A reference for the <see cref="Invoice"/>.</returns>
        public Invoice FindInvoiceByDonation(int donationId, WebUser user)
        {
            Invoice result = null;

            Donation donation = this.DbContext.Donations
                .Include(p => p.DonationItems)
                .Include(p => p.User)
                .Include("DonationItems.ProductCatalogue")
                .Where(p => p.Id == donationId)
                .FirstOrDefault();

            if (donation != null &&
                donation.User != null &&
                donation.User.Id != user.Id)
            {
                return null;
            }

            if (donation.PaymentStatus != PaymentStatus.Payed)
            {
                return null;
            }

            if (donation != null)
            {
                result = this.DbContext.Invoices
                    .Include(p => p.Donation)
                    .Include(p => p.Donation.DonationItems)
                    .Where(p => p.Donation.Id == donation.Id)
                    .FirstOrDefault();

                if (result == null)
                {
                    result = new Invoice()
                    {
                        Created = DateTime.UtcNow,
                        Donation = donation,
                        User = user,
                        InvoicePublicId = Guid.NewGuid(),
                    };

                    this.DbContext.Invoices.Add(result);
                    this.DbContext.SaveChanges();
                }

                result.User = this.DbContext.WebUser
                    .Include(p => p.Address)
                    .Where(p => p.Id == user.Id)
                    .FirstOrDefault();
            }

            if (result != null && result.User.Address == null)
            {
                result.User.Address = new DonorAddress();
            }

            return result;
        }
    }
}
