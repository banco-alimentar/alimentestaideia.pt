// -----------------------------------------------------------------------
// <copyright file="ReferralLifecycleTestSupport.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.SeleniumUITest
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using OpenQA.Selenium;
    using OpenQA.Selenium.Interactions;
    using Xunit;

    public partial class AutomatedUITests
    {
        private const string LifecycleEditPath = "/Identity/Account/Manage/CampaignEdit";
        private const string LifecycleDetailPath = "/Identity/Account/Manage/CampaignDetail";
        private const string LifecycleEditForm = "form[enctype='multipart/form-data']";

        private void LifecycleRequireOwnedHost()
        {
            Assert.True(settings.IsSelfContained,
                "LocalBrowser lifecycle cases require the self-contained SQLite host before any fixture mutation.");
            Assert.NotNull(browserApplication);
            Assert.True(new Uri(baseUrl).IsLoopback);
            browserApplication!.AssertOwnsDatabase(myApplicationDbContext);
        }

        private Referral LifecycleRead(ReferralReservation reservation)
        {
            LifecycleRequireOwnedHost();
            var referral = myApplicationDbContext.Referrals.AsNoTracking().Include(r => r.User)
                .Single(r => r.Id == reservation.Id && r.Code == reservation.Code);
            Assert.Equal(reservation.OwnerId, referral.User.Id);
            return referral;
        }

        private void LifecycleAssertUnchanged(Referral expected, ReferralReservation reservation)
        {
            var actual = LifecycleRead(reservation);
            Assert.Equal(expected.Id, actual.Id);
            Assert.Equal(expected.Code, actual.Code);
            Assert.Equal(expected.User.Id, actual.User.Id);
            Assert.Equal(expected.Name, actual.Name);
            Assert.Equal(expected.TagLine, actual.TagLine);
            Assert.Equal(expected.Active, actual.Active);
            Assert.Equal(expected.IsPublic, actual.IsPublic);
            Assert.Equal(expected.ImageUrl, actual.ImageUrl);
            Assert.Equal(expected.CreateDate, actual.CreateDate);
            Assert.Equal(expected.LinkOpenCount, actual.LinkOpenCount);
        }

        private void LifecycleOpenEdit(ReferralReservation reservation)
        {
            OpenReferralPage(LifecycleEditPath + "?id=" + reservation.Id);
            ReferralWait().Until(d => d.FindElements(By.Id("Referral_Name")).Any(e => e.Displayed));
            Assert.Equal(reservation.Id!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
                driver.FindElement(By.Id("Referral_Id")).GetAttribute("value"));
        }

        private void LifecycleSetCheckbox(string id, bool selected)
        {
            var checkbox = driver.FindElement(By.Id(id));
            if (checkbox.Selected != selected)
            {
                checkbox.Click();
            }
        }

        private void LifecycleSaveEdit()
        {
            var form = driver.FindElement(By.CssSelector(LifecycleEditForm));
            driver.FindElement(By.CssSelector(LifecycleEditForm + " input[type='submit']")).Click();
            WaitForReferralPageReplacement(form);
            Assert.Equal(CampaignHistoryPath, new Uri(driver.Url).AbsolutePath);
        }

        private IWebElement LifecycleCard(ReferralReservation reservation) =>
            ReferralWait().Until(d => d.FindElements(By.CssSelector(".card"))
                .SingleOrDefault(c => c.FindElements(By.CssSelector(".card-header-title"))
                    .Any(title => title.Text.Trim() == reservation.Code)));

        private void LifecycleAssertPresentation(ReferralReservation reservation, string name, string tagline)
        {
            OpenReferralPage("/Referral/" + reservation.Code);
            WaitForDonationRedirect();
            Assert.Equal(name, driver.FindElement(By.CssSelector(".referral-campaign-title")).Text.Trim());
            Assert.Equal(tagline, driver.FindElement(By.CssSelector(".referral-campaign-tagline")).Text.Trim());
        }

        private HttpClient LifecycleAuthenticatedClient()
        {
            LifecycleRequireOwnedHost();
            var cookies = new CookieContainer();
            var origin = new Uri(baseUrl);
            foreach (var cookie in driver.Manage().Cookies.AllCookies)
            {
                cookies.Add(origin, new System.Net.Cookie(cookie.Name, cookie.Value, cookie.Path));
            }

            return new HttpClient(new HttpClientHandler { CookieContainer = cookies, AllowAutoRedirect = false })
            {
                BaseAddress = origin,
                Timeout = TimeSpan.FromSeconds(20),
            };
        }

        private string LifecycleWriteUpload(string extension, bool oversized = false, bool alternate = false)
        {
            LifecycleRequireOwnedHost();
            string directory = Path.Combine(browserApplication!.UploadRoot, "test-inputs");
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, Guid.NewGuid().ToString("N") + extension);
            // Valid one-pixel PNG and GIF images; the oversized PNG stays below the page's 3 MB request limit.
            byte[] bytes = extension == ".txt"
                ? System.Text.Encoding.UTF8.GetBytes("Unsupported referral upload")
                : Convert.FromBase64String(alternate
                    ? "R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7"
                    : "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jRZkAAAAASUVORK5CYII=");
            if (oversized)
            {
                Array.Resize(ref bytes, (2 * 1024 * 1024) + 1);
            }

            File.WriteAllBytes(path, bytes);
            return path;
        }

        private string LifecycleUploadedPath(string storedKey)
        {
            LifecycleRequireOwnedHost();
            Assert.StartsWith("uploads/referrals/", storedKey);
            string path = Path.GetFullPath(Path.Combine(browserApplication!.UploadRoot, storedKey));
            Assert.StartsWith(Path.GetFullPath(browserApplication.UploadRoot) + Path.DirectorySeparatorChar, path);
            return path;
        }

        private string LifecycleUploadImage(ReferralReservation reservation, string file)
        {
            LifecycleOpenEdit(reservation);
            driver.FindElement(By.Id("ImageUpload")).SendKeys(file);
            LifecycleSaveEdit();
            string key = LifecycleRead(reservation).ImageUrl;
            Assert.False(string.IsNullOrWhiteSpace(key));
            Assert.Equal(File.ReadAllBytes(file), File.ReadAllBytes(LifecycleUploadedPath(key)));
            return key;
        }

        private async Task LifecycleAssertImageBytesAsync(string storedKey, byte[] expected)
        {
            using var client = LifecycleAuthenticatedClient();
            using var response = await client.GetAsync("/" + storedKey);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expected, await response.Content.ReadAsByteArrayAsync());
        }

        private void LifecycleTabTo(By selector)
        {
            var target = driver.FindElement(selector);
            for (int count = 0; count < 120; count++)
            {
                if (target.Equals(driver.SwitchTo().ActiveElement()))
                {
                    Assert.True(target.Displayed && target.Enabled, "Keyboard focus must reach an enabled visible control.");
                    return;
                }

                new Actions(driver).SendKeys(Keys.Tab).Perform();
            }

            Assert.Fail("The control was not reachable using Tab: " + selector);
        }

        private void LifecycleKeyboardReplace(string value)
        {
            string modifier = OperatingSystem.IsMacOS() ? Keys.Command : Keys.Control;
            new Actions(driver).KeyDown(modifier).SendKeys("a").KeyUp(modifier).SendKeys(value).Perform();
        }

        private void LifecycleAssertInViewport(IWebElement element)
        {
            ReferralWait().Until(_ => ScriptResult<bool>(
                "var r = arguments[0].getBoundingClientRect(); return r.top >= -1 && r.bottom <= innerHeight + 1 && r.left >= -1 && r.right <= innerWidth + 1;",
                element));
            Assert.True(element.Displayed);
        }

        private sealed class LifecycleSecondUser : IDisposable
        {
            private readonly AutomatedUITests test;
            private readonly string id;

            public LifecycleSecondUser(AutomatedUITests test)
            {
                this.test = test;
                test.LifecycleRequireOwnedHost();
                Email = "referral-other-" + Guid.NewGuid().ToString("N") + "@example.test";
                Password = "Referral-" + Guid.NewGuid().ToString("N") + "!9aA";
                using var scope = test.browserApplication!.Services.CreateScope();
                var manager = scope.ServiceProvider.GetRequiredService<UserManager<WebUser>>();
                var user = new WebUser { UserName = Email, Email = Email, EmailConfirmed = true, FullName = "Referral second user" };
                var result = manager.CreateAsync(user, Password).GetAwaiter().GetResult();
                Assert.True(result.Succeeded, string.Join(", ", result.Errors.Select(e => e.Code)));
                id = user.Id;
                Assert.Empty(manager.GetRolesAsync(user).GetAwaiter().GetResult());
            }

            public string Email { get; }

            public string Password { get; }

            public string Id => id;

            public void Dispose()
            {
                test.LifecycleRequireOwnedHost();
                using var scope = test.browserApplication!.Services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                test.browserApplication.AssertOwnsDatabase(context);
                Assert.False(context.Referrals.Any(r => r.User.Id == id));
                Assert.False(context.Donations.Any(d => d.User.Id == id));
                var manager = scope.ServiceProvider.GetRequiredService<UserManager<WebUser>>();
                var user = manager.FindByIdAsync(id).GetAwaiter().GetResult();
                Assert.NotNull(user);
                Assert.Equal(Email, user!.Email);
                var result = manager.DeleteAsync(user).GetAwaiter().GetResult();
                Assert.True(result.Succeeded, string.Join(", ", result.Errors.Select(e => e.Code)));
            }
        }
    }
}
