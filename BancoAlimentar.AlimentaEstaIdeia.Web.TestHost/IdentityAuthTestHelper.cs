// -----------------------------------------------------------------------
// <copyright file="IdentityAuthTestHelper.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.TestHost
{
    using System.Collections.Generic;

    /// <summary>
    /// Shared form field values for Identity integration tests.
    /// </summary>
    public static class IdentityAuthTestHelper
    {
        /// <summary>
        /// Builds a valid register form payload.
        /// </summary>
        /// <param name="email">User email.</param>
        /// <param name="password">Password (defaults to integration test password).</param>
        /// <returns>Form fields for the register page.</returns>
        public static Dictionary<string, string> BuildValidRegisterForm(string email, string password = null)
        {
            password ??= IntegrationTestCredentials.DefaultPassword;
            return new Dictionary<string, string>
            {
                ["Input.Email"] = email,
                ["Input.FullName"] = "Integration Auth User",
                ["Input.Password"] = password,
                ["Input.ConfirmPassword"] = password,
                ["Input.PhoneNumber"] = "912345678",
                ["Input.Nif"] = "196807050",
                ["Input.CompanyName"] = "Integration Auth Co",
                ["Input.Address.Address1"] = "Rua Teste",
                ["Input.Address.Address2"] = string.Empty,
                ["Input.Address.City"] = "Lisboa",
                ["Input.Address.PostalCode"] = "1000-001",
                ["Input.Address.Country"] = "Portugal",
            };
        }
    }
}
