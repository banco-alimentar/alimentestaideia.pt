namespace BancoAlimentar.AlimentaEstaIdeia.Model.Identity
{
    using Microsoft.AspNetCore.Identity;

    public class ApplicationUserLogin : IdentityUserLogin<string>
    {
        public virtual WebUser User { get; set; }
    }
}
