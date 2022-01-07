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

        private void CreateDonation(DonationTestData dData)
        {

            // Set Donation amount
            driver.Navigate().GoToUrl(baseUrl+"/Donation");

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
            driver.FindElement(By.Id("Name")).Click();
            driver.FindElement(By.Id("Name")).SendKeys(dData.testUserName);
            driver.FindElement(By.Id("CompanyName")).SendKeys(dData.testCompany);
            driver.FindElement(By.Id("Email")).SendKeys(dData.testUserEmail);

            js.ExecuteScript("document.querySelector('#AcceptsTermsCheckBox').checked = true");

            driver.FindElement(By.CssSelector(".text3 > span")).Click();
        }

        private void ConfirmDonation(DonationTestData dData)
        {
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
            Assert.NotNull(donation.ConfirmedPayment);
            Assert.NotNull(donation.ConfirmedPayment.Completed);
            Assert.Equal(PaymentStatus.Payed, donation.PaymentStatus);
            Assert.Equal(dData.testAmmount, donation.DonationAmount);
            Assert.Equal(dData.testUserEmail, donation.User.Email);
            Assert.Equal(dData.testCompany, donation.User.CompanyName);
            Assert.Equal(dData.testUserName, donation.User.FullName);
        }



        [Fact]
        public void Visa_Anonymous_Donation()
        {
            // Arrange
            var donationData = new DonationTestData(9, "alimentestaideia.dev@outlook.com", "Antonio Manuel Teste Visa", "Test Company");

            // Act
            CreateDonation(donationData);

            driver.FindElement(By.CssSelector("#pagamentounicre > .pmethod-img")).Click();
            driver.FindElement(By.CssSelector("form:nth-child(3) > .payment-action:nth-child(2) > span")).Click();
            driver.FindElement(By.CssSelector(".col-xs-12 > .btn")).Click();
            driver.FindElement(By.Name("cardholder")).Click();
            driver.FindElement(By.Name("cardholder")).SendKeys(donationData.testUserName);
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

            js.ExecuteScript("document.querySelector('#phone').value = ''");
            js.ExecuteScript("document.querySelector('#name').value = ''");
            js.ExecuteScript("document.querySelector('#email').value = ''");

            driver.FindElement(By.Id("phone")).Click();
            driver.FindElement(By.Id("phone")).SendKeys("1231231232");

            driver.FindElement(By.Id("name")).Click();
            driver.FindElement(By.Id("name")).SendKeys(donationData.testUserName);

            driver.FindElement(By.Id("email")).SendKeys(donationData.testUserEmail);

            driver.FindElement(By.CssSelector(".action-buttons .btn-primary")).Click();
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
            wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector(".col-xs-12:nth-child(1) .btn-primary")));

            driver.FindElement(By.CssSelector(".col-xs-12:nth-child(1) .btn-primary")).Click();

            var wait2 = new WebDriverWait(driver, TimeSpan.FromSeconds(60));
            wait2.Until(ExpectedConditions.UrlMatches("Thanks"));

            //Verify
            ConfirmDonation(donationData);
        }

        [Fact]
        public void Paypal_Anononymous_Donation()
        {
            // Arrange
            Actions builder = new Actions(driver);
            var donationData = new DonationTestData(9, "alimentestaideia.dev@outlook.com", "Antonio Manuel Teste Paypal", "Test Company");

            // Act
            CreateDonation(donationData);

            driver.FindElement(By.CssSelector("#pagamentopaypal > .pmethod-img")).Click();

            driver.FindElement(By.Id("email")).Click();
            driver.FindElement(By.Id("email")).SendKeys("sb-xkcxs2177214@personal.example.com");

            {
                var element = driver.FindElement(By.Id("btnNext"));
                builder.MoveToElement(element).Perform();
            }
            driver.FindElement(By.Id("btnNext")).Click();

            js.ExecuteScript("document.querySelector('#password').value = 'CM891Yg#'");

            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
            wait.Until(ExpectedConditions.ElementIsVisible(By.Id("btnLogin")));

            driver.FindElement(By.Id("btnLogin")).Click();

            var wait2 = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
            wait2.Until(ExpectedConditions.ElementIsVisible(By.Id("payment-submit-btn")));
            driver.FindElement(By.Id("payment-submit-btn")).Click();

            var wait3 = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
            wait3.Until(ExpectedConditions.UrlMatches("Thanks"));

            // Verify
            ConfirmDonation(donationData);
        }


        [Fact]
        public void Multibanco_Anonymous_Donation()
        {
            // Arrange
            Actions builder = new Actions(driver);
            var donationData = new DonationTestData(9, "alimentestaideia.dev@outlook.com", "Antonio Manuel Teste Paypal", "Test Company");

            // Act
            CreateDonation(donationData);

            driver.FindElement(By.CssSelector("#pagamentomb > .pmethod-img")).Click();
            driver.FindElement(By.CssSelector("form:nth-child(1) > .payment-action > span")).Click();

            var wait2 = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
            wait2.Until(ExpectedConditions.UrlMatches("Payment"));

            // Verify
            Uri theUri = new Uri(this.driver.Url);
            String pid = System.Web.HttpUtility.ParseQueryString(theUri.Query).Get("PublicId");

            Assert.NotNull(pid);

            var payments = this.myApplicationDbContext.Payments
                .Include(p => p.Donation)
                .Where(p => p.Donation.PublicId == new Guid(pid))
                .ToList();

            Assert.Single(payments);

            var payment = payments.FirstOrDefault<BasePayment>();
            Assert.Equal(PaymentStatus.WaitingPayment, payment.Donation.PaymentStatus);
            Assert.Null(payment.Completed);
        }

        //[Fact]
        //public void MbWay_Authenticated_Donation()
        //{
        //    throw new NotImplementedException();
        //}

        //[Fact]
        //public void Crate_Subscription_Authenticated()
        //{
        //    throw new NotImplementedException();
        //}


        [Fact]
        public void MbWay_Anonymous_Donation()
        {
            // Arrange
            var donationData = new DonationTestData(9, "alimentestaideia.dev@outlook.com", "Antonio Manuel Teste", "Test Company");

            // Act
            CreateDonation(donationData);

            driver.FindElement(By.CssSelector("#pagamentombway > .pmethod-img")).Click();
            driver.FindElement(By.Id("PhoneNumber")).Click();

            
            js.ExecuteScript("document.querySelector('#PhoneNumber').value = ''");
            driver.FindElement(By.Id("PhoneNumber")).SendKeys("911234567");

            driver.FindElement(By.CssSelector(".payment-form > .payment-action > span")).Click();


            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(60));
            wait.Until(ExpectedConditions.UrlMatches("Payments"));

            var wait2 = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
            wait2.Until(ExpectedConditions.UrlMatches("Thanks"));

            // Verify
            ConfirmDonation(donationData);
        }
    }

    public class DonationTestData{
        public double testAmmount { get; set; }
        public string testUserEmail { get; set; }

        public string testUserName { get; set; }

        public string testCompany { get; set; }


        public DonationTestData(double _testAmmount, String _testUserEmail, String _testUserName, String _testCompany)
        {
            testAmmount=_testAmmount;
            testUserEmail=_testUserEmail;
            testUserName = _testUserName;
            testCompany = _testCompany;
        }
    }
}
