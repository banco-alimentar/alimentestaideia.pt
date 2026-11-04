// <copyright file="PasswordResetSecurityUITests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>

namespace BancoAlimentar.AlimentaEstaIdeia.Web.SeleniumUITest
{
    using OpenQA.Selenium;
    using OpenQA.Selenium.Chrome;
    using OpenQA.Selenium.Interactions;
    using Xunit;

    public partial class AutomatedUITests
    {
        [Fact]
        [Trait("Category", "LocalBrowser")]
        [Trait("Category", "PasswordResetSecurity")]
        public Task PasswordReset_ExpiredEmailedTokenPreservesPassword() =>
            RunLocalPasswordResetSecurityScenarioAsync(async () =>
            {
                await using var account = new PasswordResetAccount(this);
                await account.RegisterAndConfirmAsync();
                Uri link = await account.RequestResetAsync();

                // Expire only after the real SMTP message has supplied the protected token.
                browserApplication!.ExpireIdentityTokens();
                OpenPasswordResetEmailLink(link, ResetPasswordPath);
                SubmitNewPassword(account.Email, account.NewPassword, account.NewPassword);
                WaitForPasswordResetError();

                Assert.True(IsPasswordResetPath(ResetPasswordPath), "An expired token must retain the reset form with feedback.");
                AssertPasswordResetLogin(account, account.OriginalPassword, true);
                AssertPasswordResetLogin(account, account.NewPassword, false);
            });

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        [Trait("Category", "LocalBrowser")]
        [Trait("Category", "PasswordResetSecurity")]
        public Task PasswordReset_MissingOrMalformedBase64TokenReturnsSafeErrorAndPreservesPassword(bool malformed) =>
            RunLocalPasswordResetSecurityScenarioAsync(async () =>
            {
                await using var account = new PasswordResetAccount(this);
                await account.RegisterAndConfirmAsync();
                Uri emailed = await account.RequestResetAsync();
                // A one-character base64url value cannot decode. Keep malformed input out of the
                // email-link helper, whose security contract deliberately rejects such links.
                string path = emailed.AbsolutePath + (malformed ? "?code=a" : string.Empty);
                passwordResetStage = malformed ? "checking malformed-token feedback" : "checking missing-token feedback";
                var response = Assert.IsAssignableFrom<IDictionary<string, object?>>(js.ExecuteAsyncScript(
                    "var done = arguments[arguments.length - 1];" +
                    "fetch(arguments[0], { credentials: 'same-origin', redirect: 'error' })" +
                    ".then(async function(r) { var body = await r.text(); done({ status: r.status," +
                    " feedback: /code|token|c[oó]digo|reset|inv[aá]lid/i.test(body)," +
                    " diagnostic: /System\\.FormatException|Stack Trace|Developer Exception Page|at Microsoft\\.AspNetCore/i.test(body) }); })" +
                    ".catch(function() { done({ status: 0, feedback: false, diagnostic: false }); });", path));
                int status = Convert.ToInt32(response["status"]);

                // Check the visible document as well as its status without returning its contents
                // to test diagnostics: development exception pages can contain the requested URL.
                driver.Navigate().GoToUrl(baseUrl + path);
                PasswordResetWait().Until(_ => ScriptResult<string>("return document.readyState;") == "complete");
                bool visibleFeedback = ScriptResult<bool>(
                    "var body = document.body; return !!body && /code|token|c[oó]digo|reset|inv[aá]lid/i.test(body.innerText)" +
                    " && !/System\\.FormatException|Stack Trace|Developer Exception Page|at Microsoft\\.AspNetCore/i.test(body.innerText);");

                // Verify the data invariant even when the safe-error assertions expose a regression.
                AssertPasswordResetLogin(account, account.OriginalPassword, true);
                AssertPasswordResetLogin(account, account.NewPassword, false);
                Assert.True(status >= 400 && status < 500,
                    "Missing or malformed reset tokens must return a safe client error; actual HTTP status: " + status + ".");
                Assert.True(Assert.IsType<bool>(response["feedback"]) && !Assert.IsType<bool>(response["diagnostic"]) && visibleFeedback,
                    "The invalid reset link must display useful feedback without exception diagnostics.");
            });

