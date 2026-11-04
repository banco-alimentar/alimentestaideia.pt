namespace BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration.Authentication
{
    /// <summary>
    /// Invalidates authentication options for all tenants.
    /// </summary>
    public interface ITenantAuthenticationOptionsCache
    {
        /// <summary>
        /// Clears cached authentication options.
        /// </summary>
        void Clear();
    }
}