// <copyright file="PasswordResetUITests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>

namespace BancoAlimentar.AlimentaEstaIdeia.Web.SeleniumUITest
{
    using Microsoft.AspNetCore.WebUtilities;
    using Microsoft.EntityFrameworkCore;
    using OpenQA.Selenium;
    using Xunit;

    public partial class AutomatedUITests
    {
        [Theory]
        [InlineData("", "data-val-required")]
        [InlineData("invalid-email", "data-val-email")]
        [Trait("Category", "LocalEmail")]
        [Trait("Category", "PasswordReset")]
        public Task PasswordReset_ForgotPasswordValidatesEmailWithoutPosting(string email, string expectedMessage) =>
            RunPasswordResetScenarioAsync(() =>
            {
                PreparePasswordResetBrowser();
                OpenPasswordResetPage(ForgotPasswordPath);
                SetTextInputRobust(By.Id("Input_Email"), email);
                var form = driver.FindElement(By.CssSelector("form[method='post']:has(#Input_Email)"));
                PasswordResetWait().Until(_ => ScriptResult<bool>(
                    "var form = arguments[0], input = document.getElementById('Input_Email'); return !!(window.jQuery && jQuery(form).data('validator')) || (!form.noValidate && input.willValidate && !input.validity.valid);", form));
                string marker = Guid.NewGuid().ToString("N");
                js.ExecuteScript(
                    "window.passwordResetValidationMarker = arguments[1]; window.passwordResetSubmitEvents = []; arguments[0].addEventListener('submit', function(e) { window.passwordResetSubmitEvents.push(e); });",
                    form, marker);

                form.FindElement(By.CssSelector("button[type='submit']")).Click();

                By errors = By.CssSelector("[data-valmsg-for='Input.Email'].field-validation-error");
                PasswordResetWait().Until(d => d.FindElements(errors)
                    .Any(element => element.Displayed && !string.IsNullOrWhiteSpace(element.Text))
                    || ScriptResult<bool>(
                        "var input = document.getElementById('Input_Email'); return !input.form.noValidate && !input.validity.valid && !!input.validationMessage;"));
                Assert.Equal(marker, js.ExecuteScript("return window.passwordResetValidationMarker;"));
                Assert.Equal(0L, Convert.ToInt64(js.ExecuteScript("return window.passwordResetSubmitEvents.filter(function(e) { return !e.defaultPrevented; }).length;")));
                var error = driver.FindElements(errors).SingleOrDefault(element => element.Displayed && !string.IsNullOrWhiteSpace(element.Text));
                var input = driver.FindElement(By.Id("Input_Email"));
                if (error != null)
                {
                    Assert.Equal(input.GetAttribute(expectedMessage), error.Text.Trim());
                }
                else
                {
                    string validity = expectedMessage == "data-val-required" ? "valueMissing" : "typeMismatch";
                    Assert.True(ScriptResult<bool>("return arguments[0].validity[arguments[1]];", input, validity),
                        "Native browser validation must reject the expected email constraint.");
                }

                Assert.True(IsPasswordResetPath(ForgotPasswordPath), "Client validation must retain the forgot-password form.");
                return Task.CompletedTask;
            });

        [Fact]
        [Trait("Category", "LocalEmail")]
        [Trait("Category", "PasswordReset")]
        public Task PasswordReset_UnknownEmailShowsConfirmationWithoutSendingEmail() =>
            RunPasswordResetScenarioAsync(async () =>
            {
                PreparePasswordResetBrowser();
                string email = NewPasswordResetEmail();
                Assert.False(myApplicationDbContext.WebUser.AsNoTracking().Any(user => user.Email == email),
                    "The unknown test recipient must not belong to an existing account.");
                using var mailbox = new MailpitClient(settings.MailpitBaseUrl!, email);
                try
                {
                    var baseline = await mailbox.CaptureBaselineAsync();
                    DateTimeOffset requestedAt = DateTimeOffset.UtcNow;
                    SubmitForgotPassword(email);
                    AssertForgotPasswordConfirmation();

                    await mailbox.AssertNoMessageAsync(baseline, requestedAt, TimeSpan.FromSeconds(5));
                    Assert.False(myApplicationDbContext.WebUser.AsNoTracking().Any(user => user.Email == email),
                        "Forgot-password must not create an unknown account.");
                }
                finally
                {
                    await mailbox.CleanupAsync();
                }
            });

