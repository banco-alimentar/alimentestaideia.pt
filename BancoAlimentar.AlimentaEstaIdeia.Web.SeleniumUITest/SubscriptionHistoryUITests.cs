// -----------------------------------------------------------------------
// <copyright file="SubscriptionHistoryUITests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.SeleniumUITest
{
    using System;
    using System.Collections.Generic;
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
        public async Task SubscriptionHistory_ContainsTableAndPreservesDatesAndDetails(int width)
        {
            if (browserApplication == null || verificationScope == null)
            {
                throw new InvalidOperationException("Subscription fixtures require the self-contained browser host.");
            }

            browserApplication.AssertOwnsDatabase(myApplicationDbContext);
            browserApplication.Configuration["FeatureManagement:SubscriptionAdmin"] = "true";
            var services = verificationScope.ServiceProvider;
            var email = $"subscription-history-{Guid.NewGuid():N}@example.test";
            var owner = await IntegrationTestDataSeeder.EnsureUserAsync(services, email, IntegrationTestCredentials.DefaultPassword);
            var statuses = new[] { SubscriptionStatus.Active, SubscriptionStatus.Inactive, SubscriptionStatus.Capture, SubscriptionStatus.Error };
            var subscriptions = new List<Subscription>();
            for (int index = 0; index < statuses.Length; index++)
            {
                var donation = (await IntegrationTestDataSeeder.SeedPendingDonationWithCreditCardAsync(services, Guid.NewGuid())).Donation;
                donation.User = owner;
                donation.DonationAmount = 5 * (index + 1);
                var subscription = new Subscription
                {
                    PublicId = Guid.NewGuid(),
                    User = owner,
                    InitialDonation = donation,
                    Created = new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(index),
                    StartTime = index == 3 ? default : new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    ExpirationTime = index == 3 ? default : new DateTime(2028, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    Status = statuses[index],
                    SubscriptionType = index == 3 ? SubscriptionPaymentType.DD : SubscriptionPaymentType.CC,
                    Frequency = index % 2 == 0 ? "_1M" : "_1Y",
                    TransactionKey = Guid.NewGuid().ToString(),
                    EasyPaySubscriptionId = Guid.NewGuid().ToString(),
                    Url = baseUrl,
                };
                myApplicationDbContext.Subscriptions.Add(subscription);
                subscriptions.Add(subscription);
            }

            await myApplicationDbContext.SaveChangesAsync();
            SetAccountHistoryViewport(width);
            Login(email, IntegrationTestCredentials.DefaultPassword);
            driver.Navigate().GoToUrl(baseUrl + "/Identity/Account/Manage/Subscriptions?culture=pt&ui-culture=pt");
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(20));
            wait.Until(d => d.FindElements(By.CssSelector("#subscriptions tbody tr")).Count == 4);
            Assert.Equal(width, ScriptResult<long>("return window.innerWidth;"));

            var panel = driver.FindElement(By.CssSelector(".subscription-history .account-history-panel"));
            Assert.True(ScriptResult<bool>(
                "var panel = arguments[0].getBoundingClientRect(); var content = arguments[0].closest('.account-manage-content').getBoundingClientRect(); return panel.left >= content.left - 1 && panel.right <= content.right + 1 && document.documentElement.scrollWidth <= window.innerWidth + 1;",
                panel));
            var region = driver.FindElement(By.CssSelector(".subscription-history .account-history-table-region"));
            Assert.Equal("0", region.GetAttribute("tabindex"));
            if (width == 375)
            {
                Assert.True(ScriptResult<bool>("return arguments[0].scrollWidth > arguments[0].clientWidth;", region));
            }

            Assert.Equal(6, driver.FindElements(By.CssSelector("#subscriptions thead th")).Count);
            Assert.Equal("4", driver.FindElement(By.Id("subscriptions-total-count")).Text);
            Assert.Equal("1", driver.FindElement(By.Id("subscriptions-active-count")).Text);
            Assert.Equal("1", driver.FindElement(By.Id("subscriptions-inactive-count")).Text);
            var rows = driver.FindElements(By.CssSelector("#subscriptions tbody tr"));
            Assert.Contains(subscriptions[3].PublicId.ToString(), rows[0].FindElement(By.CssSelector("a")).GetAttribute("href"));
            Assert.Empty(rows[0].FindElements(By.CssSelector(".subscription-history-date")));
            Assert.Contains("Requer atenção", rows[0].Text);
            Assert.Contains("Débito direto", rows[0].Text);
            var activeRow = rows.Single(row => row.FindElement(By.CssSelector("a")).GetAttribute("href")?.Contains(subscriptions[0].PublicId.ToString()) == true);
            Assert.Contains("01/01/2027", activeRow.Text);
            Assert.Contains("01/01/2028", activeRow.Text);
            Assert.Contains("Cartão de Crédito", activeRow.Text);
            Assert.Contains("A aguardar ativação", driver.FindElement(By.Id("subscriptions")).Text);

            driver.FindElement(By.CssSelector("#subscriptions thead th")).Click();
            wait.Until(d => d.FindElements(By.CssSelector("#subscriptions tbody tr"))[0]
                .FindElement(By.CssSelector("a")).GetAttribute("href")?.Contains(subscriptions[0].PublicId.ToString()) == true);
            driver.FindElement(By.CssSelector("#subscriptions_filter input")).SendKeys("Débito direto");
            wait.Until(d => d.FindElements(By.CssSelector("#subscriptions tbody tr")).Count == 1);
            Assert.Contains(subscriptions[3].PublicId.ToString(), driver.FindElement(By.CssSelector("#subscriptions tbody a")).GetAttribute("href"));
            Assert.Equal("4", driver.FindElement(By.Id("subscriptions-total-count")).Text);
        }

        [Fact]
        [Trait("Category", "LocalBrowser")]
        public async Task SubscriptionHistory_EmptyAccountAndLoadFailureDoNotInventCounts()
        {
            if (browserApplication == null || verificationScope == null)
            {
                throw new InvalidOperationException("Subscription checks require the self-contained browser host.");
            }

            browserApplication.Configuration["FeatureManagement:SubscriptionAdmin"] = "true";
            var email = $"empty-subscriptions-{Guid.NewGuid():N}@example.test";
            await IntegrationTestDataSeeder.EnsureUserAsync(verificationScope.ServiceProvider, email, IntegrationTestCredentials.DefaultPassword);
            Login(email, IntegrationTestCredentials.DefaultPassword);
            driver.Navigate().GoToUrl(baseUrl + "/Identity/Account/Manage/Subscriptions?culture=en&ui-culture=en");
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(20));
            wait.Until(d => d.FindElements(By.CssSelector("#subscriptions .dataTables_empty")).Count == 1);
            Assert.Equal("0", driver.FindElement(By.Id("subscriptions-total-count")).Text);
            Assert.Equal("0", driver.FindElement(By.Id("subscriptions-active-count")).Text);
            Assert.Equal("0", driver.FindElement(By.Id("subscriptions-inactive-count")).Text);

            var chrome = Assert.IsType<ChromeDriver>(driver);
            chrome.ExecuteCdpCommand("Network.enable", new Dictionary<string, object?>());
            chrome.ExecuteCdpCommand("Network.setBlockedURLs", new Dictionary<string, object?>
            {
                ["urls"] = new[] { "*handler=DataTableData*" },
            });
            driver.Navigate().Refresh();
            var error = wait.Until(d => d.FindElements(By.Id("subscriptions-load-error")).SingleOrDefault(element => element.Displayed));
            Assert.Equal("Could not load subscriptions. Please refresh the page and try again.", error.Text);
            Assert.False(driver.FindElement(By.Id("subscriptions-summary")).Displayed);
            Assert.False(driver.FindElement(By.Id("subscriptions-loading")).Displayed);
            Assert.Equal("false", driver.FindElement(By.Id("subscriptions")).GetAttribute("aria-busy"));
        }
    }
}
