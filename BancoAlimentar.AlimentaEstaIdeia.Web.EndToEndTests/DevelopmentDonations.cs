using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;
using System.Text.RegularExpressions;

namespace BancoAlimentar.AlimentaEstaIdeia.Web.EndToEndTests
{

    [TestClass]
    public class DevelopmentDonations : PageTest
    {
		private static Random random = new Random();


        public override BrowserNewContextOptions ContextOptions()
        {
            BrowserNewContextOptions options = base.ContextOptions();
            if (options == null)
            {
                options = new BrowserNewContextOptions();
            }
            options.Locale = "en-US";
            return options;
        }

        private static async Task CreateDonation(IPage page)
        {
            page.SetDefaultNavigationTimeout(60_000);

            // Go to https://dev.alimentestaideia.pt/
            await page.GotoAsync("https://dev.alimentestaideia.pt/");

            // Click a:has-text("Donate") >> nth=1
            await page.Locator("a:has-text(\"Donate\")").Nth(1).ClickAsync();
            await page.WaitForURLAsync("https://dev.alimentestaideia.pt/Donation");

            // Double click .more >> nth=0
            await page.Locator(".more").First.DblClickAsync();

            // Double click div:nth-child(2) > .input > .more
            await page.Locator("div:nth-child(2) > .input > .more").ClickAsync(new LocatorClickOptions
            {
                ClickCount = random.Next(1,8),
            });

            // Double click div:nth-child(3) > .input > .more
            await page.Locator("div:nth-child(3) > .input > .more").ClickAsync(new LocatorClickOptions
            {
                ClickCount = random.Next(1,8),
            });

            // Triple click div:nth-child(6) > .input > .more
            await page.Locator("div:nth-child(6) > .input > .more").ClickAsync(new LocatorClickOptions
            {
                ClickCount = random.Next(1,8),
            });

            // Double click div:nth-child(5) > .input > .more
            await page.Locator("div:nth-child(5) > .input > .more").ClickAsync(new LocatorClickOptions
            {
                ClickCount = random.Next(1,8),
            });


            // Click div:nth-child(4) > .input > .more
            await page.Locator("div:nth-child(4) > .input > .more").ClickAsync(new LocatorClickOptions
            {
                ClickCount = random.Next(1,8),
            });


            // Select 11
            await page.Locator("#FoodBankId").SelectOptionAsync(new[] { "11" });

            // Click [placeholder="Your name"]
            await page.Locator("#Name").ClickAsync();

            // Fill [placeholder="Your name"]
            await page.Locator("#Name").FillAsync("User Test - " + Guid.NewGuid().ToString());

            // Click [placeholder="Your company"]
            await page.Locator("#CompanyName").ClickAsync();

            // Fill [placeholder="Your company"]
            await page.Locator("#CompanyName").FillAsync("E2E Contoso - " + Guid.NewGuid().ToString());

            // Click [placeholder="Your e-mail"]
            await page.Locator("#Email").ClickAsync();

            // Fill [placeholder="Your e-mail"]
            await page.Locator("#Email").FillAsync("luis@luis.com");

            bool isCheckedTermsAndConditions = await page.EvaluateAsync<bool>("document.getElementById('AcceptsTermsCheckBox').checked = true;");

            Assert.IsTrue(isCheckedTermsAndConditions);

            await page.Locator("span:has-text(\"Donate\") >> nth=1").ClickAsync();
        }

        [TestMethod]
        public async Task PaypalTest()
        {
            // donation is created and navigated to payment page.
            await CreateDonation(Page);

            // Click #pagamentopaypal
            await Page.Locator("#pagamentopaypal").ClickAsync();
            await Page.WaitForURLAsync("https://www.sandbox.paypal.com/checkoutnow**");

            // Click [placeholder="Correo electrónico o número de móvil"]
            await Page.Locator("#email").ClickAsync();

            // Fill [placeholder="Correo electrónico o número de móvil"]
            await Page.Locator("#email").FillAsync("sb-rwmg435277133@personal.example.com");

            // Click text=Siguiente
            await Page.Locator("#btnNext").ClickAsync();

            // Click [placeholder="Contraseña"]
            await Page.Locator("#password").ClickAsync();

            // Fill [placeholder="Contraseña"]
            await Page.Locator("#password").FillAsync("QHZ3Vy#L");

            // Click button:has-text("Iniciar sesión")
            await Page.Locator("#btnLogin").ClickAsync();
            await Page.WaitForURLAsync("https://www.sandbox.paypal.com/webapps/hermes**");

            // Click [data-testid="submit-button-initial"]
            await Page.Locator("[data-testid=\"submit-button-initial\"]").ClickAsync();

            // Go to https://dev.alimentestaideia.pt/Thanks
            await Page.GotoAsync("https://dev.alimentestaideia.pt/Thanks**");
        }

        [TestMethod]
        public async Task TestCreditCard()
        {
            // donation is created and navigated to payment page.
            await CreateDonation(Page);

            // Click #pagamentounicre
            await Page.Locator("#pagamentounicre").ClickAsync();

            // Click span:has-text("Visa payment") >> nth=0
            await Page.Locator("span:has-text(\"Visa payment\")").First.ClickAsync();
            await Page.WaitForURLAsync("https://gateway.test.easypay.pt/**");

            // Click [placeholder="Cardholder"]
            await Page.Locator("[placeholder=\"Cardholder\"]").ClickAsync();

            // Fill [placeholder="Cardholder"]
            await Page.Locator("[placeholder=\"Cardholder\"]").FillAsync("António Silva");

            // Select 0000000000000000
            await Page.Locator("select[name=\"card_number\"]").SelectOptionAsync(new[] { "0000000000000000" });

            // Select 04
            await Page.Locator("select[name=\"card_expiration_month\"]").SelectOptionAsync(new[] { "04" });

            // Select 2026
            await Page.Locator("select[name=\"card_expiration_year\"]").SelectOptionAsync(new[] { "2026" });

            // Click [placeholder="CVV"]
            await Page.Locator("[placeholder=\"CVV\"]").ClickAsync();

            // Fill [placeholder="CVV"]
            await Page.Locator("[placeholder=\"CVV\"]").FillAsync("123");

            // Click [placeholder="Phone"]
            await Page.Locator("[placeholder=\"Phone\"]").ClickAsync();

            // Fill [placeholder="Phone"]
            await Page.Locator("[placeholder=\"Phone\"]").FillAsync("123456789");

            // Click text=Next
            await Page.Locator("text=Next").ClickAsync();

            // Click button:has-text("Confirm")
            await Page.Locator("button:has-text(\"Confirm\")").ClickAsync();
            await Page.WaitForURLAsync("https://gateway.test.easypay.pt/**/transaction/details");

            // Go to https://dev.alimentestaideia.pt/
            await Page.GotoAsync("https://dev.alimentestaideia.pt/");
        }
    }
}