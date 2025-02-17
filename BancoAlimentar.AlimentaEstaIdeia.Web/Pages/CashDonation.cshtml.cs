// -----------------------------------------------------------------------
// <copyright file="CashDonation.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Pages
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.ViewModel;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Extensions;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Features;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Model.Pages.Shared;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Models;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Telemetry;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Validation;
    using Easypay.Rest.Client.Model;
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
    /// Represent a cash donation model.
    /// This page is a replacement for https://www.bancoalimentar.pt/faca-um-donativo/.
    /// </summary>
    public class CashDonationModel : PageModel
    {
        /// <summary>
        /// Gets the name of the donation id key used to save the donation id during the donation flow.
        /// </summary>
        public const string DonationIdKey = "DonationIdKey";

        /// <summary>
        /// Gets the name of the key used to store the anonymous data in the donation flow.
        /// </summary>
        public const string SaveAnonymousUserDataFlowKey = "SaveAnonymousUserDataFlowKey";

        private readonly IUnitOfWork context;
        private readonly SignInManager<WebUser> signInManager;
        private readonly UserManager<WebUser> userManager;
        private readonly IFeatureManager featureManager;
        private readonly IStringLocalizer localizer;
        private bool isPostRequest;

        /// <summary>
        /// Initializes a new instance of the <see cref="CashDonationModel"/> class.
        /// </summary>
        /// <param name="context">Unit of work.</param>
        /// <param name="signInManager">Sign in manager.</param>
        /// <param name="userManager">User manager.</param>
        /// <param name="stringLocalizerFactory">String localizer to get localized resources.</param>
        /// <param name="featureManager">Flag feature manager.</param>
        public CashDonationModel(
            IUnitOfWork context,
            SignInManager<WebUser> signInManager,
            UserManager<WebUser> userManager,
            IStringLocalizerFactory stringLocalizerFactory,
            IFeatureManager featureManager)
        {
            this.context = context;
            this.signInManager = signInManager;
            this.userManager = userManager;
            this.featureManager = featureManager;
            this.localizer = stringLocalizerFactory.Create("Pages.Donation", typeof(DonationModel).Assembly.GetName().Name);
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
        /// Gets or sets the city of the user.
        /// </summary>
        [StringLength(256, ErrorMessage = "O tamanho máximo para a localidade é {0} caracteres.")]
        [DisplayAttribute(Name = "Localidade")]
        [BindProperty]
        public string City { get; set; }

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
        [StringLength(256, ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "PostalCodeStringLength")]
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
        /// Gets or sets the donation amount.
        /// </summary>
        [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "AmountInvalid")]
        [MinimumValue(0.5, ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "MinAmount")]
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
        [MustBeChecked(ErrorMessage = "Deve aceitar a Política de Privacidade.")]
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

            if (!this.WantsReceipt)
            {
                this.ModelState.Remove("Nif");
                this.ModelState.Remove("Address");
                this.ModelState.Remove("PostalCode");
            }

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
                            City = City,
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
                        DonationItems = GetDonationItems(),
                        WantsReceipt = WantsReceipt,
                        User = CurrentUser,
                        PaymentStatus = PaymentStatus.WaitingPayment,
                        Nif = Nif,
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
                    donation.DonationItems = GetDonationItems();
                    donation.WantsReceipt = WantsReceipt;
                    donation.User = CurrentUser;
                    donation.Nif = Nif;
                    donation.PaymentStatus = PaymentStatus.WaitingPayment;
                    donation.IsCashDonation = true;
                }

                this.UpdateUserInformation();
                this.context.Complete();

                TempData["Donation"] = donation.Id;
                HttpContext.Session.SetInt32(DonationIdKey, donation.Id);

                if (IsSubscriptionEnabled)
                {
                    TempData["SubscriptionFrequencySelected"] = SubscriptionFrequencySelected;
                    return this.RedirectToPage("/SubscriptionPayment");
                }
                else
                {
                    return this.RedirectToPage("/Payment");
                }
            }
            else
            {
                CurrentDonationFlow = new Donation();
                CurrentDonationFlow.FoodBank = this.context.FoodBank.GetById(FoodBankId);
                CurrentDonationFlow.DonationItems = GetDonationItems();
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
                this.City = address.City;
                this.PostalCode = address.PostalCode;
                this.Country = address.Country;
            }
        }

        private void UpdateUserInformation()
        {
            CurrentUser.FullName = Name;
            CurrentUser.CompanyName = CompanyName;
            if (WantsReceipt)
            {
                CurrentUser.Address.Country = Country;
                CurrentUser.Address.Address1 = Address;
                CurrentUser.Address.City = City;
                CurrentUser.Address.PostalCode = PostalCode;
                CurrentUser.Address.Country = Country;
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
                if (session_referral != null || session_referral.Length > 0)
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

                FoodBankList.Add(new SelectListItem(item.Name, item.Id.ToString(), selected));
                count++;
            }

            if (index != -1)
            {
                var targetFoodBank = FoodBankList.ElementAt(index);
                FoodBankList.RemoveAt(index);
                FoodBankList.Insert(0, targetFoodBank);
            }
            else
            {
                FoodBankList.Insert(0, new SelectListItem(string.Empty, string.Empty));
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
                SubscriptionFrequency.Add(new SelectListItem(value, value));
            }
        }

        private List<DonationItem> GetDonationItems()
        {
            return new List<DonationItem>()
                {
                    new DonationItem()
                    {
                        Price = Amount,
                        Quantity = 1,
                        ProductCatalogue = this.context.ProductCatalogue.GetCashProductCatalogue(),
                    },
                };
        }
    }
}
