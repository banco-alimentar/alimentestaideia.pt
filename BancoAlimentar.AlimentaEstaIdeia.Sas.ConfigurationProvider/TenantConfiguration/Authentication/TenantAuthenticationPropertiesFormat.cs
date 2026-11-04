namespace BancoAlimentar.AlimentaEstaIdeia.Sas.ConfigurationProvider.TenantConfiguration.Authentication
{
    using Microsoft.AspNetCore.Authentication;

    internal sealed class TenantAuthenticationPropertiesFormat : ISecureDataFormat<AuthenticationProperties>
    {
        private readonly ISecureDataFormat<AuthenticationProperties> inner;
        private readonly string tenantPurpose;

        public TenantAuthenticationPropertiesFormat(ISecureDataFormat<AuthenticationProperties> inner, int tenantId)
        {
            this.inner = inner;
            this.tenantPurpose = $"Tenant:{tenantId}";
        }

        public string Protect(AuthenticationProperties data)
        {
            return this.inner.Protect(data, this.tenantPurpose);
        }

        public string Protect(AuthenticationProperties data, string? purpose)
        {
            return this.inner.Protect(data, this.GetPurpose(purpose));
        }

        public AuthenticationProperties? Unprotect(string? protectedText)
        {
            return this.inner.Unprotect(protectedText, this.tenantPurpose);
        }

        public AuthenticationProperties? Unprotect(string? protectedText, string? purpose)
        {
            return this.inner.Unprotect(protectedText, this.GetPurpose(purpose));
        }

        private string GetPurpose(string? purpose)
        {
            return string.Concat(this.tenantPurpose, "|", purpose);
        }
    }
}