namespace BancoAlimentar.AlimentaEstaIdeia.Model.Identity
{
    using Microsoft.AspNetCore.Identity;

    public class ApplicationUserClaim : IdentityUserClaim<string>
    {
        public virtual WebUser User { get; set; }
    }
}
