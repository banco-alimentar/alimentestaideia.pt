namespace BancoAlimentar.AlimentaEstaIdeia.Web
{
    using Microsoft.Extensions.Configuration;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    public static class ConfigurationExtensions
    {
        public static bool IsSendingEmailEnabled(this IConfiguration configuration)
        {
            bool result = false;

            if (configuration != null)
            {
                result = configuration.GetValue<bool>("IsEmailEnabled");
            }

            return result;
        }

        public static string GetFilePath(this IConfiguration configuration, string key)
        {
            return configuration.GetValue<string>(key).Replace('/', Path.DirectorySeparatorChar);
        }
    }
}
