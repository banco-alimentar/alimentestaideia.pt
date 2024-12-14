// -----------------------------------------------------------------------
// <copyright file="Multibanco.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Pages.Payments
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Core;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Extensions;
    using Microsoft.ApplicationInsights;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Represent the multibanco page model.
    /// </summary>
    public class MultibancoModel : PageModel
    {
        private readonly IUnitOfWork context;
        private readonly IConfiguration configuration;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly TelemetryClient telemetryClient;
        private readonly IMail mail;

        /// <summary>
        /// Initializes a new instance of the <see cref="MultibancoModel"/> class.
        /// </summary>
        /// <param name="context">Unit of work.</param>
        /// <param name="configuration">Configuration.</param>
        /// <param name="webHostEnvironment">Web hosting environment.</param>
        /// <param name="telemetryClient">Telemetry client.</param>
        /// <param name="mail">Mail service.</param>
        public MultibancoModel(
            IUnitOfWork context,
            IConfiguration configuration,
            IWebHostEnvironment webHostEnvironment,
            TelemetryClient telemetryClient,
            IMail mail)
        {
            this.context = context;
            this.configuration = configuration;
            this.webHostEnvironment = webHostEnvironment;
            this.telemetryClient = telemetryClient;
            this.mail = mail;
        }

        /// <summary>
        /// Gets or sets the donation.
        /// </summary>
        public Donation Donation { get; set; }

        /// <summary>
        /// Execute the get operation.
        /// </summary>
        /// <param name="publicId">Public donation id.</param>
        public IActionResult OnGet(Guid publicId)
        {
            int donationId = 0;

            if (publicId != default(Guid))
            {
                donationId = this.context.Donation.GetDonationIdFromPublicId(publicId);
            }
            else
            {
                int? targetDonationId = HttpContext.Session.GetDonationId();
                if (targetDonationId.HasValue)
                {
                    donationId = targetDonationId.Value;
                }
            }

            bool backRequest = false;
            Donation = this.context.Donation.GetFullDonationById(donationId);
            if (Donation != null)
            {
                this.context.Donation.InvalidateTotalCache();
                if (this.configuration.IsSendingEmailEnabled())
                {
                    if (!backRequest)
                    {
                        if (Donation.User != null && !string.IsNullOrEmpty(Donation.User.Email))
                        {
                            this.mail.SendMultibancoReferenceMailToDonor(
                                this.configuration,
                                Donation,
                                Path.Combine(
                                    this.webHostEnvironment.WebRootPath,
                                    this.configuration.GetFilePath("Email.ReferenceToDonor.Body.Path")));
                        }
                        else
                        {
                            this.telemetryClient.TrackEvent("DonorEmailNotFound", new Dictionary<string, string>()
                        {
                            { "DonationId", donationId.ToString() },
                            { "UserId", Donation.User?.Id },
                        });
                        }
                    }
                }
            }
            else
            {
                this.telemetryClient.TrackEvent(
                    "DonationIdNotValid",
                    new Dictionary<string, string>()
                    {
                        { "DonationId", donationId.ToString() },
                    });
                ThanksModel.CompleteDonationFlow(HttpContext, this.context.User);
                return RedirectToPage("/Donation");
            }

            ThanksModel.CompleteDonationFlow(HttpContext, this.context.User);
            return Page();
        }
    }
}
