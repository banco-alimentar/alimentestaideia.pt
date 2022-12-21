using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;
using System.Text.RegularExpressions;

namespace BancoAlimentar.AlimentaEstaIdeia.Web.EndToEndTests
{

    [TestClass]
    public class DevelopmentDonations : PageTest
    {
        private static Random random = new Random();
        private static TestContext testContext;
        //private static string baseUrl = "https://localhost:44301/";
        private static string baseUrl = "https://dev.alimentestaideia.pt/";

        [ClassInitialize]
        public static void SetupTests(TestContext testContext)
        {
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            IConfigurationRoot configuration = configurationBuilder
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            baseUrl = configuration["BaseUrl"];

            DevelopmentDonations.testContext = testContext;
        }

        public override BrowserNewContextOptions ContextOptions()
        {
            BrowserNewContextOptions options = base.ContextOptions();
            if (options == null)
            {
                options = new BrowserNewContextOptions();
            }

            options.Locale = "en-US";
            options.RecordVideoDir = "videos/";
            options.ScreenSize = new ScreenSize() { Width = 1980, Height = 1080 };
            options.ViewportSize = new ViewportSize() { Width = 1980, Height = 1080 };
            return options;
        }

        private static async Task CreateDonation(IPage page, bool wantInvoice = false)
        {
            page.SetDefaultNavigationTimeout(60_000);

            await page.GotoAsync(
                baseUrl,
                new PageGotoOptions() { Timeout = 120_000 });

            // Click a:has-text("Donate") >> nth=1
            await page.Locator("a:has-text(\"Donate\")").Nth(1).ClickAsync();
            await page.WaitForURLAsync(string.Concat(baseUrl, "Donation"));

            // Double click .more >> nth=0
            await page.Locator(".more").First.DblClickAsync();

            // Double click div:nth-child(2) > .input > .more
            await page.Locator("div:nth-child(2) > .input > .more").ClickAsync(new LocatorClickOptions
            {
                ClickCount = random.Next(1, 8),
            });

            // Double click div:nth-child(3) > .input > .more
            await page.Locator("div:nth-child(3) > .input > .more").ClickAsync(new LocatorClickOptions
            {
                ClickCount = random.Next(1, 8),
            });

            // Triple click div:nth-child(6) > .input > .more
            await page.Locator("div:nth-child(6) > .input > .more").ClickAsync(new LocatorClickOptions
            {
                ClickCount = random.Next(1, 8),
            });

            // Double click div:nth-child(5) > .input > .more
            await page.Locator("div:nth-child(5) > .input > .more").ClickAsync(new LocatorClickOptions
            {
                ClickCount = random.Next(1, 8),
            });


            // Click div:nth-child(4) > .input > .more
            await page.Locator("div:nth-child(4) > .input > .more").ClickAsync(new LocatorClickOptions
            {
                ClickCount = random.Next(1, 8),
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

            if (wantInvoice)
            {
                await page.EvaluateAsync<bool>("$('input#WantsReceiptCheckBox').click()");

                // Click [placeholder="Your address"]
                await page.Locator("[placeholder=\"Your address\"]").ClickAsync();

                // Fill [placeholder="Your address"]
                await page.Locator("[placeholder=\"Your address\"]").FillAsync("my address");

                // Click [placeholder="Your postal code"]
                await page.Locator("[placeholder=\"Your postal code\"]").ClickAsync();

                // Fill [placeholder="Your postal code"]
                await page.Locator("[placeholder=\"Your postal code\"]").FillAsync("12345");

                // Click [placeholder="Your tax number"]
                await page.Locator("[placeholder=\"Your tax number\"]").ClickAsync();

                // Fill [placeholder="Your tax number"]
                await page.Locator("[placeholder=\"Your tax number\"]").FillAsync("123456789");

            }

            await page.Locator("span:has-text(\"Donate\") >> nth=1").ClickAsync();
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task PaypalTest(bool wantInvoice)
        {
            // donation is created and navigated to payment page.
            await CreateDonation(Page, wantInvoice);

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

            await Page.GotoAsync(string.Concat(baseUrl, "Thanks**"));
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task TestCreditCard(bool wantInvoice)
        {
            // donation is created and navigated to payment page.
            await CreateDonation(Page, wantInvoice);

            // Click #pagamentounicre
            await Page.Locator("#pagamentounicre").ClickAsync();

            // Click span:has-text("Visa payment") >> nth=0
            await Page.Locator("span:has-text(\"Visa payment\")").First.ClickAsync();
            await Page.ScreenshotAsync(new PageScreenshotOptions() { Path = "sc.png" });
            try
            {
                await Page.WaitForURLAsync(
                    "https://gateway.test.easypay.pt/**",
                    new PageWaitForURLOptions()
                    {
                        WaitUntil = WaitUntilState.NetworkIdle,
                    });
            }
            catch (Exception ex)
            {
                testContext.WriteLine(ex.ToString());
            }
            string html = await Page.ContentAsync();

            testContext.WriteLine(html);

            await Page.ScreenshotAsync(new PageScreenshotOptions() { Path = "sc1.png" });
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

            await Page.GotoAsync(baseUrl);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task MBWayTest(bool wantInvoice)
        {
            // donation is created and navigated to payment page.
            await CreateDonation(Page, wantInvoice);

            // Click #pagamentombway
            await Page.Locator("#pagamentombway").ClickAsync();

            // Click [aria-label="Phone Number"]
            await Page.Locator("[aria-label=\"Phone Number\"]").ClickAsync();

            // Fill [aria-label="Phone Number"]
            await Page.Locator("[aria-label=\"Phone Number\"]").FillAsync("911234567");

            // Click span:has-text("MBway") >> nth=2
            await Page.Locator("span:has-text(\"MBway\")").Nth(2).ClickAsync();
            await Page.WaitForURLAsync(string.Concat(baseUrl, "Payments/MBWayPayment**"));

            await Page.WaitForURLAsync(string.Concat(baseUrl, "Thanks**"));

        }
    }
}