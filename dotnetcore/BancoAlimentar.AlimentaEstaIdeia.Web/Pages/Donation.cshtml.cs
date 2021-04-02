namespace BancoAlimentar.AlimentaEstaIdeia.Web.Pages
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Globalization;
    using System.Linq;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.ViewModel;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Models;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Model.Pages.Shared;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Telemetry;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Validation;
    using DNTCaptcha.Core;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.Extensions.Primitives;

    public class DonationModel : PageModel
    {
        public const string DonationIdKey = "DonationIdKey";

        private readonly ILogger<IndexModel> logger;
        private readonly IUnitOfWork context;
        private readonly SignInManager<WebUser> signInManager;
        private readonly UserManager<WebUser> userManager;
        private readonly IDNTCaptchaValidatorService validatorService;
        private readonly IOptions<DNTCaptchaOptions> captchaOptions;
        private readonly IStringLocalizer localizer;

        public DonationModel(
            ILogger<IndexModel> logger,
            IUnitOfWork context,
            SignInManager<WebUser> signInManager,
            UserManager<WebUser> userManager,
            IDNTCaptchaValidatorService validatorService,
            IOptions<DNTCaptchaOptions> captchaOptions,
            IStringLocalizerFactory stringLocalizerFactory)
        {
            this.logger = logger;
            this.context = context;
            this.signInManager = signInManager;
            this.userManager = userManager;
            this.validatorService = validatorService;
            this.captchaOptions = captchaOptions;
            this.localizer = stringLocalizerFactory.Create("Pages.Donation", System.Reflection.Assembly.GetExecutingAssembly().GetName().Name);
        }

        [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "NameRequired")]
        [StringLength(256, ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "NameStringLength")]
        [DisplayAttribute(Name = "Nome")]
        [BindProperty]
        public string Name { get; set; }

        [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "AddressRequired")]
        [StringLength(256, ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "AddressStringLength")]
        [DisplayAttribute(Name = "Morada")]
        [BindProperty]
        public string Address { get; set; }

        [StringLength(256, ErrorMessage = "O tamanho máximo para a localidade é {0} caracteres.")]
        [DisplayAttribute(Name = "Localidade")]
        [BindProperty]
        public string City { get; set; }

        [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "CountryRequired")]
        [StringLength(256, ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "CountryStringLength")]
        [DisplayAttribute(Name = "País")]
        [BindProperty]
        public string Country { get; set; }

        [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "NifRequired")]
        [StringLength(256, ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "NifStringLength")]
        [RegularExpression("^[0-9 ]*$", ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "NifInvalid")]
        [Nif(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "NifInvalid")]
        [DisplayAttribute(Name = "NIF")]
        [BindProperty]
        public string Nif { get; set; }

        [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "EmailRequired")]
        [StringLength(256, ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "EmailStringLength")]
        [RegularExpression(@"\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*", ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "EmailInvalid")]
        [DisplayAttribute(Name = "Email")]
        [BindProperty]
        public string Email { get; set; }

        [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "PostalCodeRequired")]
        [StringLength(256, ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "PostalCodeStringLength")]
        [DisplayAttribute(Name = "C. Postal")]
        [BindProperty]
        public string PostalCode { get; set; }

        [DisplayAttribute(Name = "Foto")]
        [BindProperty]
        public IFormFile Picture { get; set; }

        public bool Private { get; set; }

        [DisplayAttribute(Name = "Escolha o Banco Alimentar para o qual quer doar")]
        [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "FoodBankIdRequired")]
        [BindProperty]
        public int FoodBankId { get; set; }

        [Required(ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "AmountInvalid")]
        [MinValue(0.5, ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "MinAmount")]
        //[Range(0.01111111111, 9999.99999999999999, ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "AmountInvalid")]
        [DisplayAttribute(Name = "Valor a doar")]
        [BindProperty]
        public double Amount { get; set; }

        [BindProperty]
        public string DonatedItems { get; set; }

        [BindProperty]
        public string Hidden { get; set; }

        [BindProperty]
        public bool WantsReceipt { get; set; }

        [StringLength(256, ErrorMessageResourceType = typeof(ValidationMessages), ErrorMessageResourceName = "NameStringLength")]
        [DisplayAttribute(Name = "Empresa")]
        [BindProperty]
        public string CompanyName { get; set; }

        [MustBeChecked(ErrorMessage = "Deve aceitar a Política de Privacidade.")]
        [BindProperty]
        public bool AcceptsTerms { get; set; }

        public bool IsCompany { get; set; }

        public bool IsPrivate { get; set; }

        public List<TotalDonationsResult> TotalDonations { get; set; }

        public IReadOnlyList<ProductCatalogue> ProductCatalogue { get; set; }

        [BindProperty]
        public List<SelectListItem> FoodBankList { get; set; }

        public LoginSharedModel LoginSharedModel { get; set; }

        public WebUser CurrentUser { get; set; }

        [BindProperty]
        public Donation CurrentDonationFlow { get; set; }

        private bool isPostRequest;

        public async Task OnGetAsync()
        {
            await Load();
        }

        public void LoadDonationFromFlow()
        {
            string donationPublicId = this.HttpContext.Session.GetString(DonationFlowTelemetryInitializer.DonationSessionKey);
            //donationPublicId = "498c13b1-5ccb-4973-9a2a-a33c18bb9c57";
            Guid donationId;
            if (Guid.TryParse(donationPublicId, out donationId))
            {
                int id = this.context.Donation.GetDonationIdFromPublicId(donationId);
                if (id > 0)
                {
                    CurrentDonationFlow = this.context.Donation.GetFullDonationById(id);
                    if (CurrentDonationFlow != null)
                    {
                        if (!isPostRequest)
                        {
                            this.Name = CurrentDonationFlow.User.FullName;
                            this.Address = CurrentDonationFlow.User.Address.Address1;
                            this.City = CurrentDonationFlow.User.Address.City;
                            this.PostalCode = CurrentDonationFlow.User.Address.PostalCode;
                            this.Country = CurrentDonationFlow.User.Address.Country;
                            this.Nif = CurrentDonationFlow.User.Nif;
                            this.Email = CurrentDonationFlow.User.Email;
                            this.CompanyName = CurrentDonationFlow.User.CompanyName;
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

        public async Task<ActionResult> OnPost()
        {
            isPostRequest = true;
            await Load();

            if (!validatorService.HasRequestValidCaptchaEntry(Language.English, DisplayMode.SumOfTwoNumbers))
            {
                this.ModelState.Clear();
                this.ModelState.AddModelError(captchaOptions.Value.CaptchaComponent.CaptchaInputName, this.localizer["Captcha.TextboxMessageError"].Value);
                return Page();
            }

            Guid donationId = Guid.NewGuid();

            if (this.HttpContext.Items.ContainsKey(DonationFlowTelemetryInitializer.DonationSessionKey))
            {
                donationId = (Guid)this.HttpContext.Items[DonationFlowTelemetryInitializer.DonationSessionKey];
            }
            else
            {
                this.HttpContext.Items.Add(DonationFlowTelemetryInitializer.DonationSessionKey, donationId);
            }

            this.HttpContext.Session.SetString(DonationFlowTelemetryInitializer.DonationSessionKey, donationId.ToString());

            CurrentUser = await userManager.GetUserAsync(new ClaimsPrincipal(User.Identity));
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
            else
            {
                this.ModelState.Remove("Name");
                this.ModelState.Remove("Nif");
                this.ModelState.Remove("Country");
                this.ModelState.Remove("Email");
                this.ModelState.Remove("Address");
                this.ModelState.Remove("PostalCode");
            }

            if (ModelState.IsValid)
            {
                SetCurrentUser();
                var donationItems = this.context.DonationItem.GetDonationItems(DonatedItems);
                double amount = 0d;
                foreach (var item in donationItems)
                {
                    amount += item.Quantity * item.Price;
                }

                Donation donation = null;

                if (CurrentDonationFlow == null)
                {
                    donation = new Donation()
                    {
                        PublicId = donationId,
                        DonationDate = DateTime.UtcNow,
                        DonationAmount = amount,
                        FoodBank = this.context.FoodBank.GetById(FoodBankId),
                        Referral = GetReferral(),
                        DonationItems = this.context.DonationItem.GetDonationItems(DonatedItems),
                        WantsReceipt = WantsReceipt,
                        User = CurrentUser,
                        PaymentStatus = PaymentStatus.WaitingPayment,
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
                    donation.DonationAmount = amount;
                    donation.FoodBank = this.context.FoodBank.GetById(FoodBankId);
                    donation.Referral = GetReferral();
                    donation.DonationItems = this.context.DonationItem.GetDonationItems(DonatedItems);
                    donation.WantsReceipt = WantsReceipt;
                    donation.User = CurrentUser;
                    donation.PaymentStatus = PaymentStatus.WaitingPayment;
                }

                this.context.Complete();

                TempData["Donation"] = donation.Id;
                HttpContext.Session.SetInt32(DonationIdKey, donation.Id);

                return this.RedirectToPage("/Payment");
            }
            else
            {
                return Page();
            }
        }

        private void SetCurrentUser()
        {
            if (CurrentUser != null)
            {
                if (this.HttpContext.Items.ContainsKey(UserAuthenticationTelemetryInitializer.CurrentUserKey))
                {
                    this.HttpContext.Items[UserAuthenticationTelemetryInitializer.CurrentUserKey] = CurrentUser;
                }
                else
                {
                    this.HttpContext.Items.Add(UserAuthenticationTelemetryInitializer.CurrentUserKey, CurrentUser);
                }
            }
        }

        private string GetReferral()
        {
            StringValues queryValue;
            string result = null;
            if (this.Request.Query.TryGetValue("Referral", out queryValue))
            {
                result = queryValue.ToString();
            }
            else
            {
                if (this.Request.Cookies.TryGetValue("Referral", out result))
                {
                }
            }

            return result;
        }

        private async Task Load()
        {
            CurrentUser = await userManager.GetUserAsync(new ClaimsPrincipal(User.Identity));
            SetCurrentUser();
            LoadDonationFromFlow();
            ProductCatalogue = this.context.ProductCatalogue.GetCurrentProductCatalogue();
            TotalDonations = this.context.Donation.GetTotalDonations(ProductCatalogue);
            var foodBanks = this.context.FoodBank.GetAll().ToList();
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
        }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class MinValueAttribute : ValidationAttribute, IClientModelValidator
    {
        private readonly double _minValue;

        public MinValueAttribute(double minValue)
        {
            _minValue = minValue;
            //ErrorMessage = ValidationMessages.MinAmount;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (Convert.ToDouble(value) * 0.1 < _minValue)
            {
                return new ValidationResult(ValidationMessages.MinAmount);
            }
            return ValidationResult.Success;
        }

        public void AddValidation(ClientModelValidationContext context)
        {
            MergeAttribute(context.Attributes, "data-val", "true");
            var errorMessage = FormatErrorMessage(ValidationMessages.MinAmount);
            MergeAttribute(context.Attributes, "data-val-minvalue", errorMessage);
            var minimumValue = _minValue.ToString(CultureInfo.InvariantCulture);
            MergeAttribute(context.Attributes, "data-val-minvalue-minvalue", minimumValue);
        }

        private bool MergeAttribute(
            IDictionary<string, string> attributes,
            string key,
            string value)
        {
            if (attributes.ContainsKey(key))
            {
                return false;
            }
            attributes.Add(key, value);
            return true;
        }
    }
}
