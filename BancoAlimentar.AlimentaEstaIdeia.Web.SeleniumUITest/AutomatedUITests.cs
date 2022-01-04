// -----------------------------------------------------------------------
// <copyright file="AutomatedUITests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.SeleniumUITest
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using OpenQA.Selenium;
    using OpenQA.Selenium.Chrome;
    using OpenQA.Selenium.Firefox;
    using OpenQA.Selenium.Remote;
    using OpenQA.Selenium.Support.UI;
    using OpenQA.Selenium.Interactions;
    using Xunit;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using System.Reflection;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.Extensibility;
    using SeleniumExtras.WaitHelpers;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;

    public class AutomatedUITests : IDisposable
    {
        
        IUnitOfWork myUnitOfWork;
        ApplicationDbContext myApplicationDbContext;
        private readonly IWebDriver driver;
        public IDictionary<String, Object> vars { get; private set; }
        public IJavaScriptExecutor js { get; private set; }

        const string baseUrl= "https://alimentaestaideia-developer.azurewebsites.net";

        public AutomatedUITests()
        {
            this.driver = new ChromeDriver();
            this.js = (IJavaScriptExecutor)driver;
            this.vars = new Dictionary<String, Object>();

            IConfiguration Configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
                .AddUserSecrets(Assembly.GetExecutingAssembly())
                .AddJsonFile("appsettings.json",
                                optional: true,
                                reloadOnChange: true)
                .Build();
            (myUnitOfWork, myApplicationDbContext) = GetUnitOfWork(Configuration);


            #if DEBUG
            TelemetryConfiguration.Active.DisableTelemetry = true;
            TelemetryDebugWriter.IsTracingDisabled = true;
            #endif

        }

        private static (IUnitOfWork UnitOfWork, ApplicationDbContext ApplicationDbContext) GetUnitOfWork(IConfiguration configuration)
        {
            DbContextOptionsBuilder<ApplicationDbContext> builder = new();

            builder.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"), b => b.MigrationsAssembly("BancoAlimentar.AlimentaEstaIdeia.Web"));
            ApplicationDbContext context = new ApplicationDbContext(builder.Options);
            IUnitOfWork unitOfWork = new UnitOfWork(context, new TelemetryClient(new TelemetryConfiguration("")), null, new Repository.Validation.NifApiValidator());
            return (unitOfWork, context);
        }

        public void Dispose()
        {
            this.driver.Quit();
            this.driver.Dispose();
        }

        private void CreateDonation(double testAmmount, String testUserEmail, String testUserName, String testCompany)
        {


            Actions builder = new Actions(driver);

            driver.Navigate().GoToUrl(baseUrl+"/Donation");


            driver.FindElement(By.CssSelector(".boxed:nth-child(6) .more")).Click();
            driver.FindElement(By.Id("FoodBankId")).Click();
            {
                var dropdown = driver.FindElement(By.Id("FoodBankId"));
                dropdown.FindElement(By.XPath("//option[. = 'Lisboa']")).Click();
            }
            driver.FindElement(By.Id("Name")).Click();
            driver.FindElement(By.Id("Name")).SendKeys(testUserName);
            driver.FindElement(By.Id("CompanyName")).SendKeys(testCompany);
            driver.FindElement(By.Id("Email")).SendKeys(testUserEmail);

            js.ExecuteScript("document.querySelector('#AcceptsTermsCheckBox').checked = true");

            driver.FindElement(By.CssSelector(".text3 > span")).Click();
        }

        [Fact]
        public void Visa()
        {
            Double testAmmount = 0.6;
            String testUserEmail = "alimentestaideia.dev@outlook.com";
            String testCompany = "Test Company";
            String testUserName = "Antonio Manuel Teste";
            CreateDonation(testAmmount, testUserEmail, testUserName, testCompany);

            driver.FindElement(By.CssSelector("#pagamentounicre > .pmethod-img")).Click();
            driver.FindElement(By.CssSelector("form:nth-child(3) > .payment-action:nth-child(2) > span")).Click();
            driver.FindElement(By.CssSelector(".col-xs-12 > .btn")).Click();
            driver.FindElement(By.Name("cardholder")).Click();
            driver.FindElement(By.Name("cardholder")).SendKeys(testUserName);
            {
                var dropdown = driver.FindElement(By.Name("card_expiration_month"));
                dropdown.FindElement(By.XPath("//option[. = '07']")).Click();
            }
            {
                var dropdown = driver.FindElement(By.Name("card_expiration_year"));
                dropdown.FindElement(By.XPath("//option[. = '2026']")).Click();
            }
            driver.FindElement(By.Name("card_cvv")).Click();
            driver.FindElement(By.Name("card_cvv")).SendKeys("003");
            driver.FindElement(By.Name("card_number")).Click();
            {
                var dropdown = driver.FindElement(By.Name("card_number"));
                dropdown.FindElement(By.XPath("//option[. = '0000000000000000 - success']")).Click();
            }
            driver.FindElement(By.Name("accept_privacy_policy")).Click();
            driver.FindElement(By.Id("phone")).Click();
            driver.FindElement(By.Id("phone")).SendKeys("1231231232");
            driver.FindElement(By.CssSelector(".action-buttons .btn-primary")).Click();

            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
            wait.Until(ExpectedConditions.UrlMatches("Thanks"));


            Uri theUri = new Uri(this.driver.Url);
            String pid = System.Web.HttpUtility.ParseQueryString(theUri.Query).Get("PublicId");

            Assert.NotNull(pid);

            var donations = this.myApplicationDbContext.Donations
                .Include(p => p.ConfirmedPayment)
                .Include(p => p.User)
                .Where(p => p.PublicId == new Guid(pid))
                .ToList();

            Assert.Single(donations);

            var donation = donations.FirstOrDefault<Donation>();
            Assert.NotNull(donation);
            Assert.NotNull(donation.ConfirmedPayment.Completed);
            Assert.Equal(PaymentStatus.Payed, donation.PaymentStatus);
            Assert.Equal(testAmmount, donation.DonationAmount);
            Assert.Equal(testUserEmail, donation.User.Email);
            Assert.Equal(testCompany, donation.User.CompanyName);
            Assert.Equal(testUserName, donation.User.FullName);
        }

        [Fact]
        public void DonationMbWay()
        {
            Double testAmmount = 0.6;
            String testUserEmail= "alimentestaideia.dev@outlook.com";
            String testCompany = "Test Company";
            String testUserName = "Antonio Manuel Teste";
            CreateDonation(testAmmount, testUserEmail, testUserName, testCompany);

            driver.FindElement(By.CssSelector("#pagamentombway > .pmethod-img")).Click();
            driver.FindElement(By.Id("PhoneNumber")).Click();

            
            js.ExecuteScript("document.querySelector('#PhoneNumber').value = ''");
            driver.FindElement(By.Id("PhoneNumber")).SendKeys("911234567");

            driver.FindElement(By.CssSelector(".payment-form > .payment-action > span")).Click();


            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
            wait.Until(ExpectedConditions.UrlMatches("Payments"));
            wait.Until(ExpectedConditions.UrlMatches("Thanks"));

            Assert.Contains("Thanks", this.driver.Url);

            Uri theUri = new Uri(this.driver.Url);
            String pid = System.Web.HttpUtility.ParseQueryString(theUri.Query).Get("PublicId");

            Assert.NotNull(pid);

            var donations = this.myApplicationDbContext.Donations
                .Include(p => p.ConfirmedPayment)
                .Include(p => p.User)
                .Where(p => p.PublicId == new Guid(pid))
                .ToList();

            Assert.Single(donations);

            var donation = donations.FirstOrDefault<Donation>();
            Assert.NotNull(donation);
            Assert.NotNull(donation.ConfirmedPayment.Completed);
            Assert.Equal(PaymentStatus.Payed, donation.PaymentStatus);
            Assert.Equal(testAmmount, donation.DonationAmount );
            Assert.Equal(testUserEmail,donation.User.Email);
            Assert.Equal(testCompany, donation.User.CompanyName);
            Assert.Equal(testUserName, donation.User.FullName);
        }
    }
}
