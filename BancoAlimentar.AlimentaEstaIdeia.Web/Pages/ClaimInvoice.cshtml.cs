// -----------------------------------------------------------------------
// <copyright file="Donation.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Pages
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Extensions;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Features;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Models;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Validation;
    using Microsoft.ApplicationInsights;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;
    using Microsoft.FeatureManagement;

    public class ClaimInvoice : PageModel
    {
        public const string DonationIdKey = "DonationIdKey";

        private readonly ILogger<IndexModel> logger;
        private readonly IUnitOfWork context;
        private readonly SignInManager<WebUser> signInManager;
        private readonly UserManager<WebUser> userManager;
        private readonly IFeatureManager featureManager;
        private readonly IStringLocalizer localizer;
        private readonly IMail mail;
        private readonly TelemetryClient telemetryClient;
        private bool isPostRequest;

        public ClaimInvoice(
            ILogger<IndexModel> logger,
            IUnitOfWork context,
            SignInManager<WebUser> signInManager,
            UserManager<WebUser> userManager,
            IStringLocalizerFactory stringLocalizerFactory,
            IFeatureManager featureManager,
            IMail mail,
            TelemetryClient telemetryClient)
        {
            this.logger = logger;
            this.context = context;
            this.signInManager = signInManager;
            this.userManager = userManager;
            this.featureManager = featureManager;
            this.localizer = stringLocalizerFactory.Create("Pages.ClaimInvoice", System.Reflection.Assembly.GetExecutingAssembly().GetName().Name);
            this.mail = mail;
            this.telemetryClient = telemetryClient;
        }


        [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "AddressRequired")]
        [StringLength(256, ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "AddressStringLength")]
        [DisplayAttribute(Name = "Morada")]
        [BindProperty]
        public string Address { get; set; }


        [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "NifRequired")]
        [StringLength(256, ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "NifStringLength")]
        [RegularExpression("^[0-9 ]*$", ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "NifInvalid")]
        [Nif(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "NifInvalid")]
        [DisplayAttribute(Name = "NIF")]
        [BindProperty]
        public string Nif { get; set; }


        [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "PostalCodeRequired")]
        [StringLength(256, ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "PostalCodeStringLength")]
        [DisplayAttribute(Name = "C. Postal")]
        [BindProperty]
        public string PostalCode { get; set; }

        [BindProperty]
        public string PublicId { get; set; }

        [BindProperty]
        public bool IsInvoiceSent { get; set; }

        [BindProperty]
        public bool IsWrongPublicId { get; set; }


        [MustBeChecked(ErrorMessage = "Deve aceitar a Política de Privacidade.")]
        [BindProperty]
        public bool AcceptsTerms { get; set; }


        [BindProperty]
        public Donation CurrentDonation { get; set; }


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
                return Page();
            }
        }

        public async Task<ActionResult> OnPost()
        {
            isPostRequest = true;

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var test = PublicId;

            if (Guid.TryParse(PublicId, out Guid donationId))
            {
                int id = this.context.Donation.GetDonationIdFromPublicId(donationId);
                if (id > 0)
                {
                    CurrentDonation = this.context.Donation.GetFullDonationById(id);

                    // Saving user details
                    CurrentDonation.User.Address = new DonorAddress()
                    {
                        Address1 = Address,
                        PostalCode = PostalCode,
                    };
                    CurrentDonation.User.Nif = Nif;
                    this.context.User.Modify(CurrentDonation.User);

                    this.context.Complete();

                    // Setting to true so the user will get receipt in email
                    CurrentDonation.WantsReceipt = true;

                    this.IsInvoiceSent = true;
                    await this.mail.SendInvoiceEmail(CurrentDonation);
                    this.telemetryClient.TrackEvent("ClaimInvoiceComplete");
                }
                else
                {
                    this.IsWrongPublicId = true;
                }
            }
            return Page();
        }
    }
}
