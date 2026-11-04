// <copyright file="PasswordResetTestSupport.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>

namespace BancoAlimentar.AlimentaEstaIdeia.Web.SeleniumUITest
{
    using System.Data;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using Microsoft.EntityFrameworkCore;
    using OpenQA.Selenium;
    using OpenQA.Selenium.Support.UI;
    using Xunit;

    public partial class AutomatedUITests
    {
        private const string ForgotPasswordPath = "/Identity/Account/ForgotPassword";
        private const string ResetPasswordPath = "/Identity/Account/ResetPassword";
        private const string ResetPasswordConfirmationPath = "/Identity/Account/ResetPasswordConfirmation";
        private const string ConfirmEmailPath = "/Identity/Account/ConfirmEmail";
        private const string PasswordResetLoginPath = "/Identity/Account/Login";
        private static readonly TimeSpan EmailWait = TimeSpan.FromSeconds(30);
        private string passwordResetStage = "scenario setup";

        private async Task RunPasswordResetScenarioAsync(Func<Task> scenario)
        {
            AssertPasswordResetLocalDatabase();
            try
            {
                await scenario();
            }
            catch (WebDriverException exception)
            {
                // Selenium can include the current token-bearing URL or SendKeys arguments in its exception.
                throw new InvalidOperationException("Local password-reset browser interaction failed during " + passwordResetStage
                    + " (" + exception.GetType().Name + "). Sensitive browser details were omitted.");
            }
        }

        private void AssertPasswordResetLocalDatabase()
        {
            Assert.True(settings.IsLocal, "PasswordReset/LocalEmail tests require SeleniumTest:Target=Local; exclude these categories from Developer runs.");
            Assert.True(settings.MailpitBaseUrl != null, "Local email tests require a loopback Mailpit origin.");
            Assert.True(settings.BaseUrl.IsLoopback, "Password-reset fixtures require a loopback app.");
            if (!settings.IsSelfContained)
            {
                Assert.True(settings.ExpectedDatabase.EndsWith("_selenium", StringComparison.OrdinalIgnoreCase),
                    "External password-reset fixtures require an explicitly selected dedicated _selenium database.");
                settings.AssertDatabase(settings.VerificationConnectionString);
            }

            AssertVerificationDatabase();
        }

        private WebDriverWait PasswordResetWait() => new WebDriverWait(driver, TimeSpan.FromSeconds(20));

        private static string NewPasswordResetEmail() => "selenium-" + Guid.NewGuid().ToString("N") + "@example.test";

        private void PreparePasswordResetBrowser()
        {
            passwordResetStage = "browser session reset";
            AssertPasswordResetLocalDatabase();
            driver.Navigate().GoToUrl(baseUrl + "/");
            // Registration must not inherit an existing account or a pending donation from a prior browser session.
            driver.Manage().Cookies.DeleteAllCookies();
            driver.Manage().Cookies.AddCookie(new Cookie(".AspNetCore.Culture", "c=pt|uic=pt", "/"));
        }

        private void OpenPasswordResetPage(string path)
        {
            passwordResetStage = "opening " + path;
            driver.Navigate().GoToUrl(baseUrl + path);
            PasswordResetWait().Until(_ => ScriptResult<string>("return document.readyState;") == "complete");
        }

        private void OpenPasswordResetEmailLink(Uri link, string expectedPath)
        {
            passwordResetStage = "opening the emailed reset form";
            // Validate again at the navigation boundary, including deliberately mutated test tokens.
            Uri validated = MailpitClient.ExtractIdentityLink("", link.AbsoluteUri, settings.BaseUrl, expectedPath);
            driver.Navigate().GoToUrl(validated);
            PasswordResetWait().Until(d => d.FindElements(By.Id("Input_Code")).Count == 1);
        }

        private bool IsPasswordResetPath(string expectedPath)
        {
            return Uri.TryCreate(driver.Url, UriKind.Absolute, out var current)
                && current.Scheme == settings.BaseUrl.Scheme && current.IdnHost == settings.BaseUrl.IdnHost
                && current.Port == settings.BaseUrl.Port
                && current.AbsolutePath.Equals(expectedPath, StringComparison.OrdinalIgnoreCase);
        }

        private void WaitForPasswordResetPath(string expectedPath)
        {
            passwordResetStage = "waiting for " + expectedPath;
            PasswordResetWait().Until(_ => IsPasswordResetPath(expectedPath));
        }

        private void SubmitForgotPassword(string email)
        {
            OpenPasswordResetPage(ForgotPasswordPath);
            PasswordResetWait().Until(d => d.FindElements(By.Id("Input_Email")).Any(element => element.Displayed));
            SetTextInputRobust(By.Id("Input_Email"), email);
            driver.FindElement(By.CssSelector("form[method='post']:has(#Input_Email) button[type='submit']")).Click();
        }

