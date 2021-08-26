// -----------------------------------------------------------------------
// <copyright file="Multibanco.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Pages.Payments
{
    using System.Collections.Generic;
    using System.IO;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Extensions;
    using Microsoft.ApplicationInsights;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
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
        /// <param name="id">Donation id.</param>
        public void OnGet(int id)
        {
            bool backRequest = false;
            if (TempData["Donation"] != null)
            {
                id = (int)TempData["Donation"];
            }
            else
            {
                var targetDonationId = HttpContext.Session.GetInt32(DonationModel.DonationIdKey);
                if (targetDonationId.HasValue)
                {
                    id = targetDonationId.Value;
                    backRequest = true;
                }
            }

            Donation = this.context.Donation.GetFullDonationById(id);
            this.context.Donation.InvalidateTotalCache();
            if (this.configuration.IsSendingEmailEnabled())
            {
                if (!backRequest)
                {
                    if (Donation.User != null && !string.IsNullOrEmpty(Donation.User.Email))
                    {
                        this.mail.SendMultibancoReferenceMailToDonor(
                            this.configuration, Donation, Path.Combine(this.webHostEnvironment.WebRootPath, this.configuration.GetFilePath("Email.ReferenceToDonor.Body.Path")));
                    }
                    else
                    {
                        this.telemetryClient.TrackEvent("DonorEmailNotFound", new Dictionary<string, string>()
                        {
                            { "DonationId", id.ToString() },
                            { "UserId", Donation.User?.Id },
                        });
                    }
                }
            }

            ThanksModel.CompleteDonationFlow(HttpContext, this.context.User);
        }
    }
}
