// -----------------------------------------------------------------------
// <copyright file="ConfigurationExtensions.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web
{
    using System.IO;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Configuration extensions.
    /// </summary>
    public static class ConfigurationExtensions
    {
        /// <summary>
        /// Gets if sending email is enabled or not.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        /// <returns>True if the system can sent emails, false otherwise.</returns>
        public static bool IsSendingEmailEnabled(this IConfiguration configuration)
        {
            bool result = false;

            if (configuration != null)
            {
                result = configuration.GetValue<bool>("IsEmailEnabled");
            }

            return result;
        }

        /// <summary>
        /// Gets file path from configuration.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        /// <param name="key">Configuration key.</param>
        /// <returns>File's path.</returns>
        public static string GetFilePath(this IConfiguration configuration, string key)
        {
            var config = configuration[key];
            return config.Replace('/', Path.DirectorySeparatorChar);
        }
    }
}
