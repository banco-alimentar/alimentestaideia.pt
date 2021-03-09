namespace BancoAlimentar.AlimentaEstaIdeia.Model.Initializer
{
    using System;
    using System.Linq;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;

    /// <summary>
    /// Initialize the WebUser table when migration.
    /// </summary>
    public static class AnonymousUserDbInitializer
    {
        public static void Initialize(ApplicationDbContext context)
        {
            string zeroId = new Guid().ToString();
            WebUser user = context.WebUser.Where(p => p.Id == zeroId).FirstOrDefault();
            if (user == null)
            {
                user = new WebUser()
                {
                    Id = zeroId,
                    Email = "noemail@email.com",
                    UserName = "AnonymousUser",
                };

                context.WebUser.Add(user);
                context.SaveChanges();
            }
        }
    }
}
