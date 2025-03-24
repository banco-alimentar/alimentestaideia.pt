// -----------------------------------------------------------------------
// <copyright file="Donation.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Pages.Tenants.BancoAlimentar.Pages
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using Easypay.Rest.Client.Model;
    using global::BancoAlimentar.AlimentaEstaIdeia.Model;
    using global::BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using global::BancoAlimentar.AlimentaEstaIdeia.Repository;
    using global::BancoAlimentar.AlimentaEstaIdeia.Repository.Validation;
    using global::BancoAlimentar.AlimentaEstaIdeia.Repository.ViewModel;
    using global::BancoAlimentar.AlimentaEstaIdeia.Web.Extensions;
    using global::BancoAlimentar.AlimentaEstaIdeia.Web.Features;
    using global::BancoAlimentar.AlimentaEstaIdeia.Web.Model.Pages.Shared;
    using global::BancoAlimentar.AlimentaEstaIdeia.Web.Models;
    using global::BancoAlimentar.AlimentaEstaIdeia.Web.Telemetry;
    using global::BancoAlimentar.AlimentaEstaIdeia.Web.Validation;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Primitives;
    using Microsoft.FeatureManagement;
    using static Easypay.Rest.Client.Model.SubscriptionPostRequest;

    /// <summary>
    /// Represent the donation page model.
    /// </summary>
    public class DonationModel : PageModel
    {
        /// <summary>
        /// Gets the name of the key used to store the anonymous data in the donation flow.
        /// </summary>
        public const string SaveAnonymousUserDataFlowKey = "SaveAnonymousUserDataFlowKey";

        private readonly IUnitOfWork context;
        private readonly SignInManager<WebUser> signInManager;
        private readonly UserManager<WebUser> userManager;
        private readonly IFeatureManager featureManager;
        private readonly NifApiValidator nifApiValidator;
        private readonly IStringLocalizer localizer;
        private readonly IStringLocalizer identitySharedLocalizer;
        private bool isPostRequest;

        /// <summary>
        /// Initializes a new instance of the <see cref="DonationModel"/> class.
        /// </summary>
        /// <param name="context">Unit of work.</param>
        /// <param name="signInManager">Sign in manager.</param>
        /// <param name="userManager">User manager.</param>
        /// <param name="stringLocalizerFactory">String localizer to get localized resources.</param>
        /// <param name="featureManager">Flag feature manager.</param>
        /// <param name="nifApiValidator">Nif Api validation.</param>
        public DonationModel(
            IUnitOfWork context,
            SignInManager<WebUser> signInManager,
            UserManager<WebUser> userManager,
            IStringLocalizerFactory stringLocalizerFactory,
            IFeatureManager featureManager,
            NifApiValidator nifApiValidator)
        {
            this.context = context;
            this.signInManager = signInManager;
            this.userManager = userManager;
            this.featureManager = featureManager;
            this.nifApiValidator = nifApiValidator;
            this.localizer = stringLocalizerFactory.Create("Pages.Donation", typeof(DonationModel).Assembly.GetName().Name);
            this.identitySharedLocalizer = stringLocalizerFactory.Create(typeof(IdentitySharedResources));
        }

        /// <summary>
        /// Gets or sets the name of the user.
        /// </summary>
        [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "NameRequired")]
        [StringLength(256, ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "NameStringLength")]
        [DisplayAttribute(Name = "Nome")]
        [BindProperty]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the address of the user.
        /// </summary>
        [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "AddressRequired")]
        [StringLength(256, ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "AddressStringLength")]
        [DisplayAttribute(Name = "Morada")]
        [BindProperty]
        public string Address { get; set; }

        /// <summary>
        /// Gets or sets the country of the user.
        /// </summary>
        [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "CountryRequired")]
        [StringLength(256, ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "CountryStringLength")]
        [DisplayAttribute(Name = "País")]
        [BindProperty]
        public string Country { get; set; }

        /// <summary>
        /// Gets or sets the Nif of the user.
        /// </summary>
        [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "NifRequired")]
        [StringLength(256, ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "NifStringLength")]
        [RegularExpression("^[0-9 ]*$", ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "NifInvalid")]
        [Nif(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "NifInvalid")]
        [DisplayAttribute(Name = "NIF")]
        [BindProperty]
        public string Nif { get; set; }

        /// <summary>
        /// Gets or sets the email of the user.
        /// </summary>
        [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "EmailRequired")]
        [StringLength(256, ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "EmailStringLength")]
        [RegularExpression(@"\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*", ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "EmailInvalid")]
        [DisplayAttribute(Name = "Email")]
        [BindProperty]
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets the postal code (ZIP) of the user.
        /// </summary>
        [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "PostalCodeRequired")]
        [StringLength(20, ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "PostalCodeStringLength")]
        [DisplayAttribute(Name = "C. Postal")]
        [BindProperty]
        public string PostalCode { get; set; }

        /// <summary>
        /// Gets or sets or set the id of the food bank used in the donation.
        /// </summary>
        [DisplayAttribute(Name = "Escolha o Banco Alimentar para o qual quer doar")]
        [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "FoodBankIdRequired")]
        [BindProperty]
        public int FoodBankId { get; set; }

        /// <summary>
        /// Gets or sets the donation amount for the BancoAlimentar Tenant - DonationModel .
        /// </summary>
        [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "AmountInvalidCash")]
        [MinimumValue(0.5d, ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "MinAmount")]
        [Range(0.5d, 5000d, ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "AmountInvalidCash")]
        [DisplayAttribute(Name = "Valor a doar")]
        [BindProperty]
        public double Amount { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the user wants an invoice as part of the donation.
        /// </summary>
        [BindProperty]
        public bool WantsReceipt { get; set; }

        /// <summary>
        /// Gets or sets the company name.
        /// </summary>
        [StringLength(256, ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "NameStringLength")]
        [DisplayAttribute(Name = "Empresa")]
        [BindProperty]
        public string CompanyName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the user accepts the terms or not.
        /// </summary>
        [MustBeChecked(ErrorMessage = "Deve aceitar a Política de Privacidade")]
        [BindProperty]
        public bool AcceptsTerms { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether if the user want a subscription for the current donation.
        /// </summary>
        [BindProperty]
        public bool IsSubscriptionEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether if the user is a company.
        /// </summary>
        public bool IsCompany { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether if the user is a private citizen.
        /// </summary>
        public bool IsPrivate { get; set; }

        /// <summary>
        /// Gets or sets the total donations.
        /// </summary>
        public List<TotalDonationsResult> TotalDonations { get; set; }

        /// <summary>
        /// Gets or sets the list of product catalogue items.
        /// </summary>
        public IReadOnlyList<ProductCatalogue> ProductCatalogue { get; set; }

        /// <summary>
        /// Gets or sets the list of food banks available to donate.
        /// </summary>
        [BindProperty]
        public List<SelectListItem> FoodBankList { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="LoginSharedModel"/>.
        /// </summary>
        public LoginSharedModel LoginSharedModel { get; set; }

        /// <summary>
        /// Gets or sets the current user.
        /// </summary>
        public WebUser CurrentUser { get; set; }

        /// <summary>
        /// Gets or sets what is the current donation in the flow.
        /// </summary>
        [BindProperty]
        public Donation CurrentDonationFlow { get; set; }

        /// <summary>
        /// Gets or sets a value indicating what is the selected subscription frequency.
        /// </summary>
        [BindProperty]
        public string SubscriptionFrequencySelected { get; set; }

        /// <summary>
        /// Gets or sets a value indicating what is the subscription Frequency.
        /// </summary>
        [BindProperty]
        public List<SelectListItem> SubscriptionFrequency { get; set; }

        /// <summary>
        /// Execute the get operation.
        /// </summary>
        /// <returns>A task.</returns>
        public async Task<IActionResult> OnGetAsync()
        {
            bool isMaintenanceEanbled = await featureManager.IsEnabledAsync(nameof(MaintenanceFlags.EnableMaintenance));
            if (isMaintenanceEanbled)
            {
                return RedirectToPage("/Maintenance");
            }
            else
            {
                await Load();
                if (this.HttpContext.Session.Keys.Contains("FoodBankIdContext"))
                {
                    FoodBankId = (int)this.HttpContext.Session.GetInt32("FoodBankIdContext");
                }

                Amount = 10;
                return Page();
            }
        }

        /// <summary>
        /// Loads the data from the donation flow id.
        /// This can happen when the user, clicks on donate, but go back to the donation page again.
        /// This will prevent creating a new donation object in the database.
        /// </summary>
        public void LoadDonationFromFlow()
        {
            string donationPublicId = this.HttpContext.Session.GetString(KeyNames.DonationSessionKey);
            if (Guid.TryParse(donationPublicId, out Guid donationId))
            {
                int id = this.context.Donation.GetDonationIdFromPublicId(donationId);
                if (id > 0)
                {
                    CurrentDonationFlow = this.context.Donation.GetFullDonationById(id);
                    if (CurrentDonationFlow != null)
                    {
                        if (!isPostRequest)
                        {
                            LoadUserInformation(CurrentDonationFlow.User);
                            LoadUserAddressInformation(CurrentDonationFlow.User.Address);

                            this.WantsReceipt = CurrentDonationFlow.WantsReceipt ?? false;
                        }
                    }
                }
                else
                {
                    CurrentDonationFlow = null;
                }
            }
            else
            {
                CurrentDonationFlow = null;
            }
        }

        /// <summary>
        /// Execute the post operation.
        /// </summary>
        /// <returns>A task.</returns>
        public async Task<ActionResult> OnPost()
        {
            isPostRequest = true;
            await Load(true);

            CurrentUser = await userManager.GetUserAsync(new ClaimsPrincipal(User.Identity));
            if (CurrentUser != null)
            {
                this.ModelState.Remove("Name");
                this.ModelState.Remove("Nif");
                this.ModelState.Remove("Country");
                this.ModelState.Remove("Email");
                this.ModelState.Remove("Address");
                this.ModelState.Remove("PostalCode");
                if (CurrentUser.EmailConfirmed)
                {
                    WantsReceipt = true;
                }
            }

            if (IsSubscriptionEnabled)
            {
                if (CurrentUser == null)
                {
                    ModelState.AddModelError("SubscriptionEnabled", this.localizer.GetString("SubscriptionAuthentication"));
                }
            }
            else
            {
                if (!this.WantsReceipt)
                {
                    this.ModelState.Remove("Nif");
                    this.ModelState.Remove("Address");
                    this.ModelState.Remove("PostalCode");
                }
                else
                {
                    bool isValidNif = this.nifApiValidator.IsValidNif(Nif);
                    if (!isValidNif)
                    {
                        this.ModelState.AddModelError("Nif", "Nif não é valido");
                    }
                }
            }

            if (ModelState.IsValid)
            {
                Guid donationId = Guid.NewGuid();
                if (this.HttpContext.Items.ContainsKey(KeyNames.DonationSessionKey))
                {
                    donationId = (Guid)this.HttpContext.Items[KeyNames.DonationSessionKey];
                }
                else
                {
                    this.HttpContext.Items.Add(KeyNames.DonationSessionKey, donationId);
                }

                this.HttpContext.Session.SetString(KeyNames.DonationSessionKey, donationId.ToString());

                if (CurrentUser == null)
                {
                    DonorAddress address = null;
                    if (WantsReceipt)
                    {
                        address = new DonorAddress()
                        {
                            Address1 = Address,
                            PostalCode = PostalCode,
                            Country = Country,
                        };
                    }

                    CurrentUser = this.context.User.FindOrCreateWebUser(
                        Email,
                        CompanyName,
                        Nif,
                        Name,
                        address);
                }

                SetCurrentUser();
                Donation donation = null;
                var referral = GetReferral();
                if (CurrentDonationFlow == null)
                {
                    donation = new Donation()
                    {
                        PublicId = donationId,
                        DonationDate = DateTime.UtcNow,
                        DonationAmount = Amount,
                        FoodBank = this.context.FoodBank.GetById(FoodBankId),
                        ReferralEntity = referral,
                        DonationItems = this.context.DonationItem.GetCashDonationItem(Amount),
                        WantsReceipt = WantsReceipt,
                        User = CurrentUser,
                        PaymentStatus = PaymentStatus.WaitingPayment,
                        Nif = Nif,
                        IsCashDonation = true,
                    };

                    this.context.Donation.Add(donation);
                }
                else
                {
                    donation = CurrentDonationFlow;
                    if (donation.DonationItems != null)
                    {
                        this.context.DonationItem.RemoveRange(donation.DonationItems);
                        donation.DonationItems.Clear();
                    }

                    donation.DonationDate = DateTime.UtcNow;
                    donation.DonationAmount = Amount;
                    donation.FoodBank = this.context.FoodBank.GetById(FoodBankId);
                    donation.ReferralEntity = referral;
                    donation.DonationItems = this.context.DonationItem.GetCashDonationItem(Amount);
                    donation.WantsReceipt = WantsReceipt;
                    donation.User = CurrentUser;
                    donation.Nif = Nif;
                    donation.PaymentStatus = PaymentStatus.WaitingPayment;
                }

                this.UpdateUserInformation();
                this.context.Complete();
                this.HttpContext.Session.SetDonation(donation);
                this.HttpContext.Session.SetFoodBank(donation.FoodBank);
                if (IsSubscriptionEnabled)
                {
                    return this.RedirectToPage("/SubscriptionPayment", new { donation.PublicId, frequency = SubscriptionFrequencySelected });
                }
                else
                {
                    return this.RedirectToPage("/Payment", new { donation.PublicId });
                }
            }
            else
            {
                CurrentDonationFlow = new Donation();
                CurrentDonationFlow.FoodBank = this.context.FoodBank.GetById(FoodBankId);
                CurrentDonationFlow.DonationItems = this.context.DonationItem.GetCashDonationItem(Amount);
                return Page();
            }
        }

        /// <summary>
        /// Loads the user data from the <see cref="WebUser"/> objects.
        /// </summary>
        /// <param name="user">The user to load the data from.</param>
        private void LoadUserInformation(WebUser user)
        {
            if (user != null)
            {
                this.Name = user.FullName;
                this.Nif = user.Nif;
                this.Email = user.Email;
                this.CompanyName = user.CompanyName;
            }
        }

        /// <summary>
        /// Loads the user data from the <see cref="DonorAddress"/> objects.
        /// </summary>
        /// <param name="address">The address to load the data from.</param>
        private void LoadUserAddressInformation(DonorAddress address)
        {
            if (address != null)
            {
                this.Address = address.Address1;
                this.PostalCode = address.PostalCode;
                this.Country = address.Country;
            }
        }

        private void UpdateUserInformation()
        {
            if (CurrentUser != null)
            {
                CurrentUser.FullName = Name;
                CurrentUser.CompanyName = CompanyName;
                if (WantsReceipt && CurrentUser.Address != null)
                {
                    CurrentUser.Address.Country = Country;
                    CurrentUser.Address.Address1 = Address;
                    CurrentUser.Address.PostalCode = PostalCode;
                    CurrentUser.Address.Country = Country;
                }
            }
        }

        private void SetCurrentUser()
        {
            if (CurrentUser != null)
            {
                if (this.HttpContext.Items.ContainsKey(KeyNames.CurrentUserKey))
                {
                    this.HttpContext.Items[KeyNames.CurrentUserKey] = CurrentUser;
                }
                else
                {
                    this.HttpContext.Items.Add(KeyNames.CurrentUserKey, CurrentUser);
                }
            }
        }

        private AlimentaEstaIdeia.Model.Referral GetReferral()
        {
            StringValues queryValue;
            string result = null;
            byte[] session_referral;
            if (this.Request.Query.TryGetValue("referral", out queryValue))
            {
                result = queryValue.ToString();
            }
            else
            {
                this.HttpContext.Session.TryGetValue("Referral", out session_referral);

                if (session_referral != null)
                {
                    result = System.Text.Encoding.UTF8.GetString(session_referral.ToArray());
                }
            }

            AlimentaEstaIdeia.Model.Referral referral = null;

            if (!string.IsNullOrWhiteSpace(result))
            {
                referral = this.context.ReferralRepository.GetActiveCampaignsByCode(result);
            }

            if (referral != null && string.IsNullOrEmpty(referral.Name))
            {
                referral.Name = result;
            }

            return referral;
        }

        private async Task Load(bool isPost = false)
        {
            Claim id = User.FindFirst(ClaimTypes.NameIdentifier);
            CurrentUser = this.context.User.FindUserById(id?.Value);
            SetCurrentUser();
            LoadDonationFromFlow();
            if (!isPost)
            {
                LoadUserInformation(CurrentUser);
                LoadUserAddressInformation(CurrentUser?.Address);
            }

            if (CurrentUser != null)
            {
                WantsReceipt = true;
            }

            ProductCatalogue = this.context.ProductCatalogue.GetCurrentProductCatalogue().ProductCatalogues;
            TotalDonations = this.context.Donation.GetTotalDonations(ProductCatalogue);
            var foodBanks = this.context.FoodBank.GetAll().OrderBy(x => x.Name).ToList();
            FoodBankList = new List<SelectListItem>();
            int count = 0;
            int index = -1;
            foreach (var item in foodBanks)
            {
                bool selected = false;
                if (this.CurrentDonationFlow != null &&
                    this.CurrentDonationFlow.FoodBank != null &&
                    this.CurrentDonationFlow.FoodBank.Id == item.Id)
                {
                    selected = true;
                    index = count;
                }

                if (foodBanks.Count == 1)
                {
                    selected = true;
                }

                FoodBankList.Add(new SelectListItem(item.Name, item.Id.ToString(), selected));
                count++;
            }

            if (foodBanks.Count != 1)
            {
                if (index != -1)
                {
                    // Fix ArgumentOutOfRangeException on index.
                    if (index >= FoodBankList.Count)
                    {
                        index--;
                    }

                    var targetFoodBank = FoodBankList.ElementAt(index);
                    FoodBankList.RemoveAt(index);
                    FoodBankList.Insert(0, targetFoodBank);
                }
                else
                {
                    FoodBankList.Insert(0, new SelectListItem(string.Empty, string.Empty));
                }
            }

            LoginSharedModel = new LoginSharedModel()
            {
                ExternalLogins = (await signInManager.GetExternalAuthenticationSchemesAsync()).ToList(),
                ReturnUrl = string.IsNullOrEmpty(this.Request.Path) ? "~/" : $"~{this.Request.Path.Value + this.Request.QueryString}",
                IsUserLogged = User.Identity.IsAuthenticated,
            };

            SubscriptionFrequency = new List<SelectListItem>();
            foreach (var item in Enum.GetNames(typeof(FrequencyEnum)))
            {
                string value = item.TrimStart('_');
                SubscriptionFrequency.Add(
                    new SelectListItem(
                        string.Concat(this.identitySharedLocalizer.GetString(item), " (", value, ")"), value));
            }
        }
    }
}
