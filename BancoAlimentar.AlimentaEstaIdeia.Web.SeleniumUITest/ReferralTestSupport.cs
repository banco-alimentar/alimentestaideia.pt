// -----------------------------------------------------------------------
// <copyright file="ReferralTestSupport.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.SeleniumUITest
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Globalization;
    using System.Linq;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using Microsoft.EntityFrameworkCore;
    using OpenQA.Selenium;
    using OpenQA.Selenium.Support.UI;
    using Xunit;

    public partial class AutomatedUITests
    {
        private const string CampaignHistoryPath = "/Identity/Account/Manage/CampaignsHistory";
        private static readonly CultureInfo ReferralCulture = CultureInfo.GetCultureInfo("pt");

        private WebDriverWait ReferralWait() => new WebDriverWait(driver, TimeSpan.FromSeconds(20));

        private void AssertReferralTestDatabase()
        {
            AssertVerificationDatabase();
        }

        private void PrepareReferralBrowser()
        {
            AssertReferralTestDatabase();
            driver.Manage().Window.Size = new System.Drawing.Size(1000, 600);
            driver.Navigate().GoToUrl(baseUrl + "/?culture=pt&ui-culture=pt");
            driver.Manage().Cookies.AddCookie(new Cookie(".AspNetCore.Culture", "c=pt|uic=pt", "/"));
        }

        private void OpenReferralPage(string path)
        {
            string separator = path.Contains('?') ? "&" : "?";
            driver.Navigate().GoToUrl(baseUrl + path + separator + "culture=pt&ui-culture=pt");
        }

        private void WaitForReferralPageReplacement(IWebElement oldForm)
        {
            ReferralWait().Until(d =>
            {
                try
                {
                    _ = oldForm.TagName;
                    return false;
                }
                catch (StaleElementReferenceException)
                {
                    return true;
                }
                catch (UnknownErrorException exception) when (exception.Message.Contains("Node with given id does not belong to the document", StringComparison.Ordinal))
                {
                    // Chrome can report this detached-node response while the navigation replaces
                    // the document. The caller still verifies the destination and persisted state.
                    return true;
                }
            });
            ReferralWait().Until(_ => ScriptResult<string>("return document.readyState;") == "complete");
        }

        private void SubmitCampaignCode(string code, ReferralReservation reservation)
        {
            SetTextInput(By.Name("Code"), code);
            var form = driver.FindElement(By.Id("create_section"));
            reservation.ExpectInsert = true;
            driver.FindElement(By.Id("create-campaign-submit")).Click();
            WaitForReferralPageReplacement(form);
        }

        private Referral WaitForOwnedReferral(ReferralReservation reservation)
        {
            return ReferralWait().Until(_ =>
            {
                var rows = myApplicationDbContext.Referrals.AsNoTracking().Include(r => r.User)
                    .Where(r => r.Code.ToLower() == reservation.Code.ToLower()).ToList();
                if (rows.Count == 0)
                {
                    return null;
                }

                var row = Assert.Single(rows);
                Assert.Equal(reservation.Code, row.Code);
                Assert.Equal(reservation.OwnerId, row.User.Id);
                if (reservation.Id.HasValue)
                {
                    Assert.Equal(reservation.Id.Value, row.Id);
                }

                reservation.Id = row.Id;
                return row;
            });
        }

        private int ReadReferralOpenCount(ReferralReservation reservation) =>
            myApplicationDbContext.Referrals.AsNoTracking()
                .Single(r => r.Id == reservation.Id && r.Code == reservation.Code && r.User.Id == reservation.OwnerId).LinkOpenCount;

        private void WaitForDonationRedirect()
        {
            ReferralWait().Until(d => new Uri(d.Url).AbsolutePath.Equals("/Donation", StringComparison.OrdinalIgnoreCase));
        }

        private void WaitForLoadedReferralImage(IWebElement image)
        {
            ReferralWait().Until(_ => ScriptResult<bool>(
                "return arguments[0].complete && arguments[0].naturalWidth > 0 && arguments[0].naturalHeight > 0;", image));
            Assert.True(image.Displayed);
        }

        private void AssertInactiveReferralPage(string code)
        {
            Assert.Equal("/Referral/" + code, new Uri(driver.Url).AbsolutePath);
            ReferralWait().Until(d => d.FindElements(By.CssSelector(".stage3white .text2"))
                .Any(e => e.Displayed && e.Text.Trim() == "Esta campanha de referência não está activa"));
            Assert.Empty(driver.FindElements(By.CssSelector(".referral-campaign-title")));
        }

        private IWebElement FindAdminReferralRow(string code)
        {
            int maxPages = Math.Max(1, (int)Math.Ceiling(myApplicationDbContext.Referrals.Count(r => r.Active) / 25.0));
            for (int page = 1; page <= maxPages; page++)
            {
                OpenReferralPage("/Admin/Referrals?sortBy=Code&sortDescending=false&pageIndex=" + page);
                Assert.True(new Uri(driver.Url).AbsolutePath.TrimEnd('/').Equals("/Admin/Referrals", StringComparison.OrdinalIgnoreCase),
                    "Configured SeleniumTest:Admin credentials must have access to /Admin/Referrals. Current path: " + new Uri(driver.Url).AbsolutePath);
                ReferralWait().Until(d => d.FindElements(By.CssSelector("table tbody tr")).Count > 0);
                var row = driver.FindElements(By.CssSelector("table tbody tr"))
                    .SingleOrDefault(r => r.FindElements(By.CssSelector("td:nth-child(2) a")).Any(a => a.Text.Trim() == code));
                if (row != null)
                {
                    return row;
                }
            }

            throw new InvalidOperationException("Configured SeleniumTest:ReferralMetricsCode was not found in the admin referral list.");
        }

        private sealed class ReferralReservation
        {
            public ReferralReservation(string code, string ownerId)
            {
                Code = code;
                OwnerId = ownerId;
            }

            public string Code { get; }

            public string OwnerId { get; }

            public int? Id { get; set; }

            public bool ExpectInsert { get; set; }
        }

        private sealed class ReferralFixtures : IDisposable
        {
            private readonly AutomatedUITests test;
            private readonly List<ReferralReservation> reservations = new List<ReferralReservation>();

            public ReferralFixtures(AutomatedUITests test)
            {
                this.test = test;
                test.AssertReferralTestDatabase();
                string username = test.GetRequiredSeleniumSetting("Site:Username");
                test.GetRequiredSeleniumSetting("Site:Password");
                var user = test.myApplicationDbContext.WebUser.AsNoTracking()
                    .SingleOrDefault(u => u.NormalizedEmail == username.ToUpperInvariant());
                Assert.True(user != null, "The selected Selenium Site:Username must identify an existing test user; fixtures never create users.");
                OwnerId = user!.Id;
                test.PrepareReferralBrowser();
            }

            public string OwnerId { get; }

            public ReferralReservation Reserve(string suffix = "")
            {
                string code = "sel" + Guid.NewGuid().ToString("N").Substring(0, 17 - suffix.Length) + suffix;
                Assert.False(test.myApplicationDbContext.Referrals.Any(r => r.Code == code), "Generated fixture code already exists; refusing to use it.");
                var reservation = new ReferralReservation(code, OwnerId);
                reservations.Add(reservation);
                return reservation;
            }

            public ReferralReservation Create(bool active = true)
            {
                test.AssertReferralTestDatabase();
                var reservation = Reserve();
                var referral = new Referral
                {
                    Code = reservation.Code,
                    User = test.myApplicationDbContext.WebUser.Single(u => u.Id == OwnerId),
                    Name = "Selenium " + reservation.Code,
                    TagLine = "Campanha Selenium " + reservation.Code,
                    // Absolute URL also works when staging resolves relative paths through blob storage.
                    ImageUrl = test.baseUrl + "/img/logo1.png",
                    Active = active,
                    IsPublic = false,
                    CreateDate = DateTime.UtcNow,
                };
                reservation.ExpectInsert = true;
                test.myApplicationDbContext.Referrals.Add(referral);
                test.myApplicationDbContext.SaveChanges();
                reservation.Id = referral.Id;
                return reservation;
            }

            public void Dispose()
            {
                var failures = new List<Exception>();
                foreach (var reservation in reservations.AsEnumerable().Reverse())
                {
                    try
                    {
                        Cleanup(reservation);
                    }
                    catch (Exception exception)
                    {
                        failures.Add(new InvalidOperationException("Cleanup refused or failed for owned referral " + reservation.Code + ".", exception));
                    }
                }

                if (failures.Count > 0)
                {
                    throw new AggregateException("Referral fixture cleanup requires attention; no financial records were deleted.", failures);
                }
            }

            private void Cleanup(ReferralReservation reservation)
            {
                test.AssertReferralTestDatabase();
                var context = test.myApplicationDbContext;
                context.ChangeTracker.Clear();
                // A POST may have succeeded even if the browser failed before its response arrived.
                if (!reservation.Id.HasValue && reservation.ExpectInsert)
                {
                    try
                    {
                        test.WaitForOwnedReferral(reservation);
                    }
                    catch (WebDriverTimeoutException)
                    {
                        // A failed validation/POST may correctly leave no row. Query once more below.
                    }
                }

                using var transaction = context.Database.BeginTransaction(IsolationLevel.Serializable);
                var rows = context.Referrals.Include(r => r.User)
                    .Where(r => r.Code == reservation.Code && r.User.Id == reservation.OwnerId).ToList();
                if (rows.Count == 0)
                {
                    return;
                }

                var referral = Assert.Single(rows);
                Assert.Equal(reservation.Code, referral.Code);
                Assert.Equal(reservation.OwnerId, referral.User.Id);
                reservation.Id ??= referral.Id;
                Assert.Equal(reservation.Id.Value, referral.Id);
                Assert.False(context.Donations.Any(d => EF.Property<int?>(d, "ReferralId") == referral.Id),
                    "Refusing cleanup: a donation references test referral " + reservation.Code + ". Leave the referral and all financial records intact.");
                var openings = context.ReferralLinkOpens.Where(o => o.ReferralId == referral.Id).ToList();
                context.ReferralLinkOpens.RemoveRange(openings);
                context.SaveChanges();
                context.Referrals.Remove(referral);
                context.SaveChanges();
                transaction.Commit();
            }
        }
    }
}
