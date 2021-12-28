// -----------------------------------------------------------------------
// <copyright file="ClaimInvoice.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Pages
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.IO;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Extensions;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Features;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Models;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Validation;
    using Microsoft.ApplicationInsights;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.Extensions.Localization;
    using Microsoft.FeatureManagement;

    /// <summary>
    /// Claim invoice.
    /// </summary>
    public class ClaimInvoice : PageModel
    {
        private readonly IUnitOfWork context;
        private readonly IFeatureManager featureManager;
        private readonly IMail mail;
        private readonly TelemetryClient telemetryClient;
        private readonly IStringLocalizerFactory stringLocalizerFactory;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IStringLocalizer localizer;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClaimInvoice"/> class.
        /// </summary>
        /// <param name="context">Unit of context.</param>
        /// <param name="featureManager">Feature manager.</param>
        /// <param name="webHostEnvironment">Web hosting environment.</param>
        /// <param name="stringLocalizerFactory">Localizer factory.</param>
        /// <param name="mail">Mail service.</param>
        /// <param name="telemetryClient">Telemetry Client.</param>
        public ClaimInvoice(
            IUnitOfWork context,
            IFeatureManager featureManager,
            IWebHostEnvironment webHostEnvironment,
            IStringLocalizerFactory stringLocalizerFactory,
            IMail mail,
            TelemetryClient telemetryClient)
        {
            this.context = context;
            this.featureManager = featureManager;
            this.webHostEnvironment = webHostEnvironment;
            this.stringLocalizerFactory = stringLocalizerFactory;
            this.localizer = stringLocalizerFactory.Create("Pages.ClaimInvoice", System.Reflection.Assembly.GetExecutingAssembly().GetName().Name);
            this.mail = mail;
            this.telemetryClient = telemetryClient;
        }

        /// <summary>
        /// Gets or sets the address.
        /// </summary>
        [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "AddressRequired")]
        [StringLength(256, ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "AddressStringLength")]
        [DisplayAttribute(Name = "Morada")]
        [BindProperty]
        public string Address { get; set; }

        /// <summary>
        /// Gets or sets the nif.
        /// </summary>
        [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "NifRequired")]
        [StringLength(256, ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "NifStringLength")]
        [RegularExpression("^[0-9 ]*$", ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "NifInvalid")]
        [Nif(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "NifInvalid")]
        [DisplayAttribute(Name = "NIF")]
        [BindProperty]
        public string Nif { get; set; }

        /// <summary>
        /// Gets or sets the postal code (ZIP).
        /// </summary>
        [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "PostalCodeRequired")]
        [StringLength(256, ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "PostalCodeStringLength")]
        [DisplayAttribute(Name = "C. Postal")]
        [BindProperty]
        public string PostalCode { get; set; }

        /// <summary>
        /// Gets or sets the donation public id.
        /// </summary>
        [BindProperty]
        public string PublicId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the invoice was sent.
        /// </summary>
        [BindProperty]
        public bool IsInvoiceSent { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the invoice was already generated and it is not allowed to generate again.
        /// </summary>
        [BindProperty]
        public bool IsInvoiceAlreadyGenerated { get; set; }

        /// <summary>
        /// Gets or sets the message to tell the user when IsInvoiceAlreadyGenerated is true .
        /// </summary>
        [BindProperty]
        public string InvoiceAlreadyGeneratedMessage { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the public id not valid.
        /// </summary>
        [BindProperty]
        public bool IsWrongPublicId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the user accepts the terms.
        /// </summary>
        [MustBeChecked(ErrorMessage = "Deve aceitar a Política de Privacidade.")]
        [BindProperty]
        public bool AcceptsTerms { get; set; }

        /// <summary>
        /// Gets or sets the current donation.
        /// </summary>
        [BindProperty]
        public Donation CurrentDonation { get; set; }

        /// <summary>
        /// Execute the get operation.
        /// </summary>
        /// <param name="publicId">Donation public id.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<IActionResult> OnGetAsync(string publicId)
        {
            bool isMaintenanceEanbled = await featureManager.IsEnabledAsync(nameof(MaintenanceFlags.EnableMaintenance));
            if (isMaintenanceEanbled)
            {
                return RedirectToPage("/Maintenance");
            }
            else
            {
                this.PublicId = publicId;
                if (Guid.TryParse(this.PublicId, out Guid donationId))
                {
                    Invoice invoice = this.context.Invoice.FindInvoiceByPublicId(publicId, false);

                    IsInvoiceAlreadyGenerated = invoice != null ? true : false;

                    InvoiceAlreadyGeneratedMessage = GetInvoiceAlreadyGeneratedMessage(publicId);
                }

                return Page();
            }
        }

        /// <summary>
        /// Executed the post operation.
        /// </summary>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<ActionResult> OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (Guid.TryParse(PublicId, out Guid donationId))
            {
                int id = this.context.Donation.GetDonationIdFromPublicId(donationId);
                if (id > 0)
                {
                    CurrentDonation = this.context.Donation.GetFullDonationById(id);

                    // Saving user details
                    if (CurrentDonation.User.Address != null)
                    {
                        CurrentDonation.User.Address.Address1 = Address;
                        CurrentDonation.User.Address.PostalCode = PostalCode;
                    }
                    else
                    {
                        CurrentDonation.User.Address = new DonorAddress()
                        {
                            Address1 = Address,
                            PostalCode = PostalCode,
                        };
                    }

                    this.context.User.Modify(CurrentDonation.User);
                    CurrentDonation.WantsReceipt = true;
                    CurrentDonation.Nif = Nif;
                    this.context.Donation.Modify(CurrentDonation);

                    this.context.Complete();
                    this.IsInvoiceSent = true;
                    await this.mail.GenerateInvoiceAndSendByEmail(CurrentDonation, Request);
                    this.telemetryClient.TrackEvent("ClaimInvoiceComplete", new Dictionary<string, string> { { "PublicId", PublicId } });

                    InvoiceAlreadyGeneratedMessage = GetInvoiceAlreadyGeneratedMessage(PublicId);
                }
                else
                {
                    this.IsWrongPublicId = true;
                }
            }

            return Page();
        }

        private string GetInvoiceAlreadyGeneratedMessage(string publicId)
        {
            var invoiceURl = Path.Combine(
            this.webHostEnvironment.WebRootPath,
            string.Format("/Identity/Account/Manage/GenerateInvoice?publicDonationId={0}", publicId));

            return string.Format("{0} <a href=\"{1}\">{2}</a>.", localizer.GetString("ClaimInvoiceAlreadyComplete"), invoiceURl, localizer.GetString("Here"));
        }
    }
}