        [Fact]
        [Trait("Category", "LocalBrowser")]
        [Trait("Category", "PasswordResetSecurity")]
        public Task PasswordReset_ValidTokenForAnotherAccountPreservesBothPasswords() =>
            RunLocalPasswordResetSecurityScenarioAsync(async () =>
            {
                await using var source = new PasswordResetAccount(this);
                await using var target = new PasswordResetAccount(this);
                await source.RegisterAndConfirmAsync();
                await target.RegisterAndConfirmAsync();
                Uri sourceLink = await source.RequestResetAsync();

                OpenPasswordResetEmailLink(sourceLink, ResetPasswordPath);
                SubmitNewPassword(target.Email, target.NewPassword, target.NewPassword);
                WaitForPasswordResetError();
                Assert.True(IsPasswordResetPath(ResetPasswordPath), "A token issued to another account must show a reset error.");

                AssertPasswordResetLogin(source, source.OriginalPassword, true);
                AssertPasswordResetLogin(source, target.NewPassword, false);
                AssertPasswordResetLogin(target, target.OriginalPassword, true);
                AssertPasswordResetLogin(target, target.NewPassword, false);

                // Prove that the rejected token was valid for its actual recipient and that the
                // failed cross-account attempt did not consume it or change the other account.
                PreparePasswordResetBrowser();
                OpenPasswordResetEmailLink(sourceLink, ResetPasswordPath);
                SubmitNewPassword(source.Email, source.NewPassword, source.NewPassword);
                WaitForPasswordResetPath(ResetPasswordConfirmationPath);
                AssertPasswordResetLogin(source, source.NewPassword, true);
                AssertPasswordResetLogin(source, source.OriginalPassword, false);
                AssertPasswordResetLogin(target, target.OriginalPassword, true);
                AssertPasswordResetLogin(target, target.NewPassword, false);
            });

        [Fact]
        [Trait("Category", "LocalBrowser")]
        [Trait("Category", "PasswordResetMobile")]
        public Task PasswordReset_NarrowViewportShowsValidationAndCompletesReset() =>
            RunLocalPasswordResetSecurityScenarioAsync(async () =>
            {
                await using var account = new PasswordResetAccount(this);
                await account.RegisterAndConfirmAsync();
                await WithNarrowPasswordResetViewportAsync(async () =>
                {
                    Uri link = await account.RequestResetAsync();
                    OpenPasswordResetEmailLink(link, ResetPasswordPath);
                    AssertPasswordResetControlsFitViewport();

                    SubmitNewPassword(account.Email, account.NewPassword, account.OriginalPassword);
                    WaitForPasswordResetError();
                    Assert.True(IsPasswordResetPath(ResetPasswordPath), "Mobile validation must retain the reset form.");
                    AssertPasswordResetValidationInViewport();
                    SubmitNewPassword(account.Email, account.NewPassword, account.NewPassword);
                    WaitForPasswordResetPath(ResetPasswordConfirmationPath);

                    AssertPasswordResetLogin(account, account.OriginalPassword, false);
                    AssertPasswordResetLogin(account, account.NewPassword, true);
                });
            });

        [Fact]
        [Trait("Category", "LocalBrowser")]
        [Trait("Category", "PasswordResetKeyboard")]
        public Task PasswordReset_KeyboardOnlyForgotAndResetFormsValidateAndComplete() =>
            RunLocalPasswordResetSecurityScenarioAsync(async () =>
            {
                await using var account = new PasswordResetAccount(this);
                await account.RegisterAndConfirmAsync();
                using var mailbox = new MailpitClient(settings.MailpitBaseUrl!, account.Email);
                try
                {
                    var baseline = await mailbox.CaptureBaselineAsync();
                    PreparePasswordResetBrowser();
                    OpenPasswordResetPage(ForgotPasswordPath);
                    WaitForPasswordResetClientValidation();
                    TabToPasswordResetControl(By.CssSelector("form[method='post']:has(#Input_Email) button[type='submit']"));
                    SendPasswordResetKeys(Keys.Enter);
                    WaitForPasswordResetError();
                    Assert.True(IsPasswordResetPath(ForgotPasswordPath), "Keyboard validation must retain the forgot-password form.");

                    TabToPasswordResetControl(By.Id("Input_Email"));
                    SendPasswordResetKeys(account.Email);
                    TabToPasswordResetControl(By.CssSelector("form[method='post']:has(#Input_Email) button[type='submit']"));
                    DateTimeOffset requestedAt = DateTimeOffset.UtcNow;
                    SendPasswordResetKeys(Keys.Enter);
                    AssertForgotPasswordConfirmation();
                    Uri link = await mailbox.WaitForIdentityLinkAsync(baseline, requestedAt, settings.BaseUrl, ResetPasswordPath, EmailWait);

                    OpenPasswordResetEmailLink(link, ResetPasswordPath);
                    WaitForPasswordResetClientValidation();
                    TabToPasswordResetControl(By.Id("Input_Email"));
                    SendPasswordResetKeys(account.Email);
                    SendPasswordResetKeys(Keys.Tab);
                    AssertPasswordResetKeyboardFocus(By.Id("Input_Password"));
                    SendPasswordResetKeys(account.NewPassword);
                    SendPasswordResetKeys(Keys.Tab);
                    AssertPasswordResetKeyboardFocus(By.Id("Input_ConfirmPassword"));
                    SendPasswordResetKeys(account.OriginalPassword);
                    SendPasswordResetKeys(Keys.Tab);
                    AssertPasswordResetKeyboardFocus(By.CssSelector("form[method='post']:has(#Input_Code) button[type='submit']"));
                    SendPasswordResetKeys(Keys.Enter);
                    WaitForPasswordResetError();
                    Assert.True(IsPasswordResetPath(ResetPasswordPath), "Keyboard validation must retain the reset form.");

                    TabToPasswordResetControl(By.Id("Input_ConfirmPassword"));
                    new Actions(driver).SendKeys(Keys.End).KeyDown(Keys.Shift)
                        .SendKeys(Keys.Home).KeyUp(Keys.Shift).SendKeys(account.NewPassword).Perform();
                    SendPasswordResetKeys(Keys.Tab);
                    AssertPasswordResetKeyboardFocus(By.CssSelector("form[method='post']:has(#Input_Code) button[type='submit']"));
                    SendPasswordResetKeys(Keys.Enter);
                    WaitForPasswordResetPath(ResetPasswordConfirmationPath);

                    AssertPasswordResetLogin(account, account.OriginalPassword, false);
                    AssertPasswordResetLogin(account, account.NewPassword, true);
                }
                finally
                {
                    await mailbox.CleanupAsync();
                }
            });