        private void AssertForgotPasswordConfirmation()
        {
            WaitForPasswordResetPath("/Identity/Account/ForgotPasswordConfirmation");
            PasswordResetWait().Until(d => d.FindElements(By.CssSelector(".alert-success"))
                .Any(element => element.Displayed && !string.IsNullOrWhiteSpace(element.Text)));
        }

        private void SubmitNewPassword(string email, string password, string confirmation)
        {
            passwordResetStage = "submitting the reset form";
            SetTextInputRobust(By.Id("Input_Email"), email);
            SetTextInputRobust(By.Id("Input_Password"), password);
            SetTextInputRobust(By.Id("Input_ConfirmPassword"), confirmation);
            driver.FindElement(By.CssSelector("form[method='post']:has(#Input_Code) button[type='submit']")).Click();
        }

        private void WaitForPasswordResetError() => PasswordResetWait().Until(d =>
            d.FindElements(By.CssSelector(".validation-summary-errors li, .field-validation-error"))
                .Any(element => element.Displayed && !string.IsNullOrWhiteSpace(element.Text)));

        private void AssertPasswordResetLogin(PasswordResetAccount account, string password, bool succeeds)
        {
            PreparePasswordResetBrowser();
            if (succeeds)
            {
                passwordResetStage = "password login";
                Login(account.Email, password); // Reuse the existing explicit-credentials browser helper.
                OpenPasswordResetPage("/Identity/Account/Manage/Index");
                passwordResetStage = "verifying the authenticated profile";
                PasswordResetWait().Until(d => d.FindElements(By.Id("Username")).Any(element => element.Displayed));
                Assert.True(driver.FindElement(By.Id("Username")).GetAttribute("value") == account.Email,
                    "The new session must authenticate as the unique password-reset account.");
            }
            else
            {
                OpenPasswordResetPage(PasswordResetLoginPath);
                SetTextInputRobust(By.Id("Input_Email"), account.Email);
                SetTextInputRobust(By.Id("Input_Password"), password);
                driver.FindElement(By.Id("loginBtn")).Click();
                WaitForPasswordResetError();
                Assert.True(IsPasswordResetPath(PasswordResetLoginPath), "The rejected password must not authenticate the account.");
                OpenPasswordResetPage("/Identity/Account/Manage/Index");
                WaitForPasswordResetPath(PasswordResetLoginPath);
            }
        }

        private sealed class PasswordResetAccount : IAsyncDisposable
        {
            private readonly AutomatedUITests test;
            private readonly MailpitClient mailbox;
            private readonly string addressMarker = "Selenium " + Guid.NewGuid().ToString("N");
            private string? id;
            private bool submittedRegistration;

            public PasswordResetAccount(AutomatedUITests test)
            {
                this.test = test;
                test.AssertPasswordResetLocalDatabase();
                Email = NewPasswordResetEmail();
                OriginalPassword = "Original!9a" + Guid.NewGuid().ToString("N");
                NewPassword = "Changed!7b" + Guid.NewGuid().ToString("N");
                mailbox = new MailpitClient(test.settings.MailpitBaseUrl!, Email);
            }

            public string Email { get; }

            public string OriginalPassword { get; }

            public string NewPassword { get; }

