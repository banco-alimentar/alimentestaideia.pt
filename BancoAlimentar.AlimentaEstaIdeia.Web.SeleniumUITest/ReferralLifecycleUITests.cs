// -----------------------------------------------------------------------
// <copyright file="ReferralLifecycleUITests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.SeleniumUITest
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using OpenQA.Selenium;
    using OpenQA.Selenium.Interactions;
    using Xunit;

    public partial class AutomatedUITests
    {
        [Theory]
        [InlineData(LifecycleDetailPath)]
        [InlineData(LifecycleEditPath)]
        [Trait("Category", "LocalBrowser")]
        [Trait("Scenario", "ReferralOwnership")]
        public async Task ReferralLifecycle_AnotherUserCannotGetOwnedCampaign(string path)
        {
            LifecycleRequireOwnedHost();
            using var fixtures = new ReferralFixtures(this);
            var reservation = fixtures.Create();
            var original = LifecycleRead(reservation);
            Login();
            using (var owner = LifecycleAuthenticatedClient())
            using (var allowed = await owner.GetAsync(path + "?id=" + reservation.Id))
            {
                Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
                Assert.Contains(reservation.Code, await allowed.Content.ReadAsStringAsync());
            }

            OpenReferralPage(path + "?id=" + reservation.Id);
            Assert.Equal(path, new Uri(driver.Url).AbsolutePath);

            using var other = new LifecycleSecondUser(this);
            driver.Manage().Cookies.DeleteAllCookies();
            Login(other.Email, other.Password);
            using var client = LifecycleAuthenticatedClient();
            using var history = await client.GetAsync(CampaignHistoryPath);
            Assert.Equal(HttpStatusCode.OK, history.StatusCode);

            using var rejected = await client.GetAsync(path + "?id=" + reservation.Id);

            if (path == LifecycleDetailPath)
            {
                Assert.Equal(HttpStatusCode.Redirect, rejected.StatusCode);
                Assert.Equal(CampaignHistoryPath, new Uri(new Uri(baseUrl), rejected.Headers.Location!).AbsolutePath);
            }
            else
            {
                Assert.Equal(HttpStatusCode.NotFound, rejected.StatusCode);
            }

            Assert.DoesNotContain(original.TagLine, await rejected.Content.ReadAsStringAsync());
            OpenReferralPage(path + "?id=" + reservation.Id);
            Assert.Equal(path == LifecycleDetailPath ? CampaignHistoryPath : LifecycleEditPath, new Uri(driver.Url).AbsolutePath);
            Assert.DoesNotContain(original.TagLine, driver.FindElement(By.TagName("body")).Text);
            Assert.Empty(driver.FindElements(By.Id("Referral_Name")));
            LifecycleAssertUnchanged(original, reservation);
        }

        [Fact]
        [Trait("Category", "LocalBrowser")]
        [Trait("Scenario", "ReferralOwnership")]
        public async Task ReferralLifecycle_ForgedEditWithRealAntiforgeryCannotChangeAnotherUsersCampaign()
        {
            LifecycleRequireOwnedHost();
            using var fixtures = new ReferralFixtures(this);
            var reservation = fixtures.Create();
            var original = LifecycleRead(reservation);
            using var other = new LifecycleSecondUser(this);
            Login(other.Email, other.Password);
            OpenReferralPage(CampaignHistoryPath);
            string token = RequiredAttribute(driver.FindElement(By.CssSelector("#create_section input[name='__RequestVerificationToken']")), "value");
            Assert.False(string.IsNullOrEmpty(token));
            using var client = LifecycleAuthenticatedClient();
            using var payload = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = token,
                ["Referral.Id"] = reservation.Id!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["Referral.Code"] = "forged" + reservation.Code,
                ["Referral.User.Id"] = other.Id,
                ["Referral.Name"] = "Unauthorized campaign name",
                ["Referral.TagLine"] = "Unauthorized campaign tagline",
                ["Referral.Active"] = "false",
                ["Referral.IsPublic"] = "true",
                ["RemoveImage"] = "true",
            });

            using var rejected = await client.PostAsync(LifecycleEditPath, payload);

            // A 400 would only prove an antiforgery failure, so require the ownership handler's 404.
            Assert.Equal(HttpStatusCode.NotFound, rejected.StatusCode);
            LifecycleAssertUnchanged(original, reservation);
            Assert.False(myApplicationDbContext.Referrals.AsNoTracking().Any(r => r.User.Id == other.Id));
        }

        [Fact]
        [Trait("Category", "LocalBrowser")]
        [Trait("Scenario", "ReferralOwnership")]
        public async Task ReferralLifecycle_NormalUserCannotAccessAdminReferralList()
        {
            LifecycleRequireOwnedHost();
            using var fixtures = new ReferralFixtures(this);
            var reservation = fixtures.Create();
            var original = LifecycleRead(reservation);
            Login();
            using var client = LifecycleAuthenticatedClient();
            using var history = await client.GetAsync(CampaignHistoryPath);
            Assert.Equal(HttpStatusCode.OK, history.StatusCode);

            using var rejected = await client.GetAsync("/Admin/Referrals");

            Assert.Equal(HttpStatusCode.Redirect, rejected.StatusCode);
            Assert.Equal("/Identity/Account/AccessDenied", new Uri(new Uri(baseUrl), rejected.Headers.Location!).AbsolutePath);
            Assert.DoesNotContain(reservation.Code, await rejected.Content.ReadAsStringAsync());
            OpenReferralPage("/Admin/Referrals");
            Assert.Equal("/Identity/Account/AccessDenied", new Uri(driver.Url).AbsolutePath);
            Assert.DoesNotContain(reservation.Code, driver.FindElement(By.TagName("body")).Text);
            LifecycleAssertUnchanged(original, reservation);
        }

        [Fact]
        [Trait("Category", "LocalBrowser")]
        [Trait("Scenario", "ReferralEditing")]
        public void ReferralLifecycle_EditNameAndTaglinePersistsAfterReloadAndUpdatesPublicPresentation()
        {
            LifecycleRequireOwnedHost();
            using var fixtures = new ReferralFixtures(this);
            var reservation = fixtures.Create();
            var original = LifecycleRead(reservation);
            string name = "Campanha editada " + reservation.Code;
            string tagline = "Alimentos para todos — " + reservation.Code;
            Login();
            LifecycleOpenEdit(reservation);
            SetTextInput(By.Id("Referral_Name"), name);
            SetTextInput(By.Id("Referral_TagLine"), tagline);

            LifecycleSaveEdit();

            var persisted = LifecycleRead(reservation);
            Assert.Equal(name, persisted.Name);
            Assert.Equal(tagline, persisted.TagLine);
            Assert.Equal(original.Code, persisted.Code);
            Assert.Equal(original.ImageUrl, persisted.ImageUrl);
            LifecycleOpenEdit(reservation);
            driver.Navigate().Refresh();
            Assert.Equal(name, driver.FindElement(By.Id("Referral_Name")).GetAttribute("value"));
            Assert.Equal(tagline, driver.FindElement(By.Id("Referral_TagLine")).GetAttribute("value"));
            driver.Manage().Cookies.DeleteAllCookies();
            LifecycleAssertPresentation(reservation, name, tagline);
        }

        [Fact]
        [Trait("Category", "LocalBrowser")]
        [Trait("Scenario", "ReferralEditing")]
        public void ReferralLifecycle_DeactivateAndReactivateChangesPublicLinkAndTracksOnlyActiveOpenings()
        {
            LifecycleRequireOwnedHost();
            using var fixtures = new ReferralFixtures(this);
            var reservation = fixtures.Create();
            var original = LifecycleRead(reservation);
            LifecycleAssertPresentation(reservation, original.Name, original.TagLine);
            ReferralWait().Until(_ => ReadReferralOpenCount(reservation) == 1);
            Login();
            LifecycleOpenEdit(reservation);
            LifecycleSetCheckbox("Referral_Active", false);
            LifecycleSaveEdit();
            Assert.False(LifecycleRead(reservation).Active);
            Assert.Contains("Não activa", LifecycleCard(reservation).Text);
            driver.Manage().Cookies.DeleteAllCookies();

            OpenReferralPage("/Referral/" + reservation.Code);
            AssertInactiveReferralPage(reservation.Code);
            Assert.Equal(1, ReadReferralOpenCount(reservation));
            Assert.Single(myApplicationDbContext.ReferralLinkOpens.AsNoTracking().Where(o => o.ReferralId == reservation.Id));

            Login();
            LifecycleOpenEdit(reservation);
            Assert.False(driver.FindElement(By.Id("Referral_Active")).Selected);
            LifecycleSetCheckbox("Referral_Active", true);
            LifecycleSaveEdit();
            Assert.True(LifecycleRead(reservation).Active);
            driver.Manage().Cookies.DeleteAllCookies();
            LifecycleAssertPresentation(reservation, original.Name, original.TagLine);
            ReferralWait().Until(_ => ReadReferralOpenCount(reservation) == 2
                && myApplicationDbContext.ReferralLinkOpens.Count(o => o.ReferralId == reservation.Id) == 2);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [Trait("Category", "LocalBrowser")]
        [Trait("Scenario", "ReferralEditing")]
        public void ReferralLifecycle_PublicAndPrivateSettingsPersistAndKeepDirectLinkUsable(bool isPublic)
        {
            LifecycleRequireOwnedHost();
            using var fixtures = new ReferralFixtures(this);
            var reservation = fixtures.Create();
            Login();
            // Save both transitions, so the private case cannot pass by retaining its fixture default.
            foreach (bool value in new[] { !isPublic, isPublic })
            {
                LifecycleOpenEdit(reservation);
                LifecycleSetCheckbox("Referral_IsPublic", value);
                LifecycleSaveEdit();
                Assert.Equal(value, LifecycleRead(reservation).IsPublic);
                Assert.Contains(value ? "Publica" : "Privada", LifecycleCard(reservation).Text);
            }

            LifecycleOpenEdit(reservation);
            driver.Navigate().Refresh();
            Assert.Equal(isPublic, driver.FindElement(By.Id("Referral_IsPublic")).Selected);
            var persisted = LifecycleRead(reservation);
            driver.Manage().Cookies.DeleteAllCookies();
            OpenReferralPage("/Referral/Overview/" + reservation.Code);
            if (isPublic)
            {
                Assert.NotEmpty(driver.FindElements(By.CssSelector(".as-admin-basket li")));
                Assert.Equal(persisted.Name, driver.FindElement(By.CssSelector(".card-header-title")).Text.Trim());
                Assert.DoesNotContain("Campanha privada.", driver.FindElement(By.TagName("body")).Text);
            }
            else
            {
                Assert.Contains("Campanha privada. Por favor peça ao dono da campanha", driver.FindElement(By.TagName("body")).Text);
                Assert.Empty(driver.FindElements(By.CssSelector(".as-admin-basket, .card-subtitle, .referral-campaign-title")));
            }

            LifecycleAssertPresentation(reservation, persisted.Name, persisted.TagLine);
        }

        [Fact]
        [Trait("Category", "LocalBrowser")]
        [Trait("Scenario", "ReferralImages")]
        public async Task ReferralLifecycle_ImageUploadReplaceAndRemoveUpdatesStoredBytesAndPublicDisplay()
        {
            LifecycleRequireOwnedHost();
            using var fixtures = new ReferralFixtures(this);
            var reservation = fixtures.Create();
            var original = LifecycleRead(reservation);
            Login();
            string firstFile = LifecycleWriteUpload(".png");
            string firstKey = LifecycleUploadImage(reservation, firstFile);
            await LifecycleAssertImageBytesAsync(firstKey, File.ReadAllBytes(firstFile));
            LifecycleAssertPresentation(reservation, original.Name, original.TagLine);
            WaitForLoadedReferralImage(driver.FindElement(By.CssSelector(".referral-campaign-image img")));

            string secondFile = LifecycleWriteUpload(".gif", alternate: true);
            string secondKey = LifecycleUploadImage(reservation, secondFile);
            Assert.NotEqual(firstKey, secondKey);
            Assert.False(File.Exists(LifecycleUploadedPath(firstKey)));
            await LifecycleAssertImageBytesAsync(secondKey, File.ReadAllBytes(secondFile));
            LifecycleOpenEdit(reservation);
            WaitForLoadedReferralImage(driver.FindElement(By.CssSelector(LifecycleEditForm + " img")));
            LifecycleAssertPresentation(reservation, original.Name, original.TagLine);
            var replacementImage = driver.FindElement(By.CssSelector(".referral-campaign-image img"));
            Assert.Equal(baseUrl + "/" + secondKey, replacementImage.GetAttribute("src"));
            WaitForLoadedReferralImage(replacementImage);

            LifecycleOpenEdit(reservation);
            LifecycleSetCheckbox("RemoveImage", true);
            LifecycleSaveEdit();

            Assert.True(string.IsNullOrEmpty(LifecycleRead(reservation).ImageUrl));
            Assert.False(File.Exists(LifecycleUploadedPath(secondKey)));
            LifecycleOpenEdit(reservation);
            Assert.Empty(driver.FindElements(By.CssSelector(LifecycleEditForm + " img, #RemoveImage")));
            LifecycleAssertPresentation(reservation, original.Name, original.TagLine);
            Assert.Empty(driver.FindElements(By.CssSelector(".referral-campaign-image img")));
        }

        [Theory]
        [InlineData(false, "Only JPG, PNG, GIF, and WebP images are allowed.")]
        [InlineData(true, "Referral images must be 2 MB or smaller.")]
        [Trait("Category", "LocalBrowser")]
        [Trait("Scenario", "ReferralImages")]
        public async Task ReferralLifecycle_RejectedImagePreservesExistingUsableImage(bool oversized, string feedback)
        {
            LifecycleRequireOwnedHost();
            using var fixtures = new ReferralFixtures(this);
            var reservation = fixtures.Create();
            Login();
            string file = LifecycleWriteUpload(".png");
            byte[] originalBytes = File.ReadAllBytes(file);
            string originalKey = LifecycleUploadImage(reservation, file);
            string originalPath = LifecycleUploadedPath(originalKey);
            var original = LifecycleRead(reservation);
            string directory = Path.GetDirectoryName(originalPath)!;
            string[] filesBefore = Directory.GetFiles(directory).OrderBy(p => p).ToArray();
            string invalidFile = LifecycleWriteUpload(oversized ? ".png" : ".txt", oversized);
            LifecycleOpenEdit(reservation);
            driver.FindElement(By.Id("ImageUpload")).SendKeys(invalidFile);
            var form = driver.FindElement(By.CssSelector(LifecycleEditForm));

            driver.FindElement(By.CssSelector(LifecycleEditForm + " input[type='submit']")).Click();

            WaitForReferralPageReplacement(form);
            Assert.Equal(LifecycleEditPath, new Uri(driver.Url).AbsolutePath);
            ReferralWait().Until(d => d.FindElements(By.CssSelector(".validation-summary-errors li"))
                .Any(e => e.Displayed && e.Text.Trim() == feedback));
            LifecycleAssertUnchanged(original, reservation);
            Assert.True(File.Exists(originalPath), "Rejected replacement deleted the existing usable referral image: " + feedback);
            Assert.Equal(filesBefore, Directory.GetFiles(directory).OrderBy(p => p).ToArray());
            Assert.Equal(originalBytes, File.ReadAllBytes(originalPath));
            await LifecycleAssertImageBytesAsync(originalKey, originalBytes);
            LifecycleOpenEdit(reservation);
            driver.Navigate().Refresh();
            var image = driver.FindElement(By.CssSelector(LifecycleEditForm + " img"));
            Assert.Equal(baseUrl + "/" + originalKey, image.GetAttribute("src"));
            WaitForLoadedReferralImage(image);
            LifecycleAssertPresentation(reservation, original.Name, original.TagLine);
            WaitForLoadedReferralImage(driver.FindElement(By.CssSelector(".referral-campaign-image img")));
        }

        [Theory]
        [InlineData(390, 844)]
        [InlineData(1000, 700)]
        [Trait("Category", "LocalBrowser")]
        [Trait("Scenario", "ReferralKeyboard")]
        public void ReferralLifecycle_KeyboardCreationValidationDuplicateFeedbackAndEditingAtViewport(int width, int height)
        {
            LifecycleRequireOwnedHost();
            using var fixtures = new ReferralFixtures(this);
            fixtures.Create();
            fixtures.Create();
            var reservation = fixtures.Reserve();
            Login();
            driver.Manage().Window.Size = new System.Drawing.Size(width, height);
            OpenReferralPage(CampaignHistoryPath);
            if (width < 500)
            {
                Assert.InRange(Convert.ToInt64(js.ExecuteScript("return innerWidth;")), 1, 500);
            }

            LifecycleTabTo(By.Name("Code"));
            LifecycleTabTo(By.Id("create-campaign-submit"));
            new Actions(driver).SendKeys(Keys.Enter).Perform();
            var validation = ReferralWait().Until(d => d.FindElements(By.CssSelector("[data-valmsg-for='Code'].field-validation-error"))
                .SingleOrDefault(e => e.Displayed && !string.IsNullOrWhiteSpace(e.Text)));
            Assert.Equal(driver.FindElement(By.Name("Code")).GetAttribute("data-val-required"), validation.Text.Trim());
            Assert.False(myApplicationDbContext.Referrals.Any(r => r.Code == reservation.Code));
            LifecycleTabTo(By.Name("Code"));
            LifecycleKeyboardReplace(reservation.Code);
            LifecycleTabTo(By.Id("create-campaign-submit"));
            var form = driver.FindElement(By.Id("create_section"));
            reservation.ExpectInsert = true;

            new Actions(driver).SendKeys(Keys.Enter).Perform();

            WaitForReferralPageReplacement(form);
            var created = WaitForOwnedReferral(reservation);
            Assert.True(created.Active && created.IsPublic);
            Assert.Equal(reservation.OwnerId, created.User.Id);
            Assert.True(LifecycleCard(reservation).Displayed);

            LifecycleTabTo(By.Name("Code"));
            LifecycleKeyboardReplace(reservation.Code.ToUpperInvariant());
            LifecycleTabTo(By.Id("create-campaign-submit"));
            form = driver.FindElement(By.Id("create_section"));
            // Keep genuine keyboard focus on submit while moving the viewport away from feedback.
            js.ExecuteScript("window.scrollTo(0, 0);");
            ReferralWait().Until(_ => ScriptResult<bool>(
                "return scrollY === 0 && arguments[0].getBoundingClientRect().top > innerHeight;", form));
            new Actions(driver).SendKeys(Keys.Enter).Perform();
            WaitForReferralPageReplacement(form);
            var warning = ReferralWait().Until(d => d.FindElements(By.CssSelector("#create_section > .text-danger"))
                .SingleOrDefault(e => e.Displayed && e.Text.Contains("A campanha com o código especificado já existe")));
            LifecycleAssertInViewport(warning);
            LifecycleAssertUnchanged(created, reservation);
            Assert.Equal(1, myApplicationDbContext.Referrals.Count(r => r.Code.ToLower() == reservation.Code.ToLower()));

            // Follow the card's edit link and complete the real form entirely through keyboard actions.
            LifecycleTabTo(By.CssSelector("a[href*='CampaignEdit'][href*='id=" + reservation.Id + "']"));
            new Actions(driver).SendKeys(Keys.Enter).Perform();
            ReferralWait().Until(d => d.FindElements(By.Id("Referral_Name")).Any(e => e.Displayed));
            string name = "Teclado " + reservation.Code;
            string tagline = "Campanha criada e editada pelo teclado";
            LifecycleTabTo(By.Id("Referral_Name"));
            LifecycleKeyboardReplace(name);
            new Actions(driver).SendKeys(Keys.Tab).Perform();
            Assert.Equal("Referral_TagLine", driver.SwitchTo().ActiveElement().GetAttribute("id"));
            LifecycleKeyboardReplace(tagline);
            LifecycleTabTo(By.Id("Referral_IsPublic"));
            new Actions(driver).SendKeys(Keys.Space).Perform();
            LifecycleTabTo(By.CssSelector(LifecycleEditForm + " input[type='submit']"));
            form = driver.FindElement(By.CssSelector(LifecycleEditForm));
            new Actions(driver).SendKeys(Keys.Enter).Perform();
            WaitForReferralPageReplacement(form);
            Assert.Equal(CampaignHistoryPath, new Uri(driver.Url).AbsolutePath);
            var updated = LifecycleRead(reservation);
            Assert.Equal(name, updated.Name);
            Assert.Equal(tagline, updated.TagLine);
            Assert.True(updated.Active);
            Assert.False(updated.IsPublic);
            LifecycleOpenEdit(reservation);
            driver.Navigate().Refresh();
            Assert.Equal(name, driver.FindElement(By.Id("Referral_Name")).GetAttribute("value"));
            Assert.Equal(tagline, driver.FindElement(By.Id("Referral_TagLine")).GetAttribute("value"));
            Assert.False(driver.FindElement(By.Id("Referral_IsPublic")).Selected);
        }
    }
}
