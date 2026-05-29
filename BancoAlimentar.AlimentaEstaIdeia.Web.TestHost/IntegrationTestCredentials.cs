// -----------------------------------------------------------------------
// <copyright file="IntegrationTestCredentials.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.TestHost
{
    /// <summary>
    /// Shared credentials for in-memory integration tests only. Not used in production.
    /// </summary>
    public static class IntegrationTestCredentials
    {
        /// <summary>
        /// Password that satisfies ASP.NET Identity defaults for seeded test users.
        /// </summary>
        public const string DefaultPassword = "IntegrationTestOnly1!";
    }
}
