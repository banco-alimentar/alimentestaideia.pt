// -----------------------------------------------------------------------
// <copyright file="Donation2.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Pages
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.Validation;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Extensions;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Features;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Model.Pages.Donation2;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Models;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Telemetry;
    using Easypay.Rest.Client.Model;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Primitives;
    using Microsoft.FeatureManagement;
    using static Easypay.Rest.Client.Model.SubscriptionPostRequest;

    /// <summary>
    /// Provides the new, isolated donation experience.
    /// </summary>
    public class Donation2Model : PageModel
    {
        private readonly IUnitOfWork context;
        private readonly IFeatureManager featureManager;
        private readonly NifApiValidator nifApiValidator;
        private readonly IStringLocalizer localizer;
        private readonly IStringLocalizer identitySharedLocalizer;

        /// <summary>
        /// Initializes a new instance of the <see cref="Donation2Model"/> class.
        /// </summary>
        /// <param name="context">Unit of work.</param>
        /// <param name="featureManager">Feature manager.</param>
        /// <param name="nifApiValidator">NIF validator.</param>
        /// <param name="stringLocalizerFactory">String localizer factory.</param>
        public Donation2Model(
            IUnitOfWork context,
            IFeatureManager featureManager,
            NifApiValidator nifApiValidator,
            IStringLocalizerFactory stringLocalizerFactory)
        {
            this.context = context;
            this.featureManager = featureManager;
            this.nifApiValidator = nifApiValidator;
            this.localizer = stringLocalizerFactory.Create("Pages.Donation", typeof(DonationModel).Assembly.GetName().Name);
            this.identitySharedLocalizer = stringLocalizerFactory.Create(typeof(IdentitySharedResources));
        }

        /// <summary>
        /// Gets or sets the submitted form fields.
        /// </summary>
        [BindProperty]
        public Donation2InputModel Input { get; set; } = new Donation2InputModel();

        /// <summary>
        /// Gets the products available in the active campaign.
        /// </summary>
        public IReadOnlyList<Donation2ProductViewModel> Products { get; private set; } = Array.Empty<Donation2ProductViewModel>();

        /// <summary>
        /// Gets the available destination food banks.
        /// </summary>
        public IReadOnlyList<SelectListItem> FoodBanks { get; private set; } = Array.Empty<SelectListItem>();

        /// <summary>
        /// Gets the available subscription frequencies.
        /// </summary>
        public IReadOnlyList<SelectListItem> SubscriptionFrequencies { get; private set; } = Array.Empty<SelectListItem>();

        /// <summary>
        /// Gets the country options.
        /// </summary>
        public IReadOnlyList<SelectListItem> Countries { get; private set; } = Array.Empty<SelectListItem>();

        /// <summary>
        /// Gets the active referral campaign.
        /// </summary>
        public AlimentaEstaIdeia.Model.Referral Referral { get; private set; }

        /// <summary>
        /// Gets the active campaign name.
        /// </summary>
        public string CampaignName { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the donor is authenticated.
        /// </summary>
        public bool IsAuthenticated => this.User.Identity?.IsAuthenticated == true;

        private WebUser CurrentUser { get; set; }

        private Donation CurrentDonation { get; set; }

        private Campaign CurrentCampaign { get; set; }

        /// <summary>
        /// Loads the Donation2 page.
        /// </summary>
        /// <returns>The page or maintenance redirect.</returns>
        public async Task<IActionResult> OnGetAsync()
        {
            if (await this.featureManager.IsEnabledAsync(nameof(MaintenanceFlags.EnableMaintenance)))
            {
                return this.RedirectToPage("/Maintenance");
            }

            this.LoadPageData();
            this.PopulateInitialInput();
            this.ApplyInputQuantitiesToProducts();
            this.BuildProducts();
            return this.Page();
        }

        /// <summary>
        /// Validates and creates or updates a donation.
        /// </summary>
        /// <returns>The payment page or the form with validation errors.</returns>
        public async Task<IActionResult> OnPostAsync()
        {
            if (await this.featureManager.IsEnabledAsync(nameof(MaintenanceFlags.EnableMaintenance)))
            {
                return this.RedirectToPage("/Maintenance");
            }

            this.LoadPageData();
            this.ValidateInput();
            this.BuildProducts();

            if (!this.ModelState.IsValid)
            {
                return this.Page();
            }

            string serializedItems = string.Join(
                string.Empty,
                this.Input.Items
                    .Where(item => item.Quantity > 0)
                    .Select(item => string.Create(CultureInfo.InvariantCulture, $"{item.ProductId}:{item.Quantity};")));
            ICollection<DonationItem> donationItems = this.context.DonationItem.GetDonationItems(serializedItems);
            if (donationItems.Count == 0)
            {
                this.ModelState.AddModelError("Input.Items", ValidationMessages.AmountInvalid);
                return this.Page();
            }

            WebUser donor = this.GetOrCreateDonor();
            this.SetCurrentUser(donor);
            double amount = donationItems.Sum(item => item.Quantity * item.Price);
            Guid publicId = this.GetOrCreateDonationPublicId();
            FoodBank foodBank = this.context.FoodBank.GetById(this.Input.FoodBankId.Value);
            Donation donation = this.CurrentDonation;

            if (donation == null)
            {
                donation = new Donation
                {
                    PublicId = publicId,
                    DonationItems = donationItems,
                };
                this.context.DonationItem.AttachItemsToDonation(donationItems, donation);
                this.context.Donation.Add(donation);
            }
            else
            {
                this.context.DonationItem.ReplaceDonationItems(donation, donationItems);
            }

            donation.DonationDate = DateTime.UtcNow;
            donation.DonationAmount = amount;
            donation.FoodBank = foodBank;
            donation.ReferralEntity = this.Referral;
            donation.WantsReceipt = this.Input.WantsReceipt;
            donation.User = donor;
            donation.PaymentStatus = PaymentStatus.WaitingPayment;
            donation.Nif = this.Input.Nif;
            donation.CampaignId = this.CurrentCampaign?.Id;

            this.UpdateDonor(donor);
            this.context.Complete();
            this.HttpContext.Session.SetDonation(donation);
            this.HttpContext.Session.SetFoodBank(foodBank);

            if (this.Input.IsSubscriptionEnabled)
            {
                return this.RedirectToPage(
                    "/SubscriptionPayment",
                    new { donation.PublicId, frequency = this.Input.SubscriptionFrequency });
            }

            return this.RedirectToPage("/Payment", new { donation.PublicId });
        }

        private static IReadOnlyList<SelectListItem> BuildCountries()
        {
            return CultureInfo.GetCultures(CultureTypes.SpecificCultures)
                .Select(culture =>
                {
                    try
                    {
                        return new RegionInfo(culture.Name).EnglishName;
                    }
                    catch (ArgumentException)
                    {
                        return null;
                    }
                })
                .Where(country => !string.IsNullOrWhiteSpace(country))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(country => country, StringComparer.CurrentCultureIgnoreCase)
                .Select(country => new SelectListItem(country, country))
                .ToList();
        }

        private void LoadPageData()
        {
            Claim userId = this.User.FindFirst(ClaimTypes.NameIdentifier);
            this.CurrentUser = this.context.User.FindUserById(userId?.Value);
            this.SetCurrentUser(this.CurrentUser);
            this.LoadDonationFromFlow();
            this.Referral = this.GetReferral();

            (IReadOnlyList<ProductCatalogue> ProductCatalogues, Campaign Campaign) catalogue =
                this.context.ProductCatalogue.GetCurrentProductCatalogue();
            this.CurrentCampaign = catalogue.Campaign;
            this.CampaignName = catalogue.Campaign?.Number;

            this.FoodBanks = this.context.FoodBank.GetAll()
                .Where(foodBank => foodBank.Id != 21 && foodBank.Id != 22 && foodBank.Id != 24)
                .OrderBy(foodBank => foodBank.Name)
                .ToList()
                .Select(foodBank => new SelectListItem(foodBank.Name, foodBank.Id.ToString(CultureInfo.InvariantCulture)))
                .Append(new SelectListItem("Angola", "24"))
                .Append(new SelectListItem("Cabo Verde", "22"))
                .Prepend(new SelectListItem(string.Empty, string.Empty))
                .ToList();

            this.SubscriptionFrequencies = Enum.GetNames(typeof(FrequencyEnum))
                .Select(item =>
                {
                    string value = item.TrimStart('_');
                    return new SelectListItem(
                        string.Concat(this.identitySharedLocalizer.GetString(item), " (", value, ")"),
                        value);
                })
                .ToList();
            this.Countries = BuildCountries();

            IReadOnlyDictionary<int, int> quantities = this.Input.Items
                .GroupBy(item => item.ProductId)
                .ToDictionary(group => group.Key, group => group.First().Quantity);
            this.Products = catalogue.ProductCatalogues
                .Select(product => new Donation2ProductViewModel
                {
                    Id = product.Id,
                    Name = product.Name,
                    IconUrl = product.IconUrl,
                    UnitOfMeasure = product.UnitOfMeasure,
                    QuantityPerUnit = product.Quantity ?? 0d,
                    Cost = product.Cost,
                    SelectedQuantity = quantities.TryGetValue(product.Id, out int quantity) ? quantity : 0,
                })
                .ToList();
        }

        private void PopulateInitialInput()
        {
            WebUser donor = this.CurrentUser
                ?? (this.CurrentDonation?.User?.IsAnonymous == true ? this.CurrentDonation.User : null);
            this.Input = new Donation2InputModel
            {
                Name = donor?.FullName,
                CompanyName = donor?.CompanyName,
                Email = donor?.Email,
                Country = donor?.Address?.Country ?? "Portugal",
                Address = donor?.Address?.Address1,
                PostalCode = donor?.Address?.PostalCode,
                Nif = donor?.Nif,
                WantsReceipt = this.CurrentUser != null || this.CurrentDonation?.WantsReceipt == true,
                AcceptsTerms = this.CurrentUser != null && this.context.Donation.HasCompletedDonation(this.CurrentUser.Id),
                FoodBankId = this.CurrentDonation?.FoodBank?.Id ?? this.HttpContext.Session.GetInt32("FoodBankIdContext"),
                Items = this.CurrentDonation?.DonationItems?
                    .Select(item => new Donation2ItemInputModel
                    {
                        ProductId = item.ProductCatalogue.Id,
                        Quantity = item.Quantity,
                    })
                    .ToList() ?? new List<Donation2ItemInputModel>(),
            };
        }

        private void BuildProducts()
        {
            this.Input.Items = this.Products
                .Select(product => new Donation2ItemInputModel
                {
                    ProductId = product.Id,
                    Quantity = product.SelectedQuantity,
                })
                .ToList();
        }

        private void ApplyInputQuantitiesToProducts()
        {
            IReadOnlyDictionary<int, int> quantities = this.Input.Items
                .GroupBy(item => item.ProductId)
                .ToDictionary(group => group.Key, group => group.First().Quantity);
            foreach (Donation2ProductViewModel product in this.Products)
            {
                product.SelectedQuantity = quantities.TryGetValue(product.Id, out int quantity) ? quantity : 0;
            }
        }

        private void ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(this.Input.Name))
            {
                this.ModelState.AddModelError("Input.Name", ValidationMessages.NameRequired);
            }

            if (string.IsNullOrWhiteSpace(this.Input.Email))
            {
                this.ModelState.AddModelError("Input.Email", ValidationMessages.EmailRequired);
            }

            if (string.IsNullOrWhiteSpace(this.Input.Country))
            {
                this.ModelState.AddModelError("Input.Country", ValidationMessages.CountryRequired);
            }

            if (this.Input.WantsReceipt)
            {
                if (string.IsNullOrWhiteSpace(this.Input.Address))
                {
                    this.ModelState.AddModelError("Input.Address", ValidationMessages.AddressRequired);
                }

                if (string.IsNullOrWhiteSpace(this.Input.PostalCode))
                {
                    this.ModelState.AddModelError("Input.PostalCode", ValidationMessages.PostalCodeRequired);
                }

                if (!this.nifApiValidator.IsValidNif(this.Input.Nif))
                {
                    this.ModelState.AddModelError("Input.Nif", ValidationMessages.NifInvalid);
                }
            }

            if (this.Input.IsSubscriptionEnabled && this.CurrentUser == null)
            {
                this.ModelState.AddModelError(
                    "Input.IsSubscriptionEnabled",
                    this.localizer.GetString("SubscriptionAuthentication"));
            }

            if (this.Input.IsSubscriptionEnabled
                && !this.SubscriptionFrequencies.Any(item => item.Value == this.Input.SubscriptionFrequency))
            {
                this.ModelState.AddModelError(
                    "Input.SubscriptionFrequency",
                    this.localizer.GetString("DonationSubscriptionEnabled"));
            }

            HashSet<int> currentProductIds = this.Products.Select(product => product.Id).ToHashSet();
            bool invalidItems = this.Input.Items == null
                || this.Input.Items.Count == 0
                || this.Input.Items.GroupBy(item => item.ProductId).Any(group => group.Count() > 1)
                || this.Input.Items.Any(item => !currentProductIds.Contains(item.ProductId) || item.Quantity < 0 || item.Quantity > 9999)
                || this.Input.Items.All(item => item.Quantity == 0);
            if (invalidItems)
            {
                this.ModelState.AddModelError("Input.Items", ValidationMessages.AmountInvalid);
            }

            if (!this.Input.FoodBankId.HasValue || this.context.FoodBank.GetById(this.Input.FoodBankId.Value) == null)
            {
                this.ModelState.AddModelError("Input.FoodBankId", ValidationMessages.FoodBankIdRequired);
            }
        }

        private WebUser GetOrCreateDonor()
        {
            if (this.CurrentUser != null)
            {
                return this.CurrentUser;
            }

            DonorAddress address = this.Input.WantsReceipt
                ? new DonorAddress
                {
                    Address1 = this.Input.Address,
                    PostalCode = this.Input.PostalCode,
                    Country = this.Input.Country,
                }
                : null;
            return this.context.User.FindOrCreateWebUser(
                this.Input.Email,
                this.Input.CompanyName,
                this.Input.Nif,
                this.Input.Name,
                address);
        }

        private void UpdateDonor(WebUser donor)
        {
            donor.FullName = this.Input.Name;
            donor.CompanyName = this.Input.CompanyName;
            if (this.CurrentUser?.Id == donor.Id || donor.IsAnonymous)
            {
                donor.Nif = this.Input.Nif;
            }
            if (!this.Input.WantsReceipt)
            {
                return;
            }

            donor.Address ??= new DonorAddress();
            donor.Address.Address1 = this.Input.Address;
            donor.Address.PostalCode = this.Input.PostalCode;
            donor.Address.Country = this.Input.Country;
        }

        private Guid GetOrCreateDonationPublicId()
        {
            if (this.HttpContext.Items.TryGetValue(KeyNames.DonationSessionKey, out object value) && value is Guid id)
            {
                this.HttpContext.Session.SetString(KeyNames.DonationSessionKey, id.ToString());
                return id;
            }

            Guid publicId = Guid.NewGuid();
            this.HttpContext.Items[KeyNames.DonationSessionKey] = publicId;
            this.HttpContext.Session.SetString(KeyNames.DonationSessionKey, publicId.ToString());
            return publicId;
        }

        private void LoadDonationFromFlow()
        {
            string publicIdValue = this.HttpContext.Session.GetString(KeyNames.DonationSessionKey);
            if (!Guid.TryParse(publicIdValue, out Guid publicId))
            {
                return;
            }

            int donationId = this.context.Donation.GetDonationIdFromPublicId(publicId);
            if (donationId <= 0)
            {
                return;
            }

            this.CurrentDonation = this.context.Donation.GetFullDonationById(donationId);
            if (this.CurrentDonation == null)
            {
                return;
            }

            List<BasePayment> payments = this.context.Donation.GetPaymentsForDonation(donationId);
            if (payments.Count == 0)
            {
                return;
            }

            if (this.context.Donation.TryCompleteDonationFromExistingPayment(this.CurrentDonation, payments))
            {
                this.CurrentDonation = this.context.Donation.GetFullDonationById(donationId);
                return;
            }

            Donation clonedDonation = this.context.Donation.CloneDonation(this.CurrentDonation);
            clonedDonation.PaymentStatus = PaymentStatus.NotPayed;
            clonedDonation.PublicId = Guid.NewGuid();
            this.context.Donation.Add(clonedDonation);
            this.context.Complete();
            this.CurrentDonation = clonedDonation;
            this.HttpContext.Session.SetString(KeyNames.DonationSessionKey, clonedDonation.PublicId.ToString());
        }

        private AlimentaEstaIdeia.Model.Referral GetReferral()
        {
            string referralCode = null;
            if (this.Request.Query.TryGetValue("referral", out StringValues queryValue))
            {
                referralCode = queryValue.ToString();
            }
            else if (this.HttpContext.Session.TryGetValue("Referral", out byte[] sessionValue) && sessionValue != null)
            {
                referralCode = System.Text.Encoding.UTF8.GetString(sessionValue);
            }

            if (string.IsNullOrWhiteSpace(referralCode))
            {
                return null;
            }

            AlimentaEstaIdeia.Model.Referral referral = this.context.ReferralRepository.GetActiveCampaignsByCode(referralCode);
            if (referral != null && string.IsNullOrEmpty(referral.Name))
            {
                referral.Name = referralCode;
            }

            return referral;
        }

        private void SetCurrentUser(WebUser user)
        {
            if (user != null)
            {
                this.HttpContext.Items[KeyNames.CurrentUserKey] = user;
            }
        }
    }
}