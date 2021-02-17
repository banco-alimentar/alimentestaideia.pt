using BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Data;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<BancoAlimentarAlimentaEstaIdeiaWebUser> WebUser { get; set; }
    }
}
