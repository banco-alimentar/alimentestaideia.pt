namespace BancoAlimentar.AlimentaEstaIdeia.Model.Identity
{
    using Microsoft.AspNetCore.Identity;

    public class ApplicationUserToken : IdentityUserToken<string>
    {
        public virtual WebUser User { get; set; }
    }
}
