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
    using BancoAlimentar.AlimentaEstaIdeia.Web.Model.Pages.Shared;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Models;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Telemetry;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Validation;

    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Primitives;

    public class DonationModel : PageModel
    {
        public const string DonationIdKey = "DonationIdKey";

        private readonly ILogger<IndexModel> logger;
        private readonly IUnitOfWork context;
        private readonly SignInManager<WebUser> signInManager;
        private readonly UserManager<WebUser> userManager;
        private readonly ISession session;

        public DonationModel(
            ILogger<IndexModel> logger,
            IUnitOfWork context,
            SignInManager<WebUser> signInManager,
            UserManager<WebUser> userManager)
        {
            this.logger = logger;
            this.context = context;
            this.signInManager = signInManager;
            this.userManager = userManager;
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

        public async Task OnGetAsync()
        {
            CurrentUser = await userManager.GetUserAsync(new ClaimsPrincipal(User.Identity));
            this.HttpContext.Items.Add(UserAuthenticationTelemetryInitializer.CurrentUserKey, CurrentUser);
            string refferarl = GetReferral();
            await Load();
        }

        public async Task<ActionResult> OnPost()
        {
            await Load();

            string donationId = Guid.NewGuid().ToString();

            this.HttpContext.Session.SetString(DonationFlowTelemetryInitializer.DonationSessionKey, donationId);
            if (this.HttpContext.Items.ContainsKey(DonationFlowTelemetryInitializer.DonationSessionKey))
            {
                this.HttpContext.Items[DonationFlowTelemetryInitializer.DonationSessionKey] = donationId;
            }
            else
            {
                this.HttpContext.Items.Add(DonationFlowTelemetryInitializer.DonationSessionKey, donationId);
            }

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
                this.HttpContext.Items.Add(UserAuthenticationTelemetryInitializer.CurrentUserKey, CurrentUser);
                var donationItems = this.context.DonationItem.GetDonationItems(DonatedItems);
                double amount = 0d;
                foreach (var item in donationItems)
                {
                    amount += item.Quantity * item.Price;
                }

                Donation donation = new Donation()
                {
                    PublicId = Guid.NewGuid(),
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
            ProductCatalogue = this.context.ProductCatalogue.GetCurrentProductCatalogue();
            TotalDonations = this.context.Donation.GetTotalDonations(ProductCatalogue);
            var foodBanks = this.context.FoodBank.GetAll().ToList();
            FoodBankList = new List<SelectListItem>();
            foreach (var item in foodBanks)
            {
                FoodBankList.Add(new SelectListItem(item.Name, item.Id.ToString()));
            }

            FoodBankList.Insert(0, new SelectListItem(string.Empty, string.Empty));

            LoginSharedModel = new LoginSharedModel()
            {
                ExternalLogins = (await signInManager.GetExternalAuthenticationSchemesAsync()).ToList(),
                ReturnUrl = string.IsNullOrEmpty(this.Request.Path) ? "~/" : $"~{this.Request.Path.Value + this.Request.QueryString}",
                IsUserLogged = User.Identity.IsAuthenticated,
            };
        }
    }
}
