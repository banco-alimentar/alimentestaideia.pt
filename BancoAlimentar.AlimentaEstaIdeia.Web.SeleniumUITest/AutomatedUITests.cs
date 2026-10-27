// -----------------------------------------------------------------------
// <copyright file="AutomatedUITests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.SeleniumUITest
{
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Web.TestHost;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.Data.SqlClient;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using OpenQA.Selenium;
    using OpenQA.Selenium.Chrome;
    using OpenQA.Selenium.Interactions;
    using OpenQA.Selenium.Support.UI;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Xunit;

    public partial class AutomatedUITests : IDisposable
    {

        IUnitOfWork myUnitOfWork;
        ApplicationDbContext myApplicationDbContext;
        private readonly IWebDriver driver;
        public IDictionary<String, Object> vars { get; private set; }
        public IJavaScriptExecutor js { get; private set; }
        private IConfiguration myConfiguration;
        private readonly string verificationDatabaseDescription;
        private readonly SeleniumTestSettings settings;
        private readonly BrowserTestApplication? browserApplication;
        private readonly IServiceScope? verificationScope;
        private bool disposed;

        private readonly string baseUrl;

        public AutomatedUITests()
        {
            try
            {
                // The type identifies the UserSecretsId declared in this test project.
                var builder = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .AddUserSecrets<AutomatedUITests>(optional: true)
                    .AddEnvironmentVariables();

                myConfiguration = builder.Build();
                bool headless = myConfiguration.GetValue<bool>("SeleniumTest:Headless");
                this.settings = SeleniumTestSettings.FromConfiguration(myConfiguration);
                if (this.settings.IsSelfContained)
                {
                    this.browserApplication = BrowserTestApplication.StartAsync(this.settings.MailpitSmtpPort).GetAwaiter().GetResult();
                    this.settings = this.settings.BindHost(this.browserApplication.BaseAddress, this.browserApplication.Configuration);
                    myConfiguration = this.browserApplication.Configuration;
                    this.verificationScope = this.browserApplication.Services.CreateScope();
                    myApplicationDbContext = this.verificationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    this.browserApplication.AssertOwnsDatabase(myApplicationDbContext);
                    myUnitOfWork = this.verificationScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                    this.verificationDatabaseDescription = "test-owned SQLite memory database";
                }
                else
                {
                    string connectionString = this.settings.VerificationConnectionString;
                    this.verificationDatabaseDescription = DescribeSqlDatabase(connectionString);
                    (myUnitOfWork, myApplicationDbContext) = GetUnitOfWork(connectionString);
                }

                this.baseUrl = this.settings.BaseUrl.AbsoluteUri.TrimEnd('/');
                // Validate the owned context or external SQL catalog before opening Chrome.
                var options = new ChromeOptions();
                if (headless)
                {
                    options.AddArgument("--headless=new");
                }

                this.driver = new ChromeDriver(options);
                this.js = (IJavaScriptExecutor)driver;
                this.vars = new Dictionary<String, Object>();
            }
            catch
            {
                try
                {
                    Dispose();
                }
                catch
                {
                    // Preserve the startup failure; disposal still attempts every owned resource.
                }

                throw;
            }
        }

        private static string DescribeSqlDatabase(string connectionString)
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            string server = string.IsNullOrWhiteSpace(builder.DataSource) ? "(unknown server)" : builder.DataSource;
            string database = string.IsNullOrWhiteSpace(builder.InitialCatalog) ? "(unknown database)" : builder.InitialCatalog;
            return $"{server} / {database}";
        }

        private string GetRequiredSeleniumSetting(string key)
        {
            string? value = this.settings.ReadSetting(myConfiguration, key);
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException("Configure SeleniumTest:" + this.settings.Target + ":" + key + " for this target.");
            }

            return value;
        }

        private void RequireDeveloperPaymentTarget()
        {
            if (this.settings.IsLocal)
            {
                throw new InvalidOperationException("Live payment cases require the Developer target. Use the local test selection for Local.");
            }
        }

        private static (IUnitOfWork UnitOfWork, ApplicationDbContext ApplicationDbContext) GetUnitOfWork(string connectionString)
        {
            DbContextOptionsBuilder<ApplicationDbContext> builder = new();
            builder.UseSqlServer(
                    connectionString, b => b.MigrationsAssembly("BancoAlimentar.AlimentaEstaIdeia.Web"));
            ApplicationDbContext context = new ApplicationDbContext(builder.Options);
            try
            {
                IUnitOfWork unitOfWork = new UnitOfWork(
                    context,
                    new TelemetryClient(new TelemetryConfiguration { DisableTelemetry = true }),
                    new MemoryCache(new MemoryCacheOptions()),
                    new Repository.Validation.NifApiValidator());
                return (unitOfWork, context);
            }
            catch
            {
                context.Dispose();
                throw;
            }
        }

        private static string GetRequiredConfig(IConfiguration configuration, string key) =>
            configuration[key] ?? throw new InvalidOperationException($"Configuration key '{key}' is not set.");

        private static string GetRequiredNonEmptyConfig(IConfiguration configuration, string key)
        {
            string value = GetRequiredConfig(configuration, key);
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"Configuration key '{key}' is empty.");
            }

            return value;
        }

        private bool HasAuthenticatedTestCredentialsConfigured()
        {
            string? username = this.settings.ReadSetting(myConfiguration, "Site:Username");
            string? password = this.settings.ReadSetting(myConfiguration, "Site:Password");
            return !string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password);
        }

        private DonationTestData CreateDefaultDonationTestData(double amount = 5.4) =>
            new DonationTestData(
                amount,
                GetRequiredConfig(myConfiguration, "Smtp:User"),
                GetRequiredConfig(myConfiguration, "SeleniumTest:Name"),
                GetRequiredConfig(myConfiguration, "SeleniumTest:Company"));

        public void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            try
            {
                // ChromeDriver.Dispose also quits the browser and its driver service.
                this.driver?.Dispose();
            }
            finally
            {
                try
                {
                    if (this.verificationScope != null)
                    {
                        // DI owns the verification context and unit of work in this mode.
                        this.verificationScope.Dispose();
                    }
                    else
                    {
                        // The external-mode unit of work owns its directly constructed context.
                        this.myUnitOfWork?.Dispose();
                    }
                }
                finally
                {
                    this.browserApplication?.DisposeAsync().AsTask().GetAwaiter().GetResult();
                }
            }
        }

        private void AssertVerificationDatabase()
        {
            if (this.settings.IsSelfContained)
            {
                if (this.browserApplication == null)
                {
                    throw new InvalidOperationException("Self-contained verification requires an owned browser host.");
                }

                this.browserApplication.AssertOwnsDatabase(myApplicationDbContext);
            }
            else
            {
                this.settings.AssertDatabase(myApplicationDbContext.Database.GetConnectionString() ?? string.Empty);
            }
        }

        private T ScriptResult<T>(string script, params object[] arguments) =>
            Assert.IsType<T>(this.js.ExecuteScript(script, arguments));

        private static string RequiredAttribute(IWebElement element, string name) =>
            Assert.IsType<string>(element.GetAttribute(name));

        private void SetTextInput(By locator, string value)
        {
            IWebElement element = driver.FindElement(locator);
            element.Clear();
            element.SendKeys(value);
        }

        private void SetTextInputRobust(By locator, string value)
        {
            IWebElement element = driver.FindElement(locator);
            element.Click();
            element.Clear();
            element.SendKeys(value);

            string? currentValue = element.GetAttribute("value");
            if (string.Equals(currentValue, value, StringComparison.Ordinal))
            {
                return;
            }

            js.ExecuteScript(@"
                var input = arguments[0];
                var value = arguments[1];
                input.focus();
                input.value = value;
                input.dispatchEvent(new Event('input', { bubbles: true }));
                input.dispatchEvent(new Event('change', { bubbles: true }));
                input.blur();
            ", element, value);
        }

        private void CreateDonation(DonationTestData dData, bool isLoggedIn, bool submit, bool wantsReceipt, bool setCompanyName = false)
        {

            // Set Donation amount
            driver.Navigate().GoToUrl(baseUrl + "/Donation");

            //driver.FindElement(By.Id("total")).Clear();
            //{
            //    var element = driver.FindElement(By.Id("total"));
            //    js.ExecuteScript("if(arguments[0].contentEditable === 'true') {arguments[0].innerText = '" + dData.testAmmount + "'}", element);
            //}

            //driver.FindElement(By.Id("total")).SendKeys(" ");//Force calculateChange event

            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
            wait.Until(WaitConditions.ElementIsVisible(By.CssSelector(".boxed:nth-child(1) .more")));

            driver.FindElement(By.CssSelector(".boxed:nth-child(1) .more")).Click();
            driver.FindElement(By.CssSelector(".boxed:nth-child(4) .more")).Click();
            driver.FindElement(By.CssSelector(".boxed:nth-child(5) .more")).Click();
            driver.FindElement(By.CssSelector(".boxed:nth-child(2) .more")).Click();
            driver.FindElement(By.CssSelector(".boxed:nth-child(3) .more")).Click();
            driver.FindElement(By.CssSelector(".boxed:nth-child(6) .more")).Click();

            driver.FindElement(By.Id("FoodBankId")).Click();
            {
                var dropdown = driver.FindElement(By.Id("FoodBankId"));
                dropdown.FindElement(By.XPath("//option[. = 'Lisboa']")).Click();
            }

            if (!isLoggedIn)
            {
                SetTextInput(By.Id("Name"), dData.testUserName);
                if (setCompanyName)
                {
                    SetTextInput(By.Id("CompanyName"), dData.testCompany);
                }

                SetTextInput(By.Id("Email"), dData.testUserEmail);
            }
            else
            {
                FillReceiptFieldsIfVisible();
            }

            if (wantsReceipt)
            {
                driver.FindElement(By.CssSelector(".half0 > .styled-checkbox-label-2")).Click();
                driver.FindElement(By.Id("Address")).Click();
                driver.FindElement(By.Id("Address")).SendKeys("My Address");
                driver.FindElement(By.Id("PostalCode")).SendKeys("1000-100");
                driver.FindElement(By.Id("Nif")).SendKeys("502671858");
            }

            AcceptDonationTerms();

            if (submit)
            {
                SubmitDonationForm();
            }

        }

        private void FillReceiptFieldsIfVisible()
        {
            IReadOnlyCollection<IWebElement> addressFields = driver.FindElements(By.Id("Address"));
            if (addressFields.Count == 0 || !addressFields.First().Displayed)
            {
                return;
            }

            SetTextInput(By.Id("Address"), "My Address");
            SetTextInput(By.Id("PostalCode"), "1000-100");
            SetTextInput(By.Id("Nif"), "502671858");
        }

        private void AcceptDonationTerms()
        {
            // Do not click the label: it contains an <a target="_blank"> to the privacy policy.
            // Set the checkbox/hidden field directly to avoid opening extra tabs during tests.
            js.ExecuteScript(@"
                var termsCheckbox = document.getElementById('AcceptsTermsCheckBox');
                if (termsCheckbox) {
                    termsCheckbox.checked = true;
                    termsCheckbox.dispatchEvent(new Event('change', { bubbles: true }));
                }
                if (typeof $ !== 'undefined') {
                    $('input:hidden[name=""AcceptsTerms""]').val('true');
                }
            ");
        }

        private void SubmitDonationForm()
        {
            AcceptDonationTerms();
            driver.FindElement(By.CssSelector(".text3 > span")).Click();
        }

        private void EnableSubscriptionDonation()
        {
            IReadOnlyCollection<IWebElement> subscriptionCheckboxes = driver.FindElements(By.Id("AcceptsSubscriptionCheckBox"));
            if (subscriptionCheckboxes.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Subscription donation is not available on {baseUrl}. " +
                    $"Ensure feature flag SubscriptionDonation is enabled in the dev environment.");
            }

            IWebElement subscriptionCheckbox = subscriptionCheckboxes.First();
            if (!subscriptionCheckbox.Selected)
            {
                driver.FindElement(By.CssSelector("label[for='AcceptsSubscriptionCheckBox']")).Click();
            }

            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
            wait.Until(WaitConditions.ElementIsVisible(By.Id("divSubscriptionFrequency")));

            js.ExecuteScript(@"
                var select = document.getElementById('SubscriptionFrequencySelected');
                if (select && select.options.length > 0) { select.selectedIndex = 0; }
                var subscriptionCheckbox = document.getElementById('AcceptsSubscriptionCheckBox');
                if (subscriptionCheckbox) { subscriptionCheckbox.checked = true; }
            ");

            // alertSubscription() toggles WantsReceipt; fill invoice fields if they are shown again
            FillReceiptFieldsIfVisible();
        }

        private void WaitForSubscriptionPaymentPage()
        {
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(60));
            try
            {
                wait.Until(d => d.Url.Contains("SubscriptionPayment", StringComparison.OrdinalIgnoreCase));
            }
            catch (WebDriverTimeoutException)
            {
                string validationErrors = string.Join(
                    "; ",
                    driver.FindElements(By.CssSelector(".validation-summary-errors li, .field-validation-error"))
                        .Select(e => e.Text.Trim())
                        .Where(t => !string.IsNullOrWhiteSpace(t))
                        .Distinct());

                string message =
                    $"Timed out waiting for SubscriptionPayment. Current URL: {driver.Url}.";
                if (!string.IsNullOrEmpty(validationErrors))
                {
                    message += $" Validation errors: {validationErrors}.";
                }
                else if (driver.Url.Contains("/Payment", StringComparison.OrdinalIgnoreCase)
                    && !driver.Url.Contains("SubscriptionPayment", StringComparison.OrdinalIgnoreCase))
                {
                    message += " Landed on the regular Payment page — IsSubscriptionEnabled was likely not posted.";
                }

                throw new InvalidOperationException(message);
            }

            wait.Until(WaitConditions.ElementToBeClickable(By.Id("pagamentounicre")));
        }

        private void WaitForPaymentMethodSelectionPage()
        {
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
            try
            {
                wait.Until(WaitConditions.ElementToBeClickable(By.Id("pagamentombway")));
            }
            catch (WebDriverTimeoutException)
            {
                string validationErrors = string.Join(
                    "; ",
                    driver.FindElements(By.CssSelector(".validation-summary-errors li, .field-validation-error"))
                        .Select(e => e.Text.Trim())
                        .Where(t => !string.IsNullOrWhiteSpace(t))
                        .Distinct());

                string message = $"Timed out waiting for payment method selection page. Current URL: {driver.Url}.";
                if (!string.IsNullOrEmpty(validationErrors))
                {
                    message += $" Validation errors: {validationErrors}.";
                }

                throw new InvalidOperationException(message);
            }
        }

        private void SubmitMbWayPayment(string phoneNumber)
        {
            WaitForPaymentMethodSelectionPage();
            driver.FindElement(By.Id("pagamentombway")).Click();

            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(15));
            wait.Until(WaitConditions.ElementIsVisible(By.CssSelector(".pay4 .payment-form")));
            wait.Until(WaitConditions.ElementToBeClickable(By.Id("PhoneNumber")));

            SetTextInputRobust(By.Id("PhoneNumber"), phoneNumber);
            driver.FindElement(By.CssSelector(".pay4 .payment-form .payment-action > span")).Click();
        }

        private string CollectVisiblePageErrors()
        {
            return string.Join(
                "; ",
                driver.FindElements(By.CssSelector(".alert-danger, .validation-summary-errors li, .field-validation-error"))
                    .Select(element => element.Text.Trim())
                    .Where(text => !string.IsNullOrWhiteSpace(text))
                    .Distinct());
        }

        private void WaitForMbWayPaymentFlow(TimeSpan timeout)
        {
            var wait = new WebDriverWait(driver, timeout);
            try
            {
                wait.Until(webDriver =>
                {
                    string url = webDriver.Url;
                    return url.Contains("Payments", StringComparison.OrdinalIgnoreCase)
                        || url.Contains("Thanks", StringComparison.OrdinalIgnoreCase);
                });
            }
            catch (WebDriverTimeoutException)
            {
                string pageErrors = CollectVisiblePageErrors();
                string message = $"Timed out waiting for MBWay payment flow. Current URL: {driver.Url}.";
                if (!string.IsNullOrEmpty(pageErrors))
                {
                    message += $" Page errors: {pageErrors}.";
                }
                else if (driver.Url.Contains("/Payment", StringComparison.OrdinalIgnoreCase)
                    && !driver.Url.Contains("Payments", StringComparison.OrdinalIgnoreCase))
                {
                    message += " Still on the payment method page — the MBWay request may not have been submitted or Easypay rejected it.";
                }

                throw new InvalidOperationException(message);
            }
        }

        private void WaitForThanksPage(TimeSpan timeout)
        {
            if (driver.Url.Contains("Thanks", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var wait = new WebDriverWait(driver, timeout);
            try
            {
                wait.Until(webDriver => webDriver.Url.Contains("Thanks", StringComparison.OrdinalIgnoreCase));
            }
            catch (WebDriverTimeoutException)
            {
                string pageErrors = CollectVisiblePageErrors();
                string message = $"Timed out waiting for Thanks page. Current URL: {driver.Url}.";
                if (!string.IsNullOrEmpty(pageErrors))
                {
                    message += $" Page errors: {pageErrors}.";
                }

                throw new InvalidOperationException(message);
            }
        }

        private void CompleteMbWayDonationFlow()
        {
            SubmitMbWayPayment("911234567");
            WaitForMbWayPaymentFlow(TimeSpan.FromSeconds(90));
            WaitForThanksPage(TimeSpan.FromSeconds(90));
        }

        private void Login()
        {
            Login(
                GetRequiredSeleniumSetting("Site:Username"),
                GetRequiredSeleniumSetting("Site:Password"));
        }

        private void Login(string username, string password)
        {
            driver.Navigate().GoToUrl(baseUrl + "/Identity/Account/Login");

            var waitFields = new WebDriverWait(driver, TimeSpan.FromSeconds(20));
            waitFields.Until(WaitConditions.ElementIsVisible(By.Id("Input_Email")));
            waitFields.Until(WaitConditions.ElementIsVisible(By.Id("Input_Password")));

            SetTextInputRobust(By.Id("Input_Email"), username);
            SetTextInputRobust(By.Id("Input_Password"), password);

            driver.FindElement(By.Id("loginBtn")).Click();

            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(15));
            wait.Until(d => !d.Url.Contains("/Account/Login", StringComparison.OrdinalIgnoreCase));
        }

        private Donation WaitForConfirmedDonation(Guid publicId, TimeSpan timeout)
        {
            DateTime deadline = DateTime.UtcNow.Add(timeout);
            string lastState = "not found";
            while (DateTime.UtcNow < deadline)
            {
                this.myApplicationDbContext.ChangeTracker.Clear();
                Donation? donation = this.myApplicationDbContext.Donations
                    .Include(p => p.ConfirmedPayment)
                    .Include(p => p.User)
                    .SingleOrDefault(p => p.PublicId == publicId);

                if (donation == null)
                {
                    lastState = "not found";
                }
                else if (IsDonationConfirmed(donation))
                {
                    return donation;
                }
                else
                {
                    lastState =
                        $"found (PaymentStatus={donation.PaymentStatus}, ConfirmedPaymentCompleted={donation.ConfirmedPayment?.Completed})";
                }

                Thread.Sleep(TimeSpan.FromSeconds(3));
            }

            if (lastState == "not found")
            {
                throw new InvalidOperationException(
                    $"Donation {publicId} was not found in SQL [{this.verificationDatabaseDescription}] within {timeout.TotalSeconds} seconds. " +
                    $"Check the selected {this.settings.Target} verification database matches the application. Never use production.");
            }

            throw new InvalidOperationException(
                $"Donation {publicId} exists in SQL [{this.verificationDatabaseDescription}] but was not confirmed within {timeout.TotalSeconds} seconds ({lastState}). " +
                "If the Thanks page already appeared, the EasyPay webhook may be slow, or PaymentStatus/ConfirmedPayment may not be updated yet.");
        }

        private bool IsDonationConfirmed(Donation donation)
        {
            if (donation.PaymentStatus != PaymentStatus.Payed)
            {
                return false;
            }

            if (donation.ConfirmedPayment?.Completed != null)
            {
                return true;
            }

            return this.myApplicationDbContext.Payments
                .Any(p => p.Donation.Id == donation.Id
                    && p.Completed != null
                    && (p.Status == "ok" || p.Status == "Success" || p.Status == "COMPLETED"));
        }

        private string ConfirmDonation(DonationTestData dData)
        {
            Assert.Contains("Thanks", this.driver.Url);

            Uri theUri = new Uri(this.driver.Url);
            string? pid = System.Web.HttpUtility.ParseQueryString(theUri.Query).Get("PublicId");

            Assert.NotNull(pid);
            Guid publicId = Guid.Parse(pid);

            Donation donation = WaitForConfirmedDonation(publicId, TimeSpan.FromSeconds(90));

            Assert.Equal(dData.testAmmount, donation.DonationAmount);
            Assert.Equal(dData.testUserEmail, donation.User.Email);
            Assert.Equal(dData.ExpectedCompanyName, donation.User.CompanyName);
            Assert.Equal(dData.testUserName, donation.User.FullName);

            return pid;
        }

        private BasePayment WaitForPayment(Guid publicId, TimeSpan timeout)
        {
            DateTime deadline = DateTime.UtcNow.Add(timeout);
            while (DateTime.UtcNow < deadline)
            {
                this.myApplicationDbContext.ChangeTracker.Clear();
                BasePayment? payment = this.myApplicationDbContext.Payments
                    .Include(p => p.Donation)
                    .SingleOrDefault(p => p.Donation.PublicId == publicId);

                if (payment != null)
                {
                    return payment;
                }

                Thread.Sleep(TimeSpan.FromSeconds(2));
            }

            throw new InvalidOperationException(
                $"Payment for donation {publicId} was not found in SQL [{this.verificationDatabaseDescription}] within {timeout.TotalSeconds} seconds. " +
                $"Ensure the verification connection string matches the database used by {baseUrl}.");
        }

        private Invoice WaitForInvoice(Guid donationPublicId, TimeSpan timeout)
        {
            DateTime deadline = DateTime.UtcNow.Add(timeout);
            while (DateTime.UtcNow < deadline)
            {
                this.myApplicationDbContext.ChangeTracker.Clear();
                Invoice? invoice = this.myApplicationDbContext.Invoices
                    .Include(i => i.User)
                    .Include(i => i.Donation)
                    .SingleOrDefault(i => i.Donation.PublicId == donationPublicId);

                if (invoice != null)
                {
                    return invoice;
                }

                Thread.Sleep(TimeSpan.FromSeconds(2));
            }

            string validationErrors = string.Join(
                "; ",
                driver.FindElements(By.CssSelector(".validation-summary-errors li, .field-validation-error"))
                    .Select(e => e.Text.Trim())
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Distinct());

            string message =
                $"Invoice for donation {donationPublicId} was not found in SQL [{this.verificationDatabaseDescription}] within {timeout.TotalSeconds} seconds.";
            if (!string.IsNullOrEmpty(validationErrors))
            {
                message += $" ClaimInvoice validation errors: {validationErrors}.";
            }

            throw new InvalidOperationException(message);
        }


        [Fact]
        [Trait("Category", "LivePayment")]
        public void Create_Subscription_Authenticated()
        {
            RequireDeveloperPaymentTarget();
            if (!HasAuthenticatedTestCredentialsConfigured())
            {
                return;
            }

            // Arrange
            var donationData = CreateDefaultDonationTestData();

            Login();

            CreateDonation(donationData, true, false, false);

            EnableSubscriptionDonation();
            SubmitDonationForm();

            WaitForSubscriptionPaymentPage();

            driver.FindElement(By.Id("pagamentounicre")).Click();
            driver.FindElement(By.CssSelector(".pay2 .payment-action > span")).Click();
        }

        [Fact]
        [Trait("Category", "LivePayment")]
        public void Visa_Anonymous_Donation_No_Receipt()
        {
            RequireDeveloperPaymentTarget();
            // Arrange
            var donationData = CreateDefaultDonationTestData();

            // Act
            CreateDonation(donationData, false, true, false);

            driver.FindElement(By.CssSelector("#pagamentounicre > .pmethod-img")).Click();
            driver.FindElement(By.CssSelector(".pay2 .payment-action > span")).Click();

            var easyPayWait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
            easyPayWait.Until(d => d.Url.Contains("easypay.pt", StringComparison.OrdinalIgnoreCase));

            var cardNumberSelect = new WebDriverWait(driver, TimeSpan.FromSeconds(20));
            cardNumberSelect.Until(WaitConditions.ElementExists(By.CssSelector("select[name='card_number']")));
            driver.FindElement(By.CssSelector("select[name='card_number']")).SendKeys("0000000000000000");
            driver.FindElement(By.CssSelector("select[name='card_expiration_month']")).SendKeys("01");
            driver.FindElement(By.CssSelector("select[name='card_expiration_year']")).SendKeys("2028");
            driver.FindElement(By.CssSelector("input[name='card_cvv'], [placeholder='CVV']")).SendKeys("123");

            IWebElement? cardholder = driver.FindElements(By.CssSelector("[placeholder='Titular'], [name='cardholder']")).FirstOrDefault();
            if (cardholder != null)
            {
                cardholder.Clear();
                cardholder.SendKeys(donationData.testUserName);
            }

            IWebElement? phone = driver.FindElements(By.CssSelector("[placeholder='Telefone'], #phone")).FirstOrDefault();
            if (phone != null)
            {
                phone.Clear();
                phone.SendKeys("911234567");
            }

            driver.FindElements(By.CssSelector("button, .btn, [type='submit']"))
                .First(e => e.Text.Contains("Seguinte", StringComparison.OrdinalIgnoreCase) || e.Text.Contains("Next", StringComparison.OrdinalIgnoreCase))
                .Click();

            var confirmWait = new WebDriverWait(driver, TimeSpan.FromSeconds(20));
            confirmWait.Until(WaitConditions.ElementToBeClickable(
                By.XPath("//button[contains(.,'Confirmar') or contains(.,'Confirm')]")));
            driver.FindElement(By.XPath("//button[contains(.,'Confirmar') or contains(.,'Confirm')]")).Click();

            var thanksWait = new WebDriverWait(driver, TimeSpan.FromSeconds(90));
            thanksWait.Until(WaitConditions.UrlContains("Thanks"));

            //Verify
            ConfirmDonation(donationData);
        }

        [Fact]
        [Trait("Category", "LivePayment")]
        public void Paypal_Anononymous_Donation_No_Receipt()
        {
            RequireDeveloperPaymentTarget();
            // Arrange
            Actions builder = new Actions(driver);
            var donationData = CreateDefaultDonationTestData();

            // Act
            CreateDonation(donationData, false, true, false);

            var paymentPageWait = new WebDriverWait(driver, TimeSpan.FromSeconds(20));
            paymentPageWait.Until(WaitConditions.ElementExists(
                By.CssSelector("#pagamentounicre, #pagamentomb, #pagamentopaypal")));

            var paypalButtons = driver.FindElements(By.CssSelector("#pagamentopaypal > .pmethod-img"));
            Assert.True(
                paypalButtons.Count > 0,
                "PayPal is not shown on the payment page. Enable IsPayPalEnabled for the dev tenant at " + baseUrl + ".");
            paypalButtons[0].Click();

            var paypalEmailWait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
            paypalEmailWait.Until(WaitConditions.ElementIsVisible(By.Id("email")));
            driver.FindElement(By.Id("email")).SendKeys(GetRequiredConfig(myConfiguration, "SeleniumTest:PaypalSandbox:Username"));

            {
                var element = driver.FindElement(By.Id("btnNext"));
                builder.MoveToElement(element).Perform();
            }
            driver.FindElement(By.Id("btnNext")).Click();

            string paypalPassword = GetRequiredConfig(myConfiguration, "SeleniumTest:PaypalSandbox:Password");
            js.ExecuteScript("document.querySelector('#password').value = arguments[0]", paypalPassword);

            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
            wait.Until(WaitConditions.ElementIsVisible(By.Id("btnLogin")));

            driver.FindElement(By.Id("btnLogin")).Click();

            var wait2 = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
            wait2.Until(WaitConditions.ElementToBeClickable(
                By.CssSelector("[data-testid='submit-button-initial'], #payment-submit-btn")));
            IWebElement submitButton = driver.FindElements(By.CssSelector("[data-testid='submit-button-initial'], #payment-submit-btn")).First();
            submitButton.Click();

            var wait3 = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
            wait3.Until(WaitConditions.UrlContains("Thanks"));

            // Verify
            ConfirmDonation(donationData);
        }


        [Fact]
        [Trait("Category", "LivePayment")]
        public void Multibanco_Anonymous_Donation_No_Receipt()
        {
            RequireDeveloperPaymentTarget();
            // Arrange
            Actions builder = new Actions(driver);
            var donationData = new DonationTestData(5.4, GetRequiredConfig(myConfiguration, "Smtp:User"), "Antonio Manuel Teste Paypal", "Test Company");

            // Act
            CreateDonation(donationData, false, true, false);

            driver.FindElement(By.CssSelector("#pagamentomb > .pmethod-img")).Click();
            driver.FindElement(By.CssSelector("form:nth-child(1) > .payment-action > span")).Click();

            var wait2 = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
            wait2.Until(WaitConditions.UrlContains("Payment"));

            // Verify
            Uri theUri = new Uri(this.driver.Url);
            string? pid = System.Web.HttpUtility.ParseQueryString(theUri.Query).Get("PublicId");

            Assert.NotNull(pid);
            Guid publicId = Guid.Parse(pid);

            BasePayment payment = WaitForPayment(publicId, TimeSpan.FromSeconds(30));
            Assert.Equal(PaymentStatus.WaitingPayment, payment.Donation.PaymentStatus);
            Assert.Null(payment.Completed);
        }

        [Fact]
        [Trait("Category", "LivePayment")]
        public void MbWay_Donation_With_Receipt()
        {
            RequireDeveloperPaymentTarget();
            // Arrange
            var donationData = new DonationTestData(
                5.4,
                GetRequiredConfig(myConfiguration, "Smtp:User"),
                "Antonio Manuel Teste",
                "Test Company",
                "Test Company");

            // Act
            CreateDonation(donationData, false, true, true, setCompanyName: true);

            CompleteMbWayDonationFlow();

            // Verify
            ConfirmDonation(donationData);
        }
        [Fact]
        [Trait("Category", "LivePayment")]
        public void Claim_Invoice_Donation()
        {
            RequireDeveloperPaymentTarget();
            // Arrange
            var donationData = new DonationTestData(5.4, GetRequiredConfig(myConfiguration, "Smtp:User"), "Antonio Manuel Teste", "Test Company");

            // Act
            CreateDonation(donationData, false, true, false);

            CompleteMbWayDonationFlow();

            // Verify
            string publicId = ConfirmDonation(donationData);


            // Claim Invoice

            driver.Navigate().GoToUrl(baseUrl + "/ClaimInvoice?publicId=" + publicId);
            driver.FindElement(By.Id("Address")).Click();
            driver.FindElement(By.Id("Address")).SendKeys("My Address");
            driver.FindElement(By.Id("PostalCode")).SendKeys("1000-000");
            driver.FindElement(By.Id("Nif")).Click();
            driver.FindElement(By.Id("Nif")).SendKeys("196807050");
            js.ExecuteScript(@"
                var termsCheckbox = document.getElementById('AcceptsTermsCheckBox');
                if (termsCheckbox) {
                    termsCheckbox.checked = true;
                    termsCheckbox.dispatchEvent(new Event('change', { bubbles: true }));
                }
                if (typeof $ !== 'undefined') {
                    $('input:hidden[name=""AcceptsTerms""]').val('true');
                }

                // ClaimInvoice model binds this non-nullable property on POST.
                var invoiceMessage = document.querySelector('input[name=""InvoiceAlreadyGeneratedMessage""]');
                if (!invoiceMessage) {
                    invoiceMessage = document.createElement('input');
                    invoiceMessage.type = 'hidden';
                    invoiceMessage.name = 'InvoiceAlreadyGeneratedMessage';
                    document.forms[0].appendChild(invoiceMessage);
                }
                invoiceMessage.value = 'selenium';
            ");

            driver.FindElement(By.CssSelector(".text3 > span")).Click();


            var donations = this.myApplicationDbContext.Donations
               .Include(p => p.ConfirmedPayment)
               .Include(p => p.User)
               .Where(p => p.PublicId == new Guid(publicId))
               .ToList();

            Assert.Single(donations);
            var donation = donations.FirstOrDefault<Donation>();
            Assert.NotNull(donation);

            this.myApplicationDbContext.Entry<Donation>(donation).Reload();

            Invoice? invoice = WaitForInvoice(donation.PublicId, TimeSpan.FromSeconds(45));
            Assert.NotNull(invoice);
            Assert.False(invoice.IsCanceled);
            Assert.NotEqual(Guid.Empty, invoice.BlobName);
            Assert.NotEmpty(invoice.Number);
            Assert.NotNull(invoice.User);
            Assert.True(donation.WantsReceipt);

        }



        [Fact]
        [Trait("Category", "LivePayment")]
        public void MbWay_Anonymous_Donation_No_Receipt()
        {
            RequireDeveloperPaymentTarget();
            // Arrange
            var donationData = new DonationTestData(5.4, GetRequiredConfig(myConfiguration, "Smtp:User"), "Antonio Manuel Teste", "Test Company");

            // Act
            CreateDonation(donationData, false, true, false);

            CompleteMbWayDonationFlow();

            // Verify
            ConfirmDonation(donationData);
        }
    }

    internal static class WaitConditions
    {
        public static Func<IWebDriver, IWebElement> ElementExists(By locator)
        {
            return driver => driver.FindElement(locator);
        }

        public static Func<IWebDriver, IWebElement?> ElementIsVisible(By locator)
        {
            return driver =>
            {
                IWebElement element = driver.FindElement(locator);
                return element.Displayed ? element : null;
            };
        }

        public static Func<IWebDriver, IWebElement?> ElementToBeClickable(By locator)
        {
            return driver =>
            {
                IWebElement element = driver.FindElement(locator);
                return element.Displayed && element.Enabled ? element : null;
            };
        }

        public static Func<IWebDriver, bool> UrlContains(string value)
        {
            return driver => driver.Url.Contains(value, StringComparison.OrdinalIgnoreCase);
        }
    }

    public class DonationTestData
    {
        public const string? DefaultCompanyName = null;

        public double testAmmount { get; set; }
        public string testUserEmail { get; set; }

        public string testUserName { get; set; }

        public string testCompany { get; set; }

        public string? ExpectedCompanyName { get; set; }

        public DonationTestData(
            double testAmount,
            string testUserEmail,
            string testUserName,
            string testCompany,
            string? expectedCompanyName = DefaultCompanyName)
        {
            this.testAmmount = testAmount;
            this.testUserEmail = testUserEmail;
            this.testUserName = testUserName;
            this.testCompany = testCompany;
            this.ExpectedCompanyName = expectedCompanyName;
        }
    }
}
