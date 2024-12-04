// -----------------------------------------------------------------------
// <copyright file="InvoiceRepository.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Data;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Resources;
    using System.Runtime.CompilerServices;
    using BancoAlimentar.AlimentaEstaIdeia.Common.Repository.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.Validation;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using Easypay.Rest.Client.Model;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Caching.Memory;

    /// <summary>
    /// Default implementation for the <see cref="Invoice"/> repository pattern.
    /// </summary>
    public class InvoiceRepository : GenericRepository<Invoice, ApplicationDbContext>
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
        /// <param name="tenant">Current tenant.</param>
        /// <param name="invoiceStatusResult">Status of the operation.</param>
        /// <param name="generateInvoice">True to generate the invoice if not found.</param>
        /// <returns>A reference to the <see cref="Invoice"/>.</returns>
        public Invoice FindInvoiceByPublicId(string publicId, Tenant tenant, out InvoiceStatusResult invoiceStatusResult, bool generateInvoice = true)
        {
            Invoice result = null;

            Guid targetPublicId;
            if (Guid.TryParse(publicId, out targetPublicId))
            {
                Donation donation = this.DbContext.Donations
                    .Include(p => p.User)
                    .Where(p => p.PublicId == targetPublicId)
                    .FirstOrDefault();

                if (donation != null)
                {
                    result = this.GetOrCreateInvoiceByDonation(donation.Id, donation.User, tenant, out invoiceStatusResult, generateInvoice);
                    if (result.IsCanceled)
                    {
                        invoiceStatusResult = InvoiceStatusResult.InvoiceCanceled;
                    }

                    Dictionary<string, string> telemetryData = new Dictionary<string, string>
                        {
                            { "publicId", publicId },
                            { "donation.Id", donation.Id.ToString() },
                            { "InvoiceId", result?.Id.ToString() },
                            { "InvoiceStatusResult", invoiceStatusResult.ToString() },
                        };
                    this.TelemetryClient.TrackEvent("FindInvoiceByPublicId", telemetryData);

                    if (result.IsCanceled)
                    {
                        // we check again here to allow the telemetry to be sent.
                        result = null;
                    }
                }
                else
                {
                    this.TelemetryClient.TrackEvent(
                        "PublicDonationIdNotFound",
                        new Dictionary<string, string>()
                        {
                                { "PublicId", publicId },
                        });
                    invoiceStatusResult = InvoiceStatusResult.DonationNotFound;
                }
            }
            else
            {
                this.TelemetryClient.TrackException(new ArgumentException($"FindInvoiceByPublicId called with invalid Guid {publicId}"));
                invoiceStatusResult = InvoiceStatusResult.DonationNotFound;
            }

            return result;
        }

        /// <summary>
        /// Creates a new invoice for the specified donation and for the user, or returns the invoice if it already exists.
        /// </summary>
        /// <param name="donationId">Donation id.</param>
        /// <param name="user"><see cref="WebUser"/>.</param>
        /// <param name="tenant">Current tenant.</param>
        /// <param name="invoiceStatusResult">Status of the operation.</param>
        /// <param name="generateInvoice">True to generate the invoice if not found.</param>
        /// <returns>A reference for the <see cref="Invoice"/>.</returns>
        public Invoice GetOrCreateInvoiceByDonation(int donationId, WebUser user, Tenant tenant, out InvoiceStatusResult invoiceStatusResult, bool generateInvoice = true)
        {
            Invoice result = null;
            Donation donation = this.DbContext.Donations
                .Include(p => p.DonationItems)
                .Include(p => p.User)
                .Include(p => p.ConfirmedPayment)
                .Include(p => p.FoodBank)
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
                invoiceStatusResult = InvoiceStatusResult.DonationNotFound;
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
                    invoiceStatusResult = InvoiceStatusResult.NifNotValid;
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
                    invoiceStatusResult = InvoiceStatusResult.DonationUserNotFound;
                    return null;
                }

                if (donation != null && donation.PaymentStatus != PaymentStatus.Payed)
                {
                    this.TelemetryClient.TrackEvent(
                        "CreateInvoice-InvoiceWithPaymentStatusNotPayed",
                        new Dictionary<string, string>()
                        {
                            { "DonationId", donationId.ToString() },
                            { "UserId", user.Id },
                            { "Function", nameof(this.GetOrCreateInvoiceByDonation) },
                        });
                    invoiceStatusResult = InvoiceStatusResult.NotPayed;
                    return null;
                }

                this.FixConfirmedPayment(donation);
                if (donation != null && donation.ConfirmedPayment == null)
                {
                    // this is a request for invoice was made before we started to store the payment information
                    // from easypy, so we can't create the invoice.
                    this.TelemetryClient.TrackEvent(
                        "CreateInvoice-ConfirmedPaymentNull",
                        new Dictionary<string, string>()
                        {
                            { "DonationId", donationId.ToString() },
                            { "UserId", user.Id },
                            { "Function", nameof(this.GetOrCreateInvoiceByDonation) },
                        });
                    invoiceStatusResult = InvoiceStatusResult.ConfirmedPaymentIsNull;
                    return null;
                }

                DateTime donationPaidDate = donation.ConfirmedPayment.Created;

                // Check if we are on another year then when donation was made, and if so, we can only create invoice if < Jan 15th.
                if (donation != null && DateTime.Now.Year > donationPaidDate.Year)
                {
                    if (!(DateTime.Now.Year == donationPaidDate.Year + 1 && DateTime.Now.Month == 1 && DateTime.Now.Day < 16))
                    {
                        this.TelemetryClient.TrackEvent(
                            "CreateInvoice-InvoiceRequestedTooLate",
                            new Dictionary<string, string>()
                            {
                                                    { "DonationId", donationId.ToString() },
                                                    { "UserId", user.Id },
                                                    { "Function", nameof(this.GetOrCreateInvoiceByDonation) },
                            });
                        invoiceStatusResult = InvoiceStatusResult.DonationIsOneYearOld;
                        return Invoice.DefaultInvalidInvoice;
                    }
                }

                if ((donation != null && donation.ConfirmedPayment == null) || (donation != null && PaymentStatusMessages.FailedPaymentMessages.Any(p => p == donation.ConfirmedPayment?.Status)))
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
                    invoiceStatusResult = InvoiceStatusResult.ConfirmedFailedPaymentStatus;
                    return null;
                }

                var strategy = this.DbContext.Database.CreateExecutionStrategy();
                strategy.ExecuteInTransaction(
                    this.DbContext,
                    operation: (context) =>
                    {
                        if (donation != null)
                        {
                            if (tenant.InvoicingStrategy == Sas.Model.Strategy.InvoicingStrategy.SingleInvoiceTable)
                            {
                                result = this.DbContext.Invoices
                                    .Include(p => p.Donation)
                                    .Include(p => p.Donation.DonationItems)
                                    .Where(p => p.Donation.Id == donation.Id)
                                    .FirstOrDefault();
                            }
                            else if (tenant.InvoicingStrategy == Sas.Model.Strategy.InvoicingStrategy.MultipleTablesPerFoodBank)
                            {
                                result = this.DbContext.Invoices
                                   .Include(p => p.Donation)
                                   .Include(p => p.Donation.DonationItems)
                                   .Where(p => p.Donation.Id == donation.Id && p.FoodBank.Id == donation.FoodBank.Id)
                                   .FirstOrDefault();
                            }
                        }

                        context.SaveChanges(acceptAllChangesOnSuccess: false);
                    },
                    verifySucceeded: (context) => { return true; });

                strategy.ExecuteInTransaction(
                    this.DbContext,
                    operation: (context) =>
                    {
                        if (donation != null && generateInvoice && result == null)
                        {
                            int sequence = GetNextSequence(donation.ConfirmedPayment.Created, context, tenant, donation.FoodBank.Id);
                            string invoiceFormat = this.GetInvoiceFormat();

                            if (sequence > 0)
                            {
                                result = new Invoice()
                                {
                                    Created = DateTime.Now,
                                    Donation = donation,
                                    User = user,
                                    BlobName = Guid.NewGuid(),
                                    Sequence = sequence,
                                    Number = string.Format(invoiceFormat, sequence.ToString("D4"), donation.ConfirmedPayment.Created.Year),
                                    FoodBank = donation.FoodBank,
                                    Year = donation.ConfirmedPayment.Created.Year,
                                };

                                this.DbContext.Invoices.Add(result);
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
                        if (donation != null)
                        {
                            if (tenant?.InvoicingStrategy == Sas.Model.Strategy.InvoicingStrategy.SingleInvoiceTable)
                            {
                                return this.DbContext.Invoices
                                    .Where(p =>
                                        p.Donation.Id == donationId &&
                                        p.User.Id == user.Id).FirstOrDefault() != null;
                            }
                            else if (tenant?.InvoicingStrategy == Sas.Model.Strategy.InvoicingStrategy.MultipleTablesPerFoodBank)
                            {
                                return this.DbContext.Invoices
                                    .Where(p =>
                                        p.Donation.Id == donationId &&
                                        p.FoodBank.Id == donation.FoodBank.Id &&
                                        p.User.Id == user.Id).FirstOrDefault() != null;
                            }
                            else
                            {
                                return false;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    });

                this.DbContext.ChangeTracker.AcceptAllChanges();
                this.DbContext.SaveChanges();
                if (result != null)
                {
                    result.User = this.DbContext.WebUser
                        .Include(p => p.Address)
                        .Where(p => p.Id == user.Id)
                        .FirstOrDefault();
                }

                if (result != null && result.User != null && result.User.Address == null)
                {
                    result.User.Address = new DonorAddress();
                }
            }

            invoiceStatusResult = InvoiceStatusResult.GeneratedOk;
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
        /// <param name="portugalDateTime">This is the local time for Portugal of when the payment for this donation was made,  generating the next sequence.</param>
        /// <param name="context">ApplicationDbContext for transaction support.</param>
        /// <param name="tenant">Current tenant.</param>
        /// <param name="foodBankId">Food bank id.</param>
        private static int GetNextSequence(DateTime portugalDateTime, ApplicationDbContext context, Tenant tenant, int foodBankId)
        {
            int result = 0;

            int invoiceYear = portugalDateTime.Year;

            if (tenant.InvoicingStrategy == Sas.Model.Strategy.InvoicingStrategy.SingleInvoiceTable)
            {
                // Check for empty invoice table
                bool isEmpty = context.Invoices.Count() < 1;
                if (!isEmpty)
                {
                    var count = context.Invoices.Where(p => p.Created.Year == invoiceYear).Count();
                    if (count > 0)
                    {
                        result = context.Invoices
                            .Where(p => p.Created.Year == invoiceYear)
                            .Max(p => p.Sequence);
                    }
                }
            }
            else if (tenant.InvoicingStrategy == Sas.Model.Strategy.InvoicingStrategy.MultipleTablesPerFoodBank)
            {
                // Check for empty invoice table
                bool isEmpty = context.Invoices.Where(p => p.FoodBank.Id == foodBankId).Count() < 1;
                if (!isEmpty)
                {
                    var count = context.Invoices.Where(p => p.FoodBank.Id == foodBankId && p.Created.Year == invoiceYear).Count();
                    if (count > 0)
                    {
                        result = context.Invoices
                            .Where(p => p.FoodBank.Id == foodBankId && p.Created.Year == invoiceYear)
                            .Max(p => p.Sequence);
                    }
                }
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
                if (stream != null)
                {
                    using (ResourceReader reader = new ResourceReader(stream))
                    {
                        using (ResourceSet resourceSet = new ResourceSet(reader))
                        {
                            result = resourceSet.GetString("Name");
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Try to fix the issue where we have a payed donation but no confirmed payment.
        /// </summary>
        /// <param name="value">A reference to the <see cref="Donation"/>.</param>
        private void FixConfirmedPayment(Donation value)
        {
            if (value.ConfirmedPayment == null && value.PaymentStatus == PaymentStatus.Payed)
            {
                List<BasePayment> payments = this.DbContext.Payments
                     .Where(p => p.Donation.Id == value.Id && (p.Status == "ok" || p.Status == "Success" || p.Status == "COMPLETED"))
                     .ToList();

                if (payments.Count == 1)
                {
                    value.ConfirmedPayment = payments.First();
                }

                this.DbContext.SaveChanges();
            }
        }
    }
}
