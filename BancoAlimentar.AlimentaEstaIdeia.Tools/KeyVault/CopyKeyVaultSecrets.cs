namespace BancoAlimentar.AlimentaEstaIdeia.Tools.KeyVault
{
    using Azure;
    using Azure.Core;
    using Azure.Identity;
    using Azure.Security.KeyVault.Secrets;
    using System;
    using System.Threading.Tasks;

    public class CopyKeyVaultSecrets
    {
        public static async Task Copy(Uri source, Uri destination)
        {
            if (source != null && destination != null && source != destination)
            {
                TokenCredential credential = new DefaultAzureCredential();
                credential = new AzureCliCredential(new AzureCliCredentialOptions() { TenantId = "65004861-f3b7-448e-aa2c-6485af17f703" });
                SecretClient sourceClient = new SecretClient(vaultUri: source, credential: credential);
                SecretClient targetClient = new SecretClient(vaultUri: destination, credential: credential);

                AsyncPageable<SecretProperties> page = sourceClient.GetPropertiesOfSecretsAsync();
                await foreach (SecretProperties secretItem in page)
                {
                    Response<KeyVaultSecret> responseSecret = await sourceClient.GetSecretAsync(secretItem.Name);
                    await targetClient.SetSecretAsync(responseSecret.Value);
                }
            }
        }
    }
}
