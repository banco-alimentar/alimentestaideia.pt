// -----------------------------------------------------------------------
// <copyright file="InvoiceRepository.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System;
    using System.Data;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Resources;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.FileProviders;

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

        /// <summary>
        /// Find the invoice from the donation public id.
        /// </summary>
        /// <param name="publicId">A reference to the donation public id.</param>
        /// <returns>A reference to the <see cref="Invoice"/>.</returns>
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
                    using (var transaction = this.DbContext.Database.BeginTransaction(IsolationLevel.Serializable))
                    {
                        int sequence = this.GetNextSequence();
                        string invoiceFormat = this.GetInvoiceFormat();

                        result = new Invoice()
                        {
                            Created = DateTime.UtcNow,
                            Donation = donation,
                            User = user,
                            InvoicePublicId = Guid.NewGuid(),
                            Sequence = sequence,
                            Number = string.Format(invoiceFormat, sequence.ToString("D4"), DateTime.Now.Year),
                        };

                        this.DbContext.Invoices.Add(result);
                        this.DbContext.SaveChanges();

                        transaction.Commit();
                    }
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

        /// <summary>
        /// Gets the invoice format from the resource file.
        /// </summary>
        /// <returns>The invoice format.</returns>
        private string GetInvoiceFormat()
        {
            string result = null;

            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("BancoAlimentar.AlimentaEstaIdeia.Repository.Resources.InvoiceRepository.resources"))
            {
                using (ResourceReader reader = new ResourceReader(stream))
                {
                    using (ResourceSet resourceSet = new ResourceSet(reader))
                    {
                        result = resourceSet.GetString("Name");
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Gets the next sequence id in the database to calculate the invoice number.
        /// </summary>
        /// <returns>Return the number of invoices for the current year + 1.</returns>
        private int GetNextSequence()
        {
            int result = -1;

            int currentYear = DateTime.Now.Year;

            result = this.DbContext.Invoices
                .Where(p => p.Created.Year == currentYear)
                .Count();

            result++;

            return result;
        }
    }
}
