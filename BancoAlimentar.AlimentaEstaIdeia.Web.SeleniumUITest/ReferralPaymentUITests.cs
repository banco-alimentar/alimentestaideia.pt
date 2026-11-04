// <copyright file="ReferralPaymentUITests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>

namespace BancoAlimentar.AlimentaEstaIdeia.Web.SeleniumUITest
{
    using System.Net;
    using System.Text.RegularExpressions;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Web.TestHost;
    using Microsoft.EntityFrameworkCore;
    using Newtonsoft.Json.Linq;
    using OpenQA.Selenium;
    using Xunit;

    public partial class AutomatedUITests
    {
        [Theory]
        [InlineData("Coração à Mesa: Alimente esta ideia!")]
        [InlineData("Campanha <Mesa> & \"Partilha\"")]
        [Trait("Category", "LocalBrowser")]
        [Trait("Scenario", "ReferralPaymentConfirmation")]
        public async Task Referral_LocalPayment_RetainsAttributionInEmailAndAdminDetails(string campaignName)
        {
            Assert.True(settings.IsSelfContained && browserApplication != null,
                "Simulated payment tests require the owned local browser host.");
            browserApplication!.AssertOwnsDatabase(myApplicationDbContext);
            PrepareReferralBrowser();
            var referral = myApplicationDbContext.Referrals.Single(r => r.Code == GetRequiredSeleniumSetting("ReferralMetricsCode"));
            referral.Name = campaignName;
            myApplicationDbContext.SaveChanges();
            string recipient = NewPasswordResetEmail();
            using var mailbox = new MailpitClient(settings.MailpitBaseUrl!, recipient);
            try
            {
                OpenReferralPage("/Referral/" + referral.Code);
                WaitForDonationRedirect();
                Assert.Equal(campaignName, driver.FindElement(By.CssSelector(".referral-campaign-title")).Text.Trim());
                CreateDonation(new DonationTestData(5.4, recipient, "Doador Selenium", "Teste local"), false, false, false);
                Assert.All(driver.FindElements(By.CssSelector(".donation-item")), input => Assert.Equal("1", input.GetAttribute("value")));
                driver.FindElement(By.CssSelector("#donationForm .text3")).Click();
                try
                {
                    ReferralWait().Until(d => new Uri(d.Url).AbsolutePath == "/Payment");
                }
                catch (WebDriverTimeoutException)
                {
                    string invalid = ScriptResult<string>("return JSON.stringify(Array.from(document.querySelectorAll('input,select')).filter(e=>!e.validity.valid).map(e=>e.name));");
                    string validation = ScriptResult<string>("var f=document.getElementById('Email')?.form; var v=window.jQuery && f && jQuery(f).data('validator'); return JSON.stringify(v ? v.errorList.map(e=>({name:e.element.name,message:e.message})) : []);");
                    string heading = ScriptResult<string>("return document.querySelector('h1')?.innerText || document.title;");
                    string state = ScriptResult<string>("return JSON.stringify({forms:document.forms.length,items:document.getElementById('DonatedItems')?.value,total:document.getElementById('total')?.innerText,submitInputs:document.querySelectorAll('#submit').length});");
                    throw new InvalidOperationException("Local donation submission failed. Server fields: " + browserApplication.LastDonationErrors + "; heading: " + heading + "; state: " + state + "; invalid fields: " + invalid + "; validation: " + validation + "; errors: " + CollectVisiblePageErrors());
                }

                WaitForPaymentMethodSelectionPage();
                var pending = myApplicationDbContext.Donations.AsNoTracking().Include(d => d.ReferralEntity)
                    .Single(d => d.User.Email == recipient);
                Assert.Equal(referral.Id, pending.ReferralEntity.Id);
                Assert.NotEqual(PaymentStatus.Payed, pending.PaymentStatus);
                SubmitMbWayPayment("910000000");
                MBWayPayment payment;
                try
                {
                    payment = ReferralWait().Until(_ => myApplicationDbContext.Payments.AsNoTracking().OfType<MBWayPayment>()
                        .SingleOrDefault(p => p.Donation.Id == pending.Id));
                }
                catch (WebDriverTimeoutException)
                {
                    throw new InvalidOperationException("Local MBWay checkout did not create a payment. Current path: " + new Uri(driver.Url).PathAndQuery + "; errors: " + CollectVisiblePageErrors());
                }
                var payload = JObject.Parse(EasyPayWebhookPayloadBuilder.BuildMBWayPaymentNotification(
                    pending.PublicId, payment.TransactionKey, payment.EasyPayPaymentId, pending.DonationAmount));
                // The shared builder uses the public donation ID for integration-verifier fixtures.
                // This host retains real provider verification, which requires the actual payment ID.
                payload["id"] = payment.EasyPayPaymentId;
                string json = payload.ToString(Newtonsoft.Json.Formatting.None);
                Assert.Equal(403, PostBrowserPaymentNotification(json));
                Assert.NotEqual(PaymentStatus.Payed, myApplicationDbContext.Donations.AsNoTracking().Single(d => d.Id == pending.Id).PaymentStatus);
                var baseline = await mailbox.CaptureBaselineAsync();
                DateTimeOffset requestedAt = DateTimeOffset.UtcNow;

                browserApplication.Payments.MarkPaid(Guid.Parse(payment.EasyPayPaymentId));
                Assert.Equal(200, PostBrowserPaymentNotification(json));

                var paid = myApplicationDbContext.Donations.AsNoTracking().Include(d => d.ReferralEntity).Include(d => d.ConfirmedPayment)
                    .Single(d => d.Id == pending.Id);
                Assert.Equal(PaymentStatus.Payed, paid.PaymentStatus);
                Assert.NotNull(paid.ConfirmedPayment);
                Assert.Equal(payment.Id, paid.ConfirmedPayment.Id);
                Assert.Equal(referral.Id, paid.ReferralEntity.Id);
                OpenReferralPage("/Thanks?publicId=" + paid.PublicId);
                Assert.Equal("/Thanks", new Uri(driver.Url).AbsolutePath);
                var message = await mailbox.WaitForMessageAsync(baseline, requestedAt,
                    browserApplication.Configuration["Email.ConfirmPaymentNoInvoice.Subject"]!, EmailWait);
                string text = Regex.Replace(WebUtility.HtmlDecode(Regex.Replace(message.Html, "<[^>]+>", " ")), @"\s+", " ");
                string expected = "Muito obrigado pela sua participação na campanha do Banco Alimentar no contexto da campanha " + campaignName;
                Assert.True(text.Contains(expected, StringComparison.Ordinal), "The real SMTP confirmation must include the referral campaign in its thank-you sentence.");
                Assert.True(text.Contains("Recebemos a confirmação do pagamento da sua doação com MBWay", StringComparison.Ordinal),
                    "The confirmation must retain the rest of the payment message.");
                Assert.False(message.Html.Contains("{referralCampaignName}", StringComparison.Ordinal), "The campaign placeholder must be rendered.");
                Assert.False(message.Html.Contains("<Mesa>", StringComparison.Ordinal), "Campaign names must not become HTML elements.");

                driver.Manage().Cookies.DeleteAllCookies();
                Login(GetRequiredSeleniumSetting("Admin:Username"), GetRequiredSeleniumSetting("Admin:Password"));
                OpenReferralPage("/Admin/Donations/Details?id=" + paid.Id);
                var link = driver.FindElement(By.CssSelector("a[href*='/Admin/Referrals/Details']"));
                Assert.Equal(referral.Code, link.Text.Trim());
                Assert.Equal(baseUrl + "/Admin/Referrals/Details/" + referral.Id, link.GetAttribute("href"));
                link.Click();
                ReferralWait().Until(d => new Uri(d.Url).AbsolutePath == "/Admin/Referrals/Details/" + referral.Id);
                Assert.True(driver.FindElements(By.CssSelector("table tbody tr a"))
                    .Any(element => element.GetAttribute("href") == baseUrl + "/Admin/Donations/Details?id=" + paid.Id),
                    "The linked referral details must include the completed donation.");
            }
            finally
            {
                await mailbox.CleanupAsync();
            }
        }

        private int PostBrowserPaymentNotification(string json)
        {
            return Convert.ToInt32(js.ExecuteAsyncScript(
                "var done = arguments[arguments.length - 1]; fetch('/easypay/payment', {method:'POST'," +
                "headers:{'Content-Type':'application/json'},body:arguments[0]}).then(r=>done(r.status)).catch(()=>done(0));", json));
        }
    }
}
