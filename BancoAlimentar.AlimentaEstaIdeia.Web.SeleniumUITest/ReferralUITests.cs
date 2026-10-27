// -----------------------------------------------------------------------
// <copyright file="ReferralUITests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.SeleniumUITest
{
    using System;
    using System.Linq;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using Microsoft.EntityFrameworkCore;
    using OpenQA.Selenium;
    using Xunit;

    public partial class AutomatedUITests
    {
        [Fact]
        [Trait("Category", "Referral")]
        public void Referral_CreateCampaign_ShowsOwnedCardAndLoadedQrImage()
        {
            using var fixtures = new ReferralFixtures(this);
            Login();
            OpenReferralPage(CampaignHistoryPath);
            var reservation = fixtures.Reserve(); // Register before the browser can POST.
            DateTime started = DateTime.UtcNow;

            SubmitCampaignCode(reservation.Code, reservation);

            var referral = WaitForOwnedReferral(reservation);
            Assert.True(referral.Active);
            Assert.True(referral.IsPublic);
            Assert.InRange(referral.CreateDate, started.AddDays(-1), DateTime.UtcNow.AddDays(1));
            var card = ReferralWait().Until(d => d.FindElements(By.CssSelector(".card"))
                .SingleOrDefault(c => c.FindElements(By.CssSelector(".card-header-title"))
                    .Any(h => h.Text.Trim() == reservation.Code)));
            var link = card.FindElement(By.CssSelector("a.card-link"));
            Assert.Equal(baseUrl + "/Referral/" + reservation.Code, link.GetAttribute("href"));
            var qr = card.FindElement(By.CssSelector("img[src^='data:image/']"));
            Assert.StartsWith("data:image/png;base64,", qr.GetAttribute("src"));
            WaitForLoadedReferralImage(qr);

            link.Click();
            WaitForDonationRedirect();
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        [Trait("Category", "Referral")]
        public void Referral_DuplicateCode_PreservesIdentityAndAutomaticallyScrollsWarning(bool uppercase)
        {
            using var fixtures = new ReferralFixtures(this);
            var campaign = fixtures.Create();
            // Two cards guarantee the form begins below the finite viewport, even for a new user.
            fixtures.Create();
            Login();
            OpenReferralPage(CampaignHistoryPath);
            var form = driver.FindElement(By.Id("create_section"));
            SetTextInput(By.Name("Code"), uppercase ? campaign.Code.ToUpperInvariant() : campaign.Code);
            js.ExecuteScript("window.scrollTo(0, 0);");
            ReferralWait().Until(_ => ScriptResult<bool>(
                "return window.scrollY === 0 && arguments[0].getBoundingClientRect().top > window.innerHeight;", form));

            // Submit from the top: WebDriver.Click would scroll the button and could mask a regression.
            js.ExecuteScript("arguments[0].requestSubmit(arguments[1]);", form, driver.FindElement(By.Id("create-campaign-submit")));
            WaitForReferralPageReplacement(form);
            var warning = ReferralWait().Until(d => d.FindElements(By.CssSelector("#create_section > .text-danger"))
                .SingleOrDefault(e => e.Displayed && e.Text.Contains("A campanha com o código especificado já existe")));
            try
            {
                ReferralWait().Until(_ => ScriptResult<bool>(
                    // scrollIntoView can round the final scroll position to an integral CSS pixel.
                    "var r = arguments[0].getBoundingClientRect(); return window.scrollY > 0 && r.top >= -1 && r.bottom <= window.innerHeight + 1 && r.left >= -1 && r.right <= window.innerWidth + 1;", warning));
            }
            catch (WebDriverTimeoutException)
            {
                string geometry = ScriptResult<string>("var r = arguments[0].getBoundingClientRect(); return JSON.stringify({scrollY:window.scrollY,width:window.innerWidth,height:window.innerHeight,top:r.top,bottom:r.bottom,left:r.left,right:r.right});", warning);
                throw new InvalidOperationException("Duplicate feedback was not fully visible after automatic scrolling: " + geometry);
            }

            var persisted = WaitForOwnedReferral(campaign);
            Assert.Equal(campaign.Id, persisted.Id);
            Assert.Equal(campaign.OwnerId, persisted.User.Id);
            Assert.Equal(campaign.Code, persisted.Code);
            Assert.True(persisted.Active);
            Assert.Equal(0, persisted.LinkOpenCount);
            Assert.Equal(0, myApplicationDbContext.ReferralLinkOpens.Count(o => o.ReferralId == campaign.Id));
        }

        [Theory]
        [InlineData("", "data-val-required")]
        [InlineData("ab", "data-val-length")]
        [InlineData("symbols", "data-val-regex")]
        [Trait("Category", "Referral")]
        public void Referral_InvalidCode_ShowsClientValidationWithoutPostingOrInserting(string invalidCode, string messageAttribute)
        {
            using var fixtures = new ReferralFixtures(this);
            var reservation = invalidCode == "symbols" ? fixtures.Reserve("!") : null;
            string code = reservation?.Code ?? invalidCode;
            Login();
            OpenReferralPage(CampaignHistoryPath);
            int before = myApplicationDbContext.Referrals.Count(r => r.User.Id == fixtures.OwnerId);
            int matchingBefore = myApplicationDbContext.Referrals.Count(r => r.Code == code);
            SetTextInput(By.Name("Code"), code);
            var form = driver.FindElement(By.Id("create_section"));
            ReferralWait().Until(_ => ScriptResult<bool>(
                "return !!(window.jQuery && jQuery(arguments[0]).data('validator'));", form));
            string marker = Guid.NewGuid().ToString("N");
            js.ExecuteScript(
                "window.referralValidationMarker = arguments[1]; window.referralSubmitCount = 0; arguments[0].addEventListener('submit', function(event) { if (!event.defaultPrevented) window.referralSubmitCount++; });",
                form, marker);

            if (reservation != null)
            {
                reservation.ExpectInsert = true;
            }

            driver.FindElement(By.Id("create-campaign-submit")).Click();

            var error = ReferralWait().Until(d => d.FindElements(By.CssSelector("[data-valmsg-for='Code'].field-validation-error"))
                .SingleOrDefault(e => e.Displayed && !string.IsNullOrWhiteSpace(e.Text)));
            Assert.False(string.IsNullOrWhiteSpace(error.Text));
            Assert.Equal(marker, js.ExecuteScript("return window.referralValidationMarker;"));
            Assert.Equal(0L, Convert.ToInt64(js.ExecuteScript("return window.referralSubmitCount;")));
            var input = driver.FindElement(By.Name("Code"));
            Assert.Equal(input.GetAttribute(messageAttribute), error.Text.Trim());
            Assert.True(ScriptResult<bool>("return !!jQuery(arguments[0]).data('validator').invalid.Code;", form));
            if (reservation != null)
            {
                reservation.ExpectInsert = false;
            }

            Assert.Equal(matchingBefore, myApplicationDbContext.Referrals.Count(r => r.Code == code));
            Assert.Equal(before, myApplicationDbContext.Referrals.Count(r => r.User.Id == fixtures.OwnerId));
        }

        [Fact]
        [Trait("Category", "Referral")]
        public void Referral_ActiveLink_ShowsCampaignAndRecordsExactlyOneOpening()
        {
            using var fixtures = new ReferralFixtures(this);
            var campaign = fixtures.Create();
            var expected = WaitForOwnedReferral(campaign);
            DateTime started = DateTime.UtcNow;

            OpenReferralPage("/Referral/" + campaign.Code);
            WaitForDonationRedirect();

            Assert.Equal(expected.Name, driver.FindElement(By.CssSelector(".referral-campaign-title")).Text.Trim());
            Assert.Equal(expected.TagLine, driver.FindElement(By.CssSelector(".referral-campaign-tagline")).Text.Trim());
            var image = driver.FindElement(By.CssSelector(".referral-campaign-image img"));
            Assert.Equal(expected.ImageUrl, image.GetAttribute("src"));
            WaitForLoadedReferralImage(image);
            ReferralWait().Until(_ => ReadReferralOpenCount(campaign) == 1
                && myApplicationDbContext.ReferralLinkOpens.Count(o => o.ReferralId == campaign.Id) == 1);
            var opening = Assert.Single(myApplicationDbContext.ReferralLinkOpens.AsNoTracking()
                .Where(o => o.ReferralId == campaign.Id).ToList());
            Assert.InRange(opening.OpenedAtUtc, started.AddSeconds(-5), DateTime.UtcNow.AddSeconds(5));
            Assert.Equal(1, ReadReferralOpenCount(campaign));
        }

        [Fact]
        [Trait("Category", "Referral")]
        public void Referral_InactiveLink_ShowsErrorWithoutTracking()
        {
            using var fixtures = new ReferralFixtures(this);
            var campaign = fixtures.Create(active: false);

            OpenReferralPage("/Referral/" + campaign.Code);
            AssertInactiveReferralPage(campaign.Code);

            Assert.False(WaitForOwnedReferral(campaign).Active);
            Assert.Equal(0, ReadReferralOpenCount(campaign));
            Assert.Empty(myApplicationDbContext.ReferralLinkOpens.AsNoTracking().Where(o => o.ReferralId == campaign.Id));
        }

        [Fact]
        [Trait("Category", "Referral")]
        public void Referral_UnknownLink_ShowsErrorWithoutCreatingOrTrackingReferral()
        {
            using var fixtures = new ReferralFixtures(this);
            var unknown = fixtures.Reserve();
            Assert.False(myApplicationDbContext.ReferralLinkOpens.Any(o => o.Referral.Code == unknown.Code));

            OpenReferralPage("/Referral/" + unknown.Code);
            AssertInactiveReferralPage(unknown.Code);

            Assert.False(myApplicationDbContext.Referrals.Any(r => r.Code == unknown.Code));
            Assert.False(myApplicationDbContext.ReferralLinkOpens.Any(o => o.Referral.Code == unknown.Code));
        }

        [Theory]
        [InlineData(CampaignHistoryPath)]
        [InlineData("/Admin/Referrals")]
        [Trait("Category", "Referral")]
        public void Referral_AnonymousProtectedPage_RedirectsToLogin(string path)
        {
            PrepareReferralBrowser();
            driver.Manage().Cookies.DeleteAllCookies();

            OpenReferralPage(path);

            ReferralWait().Until(d => new Uri(d.Url).AbsolutePath.Equals("/Identity/Account/Login", StringComparison.OrdinalIgnoreCase));
            var returnUrl = System.Web.HttpUtility.ParseQueryString(new Uri(driver.Url).Query)["ReturnUrl"];
            Assert.NotNull(returnUrl);
            Assert.Equal(path, returnUrl!.Split('?')[0]);
            Assert.True(driver.FindElement(By.Id("Input_Email")).Displayed);
            Assert.True(driver.FindElement(By.Id("Input_Password")).Displayed);
        }

        [Fact]
        [Trait("Category", "Referral")]
        public void Referral_AdminList_ShowsImageFirstAndPaidOnlyMetrics()
        {
            PrepareReferralBrowser();
            string username = GetRequiredSeleniumSetting("Admin:Username");
            string password = GetRequiredSeleniumSetting("Admin:Password");
            string code = GetRequiredSeleniumSetting("ReferralMetricsCode");
            var sample = myApplicationDbContext.Referrals.AsNoTracking().Include(r => r.Donations)
                .SingleOrDefault(r => r.Code == code && r.Active);
            Assert.True(sample != null, "SeleniumTest:ReferralMetricsCode must identify a stable active staging referral.");
            Assert.False(string.IsNullOrWhiteSpace(sample!.ImageUrl), "The configured metrics sample must have an image.");
            var paid = sample.Donations.Where(d => d.PaymentStatus == PaymentStatus.Payed).ToList();
            var pending = sample.Donations.Where(d => d.PaymentStatus == PaymentStatus.WaitingPayment || d.PaymentStatus == PaymentStatus.NotPayed).ToList();
            Assert.True(paid.Count > 0, "SeleniumTest:ReferralMetricsCode must have at least one paid donation.");
            Assert.True(pending.Count > 0, "SeleniumTest:ReferralMetricsCode must have at least one pending donation.");
            Assert.True(pending.Sum(d => d.DonationAmount) > 0, "The stable sample must contain a positive pending amount to exercise paid-only totals.");
            DateTime lastPaidDate = paid.Max(d => d.DonationDate);
            DateTime lastPendingDate = pending.Max(d => d.DonationDate);
            Assert.True(lastPendingDate > lastPaidDate,
                "The stable sample needs a pending donation newer than its last paid donation to exercise the paid-only date.");
            Assert.NotEqual(lastPaidDate.ToString("g", ReferralCulture), lastPendingDate.ToString("g", ReferralCulture));
            string paidRatio = (sample.LinkOpenCount / (double)paid.Count).ToString("F2", ReferralCulture);
            string allDonationsRatio = (sample.LinkOpenCount / (double)sample.Donations.Count).ToString("F2", ReferralCulture);
            Assert.True(paidRatio != allDonationsRatio,
                "The stable sample needs enough openings to distinguish paid-only and all-donation ratios after rounding to two decimals.");
            Login(username, password);

            // Only visit list pages. Following the sample's referral URL would mutate its opening metrics.
            var row = FindAdminReferralRow(sample.Code);
            var cells = row.FindElements(By.CssSelector("td"));
            Assert.Equal(10, cells.Count);
            Assert.Equal("Imagem", driver.FindElement(By.CssSelector("table thead th:first-child")).Text.Trim());
            var image = Assert.Single(cells[0].FindElements(By.TagName("img")));
            WaitForLoadedReferralImage(image);
            Assert.Equal(sample.Code, cells[1].Text.Trim());
            Assert.Equal(sample.LinkOpenCount.ToString(ReferralCulture), cells[4].Text.Trim());
            Assert.Equal(paidRatio, cells[5].Text.Trim());
            Assert.NotEqual(allDonationsRatio, cells[5].Text.Trim());
            Assert.Equal(paid.Count.ToString(ReferralCulture), cells[6].Text.Trim());
            Assert.NotEqual(sample.Donations.Count.ToString(ReferralCulture), cells[6].Text.Trim());
            Assert.Equal(paid.Sum(d => d.DonationAmount).ToString("F2", ReferralCulture) + " €", cells[7].Text.Trim());
            Assert.NotEqual((paid.Sum(d => d.DonationAmount) + pending.Sum(d => d.DonationAmount)).ToString("F2", ReferralCulture) + " €", cells[7].Text.Trim());
            Assert.Equal(lastPaidDate.ToString("g", ReferralCulture), cells[8].Text.Trim());
            Assert.NotEqual(sample.Donations.Max(d => d.DonationDate).ToString("g", ReferralCulture), cells[8].Text.Trim());
            Assert.Equal(sample.LinkOpenCount, myApplicationDbContext.Referrals.AsNoTracking().Single(r => r.Id == sample.Id).LinkOpenCount);
        }
    }
}
