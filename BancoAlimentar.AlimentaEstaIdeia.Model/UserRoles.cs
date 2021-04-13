// -----------------------------------------------------------------------
// <copyright file="UserRoles.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Model
{
    /// <summary>
    /// Define the user roles in the system.
    /// </summary>
    public enum UserRoles
    {
        /// <summary>
        /// This is the super admin it has access to everything.
        /// </summary>
        SuperAdmin,

        /// <summary>
        /// Admin role, it has access to the /admin/ area.
        /// </summary>
        Admin,
    }
}
