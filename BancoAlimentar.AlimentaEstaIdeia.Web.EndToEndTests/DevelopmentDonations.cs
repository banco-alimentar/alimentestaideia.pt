using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;
using System.Text.RegularExpressions;

namespace BancoAlimentar.AlimentaEstaIdeia.Web.EndToEndTests
{

	[TestClass]
	public class DevelopmentDonations : PageTest
	{
		private static async Task CreateDonation(IPage page)
		{
			// Go to https://dev.alimentestaideia.pt/
			await page.GotoAsync("https://dev.alimentestaideia.pt/");

			// Click a:has-text("Donate") >> nth=1
			await page.Locator("a:has-text(\"Donate\")").Nth(1).ClickAsync();
			await page.WaitForURLAsync("https://dev.alimentestaideia.pt/Donation");

			// Double click .more >> nth=0
			await page.Locator(".more").First.DblClickAsync();

			// Double click div:nth-child(2) > .input > .more
			await page.Locator("div:nth-child(2) > .input > .more").DblClickAsync();

			// Double click div:nth-child(3) > .input > .more
			await page.Locator("div:nth-child(3) > .input > .more").DblClickAsync();

			// Triple click div:nth-child(6) > .input > .more
			await page.Locator("div:nth-child(6) > .input > .more").ClickAsync(new LocatorClickOptions
			{
				ClickCount = 3,
			});

			// Double click div:nth-child(5) > .input > .more
			await page.Locator("div:nth-child(5) > .input > .more").DblClickAsync();

			// Click div:nth-child(4) > .input > .more
			await page.Locator("div:nth-child(4) > .input > .more").ClickAsync();

			// Select 11
			await page.Locator("#FoodBankId").SelectOptionAsync(new[] { "11" });

			// Click [placeholder="Your name"]
			await page.Locator("#Name").ClickAsync();

			// Fill [placeholder="Your name"]
			await page.Locator("#Name").FillAsync("User Test");

			// Click [placeholder="Your company"]
			await page.Locator("#CompanyName").ClickAsync();

			// Fill [placeholder="Your company"]
			await page.Locator("#CompanyName").FillAsync("E2E Contoso");

			// Click [placeholder="Your e-mail"]
			await page.Locator("#Email").ClickAsync();

			// Fill [placeholder="Your e-mail"]
			await page.Locator("#Email").FillAsync("test@localhost.com");

			bool isCheckedTermsAndConditions = await page.EvaluateAsync<bool>("document.getElementById('AcceptsTermsCheckBox').checked = true;");

			Assert.IsTrue(isCheckedTermsAndConditions);

			await page.Locator("span:has-text(\"Donate\") >> nth=1").ClickAsync();
		}

		[TestMethod]
		public async Task PaypalTest()
		{
			Page.SetDefaultNavigationTimeout(300000);

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
	}
}