        [Fact]
        [Trait("Category", "LocalEmail")]
        [Trait("Category", "PasswordReset")]
        public Task PasswordReset_EmailedLinkChangesPasswordAndRejectsOldPassword() =>
            RunPasswordResetScenarioAsync(async () =>
            {
                await using var account = new PasswordResetAccount(this);
                await account.RegisterAndConfirmAsync();
                Uri link = await account.RequestResetAsync();

                OpenPasswordResetEmailLink(link, ResetPasswordPath);
                SubmitNewPassword(account.Email, account.NewPassword, account.NewPassword);
                WaitForPasswordResetPath(ResetPasswordConfirmationPath);

                AssertPasswordResetLogin(account, account.OriginalPassword, false);
                AssertPasswordResetLogin(account, account.NewPassword, true);
            });

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [Trait("Category", "LocalEmail")]
        [Trait("Category", "PasswordReset")]
        public Task PasswordReset_EmailedLinkRejectsMismatchedOrWeakPassword(bool mismatched) =>
            RunPasswordResetScenarioAsync(async () =>
            {
                await using var account = new PasswordResetAccount(this);
                await account.RegisterAndConfirmAsync();
                Uri link = await account.RequestResetAsync();
                OpenPasswordResetEmailLink(link, ResetPasswordPath);

                // Eight lowercase letters pass the form's length rule but fail the Identity password policy.
                string password = mismatched ? account.NewPassword : "abcdefgh";
                string confirmation = mismatched ? account.OriginalPassword : password;
                SubmitNewPassword(account.Email, password, confirmation);
                WaitForPasswordResetError();

                Assert.True(IsPasswordResetPath(ResetPasswordPath), "Rejected passwords must retain the reset form.");
                AssertPasswordResetLogin(account, account.OriginalPassword, true);
                AssertPasswordResetLogin(account, password, false);
            });

        [Fact]
        [Trait("Category", "LocalEmail")]
        [Trait("Category", "PasswordReset")]
        public Task PasswordReset_WellFormedInvalidTokenShowsErrorAndPreservesPassword() =>
            RunPasswordResetScenarioAsync(async () =>
            {
                await using var account = new PasswordResetAccount(this);
                await account.RegisterAndConfirmAsync();
                Uri emailed = await account.RequestResetAsync();
                // Mutate only the emailed token; do not create an Identity token through a manager or SQL.
                string invalidCode = WebEncoders.Base64UrlEncode(System.Text.Encoding.UTF8.GetBytes("invalid-" + Guid.NewGuid().ToString("N")));
                Uri invalid = new Uri(QueryHelpers.AddQueryString(emailed.GetLeftPart(UriPartial.Path), "code", invalidCode));

                OpenPasswordResetEmailLink(invalid, ResetPasswordPath);
                SubmitNewPassword(account.Email, account.NewPassword, account.NewPassword);
                WaitForPasswordResetError();

                Assert.True(IsPasswordResetPath(ResetPasswordPath), "An invalid token must retain the reset form.");
                AssertPasswordResetLogin(account, account.OriginalPassword, true);
                AssertPasswordResetLogin(account, account.NewPassword, false);
            });

        [Fact]
        [Trait("Category", "LocalEmail")]
        [Trait("Category", "PasswordReset")]
        public Task PasswordReset_ConsumedEmailedTokenCannotResetPasswordAgain() =>
            RunPasswordResetScenarioAsync(async () =>
            {
                await using var account = new PasswordResetAccount(this);
                await account.RegisterAndConfirmAsync();
                Uri link = await account.RequestResetAsync();
                OpenPasswordResetEmailLink(link, ResetPasswordPath);
                SubmitNewPassword(account.Email, account.NewPassword, account.NewPassword);
                WaitForPasswordResetPath(ResetPasswordConfirmationPath);

                OpenPasswordResetEmailLink(link, ResetPasswordPath);
                SubmitNewPassword(account.Email, account.OriginalPassword, account.OriginalPassword);
                WaitForPasswordResetError();

                Assert.True(IsPasswordResetPath(ResetPasswordPath), "A consumed token must retain the reset form with an error.");
                AssertPasswordResetLogin(account, account.OriginalPassword, false);
                AssertPasswordResetLogin(account, account.NewPassword, true);
            });
    }
}
