// -----------------------------------------------------------------------
// <copyright file="DonationHistoryUITests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.SeleniumUITest
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Web.TestHost;
    using OpenQA.Selenium;
    using OpenQA.Selenium.Chrome;
    using OpenQA.Selenium.Support.UI;
    using Xunit;

    public partial class AutomatedUITests
    {
        [Theory]
        [InlineData(1242)]
        [InlineData(768)]
        [InlineData(375)]
        [Trait("Category", "LocalBrowser")]
        public async Task DonationHistory_ContainsTableAndPreservesDonationActions(int width)
        {
            if (browserApplication == null || verificationScope == null)
            {
                throw new InvalidOperationException("Donation history fixtures require the self-contained browser host.");
            }

            browserApplication.AssertOwnsDatabase(myApplicationDbContext);
            var services = verificationScope.ServiceProvider;
            var paid = (await IntegrationTestDataSeeder.SeedPendingDonationWithCreditCardAsync(services, Guid.NewGuid())).Donation;
            var invalidNif = (await IntegrationTestDataSeeder.SeedPendingDonationWithCreditCardAsync(services, Guid.NewGuid())).Donation;
            paid.PaymentStatus = PaymentStatus.Payed;
            paid.Nif = "196807050";
            paid.ConfirmedPayment = paid.PaymentList.Single();
            invalidNif.PaymentStatus = PaymentStatus.Payed;
            invalidNif.ConfirmedPayment = invalidNif.PaymentList.Single();
            var pendingId = Guid.NewGuid();
            var recurringPendingId = Guid.NewGuid();
            await IntegrationTestDataSeeder.SeedPendingDonationWithCreditCardAsync(services, pendingId);
            await IntegrationTestDataSeeder.SeedPendingDonationWithCreditCardAsync(services, recurringPendingId);
            var pending = myApplicationDbContext.Donations.Single(d => d.PublicId == pendingId);
            var recurringPending = myApplicationDbContext.Donations.Single(d => d.PublicId == recurringPendingId);
            var ownerEmail = $"donation-history-{Guid.NewGuid():N}@example.test";
            var owner = await IntegrationTestDataSeeder.EnsureUserAsync(services, ownerEmail, IntegrationTestCredentials.DefaultPassword);
            owner.Nif = "000000000";
            invalidNif.Nif = "000000000";
            foreach (var donation in new[] { paid, invalidNif, pending, recurringPending })
            {
                donation.User = owner;
            }

            var subscription = new Subscription
            {
                PublicId = Guid.NewGuid(),
                User = owner,
                InitialDonation = paid,
                Created = DateTime.UtcNow,
                StartTime = DateTime.UtcNow,
                ExpirationTime = DateTime.UtcNow.AddYears(1),
                Status = SubscriptionStatus.Active,
                Frequency = "_1M",
                TransactionKey = Guid.NewGuid().ToString(),
                EasyPaySubscriptionId = Guid.NewGuid().ToString(),
                Url = baseUrl,
                Donations = new List<SubscriptionDonations>
                {
                    new SubscriptionDonations { Donation = paid },
                    new SubscriptionDonations { Donation = recurringPending },
                },
            };
            myApplicationDbContext.Subscriptions.Add(subscription);
            myApplicationDbContext.SaveChanges();

            SetAccountHistoryViewport(width);
            Login(ownerEmail, IntegrationTestCredentials.DefaultPassword);
            driver.Navigate().GoToUrl(baseUrl + "/Identity/Account/Manage/DonationHistory?culture=pt&ui-culture=pt");
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(20));
            WaitForDonationHistory(wait, "#donationHistory tbody tr", 3);
            Assert.Equal(width, ScriptResult<long>("return window.innerWidth;"));

            var panel = driver.FindElement(By.CssSelector(".account-history-panel"));
            Assert.True(ScriptResult<bool>(
                "var panel = arguments[0].getBoundingClientRect(); var content = arguments[0].closest('.account-manage-content').getBoundingClientRect(); return panel.left >= content.left - 1 && panel.right <= content.right + 1 && document.documentElement.scrollWidth <= window.innerWidth + 1;",
                panel));
            var region = driver.FindElement(By.CssSelector(".account-history-table-region"));
            Assert.Equal("0", region.GetAttribute("tabindex"));
            if (width == 375)
            {
                Assert.True(ScriptResult<bool>("return arguments[0].scrollWidth > arguments[0].clientWidth;", region));
            }

            Assert.Equal(6, driver.FindElements(By.CssSelector("#donationHistory thead th")).Count);
            var paidRow = driver.FindElement(By.CssSelector($"#donationHistory a[href*='GenerateInvoice?publicDonationId={paid.PublicId}']")).FindElement(By.XPath("./ancestor::tr"));
            Assert.Contains("Pago", paidRow.Text);
            Assert.Single(paidRow.FindElements(By.CssSelector($"a[href*='Subscriptions/Details?PublicId={subscription.PublicId}']")));
            Assert.Single(driver.FindElements(By.CssSelector("#donationHistory .donation-history-nif")));
            Assert.Single(driver.FindElements(By.CssSelector($"#donationHistory a[href='/Payment?publicDonationId={pendingId}']")));
            Assert.Empty(driver.FindElements(By.CssSelector($"#donationHistory a[href='/Payment?publicDonationId={recurringPendingId}']")));
            Assert.Contains("Pagamento pendente", driver.FindElement(By.Id("donationHistory")).Text);
            Assert.DoesNotContain("WaitingPayment", driver.FindElement(By.Id("donationHistory")).Text);

            var summary = driver.FindElements(By.CssSelector(".account-history-stat dd"));
            Assert.Equal("10,00 €", summary[0].Text);
            Assert.Equal("2", summary[1].Text);

            driver.FindElement(By.CssSelector("#donationHistory_filter input")).SendKeys(pendingId.ToString());
            wait.Until(d => d.FindElements(By.CssSelector("#donationHistory tbody tr")).Count == 1
                && d.FindElements(By.CssSelector($"#donationHistory a[href='/Payment?publicDonationId={pendingId}']")).Count == 1);
        }

        [Fact]
        [Trait("Category", "LocalBrowser")]
        public async Task DonationHistory_EmptyAccountHidesFirstDateAndReportsLoadFailure()
        {
            if (browserApplication == null || verificationScope == null)
            {
                throw new InvalidOperationException("Donation history checks require the self-contained browser host.");
            }

            var email = $"empty-history-{Guid.NewGuid():N}@example.test";
            await IntegrationTestDataSeeder.EnsureUserAsync(verificationScope.ServiceProvider, email, IntegrationTestCredentials.DefaultPassword);
            Login(email, IntegrationTestCredentials.DefaultPassword);
            driver.Navigate().GoToUrl(baseUrl + "/Identity/Account/Manage/DonationHistory?culture=en&ui-culture=en");
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(20));
            WaitForDonationHistory(wait, "#donationHistory .dataTables_empty", 1);
            Assert.Equal("—", driver.FindElements(By.CssSelector(".account-history-stat dd"))[2].Text);

            js.ExecuteScript("jQuery('#donationHistory').DataTable().ajax.url('/missing-donation-history-handler').load();");
            var error = wait.Until(d => d.FindElements(By.Id("donation-history-load-error")).SingleOrDefault(e => e.Displayed));
            Assert.Equal("Could not load donation history. Refresh the page and try again.", error.Text);
        }

        private void SetAccountHistoryViewport(int width)
        {
            driver.Manage().Window.Size = new Size(width, 900);
            Assert.IsType<ChromeDriver>(driver).ExecuteCdpCommand("Emulation.setDeviceMetricsOverride", new Dictionary<string, object?>
            {
                ["width"] = width,
                ["height"] = 900,
                ["deviceScaleFactor"] = 1,
                ["mobile"] = width < 576,
            });
        }

        private void WaitForDonationHistory(WebDriverWait wait, string selector, int count)
        {
            try
            {
                wait.Until(d => d.FindElements(By.CssSelector(selector)).Count == count);
            }
            catch (WebDriverTimeoutException exception)
            {
                string state = ScriptResult<string>(
                    "return JSON.stringify({table:!!document.getElementById('donationHistory'),initialized:jQuery.fn.dataTable.isDataTable('#donationHistory'),rows:document.querySelectorAll('#donationHistory tbody tr').length,error:document.querySelector('#donation-history-load-error')?.innerText});");
                string browserErrors = string.Join("\n", driver.Manage().Logs.GetLog(LogType.Browser).Select(entry => entry.Message));
                throw new InvalidOperationException("Donation history did not load: " + state + "\n" + browserErrors, exception);
            }
        }
    }
}
