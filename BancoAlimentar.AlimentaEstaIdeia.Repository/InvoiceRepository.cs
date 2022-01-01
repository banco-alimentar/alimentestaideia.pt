// -----------------------------------------------------------------------
// <copyright file="InvoiceRepository.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Resources;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.Validation;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.FileProviders;

    /// <summary>
    /// Default implementation for the <see cref="Invoice"/> repository pattern.
    /// </summary>
    public class InvoiceRepository : GenericRepository<Invoice>
    {
        private readonly NifApiValidator nifApiValidator;

        /// <summary>
        /// Initializes a new instance of the <see cref="InvoiceRepository"/> class.
        /// </summary>
        /// <param name="context"><see cref="ApplicationDbContext"/> instance.</param>
        /// <param name="memoryCache">A reference to the Memory cache system.</param>
        /// <param name="telemetryClient">Telemetry Client.</param>
        /// <param name="nifApiValidator">Nif validation api.</param>
        public InvoiceRepository(
            ApplicationDbContext context,
            IMemoryCache memoryCache,
            TelemetryClient telemetryClient,
            NifApiValidator nifApiValidator)
            : base(context, memoryCache, telemetryClient)
        {
            this.nifApiValidator = nifApiValidator;
        }

        /// <summary>
        /// Find the invoice from the donation public id.
        /// </summary>
        /// <param name="publicId">A reference to the donation public id.</param>
        /// <param name="generateInvoice">True to generate the invoice if not found.</param>
        /// <returns>A reference to the <see cref="Invoice"/>.</returns>
        public Invoice FindInvoiceByPublicId(string publicId, bool generateInvoice = true)
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

                    if (donation != null)
                    {
                        result = this.GetOrCreateInvoiceByDonation(donation.Id, donation.User, generateInvoice);
                        var telemetryData = new Dictionary<string, string> { { "publicId", publicId }, { "donation.Id", donation.Id.ToString() } };
                        this.TelemetryClient.TrackEvent("FindInvoiceByPublicId", telemetryData);
                    }
                    else
                    {
                        this.TelemetryClient.TrackEvent(
                            "PublicDonationIdNotFound",
                            new Dictionary<string, string>()
                            {
                                { "PublicId", publicId },
                            });
                    }
                }
                else
                {
                    this.TelemetryClient.TrackException(new ArgumentException($"FindInvoiceByPublicId called with invalid Guid {publicId}"));
                }
            }

            return result;
        }

        /// <summary>
        /// Creates a new invoice for the specified donation and for the user, or returns the invoice if it already exists.
        /// </summary>
        /// <param name="donationId">Donation id.</param>
        /// <param name="user"><see cref="WebUser"/>.</param>
        /// <param name="generateInvoice">True to generate the invoice if not found.</param>
        /// <returns>A reference for the <see cref="Invoice"/>.</returns>
        public Invoice GetOrCreateInvoiceByDonation(int donationId, WebUser user, bool generateInvoice = true)
        {
            Invoice result = null;
            Donation donation = this.DbContext.Donations
                .Include(p => p.DonationItems)
                .Include(p => p.User)
                .Include(p => p.ConfirmedPayment)

                // .Include("DonationItems.ProductCatalogue")
                .Where(p => p.Id == donationId)
                .FirstOrDefault();

            if (donation == null)
            {
                this.TelemetryClient.TrackEvent(
                    "FindInvoiceByDonation-DonationNotFound",
                    new Dictionary<string, string>()
                    {
                            { "DonationId", donationId.ToString() },
                            { "UserId", user?.Id },
                            { "Function", nameof(this.GetOrCreateInvoiceByDonation) },
                    });
                return null;
            }

            string nif = donation.Nif;
            string usersNif = donation.User.Nif;

            if (!this.nifApiValidator.IsValidNif(nif))
            {
                if (this.nifApiValidator.IsValidNif(usersNif))
                {
                    nif = donation.User.Nif;
                }
                else
                {
                    return null;
                }
            }

            lock (this)
            {
                if (donation != null &&
                    donation.User != null &&
                    donation.User.Id != user.Id)
                {
                    this.TelemetryClient.TrackEvent(
                        "CreateInvoice-DonationUserIdNotFound",
                        new Dictionary<string, string>()
                        {
                            { "DonationId", donationId.ToString() },
                            { "UserId", user.Id },
                            { "Function", nameof(this.GetOrCreateInvoiceByDonation) },
                        });
                    return null;
                }

                if (donation.PaymentStatus != PaymentStatus.Payed)
                {
                    this.TelemetryClient.TrackEvent(
                        "CreateInvoice-InvoiceWithPaymentStatusNotPayed",
                        new Dictionary<string, string>()
                        {
                            { "DonationId", donationId.ToString() },
                            { "UserId", user.Id },
                            { "Function", nameof(this.GetOrCreateInvoiceByDonation) },
                        });
                    return null;
                }

                if (donation.ConfirmedPayment == null || PaymentStatusMessages.FailedPaymentMessages.Any(p => p == donation.ConfirmedPayment?.Status))
                {
                    this.TelemetryClient.TrackEvent(
                       "CreateInvoice-ConfirmedFailedPaymentStatus",
                       new Dictionary<string, string>()
                       {
                            { "DonationId", donationId.ToString() },
                            { "UserId", user.Id },
                            { "ConfirmedPaymentStatusId", donation.ConfirmedPayment?.Id.ToString() },
                            { "Function", nameof(this.GetOrCreateInvoiceByDonation) },
                       });
                    return null;
                }

                var strategy = this.DbContext.Database.CreateExecutionStrategy();
                strategy.ExecuteInTransaction(
                    this.DbContext,
                    operation: (context) =>
                    {
                        result = context.Invoices
                            .Include(p => p.Donation)
                            .Include(p => p.Donation.DonationItems)
                            .Where(p => p.Donation.Id == donation.Id)
                            .FirstOrDefault();
                        context.SaveChanges(acceptAllChangesOnSuccess: false);
                    },
                    verifySucceeded: (context) => { return true; });

                strategy.ExecuteInTransaction(
                    this.DbContext,
                    operation: (context) =>
                    {
                        if (generateInvoice && result == null)
                        {
                            DateTime portugalDateTimeNow = DateTime.Now.GetPortugalDateTime();

                            int sequence = GetNextSequence(portugalDateTimeNow, context);
                            string invoiceFormat = this.GetInvoiceFormat();

                            if (sequence > 0)
                            {
                                result = new Invoice()
                                {
                                    Created = portugalDateTimeNow,
                                    Donation = donation,
                                    User = user,
                                    BlobName = Guid.NewGuid(),
                                    Sequence = sequence,
                                    Number = string.Format(invoiceFormat, sequence.ToString("D4"), DateTime.Now.Year),
                                };

                                context.Invoices.Add(result);
                                context.SaveChanges(acceptAllChangesOnSuccess: false);
                            }
                            else
                            {
                                this.TrackExceptionTelemetry($"FindInvoiceByDonation Invoice Sequence number was {sequence}", donationId, user.Id);
                            }
                        }
                    },
                    verifySucceeded: (context) =>
                    {
                        return context.Invoices
                            .Where(p =>
                                p.Donation.Id == donationId &&
                                p.User.Id == user.Id).FirstOrDefault() != null;
                    });

                if (result != null)
                {
                    result.User = this.DbContext.WebUser
                        .Include(p => p.Address)
                        .Where(p => p.Id == user.Id)
                        .FirstOrDefault();
                }

                if (result != null && result.User.Address == null)
                {
                    result.User.Address = new DonorAddress();
                }
            }

            return result;
        }

        /// <summary>
        /// Gets the normalized <see cref="Invoice"/> name.
        /// </summary>
        /// <param name="value">A reference to the <see cref="Invoice"/>.</param>
        /// <returns>The normalized invoice name, that for now stars with RECIBO Nº, but in the future will be localized.</returns>
        public string GetInvoiceName(Invoice value)
        {
            string result = null;

            if (value != null)
            {
                result = $"RECIBO Nº {value.Number}";
            }

            return result;
        }

        /// <summary>
        /// Gets all the invoices for the current user.
        /// </summary>
        /// <param name="userId">User id.</param>
        /// <returns>A <see cref="List{Invoice}"/> with all the user invoice.</returns>
        public List<Invoice> GetAllInvoicesFromUserId(string userId)
        {
            List<Invoice> result = new List<Invoice>();

            if (!string.IsNullOrEmpty(userId))
            {
                result = this.DbContext.Invoices
                    .Where(p => p.User.Id == userId)
                    .ToList();
            }

            return result;
        }

        /// <summary>
        /// Gets the next sequence id in the database to calculate the invoice number.
        /// </summary>
        /// <returns>Return the number of invoices for the current year + 1.</returns>
        /// <param name="portugalDateTimeNow">This is the local time for Portugal when generating the next sequence.</param>
        /// <param name="context">ApplicationDbContext for transaction support.</param>
        private static int GetNextSequence(DateTime portugalDateTimeNow, ApplicationDbContext context)
        {
            int result = 0;

            int currentYear = portugalDateTimeNow.Year;

            // Check for empty invoice table
            bool isEmpty = context.Invoices.Count() < 1;
            if (!isEmpty)
            {
                result = context.Invoices
                    .Where(p => p.Created.Year == currentYear)
                    .Max(p => p.Sequence);
            }

            result++;
            return result;
        }

        /// <summary>
        /// Tracks an ExceptionTelemetry to App Insights.
        /// </summary>
        /// <param name="message">The message of the exception.</param>
        /// <param name="donationId">The donation id that it refers to.</param>
        /// <param name="userId">The userId that was passed to the method.</param>
        private void TrackExceptionTelemetry(string message, int donationId, string userId)
        {
            ExceptionTelemetry exceptionTelemetry = new ExceptionTelemetry(new InvalidOperationException(message));
            exceptionTelemetry.Properties.Add("DonationId", donationId.ToString());
            exceptionTelemetry.Properties.Add("UserId", userId);
            this.TelemetryClient?.TrackException(exceptionTelemetry);
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
    }
}
