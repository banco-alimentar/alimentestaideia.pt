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
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
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
        /// <summary>
        /// Initializes a new instance of the <see cref="InvoiceRepository"/> class.
        /// </summary>
        /// <param name="context"><see cref="ApplicationDbContext"/> instance.</param>
        /// <param name="memoryCache">A reference to the Memory cache system.</param>
        /// <param name="telemetryClient">Telemetry Client.</param>
        public InvoiceRepository(ApplicationDbContext context, IMemoryCache memoryCache, TelemetryClient telemetryClient)
            : base(context, memoryCache, telemetryClient)
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

                    if (donation != null)
                    {
                        result = this.FindInvoiceByDonation(donation.Id, donation.User);
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
        /// Gets the invoice for the specified donation and for the user.
        /// </summary>
        /// <param name="donationId">Donation id.</param>
        /// <param name="user"><see cref="WebUser"/>.</param>
        /// <returns>A reference for the <see cref="Invoice"/>.</returns>
        public Invoice FindInvoiceByDonation(int donationId, WebUser user)
        {
            Invoice result = null;

            lock (this)
            {
                Donation donation = this.DbContext.Donations
                    .Include(p => p.DonationItems)
                    .Include(p => p.User)
                    .Include(p => p.ConfirmedPayment)
                    .Include("DonationItems.ProductCatalogue")
                    .Where(p => p.Id == donationId)
                    .FirstOrDefault();

                if (donation == null)
                {
                    this.TelemetryClient.TrackEvent(
                        "CreateInvoice-DonationNotFound",
                        new Dictionary<string, string>()
                        {
                            { "DonationId", donationId.ToString() },
                            { "UserId", user?.Id },
                            { "Function", nameof(this.FindInvoiceByDonation) },
                        });
                    return null;
                }

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
                            { "Function", nameof(this.FindInvoiceByDonation) },
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
                            { "Function", nameof(this.FindInvoiceByDonation) },
                        });
                    return null;
                }

                if (PaymentStatusMessages.FailedPaymentMessages.Any(p => p == donation.ConfirmedPayment.Status))
                {
                    this.TelemetryClient.TrackEvent(
                       "CreateInvoice-ConfirmedFailedPaymentStatus",
                       new Dictionary<string, string>()
                       {
                            { "DonationId", donationId.ToString() },
                            { "UserId", user.Id },
                            { "ConfirmedPaymentStatusId", donation.ConfirmedPayment.Id.ToString() },
                            { "Function", nameof(this.FindInvoiceByDonation) },
                       });
                    return null;
                }

                if (donation != null)
                {
                    using (var readTransaction = this.DbContext.Database.BeginTransaction(IsolationLevel.RepeatableRead))
                    {
                        result = this.DbContext.Invoices
                        .Include(p => p.Donation)
                        .Include(p => p.Donation.DonationItems)
                        .Where(p => p.Donation.Id == donation.Id)
                        .FirstOrDefault();
                        readTransaction.Commit();
                    }

                    using (var transaction = this.DbContext.Database.BeginTransaction(IsolationLevel.Serializable))
                    {
                        if (result == null)
                        {
                            DateTime portugalDateTimeNow = DateTime.Now.GetPortugalDateTime();

                            int sequence = this.GetNextSequence(portugalDateTimeNow);
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

                                this.DbContext.Invoices.Add(result);
                                this.DbContext.SaveChanges();

                                transaction.Commit();
                            }
                            else
                            {
                                transaction.Rollback();
                                this.TrackExceptionTelemetry($"FindInvoiceByDonation Invoice Sequence number was {sequence}", donationId, user.Id);
                            }
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

        /// <summary>
        /// Gets the next sequence id in the database to calculate the invoice number.
        /// </summary>
        /// <returns>Return the number of invoices for the current year + 1.</returns>
        /// <param name="portugalDateTimeNow">This is the local time for Portugal when generating the next sequence.</param>
        private int GetNextSequence(DateTime portugalDateTimeNow)
        {
            int result = 0;

            int currentYear = portugalDateTimeNow.Year;

            // Check for empty invoice table
            bool isEmpty = this.DbContext.Invoices.Count() < 1;
            if (!isEmpty)
            {
                result = this.DbContext.Invoices
                .Where(p => p.Created.Year == currentYear)
                .Max(p => p.Sequence);
            }

            result++;
            return result;
        }
    }
}
