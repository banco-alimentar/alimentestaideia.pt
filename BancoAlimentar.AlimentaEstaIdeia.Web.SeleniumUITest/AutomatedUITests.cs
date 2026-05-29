// -----------------------------------------------------------------------
// <copyright file="AutomatedUITests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.SeleniumUITest
{
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.Data.SqlClient;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Configuration;
    using OpenQA.Selenium;
    using OpenQA.Selenium.Chrome;
    using OpenQA.Selenium.Interactions;
    using OpenQA.Selenium.Support.UI;
    using SeleniumExtras.WaitHelpers;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Xunit;

    public class AutomatedUITests : IDisposable
    {

        IUnitOfWork myUnitOfWork;
        ApplicationDbContext myApplicationDbContext;
        private readonly IWebDriver driver;
        public IDictionary<String, Object> vars { get; private set; }
        public IJavaScriptExecutor js { get; private set; }
        private IConfiguration myConfiguration;
        private readonly string verificationDatabaseDescription;

        const string baseUrl = "https://dev.alimentestaideia.pt";

        public AutomatedUITests()
        {
            this.driver = new ChromeDriver();
            this.js = (IJavaScriptExecutor)driver;
            this.vars = new Dictionary<String, Object>();

            //IConfiguration myConfiguration = new ConfigurationBuilder()
            //    .AddUserSecrets(Assembly.GetExecutingAssembly(), true)
            //    .AddJsonFile("appsettings.json",
            //                    optional: true,
            //                    reloadOnChange: true)
            //    .Build();


            // the type specified here is just so the secrets library can
            // find the UserSecretId we added in the csproj file
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .AddUserSecrets<AutomatedUITests>(optional: true)
                .AddEnvironmentVariables();

            myConfiguration = builder.Build();

            string? connectionString = myConfiguration["SeleniumTest:VerificationConnectionString"];
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                connectionString = GetRequiredConfig(myConfiguration, "ConnectionStrings:DefaultConnection");
            }

            if (connectionString.Contains("#{", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Set SeleniumTest:VerificationConnectionString via user secrets to the dev connection from Key Vault 'alimentestaideia-key' " +
                    "(database alimentestaideia.core_staging). It must match " + baseUrl + ", not production.");
            }

            this.verificationDatabaseDescription = DescribeSqlDatabase(connectionString);
            this.AssertVerificationDatabaseMatchesTestSite(connectionString);
            (myUnitOfWork, myApplicationDbContext) = GetUnitOfWork(connectionString);
        }

        private static string DescribeSqlDatabase(string connectionString)
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            string server = string.IsNullOrWhiteSpace(builder.DataSource) ? "(unknown server)" : builder.DataSource;
            string database = string.IsNullOrWhiteSpace(builder.InitialCatalog) ? "(unknown database)" : builder.InitialCatalog;
            return $"{server} / {database}";
        }

        private void AssertVerificationDatabaseMatchesTestSite(string connectionString)
        {
            var sql = new SqlConnectionStringBuilder(connectionString);
            string catalog = sql.InitialCatalog ?? string.Empty;

            bool testingAgainstDevSite = baseUrl.Contains("dev.", StringComparison.OrdinalIgnoreCase)
                || baseUrl.Contains("developer", StringComparison.OrdinalIgnoreCase);

            if (testingAgainstDevSite && catalog.Equals("alimentestaideia.core", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Selenium tests target " + baseUrl + " but the verification connection uses production database 'alimentestaideia.core'. " +
                    "Set SeleniumTest:VerificationConnectionString to the staging connection from Key Vault 'alimentestaideia-key' " +
                    "(Initial Catalog=alimentestaideia.core_staging).");
            }

            string? expectedCatalog = myConfiguration["SeleniumTest:ExpectedVerificationDatabase"];
            if (!string.IsNullOrWhiteSpace(expectedCatalog)
                && !catalog.Equals(expectedCatalog, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Selenium tests expect verification database '{expectedCatalog}' but the connection uses '{catalog}'. " +
                    "Update SeleniumTest:VerificationConnectionString or SeleniumTest:ExpectedVerificationDatabase.");
            }
        }

        private static (IUnitOfWork UnitOfWork, ApplicationDbContext ApplicationDbContext) GetUnitOfWork(string connectionString)
        {
            DbContextOptionsBuilder<ApplicationDbContext> builder = new();
            builder.UseSqlServer(
                    connectionString, b => b.MigrationsAssembly("BancoAlimentar.AlimentaEstaIdeia.Web"));
            ApplicationDbContext context = new ApplicationDbContext(builder.Options);
            IUnitOfWork unitOfWork = new UnitOfWork(
                context,
                new TelemetryClient(new TelemetryConfiguration()),
                new MemoryCache(new MemoryCacheOptions()),
                new Repository.Validation.NifApiValidator());
            return (unitOfWork, context);
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
            string? username = myConfiguration["SeleniumTest:Site:Username"];
            string? password = myConfiguration["SeleniumTest:Site:Password"];
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
            this.driver.Quit();
            this.driver.Dispose();
        }

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

            string currentValue = element.GetAttribute("value");
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
            wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector(".boxed:nth-child(1) .more")));

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
            wait.Until(ExpectedConditions.ElementIsVisible(By.Id("divSubscriptionFrequency")));

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

            wait.Until(ExpectedConditions.ElementToBeClickable(By.Id("pagamentounicre")));
        }

        private void WaitForPaymentMethodSelectionPage()
        {
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
            try
            {
                wait.Until(ExpectedConditions.ElementToBeClickable(By.Id("pagamentombway")));
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
            SetTextInput(By.Id("PhoneNumber"), phoneNumber);
            driver.FindElement(By.CssSelector(".payment-form .payment-action > span")).Click();
        }

        private void Login()
        {
            driver.Navigate().GoToUrl(baseUrl + "/Identity/Account/Login");

            var waitFields = new WebDriverWait(driver, TimeSpan.FromSeconds(20));
            waitFields.Until(ExpectedConditions.ElementIsVisible(By.Id("Input_Email")));
            waitFields.Until(ExpectedConditions.ElementIsVisible(By.Id("Input_Password")));

            string username = GetRequiredNonEmptyConfig(myConfiguration, "SeleniumTest:Site:Username");
            string password = GetRequiredNonEmptyConfig(myConfiguration, "SeleniumTest:Site:Password");

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
                    $"The browser completed payment on {baseUrl}, which stores data in alimentestaideia.core_staging (Key Vault alimentestaideia-key). " +
                    "Set SeleniumTest:VerificationConnectionString to that database, not production alimentestaideia.core.");
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
        public void Create_Subscription_Authenticated()
        {
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
        public void Visa_Anonymous_Donation_No_Receipt()
        {
            // Arrange
            var donationData = CreateDefaultDonationTestData();

            // Act
            CreateDonation(donationData, false, true, false);

            driver.FindElement(By.CssSelector("#pagamentounicre > .pmethod-img")).Click();
            driver.FindElement(By.CssSelector(".pay2 .payment-action > span")).Click();

            var easyPayWait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
            easyPayWait.Until(d => d.Url.Contains("easypay.pt", StringComparison.OrdinalIgnoreCase));

            var cardNumberSelect = new WebDriverWait(driver, TimeSpan.FromSeconds(20));
            cardNumberSelect.Until(ExpectedConditions.ElementExists(By.CssSelector("select[name='card_number']")));
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
            confirmWait.Until(ExpectedConditions.ElementToBeClickable(
                By.XPath("//button[contains(.,'Confirmar') or contains(.,'Confirm')]")));
            driver.FindElement(By.XPath("//button[contains(.,'Confirmar') or contains(.,'Confirm')]")).Click();

            var thanksWait = new WebDriverWait(driver, TimeSpan.FromSeconds(90));
            thanksWait.Until(ExpectedConditions.UrlMatches("Thanks"));

            //Verify
            ConfirmDonation(donationData);
        }

        [Fact]
        public void Paypal_Anononymous_Donation_No_Receipt()
        {
            // Arrange
            Actions builder = new Actions(driver);
            var donationData = CreateDefaultDonationTestData();

            // Act
            CreateDonation(donationData, false, true, false);

            driver.FindElement(By.CssSelector("#pagamentopaypal > .pmethod-img")).Click();

            var paypalEmailWait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
            paypalEmailWait.Until(ExpectedConditions.ElementIsVisible(By.Id("email")));
            driver.FindElement(By.Id("email")).SendKeys(GetRequiredConfig(myConfiguration, "SeleniumTest:PaypalSandbox:Username"));

            {
                var element = driver.FindElement(By.Id("btnNext"));
                builder.MoveToElement(element).Perform();
            }
            driver.FindElement(By.Id("btnNext")).Click();

            string paypalPassword = GetRequiredConfig(myConfiguration, "SeleniumTest:PaypalSandbox:Password");
            js.ExecuteScript("document.querySelector('#password').value = arguments[0]", paypalPassword);

            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
            wait.Until(ExpectedConditions.ElementIsVisible(By.Id("btnLogin")));

            driver.FindElement(By.Id("btnLogin")).Click();

            var wait2 = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
            wait2.Until(ExpectedConditions.ElementToBeClickable(
                By.CssSelector("[data-testid='submit-button-initial'], #payment-submit-btn")));
            IWebElement submitButton = driver.FindElements(By.CssSelector("[data-testid='submit-button-initial'], #payment-submit-btn")).First();
            submitButton.Click();

            var wait3 = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
            wait3.Until(ExpectedConditions.UrlMatches("Thanks"));

            // Verify
            ConfirmDonation(donationData);
        }


        [Fact]
        public void Multibanco_Anonymous_Donation_No_Receipt()
        {
            // Arrange
            Actions builder = new Actions(driver);
            var donationData = new DonationTestData(5.4, GetRequiredConfig(myConfiguration, "Smtp:User"), "Antonio Manuel Teste Paypal", "Test Company");

            // Act
            CreateDonation(donationData, false, true, false);

            driver.FindElement(By.CssSelector("#pagamentomb > .pmethod-img")).Click();
            driver.FindElement(By.CssSelector("form:nth-child(1) > .payment-action > span")).Click();

            var wait2 = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
            wait2.Until(ExpectedConditions.UrlMatches("Payment"));

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
        public void MbWay_Donation_With_Receipt()
        {
            // Arrange
            var donationData = new DonationTestData(
                5.4,
                GetRequiredConfig(myConfiguration, "Smtp:User"),
                "Antonio Manuel Teste",
                "Test Company",
                "Test Company");

            // Act
            CreateDonation(donationData, false, true, true, setCompanyName: true);

            SubmitMbWayPayment("911234567");


            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(60));
            wait.Until(ExpectedConditions.UrlMatches("Payments"));

            var wait2 = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
            wait2.Until(ExpectedConditions.UrlMatches("Thanks"));

            // Verify
            ConfirmDonation(donationData);
        }
        [Fact]
        public void Claim_Invoice_Donation()
        {
            // Arrange
            var donationData = new DonationTestData(5.4, GetRequiredConfig(myConfiguration, "Smtp:User"), "Antonio Manuel Teste", "Test Company");

            // Act
            CreateDonation(donationData, false, true, false);

            SubmitMbWayPayment("911234567");


            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(60));
            wait.Until(ExpectedConditions.UrlMatches("Payments"));

            var wait2 = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
            wait2.Until(ExpectedConditions.UrlMatches("Thanks"));

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
        public void MbWay_Anonymous_Donation_No_Receipt()
        {
            // Arrange
            var donationData = new DonationTestData(5.4, GetRequiredConfig(myConfiguration, "Smtp:User"), "Antonio Manuel Teste", "Test Company");

            // Act
            CreateDonation(donationData, false, true, false);

            SubmitMbWayPayment("911234567");


            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(60));
            wait.Until(ExpectedConditions.UrlMatches("Payments"));

            var wait2 = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
            wait2.Until(ExpectedConditions.UrlMatches("Thanks"));

            // Verify
            ConfirmDonation(donationData);
        }
    }

    public class DonationTestData
    {
        public const string DefaultCompanyName = "Nada";

        public double testAmmount { get; set; }
        public string testUserEmail { get; set; }

        public string testUserName { get; set; }

        public string testCompany { get; set; }

        public string ExpectedCompanyName { get; set; }

        public DonationTestData(
            double testAmount,
            string testUserEmail,
            string testUserName,
            string testCompany,
            string expectedCompanyName = DefaultCompanyName)
        {
            this.testAmmount = testAmount;
            this.testUserEmail = testUserEmail;
            this.testUserName = testUserName;
            this.testCompany = testCompany;
            this.ExpectedCompanyName = expectedCompanyName;
        }
    }
}