        private Task RunLocalPasswordResetSecurityScenarioAsync(Func<Task> scenario)
        {
            // This guard must run before constructing accounts, changing the viewport, or
            // expiring host tokens. LocalEmail also supports an external local application.
            Assert.True(settings.IsSelfContained && browserApplication != null,
                "LocalBrowser password-reset tests require the test-owned BrowserTestApplication and SQLite databases.");
            return RunPasswordResetScenarioAsync(scenario);
        }

        private async Task WithNarrowPasswordResetViewportAsync(Func<Task> scenario)
        {
            var chrome = (ChromeDriver)driver;
            chrome.ExecuteCdpCommand("Emulation.setDeviceMetricsOverride", new Dictionary<string, object?>
            {
                ["width"] = 390,
                ["height"] = 844,
                ["deviceScaleFactor"] = 1,
                ["mobile"] = true,
            });
            try
            {
                await scenario();
            }
            finally
            {
                chrome.ExecuteCdpCommand("Emulation.clearDeviceMetricsOverride", new Dictionary<string, object?>());
            }
        }

        private void AssertPasswordResetControlsFitViewport()
        {
            Assert.True(ScriptResult<bool>(
                "return window.innerWidth <= 430 && document.documentElement.scrollWidth <= window.innerWidth + 1;"),
                "The mobile password-reset page must fit a narrow viewport without horizontal overflow.");
            foreach (string id in new[] { "Input_Email", "Input_Password", "Input_ConfirmPassword" })
            {
                Assert.True(ScriptResult<bool>(
                    "var r = arguments[0].getBoundingClientRect(); return r.width > 0 && r.left >= 0 && r.right <= window.innerWidth + 1;",
                    driver.FindElement(By.Id(id))), "Every reset input must fit the narrow viewport.");
            }
        }

        private void AssertPasswordResetValidationInViewport()
        {
            Assert.True(ScriptResult<bool>(
                "return Array.from(document.querySelectorAll('.validation-summary-errors li, .field-validation-error')).some(function(e) {" +
                " var r = e.getBoundingClientRect(); return !!e.innerText.trim() && r.width > 0 && r.height > 0" +
                " && r.top >= 0 && r.bottom <= window.innerHeight && r.left >= 0 && r.right <= window.innerWidth + 1; });"),
                "Mobile reset validation must be visible in the viewport after submission.");
        }

        private void WaitForPasswordResetClientValidation() => PasswordResetWait().Until(_ => ScriptResult<bool>(
            "var input = document.getElementById('Input_Email'); return !!(window.jQuery && input && jQuery(input.form).data('validator'));"));

        private void TabToPasswordResetControl(By locator)
        {
            passwordResetStage = "keyboard navigation on an Identity form";
            IWebElement target = PasswordResetWait().Until(d => d.FindElements(locator).FirstOrDefault(element => element.Displayed && element.Enabled));
            for (int count = 0; count < 80; count++)
            {
                if (ScriptResult<bool>("return document.activeElement === arguments[0];", target))
                {
                    return;
                }

                SendPasswordResetKeys(Keys.Tab);
            }

            Assert.Fail("The Identity form control must be reachable using the Tab key.");
        }

        private void AssertPasswordResetKeyboardFocus(By locator) => Assert.True(
            ScriptResult<bool>("return document.activeElement === arguments[0];", driver.FindElement(locator)),
            "Keyboard navigation must reach the next expected Identity form control.");

        private void SendPasswordResetKeys(string keys)
        {
            passwordResetStage = "typing or submitting an Identity form with the keyboard";
            new Actions(driver).SendKeys(keys).Perform();
        }
    }
}
