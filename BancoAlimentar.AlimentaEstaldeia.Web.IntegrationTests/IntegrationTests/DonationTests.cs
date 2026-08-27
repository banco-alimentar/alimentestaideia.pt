// -----------------------------------------------------------------------
// <copyright file="DonationTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaldeia.Web.IntegrationTests.IntegrationTests
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using AngleSharp.Html.Dom;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Testing.Common;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Models;
    using BancoAlimentar.AlimentaEstaIdeia.Web.TestHost;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;
    using Xunit.Abstractions;

    /// <summary>
    /// Class to test the donation process.
    /// </summary>
    public class DonationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient client;
        private readonly CustomWebApplicationFactory factory;
        private readonly ITestOutputHelper outputHelper;
        private DonationRepository donationRepository;
        private UserManager<WebUser> userManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="DonationTests"/> class.
        /// </summary>
        /// <param name="factory">Factory class.</param>
        /// <param name="outputHelper">Test output helper.</param>
        public DonationTests(CustomWebApplicationFactory factory, ITestOutputHelper outputHelper)
        {
            this.factory = factory;
            this.outputHelper = outputHelper;
            this.client = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var serviceProvider = services.BuildServiceProvider();
                    this.donationRepository = serviceProvider.GetRequiredService<DonationRepository>();
                    this.userManager = serviceProvider.GetRequiredService<UserManager<WebUser>>();
                });
            })
            .CreateClient();
        }

        /// <summary>
        /// Checks if an anonymous user can make a donation without a receipt.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task Can_AnonymousUser_Donate_WithoutReceipt()
        {
            // Arrange
            var defaultPage = await this.client.GetAsync("/Donation");
            Assert.True(defaultPage.IsSuccessStatusCode, await defaultPage.Content.ReadAsStringAsync());
            var content = await HtmlHelpers.GetDocumentAsync(defaultPage);
            var email = "testname1@test.com";

            // Act
            var response = await this.client.SendAsync(
                (IHtmlFormElement)content.QuerySelector("form[id='donationForm']"),
                (IHtmlElement)content.QuerySelector("button[id='donationSubmit'], button[id='submit'], input[id='submit']"),
                new Dictionary<string, string>
                {
                    ["DonatedItems"] = "1:1;2:1;3:1;4:1;5:1;6:1;",
                    ["FoodBankId"] = "1",
                    ["Name"] = "Test Name",
                    ["Amount"] = "1",
                    ["CompanyName"] = "Test Company",
                    ["Email"] = email,
                    ["Country"] = "Portugal",
                    ["WantsReceipt"] = "false",
                    ["AcceptsTerms"] = "true",
                });

            Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());

            // verify if anonymous user was created.
            var user = await this.userManager.FindByEmailAsync(email);
            Assert.NotNull(user);

            // Verify if it was able to create a donation for this user.
            var userDonations = this.donationRepository.GetUserDonation(user.Id);
            Assert.Single(userDonations);

            // Verify if it was able to redirect to Payment page.
            Assert.Equal(HttpStatusCode.OK, defaultPage.StatusCode);
            Assert.Equal("/Payment", response.RequestMessage.RequestUri.AbsolutePath);
        }

        /// <summary>
        /// Checks if an annonymous user can make a donation with a receipt.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task Can_AnonymousUser_Donate_WithReceipt()
        {
            var defaultPage = await this.client.GetAsync("/Donation");
            Assert.True(defaultPage.IsSuccessStatusCode, await defaultPage.Content.ReadAsStringAsync());
            var content = await HtmlHelpers.GetDocumentAsync(defaultPage);
            var email = "testname2@test.com";

            // Act
            var response = await this.client.SendAsync(
                (IHtmlFormElement)content.QuerySelector("form[id='donationForm']"),
                (IHtmlElement)content.QuerySelector("button[id='donationSubmit'], button[id='submit'], input[id='submit']"),
                new Dictionary<string, string>
                {
                    ["DonatedItems"] = "1:1;2:1;3:1;4:1;5:1;6:1;",
                    ["FoodBankId"] = "1",
                    ["Name"] = "Test Name",
                    ["Amount"] = "1",
                    ["CompanyName"] = "Test Company",
                    ["Email"] = email,
                    ["Address"] = "Test Address",
                    ["PostalCode"] = "123456",
                    ["Nif"] = "196807050",
                    ["Country"] = "Portugal",
                    ["WantsReceipt"] = "true",
                    ["AcceptsTerms"] = "true",
                });

            Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());

            // verify if anonymous user was created.
            var user = await this.userManager.FindByEmailAsync(email);
            Assert.NotNull(user);

            // Verify if it was able to create a donation for this user.
            var userDonations = this.donationRepository.GetUserDonation(user.Id);
            Assert.Single(userDonations);

            // Verify if it was able to redirect to Payment page.
            Assert.Equal(HttpStatusCode.OK, defaultPage.StatusCode);
            Assert.Equal("/Payment", response.RequestMessage.RequestUri.AbsolutePath);
        }

        /// <summary>
        /// Checks if a donation attempt fails ModelState validation if coutry is incorrect.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task Cannot_Donate_With_Invalid_Country()
        {
            var defaultPage = await this.client.GetAsync("/Donation");
            Assert.True(defaultPage.IsSuccessStatusCode, await defaultPage.Content.ReadAsStringAsync());
            var content = await HtmlHelpers.GetDocumentAsync(defaultPage);
            var email = "testname232@test.com";

            // Act
            var response = await this.client.SendAsync(
                (IHtmlFormElement)content.QuerySelector("form[id='donationForm']"),
                (IHtmlElement)content.QuerySelector("button[id='donationSubmit'], button[id='submit'], input[id='submit']"),
                new Dictionary<string, string>
                {
                    ["DonatedItems"] = "1:1;2:1;3:1;4:1;5:1;6:1;",
                    ["FoodBankId"] = "1",
                    ["Name"] = "Test Name",
                    ["Amount"] = "1",
                    ["CompanyName"] = "Test Company",
                    ["Email"] = email,
                    ["Address"] = "Test Address",
                    ["PostalCode"] = "123456",
                    ["Nif"] = "196807050",
                    ["Country"] = "Test",
                    ["WantsReceipt"] = "true",
                    ["AcceptsTerms"] = "true",
                });

            Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());

            // verify if anonymous user was created.
            var user = await this.userManager.FindByEmailAsync(email);
            Assert.Null(user);
        }

        /// <summary>
        /// Checks if an anonymous user can not make a donation with missing fileds.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task AnonymousUser_Cannot_Donate_WithMissingRequiredFields()
        {
            var defaultPage = await this.client.GetAsync("/Donation");
            Assert.True(defaultPage.IsSuccessStatusCode, await defaultPage.Content.ReadAsStringAsync());
            var content = await HtmlHelpers.GetDocumentAsync(defaultPage);
            var email = "testname3@test.com";

            // Act
            var response = await this.client.SendAsync(
                (IHtmlFormElement)content.QuerySelector("form[id='donationForm']"),
                (IHtmlElement)content.QuerySelector("button[id='donationSubmit'], button[id='submit'], input[id='submit']"),
                new Dictionary<string, string>
                {
                    ["DonatedItems"] = "1:1;2:1;3:1;4:1;5:1;6:1;",
                    ["FoodBankId"] = "1",
                    ["Name"] = "Test Name",
                    ["Email"] = email,
                    ["Amount"] = "1",
                    ["CompanyName"] = "Test Company",
                    ["Country"] = "Portugal",
                    ["WantsReceipt"] = "true",
                    ["AcceptsTerms"] = "false",
                });

            Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());

            // verify if anonymous user was created.
            var user = await this.userManager.FindByEmailAsync(email);
            Assert.Null(user);

            // Verify if it stays on the donation page.
            Assert.Equal(HttpStatusCode.OK, defaultPage.StatusCode);
            Assert.Equal("/Donation", response.RequestMessage.RequestUri.AbsolutePath);

            var resultDocument = await HtmlHelpers.GetDocumentAsync(response);
            AssertFieldValidationError(resultDocument, "AcceptsTerms", "Política de Privacidade");
        }

        /// <summary>
        /// The donation form must expose per-field validation message placeholders so jQuery unobtrusive
        /// validation can display errors (ValidationSummary is model-only on this page).
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task Donation_Get_IncludesFieldValidationMessageSpans()
        {
            var response = await this.client.GetAsync("/Donation");
            response.EnsureSuccessStatusCode();
            var document = await HtmlHelpers.GetDocumentAsync(response);

            foreach (var fieldName in new[]
                     {
                         "Name",
                         "Email",
                         "Country",
                         "FoodBankId",
                         "AcceptsTerms",
                         "DonatedItems",
                         "Address",
                         "PostalCode",
                         "Nif",
                     })
            {
                AssertFieldHasValidationMessagePlaceholder(document, fieldName);
            }

            var nameInput = document.QuerySelector("input[name='Name']") as IHtmlInputElement;
            Assert.NotNull(nameInput);
            Assert.Equal("true", nameInput.GetAttribute("data-val"));
            Assert.False(string.IsNullOrWhiteSpace(nameInput.GetAttribute("data-val-required")));

            Assert.NotNull(document.QuerySelector("input#submit[type='submit']"));
            Assert.NotNull(document.QuerySelector(".text7 span.text3"));

            var donatedItemsInput = document.QuerySelector("input[name='DonatedItems']") as IHtmlInputElement;
            Assert.NotNull(donatedItemsInput);
            Assert.Equal("false", donatedItemsInput.GetAttribute("data-val"));
        }

        /// <summary>
        /// Server-side validation errors must render next to each field, not only in the summary.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task Donation_Post_ShowsFieldValidationErrors_WhenRequiredFieldsMissing()
        {
            var getResponse = await this.client.GetAsync("/Donation");
            getResponse.EnsureSuccessStatusCode();
            var content = await HtmlHelpers.GetDocumentAsync(getResponse);
            var form = Assert.IsAssignableFrom<IHtmlFormElement>(content.QuerySelector("form[id='donationForm']"));
            var submit = Assert.IsAssignableFrom<IHtmlElement>(content.QuerySelector("button[id='donationSubmit'], button[id='submit'], input[id='submit']"));

            var postResponse = await this.client.SendAsync(
                form,
                submit,
                new Dictionary<string, string>
                {
                    ["DonatedItems"] = "1:1;2:1;3:1;4:1;5:1;6:1;",
                    ["FoodBankId"] = "1",
                    ["Name"] = string.Empty,
                    ["Email"] = string.Empty,
                    ["Country"] = string.Empty,
                    ["WantsReceipt"] = "false",
                    ["AcceptsTerms"] = "true",
                });

            postResponse.EnsureSuccessStatusCode();
            Assert.Equal("/Donation", postResponse.RequestMessage?.RequestUri?.AbsolutePath);

            var resultDocument = await HtmlHelpers.GetDocumentAsync(postResponse);
            AssertFieldValidationError(resultDocument, "Name", ValidationMessages.NameRequired);
            AssertFieldValidationError(resultDocument, "Email", ValidationMessages.EmailRequired);
            AssertFieldValidationError(resultDocument, "Country", ValidationMessages.CountryRequired);
            AssertPropertyErrorsNotInValidationSummary(resultDocument, ValidationMessages.NameRequired);
        }

        /// <summary>
        /// Submitting without donated items shows the amount/items message on the DonatedItems field span.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task Donation_Post_ShowsDonatedItemsError_WhenNoItemsSelected()
        {
            var getResponse = await this.client.GetAsync("/Donation");
            getResponse.EnsureSuccessStatusCode();
            var content = await HtmlHelpers.GetDocumentAsync(getResponse);
            var form = Assert.IsAssignableFrom<IHtmlFormElement>(content.QuerySelector("form[id='donationForm']"));
            var submit = Assert.IsAssignableFrom<IHtmlElement>(content.QuerySelector("button[id='donationSubmit'], button[id='submit'], input[id='submit']"));

            var postResponse = await this.client.SendAsync(
                form,
                submit,
                new Dictionary<string, string>
                {
                    ["DonatedItems"] = string.Empty,
                    ["FoodBankId"] = "1",
                    ["Name"] = "Test Name",
                    ["Email"] = "donation-validation@test.com",
                    ["Country"] = "Portugal",
                    ["WantsReceipt"] = "false",
                    ["AcceptsTerms"] = "true",
                });

            postResponse.EnsureSuccessStatusCode();
            Assert.Equal("/Donation", postResponse.RequestMessage?.RequestUri?.AbsolutePath);

            var resultDocument = await HtmlHelpers.GetDocumentAsync(postResponse);
            var donatedItemsSpan = resultDocument.QuerySelector("span[data-valmsg-for='DonatedItems']");
            Assert.True(donatedItemsSpan != null, "Expected validation message span for 'DonatedItems'.");
            var className = donatedItemsSpan.GetAttribute("class") ?? string.Empty;
            Assert.Contains("field-validation-error", className, StringComparison.OrdinalIgnoreCase);
            Assert.False(string.IsNullOrWhiteSpace(donatedItemsSpan.TextContent));
            AssertPropertyErrorsNotInValidationSummary(resultDocument, donatedItemsSpan.TextContent);
        }

        /// <summary>
        /// Donation submitted after visiting /Referral?text= links the referral to the donation via session.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [Fact]
        public async Task Can_AnonymousUser_Donate_WithReferralCode()
        {
            var webFactory = this.factory.WithWebHostBuilder(_ => { });
            IntegrationTestDataSeeder.ReferralSeed referralSeed;
            using (var scope = webFactory.Services.CreateScope())
            {
                referralSeed = await IntegrationTestDataSeeder.SeedActiveReferralAsync(scope.ServiceProvider);
            }

            var client = webFactory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = true,
            });
            var email = $"referral-donor-{Guid.NewGuid():N}@integration.test";
            var referralLanding = await client.GetAsync($"/Referral?text={Uri.EscapeDataString(referralSeed.Code)}");
            Assert.True(referralLanding.IsSuccessStatusCode, await referralLanding.Content.ReadAsStringAsync());
            Assert.Equal("/Donation", referralLanding.RequestMessage?.RequestUri?.AbsolutePath);

            var donationPage = await client.GetAsync("/Donation");
            Assert.True(donationPage.IsSuccessStatusCode, await donationPage.Content.ReadAsStringAsync());
            var content = await HtmlHelpers.GetDocumentAsync(donationPage);
            var donationForm = content.QuerySelector("form[id='donationForm']") as IHtmlFormElement;
            Assert.NotNull(donationForm);

            var response = await client.SendAsync(
                donationForm,
                (IHtmlElement)content.QuerySelector("button[id='donationSubmit'], button[id='submit'], input[id='submit']"),
                new Dictionary<string, string>
                {
                    ["DonatedItems"] = "1:1;2:1;3:1;4:1;5:1;6:1;",
                    ["FoodBankId"] = "1",
                    ["Name"] = "Referral Donor",
                    ["Amount"] = "1",
                    ["CompanyName"] = "Test Company",
                    ["Email"] = email,
                    ["Country"] = "Portugal",
                    ["WantsReceipt"] = "false",
                    ["AcceptsTerms"] = "true",
                });

            Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
            Assert.Equal("/Payment", response.RequestMessage.RequestUri.AbsolutePath);

            using var assertScope = webFactory.Services.CreateScope();
            var context = assertScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var donation = await context.Donations
                .Include(d => d.ReferralEntity)
                .Include(d => d.User)
                .FirstAsync(d => d.User.Email == email);
            Assert.NotNull(donation.ReferralEntity);
            Assert.Equal(referralSeed.ReferralId, donation.ReferralEntity.Id);
        }

        /// <summary>
        /// Checks if donation page is being redirected to maintenance when enabled.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [Fact]
        public async Task Can_Redirect_To_MaintenancePage_When_MaintenenceIsEnabled()
        {
            var client = this.factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string>
                    {
                        ["FeatureManagement:EnableMaintenance"] = "true",
                    });
                });
            }).CreateClient();

            var response = await client.GetAsync("/Donation");

            // Assert
            response.EnsureSuccessStatusCode(); // Status Code 200-299
            Assert.Equal("/Maintenance", response.RequestMessage.RequestUri.AbsolutePath);
        }

        private static void AssertFieldHasValidationMessagePlaceholder(IHtmlDocument document, string fieldName)
        {
            var span = document.QuerySelector($"span[data-valmsg-for='{fieldName}']");
            Assert.True(span != null, $"Expected validation message placeholder for '{fieldName}' on /Donation.");
        }

        private static void AssertFieldValidationError(IHtmlDocument document, string fieldName, string expectedMessage)
        {
            var span = document.QuerySelector($"span[data-valmsg-for='{fieldName}']");
            Assert.True(span != null, $"Expected validation message span for '{fieldName}'.");
            var className = span.GetAttribute("class") ?? string.Empty;
            Assert.Contains("field-validation-error", className, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(expectedMessage, span.TextContent, StringComparison.OrdinalIgnoreCase);
        }

        private static void AssertPropertyErrorsNotInValidationSummary(IHtmlDocument document, string propertyErrorMessage)
        {
            var summary = document.QuerySelector(".validation-summary-errors");
            if (summary == null)
            {
                return;
            }

            Assert.DoesNotContain(propertyErrorMessage, summary.TextContent, StringComparison.OrdinalIgnoreCase);
        }
    }
}
