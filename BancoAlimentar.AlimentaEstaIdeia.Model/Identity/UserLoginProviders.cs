// -----------------------------------------------------------------------
// <copyright file="UserLoginProviders.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Model.Identity
{
    /// <summary>
    /// Known login provider keys used for reporting.
    /// </summary>
    public static class UserLoginProviders
    {
        /// <summary>
        /// Local password authentication.
        /// </summary>
        public const string Password = "Password";

        /// <summary>
        /// Resolves a display-friendly provider name.
        /// </summary>
        /// <param name="loginProvider">Stored provider key.</param>
        /// <returns>Human-readable provider name.</returns>
        public static string GetDisplayName(string loginProvider)
        {
            return loginProvider switch
            {
                Password => "Password",
                "Google" => "Google",
                "Facebook" => "Facebook",
                "Microsoft" => "Microsoft",
                "Twitter" => "Twitter",
                _ => loginProvider ?? "Unknown",
            };
        }
    }
}