            public async Task RegisterAndConfirmAsync()
            {
                test.PreparePasswordResetBrowser();
                Assert.False(test.myApplicationDbContext.WebUser.AsNoTracking().Any(user => user.Email == Email || user.UserName == Email),
                    "Refusing to use an existing account for password-reset tests.");
                var baseline = await mailbox.CaptureBaselineAsync();
                test.OpenPasswordResetPage("/Identity/Account/Register");
                test.SetTextInputRobust(By.Id("Input_Email"), Email);
                test.SetTextInputRobust(By.Id("Input_FullName"), "Selenium password reset");
                test.SetTextInputRobust(By.Id("Input_Password"), OriginalPassword);
                test.SetTextInputRobust(By.Id("Input_ConfirmPassword"), OriginalPassword);
                test.SetTextInputRobust(By.Id("Input_Nif"), "502671858");
                test.SetTextInputRobust(By.Id("Input_PhoneNumber"), "910000000");
                test.SetTextInputRobust(By.Id("Input_CompanyName"), "Selenium test company");
                test.SetTextInputRobust(By.Id("Input_Address_Address1"), addressMarker);
                test.SetTextInputRobust(By.Id("Input_Address_Address2"), "Test address");
                test.SetTextInputRobust(By.Id("Input_Address_City"), "Lisboa");
                test.SetTextInputRobust(By.Id("Input_Address_PostalCode"), "1000-001");
                test.SetTextInputRobust(By.Id("Input_Address_Country"), "Portugal");
                DateTimeOffset requestedAt = DateTimeOffset.UtcNow;
                submittedRegistration = true;
                test.driver.FindElement(By.Id("registerBtn")).Click();
                test.passwordResetStage = "waiting for the registered account";
                try
                {
                    id = test.PasswordResetWait().Until(_ => test.myApplicationDbContext.WebUser.AsNoTracking()
                        .Where(user => user.Email == Email && user.UserName == Email).Select(user => user.Id).SingleOrDefault());
                }
                catch (WebDriverTimeoutException)
                {
                    string errors = test.CollectVisiblePageErrors().Replace(Email, "[test email]", StringComparison.Ordinal)
                        .Replace(OriginalPassword, "[password]", StringComparison.Ordinal);
                    string invalidFields = test.ScriptResult<string>("return JSON.stringify(Array.from(document.querySelectorAll('#registerForm input')).filter(e => !e.validity.valid).map(e => e.name));");
                    throw new InvalidOperationException("Registration did not create the test account. Invalid fields: " + invalidFields + "; validation: " + errors);
                }
                Uri confirmation = await mailbox.WaitForIdentityLinkAsync(baseline, requestedAt, test.settings.BaseUrl, ConfirmEmailPath, EmailWait);
                var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(confirmation.Query);
                Assert.True(query["userId"].Count == 1 && query["userId"][0] == id,
                    "The captured confirmation must belong to the account created by this test.");
                test.driver.Navigate().GoToUrl(confirmation);
                test.WaitForPasswordResetPath("/Identity/Account/ConfirmEmailMessage");
                test.PasswordResetWait().Until(_ => test.myApplicationDbContext.WebUser.AsNoTracking()
                    .Any(user => user.Id == id && user.Email == Email && user.EmailConfirmed));
                test.AssertPasswordResetLogin(this, OriginalPassword, true);
                test.PreparePasswordResetBrowser();
            }

            public async Task<Uri> RequestResetAsync()
            {
                test.AssertPasswordResetLocalDatabase();
                var baseline = await mailbox.CaptureBaselineAsync();
                DateTimeOffset requestedAt = DateTimeOffset.UtcNow;
                test.SubmitForgotPassword(Email);
                test.AssertForgotPasswordConfirmation();
                return await mailbox.WaitForIdentityLinkAsync(baseline, requestedAt, test.settings.BaseUrl, ResetPasswordPath, EmailWait);
            }

            public async ValueTask DisposeAsync()
            {
                try
                {
                    CleanupAccount();
                }
                finally
                {
                    try
                    {
                        await mailbox.CleanupAsync();
                    }
                    finally
                    {
                        mailbox.Dispose();
                    }
                }
            }

            private void CleanupAccount()
            {
                if (!submittedRegistration)
                {
                    return;
                }

                test.AssertPasswordResetLocalDatabase();
                var context = test.myApplicationDbContext;
                context.ChangeTracker.Clear();
                using var transaction = context.Database.BeginTransaction(IsolationLevel.Serializable);
                // The registration may have committed before a browser/email failure. Resolve only this reserved recipient.
                WebUser? user = context.WebUser.Include(item => item.Address)
                    .SingleOrDefault(item => item.Email == Email && item.UserName == Email);
                if (user == null)
                {
                    Assert.False(id != null && context.WebUser.Any(item => item.Id == id),
                        "Refusing cleanup because the created account no longer has its reserved email and username.");
                    return;
                }

                id ??= user.Id;
                Assert.True(user.Id == id && user.Email == Email && user.NormalizedEmail == Email.ToUpperInvariant(),
                    "Refusing account cleanup because the created ID and unique recipient do not match.");
                Assert.False(context.Donations.Any(donation => donation.User.Id == id),
                    "Refusing account cleanup because donations reference the test account.");
                Assert.False(context.Referrals.Any(referral => referral.User.Id == id),
                    "Refusing account cleanup because referrals reference the test account.");
                Assert.False(context.UserRoles.Any(role => role.UserId == id),
                    "Refusing cleanup of a test account that acquired a role.");

                var address = user.Address;
                if (address != null)
                {
                    Assert.True(address.Address1 == addressMarker, "Refusing cleanup of an address not created by this registration.");
                    Assert.False(context.WebUser.Any(other => other.Id != id && other.Address.Id == address.Id),
                        "Refusing cleanup of an address shared by another account.");
                }

                // Existing model/snapshot configure cascade deletion for Identity tokens, claims, logins and login events.
                // Load only this user's dependents so EF also handles the known Identity relationships explicitly.
                context.Entry(user).Collection(item => item.Tokens).Load();
                context.Entry(user).Collection(item => item.Claims).Load();
                context.Entry(user).Collection(item => item.Logins).Load();
                context.WebUser.Remove(user);
                if (address != null)
                {
                    context.DonorAddresses.Remove(address);
                }

                context.SaveChanges();
                transaction.Commit();
            }
        }
    }
}
