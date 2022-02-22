namespace BancoAlimentar.AlimentaEstaIdeia.Tools.KeyVault
{
    using Azure;
    using Azure.Core;
    using Azure.Identity;
    using Azure.Security.KeyVault.Secrets;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class DuplicateEasyPayAndPayPalConfig
    {
        public static async Task Execute(Uri source, ApplicationDbContext context)
        {
            TokenCredential credential = new DefaultAzureCredential();
            credential = new AzureCliCredential(new AzureCliCredentialOptions() { TenantId = "65004861-f3b7-448e-aa2c-6485af17f703" });
            SecretClient sourceClient = new SecretClient(vaultUri: source, credential: credential);
            var accountId = await sourceClient.GetSecretAsync("Easypay--AccountId");
            var apiKey = await sourceClient.GetSecretAsync("Easypay--ApiKey");
            var clientId = await sourceClient.GetSecretAsync("PayPal--clientId");
            var clientSecret = await sourceClient.GetSecretAsync("PayPal--clientSecret");

            foreach (var foodBank in context.FoodBanks.ToList())
            {
                await sourceClient.SetSecretAsync(new KeyVaultSecret($"Easypay--AccountId-{foodBank.Id}", accountId.Value.Value));
                await sourceClient.SetSecretAsync(new KeyVaultSecret($"Easypay--ApiKey-{foodBank.Id}", apiKey.Value.Value));
                await sourceClient.SetSecretAsync(new KeyVaultSecret($"PayPal--clientId-{foodBank.Id}", clientId.Value.Value));
                await sourceClient.SetSecretAsync(new KeyVaultSecret($"PayPal--clientSecret-{foodBank.Id}", clientSecret.Value.Value));
            }

        }
    }
}
