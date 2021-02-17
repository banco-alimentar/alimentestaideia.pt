using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Data
{
    // Add profile data for application users by adding properties to the BancoAlimentarAlimentaEstaIdeiaWebUser class
    public class BancoAlimentarAlimentaEstaIdeiaWebUser : IdentityUser
    {
        public string PreferedFoodBank { get; set; }
    }
}
