namespace BancoAlimentar.AlimentaEstaIdeia.Model.Identity
{
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Identity;

    public class ApplicationRole : IdentityRole
    {
        public ApplicationRole()
        {

        }

        public ApplicationRole(string rolName)
            : base(rolName)
        { }

        public virtual ICollection<ApplicationUserRole> UserRoles { get; set; }

        public virtual ICollection<ApplicationRoleClaim> RoleClaims { get; set; }
    }
}
