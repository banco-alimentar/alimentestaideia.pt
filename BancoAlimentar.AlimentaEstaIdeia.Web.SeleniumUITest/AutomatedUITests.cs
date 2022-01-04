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

    public class AutomatedUITests : IDisposable
    {
        
        IUnitOfWork myUnitOfWork;
        ApplicationDbContext myApplicationDbContext;
        private readonly IWebDriver driver;
        public IDictionary<String, Object> vars { get; private set; }
        public IJavaScriptExecutor js { get; private set; }

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

        [Fact]
        public void Visa()
        {
            Actions builder = new Actions(driver);

            driver.Navigate().GoToUrl("https://alimentaestaideia-developer.azurewebsites.net/");
            //driver.Manage().Window.Size = new System.Drawing.Size(1680, 1077);
            {
                var element = driver.FindElement(By.CssSelector(".btn-donate"));    
                builder.MoveToElement(element).Perform();
            }
            driver.FindElement(By.CssSelector(".btn-donate")).Click();
            driver.FindElement(By.CssSelector(".boxed:nth-child(6) .more")).Click();
            driver.FindElement(By.Id("FoodBankId")).Click();
            {
                var dropdown = driver.FindElement(By.Id("FoodBankId"));
                dropdown.FindElement(By.XPath("//option[. = 'Lisboa']")).Click();
            }
            driver.FindElement(By.Id("Name")).Click();
            driver.FindElement(By.Id("Name")).SendKeys("Tiago Andrade e Silva");
            driver.FindElement(By.Id("CompanyName")).SendKeys("Microsoft");
            driver.FindElement(By.Id("Email")).SendKeys("tiagonmas@hotmail.com");

            {
                var element2 = driver.FindElement(By.Id("AcceptsTermsCheckBox"));
                builder.MoveToElement(element2).Perform();
            }

            driver.FindElement(By.Id("AcceptsTermsCheckBox")).Click();
            driver.FindElement(By.CssSelector(".text3 > span")).Click(); 
            driver.FindElement(By.CssSelector("#pagamentounicre > .pmethod-img")).Click();
            driver.FindElement(By.CssSelector("form:nth-child(3) > .payment-action:nth-child(2) > span")).Click();
            driver.FindElement(By.CssSelector(".col-xs-12 > .btn")).Click();
            driver.FindElement(By.Name("cardholder")).Click();
            driver.FindElement(By.Name("cardholder")).SendKeys("Tiago Andrade e Silva");
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

            // /Thanks?PublicId=8096f553-5395-4431-97fa-94b47e6e7d60
            Assert.Equal("Alimente esta ideia", this.driver.Title);
            Assert.Contains("Thanks", this.driver.Url);
        }

        [Fact]
        public void DonationMbWay()
        {
            IJavaScriptExecutor js = (IJavaScriptExecutor)driver;

            driver.Navigate().GoToUrl("https://alimentaestaideia-developer.azurewebsites.net/Donation");
            //driver.FindElement(By.CssSelector(".btn-donate-text-size:nth-child(2)")).Click();
            driver.FindElement(By.CssSelector(".boxed:nth-child(6) > .input")).Click();
            driver.FindElement(By.CssSelector(".boxed:nth-child(6) .more")).Click();
            driver.FindElement(By.CssSelector(".text3 > span")).Click();
            driver.FindElement(By.Id("Name")).Click();
            driver.FindElement(By.Id("Name")).SendKeys("Tiago Andrade e Silva");
            driver.FindElement(By.Id("CompanyName")).SendKeys("Microsoft");
            driver.FindElement(By.Id("Email")).SendKeys("tiagonmas@hotmail.com");
            driver.FindElement(By.Id("FoodBankId")).Click();
            {
                var dropdown = driver.FindElement(By.Id("FoodBankId"));
                dropdown.FindElement(By.XPath("//option[. = 'Lisboa']")).Click();
            }

            
            js.ExecuteScript("document.querySelector('#AcceptsTermsCheckBox').checked = true");

            driver.FindElement(By.CssSelector(".text3 > img")).Click();
            driver.FindElement(By.CssSelector("#pagamentombway > .pmethod-img")).Click();
            driver.FindElement(By.Id("PhoneNumber")).Click();

            
            js.ExecuteScript("document.querySelector('#PhoneNumber').value = ''");
            driver.FindElement(By.Id("PhoneNumber")).SendKeys("911234567");

            driver.FindElement(By.CssSelector(".payment-form > .payment-action > span")).Click();


            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
            wait.Until(ExpectedConditions.UrlMatches("Payments"));
            wait.Until(ExpectedConditions.UrlMatches("Thanks"));

            Assert.Equal("Alimente esta ideia", this.driver.Title);
            Assert.Contains("Thanks", this.driver.Url);

            Uri theUri = new Uri(this.driver.Url);
            String pid = System.Web.HttpUtility.ParseQueryString(theUri.Query).Get("PublicId");

            Assert.NotNull(pid);

            var donation = this.myApplicationDbContext.Donations
                .Include(p => p.ConfirmedPayment)
                .Where(p => p.PublicId == new Guid(pid))
                .ToList();

            Assert.Single(donation);    

        }
    }
}