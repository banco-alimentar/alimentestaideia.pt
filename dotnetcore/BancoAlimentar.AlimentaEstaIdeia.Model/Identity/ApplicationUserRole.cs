namespace BancoAlimentar.AlimentaEstaIdeia.Model.Identity
{
    using Microsoft.AspNetCore.Identity;

    public class ApplicationUserRole : IdentityUserRole<string>
    {
        public virtual WebUser User { get; set; }
        public virtual ApplicationRole Role { get; set; }
    }
}
