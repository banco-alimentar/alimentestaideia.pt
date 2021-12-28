// -----------------------------------------------------------------------
// <copyright file="SessionExtensions.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web
{
    using Microsoft.AspNetCore.Http;
    using Newtonsoft.Json;

    /// <summary>
    /// Sesion extension methods.
    /// </summary>
    public static class SessionExtensions
    {
        /// <summary>
        /// Save the instance of the object, serialized as json, in the session objec with specified key.
        /// </summary>
        /// <param name="session">A reference to the <see cref="ISession"/>.</param>
        /// <param name="key">Key name.</param>
        /// <param name="value">Object to save in session.</param>
        public static void SaveObjectAsJson(this ISession session, string key, object value)
        {
            string json = JsonConvert.SerializeObject(value);
            session.SetString(key, json);
        }

        /// <summary>
        /// Gets the object, serialized as json, from the session.
        /// </summary>
        /// <typeparam name="T">Type of the object serialized as json.</typeparam>
        /// <param name="session">A reference to the <see cref="ISession"/>.</param>
        /// <param name="key">Key name.</param>
        /// <returns>A instance of the object that was deserialized from json.</returns>
        public static T GetObjectFromJson<T>(this ISession session, string key)
        {
            T result = default(T);
            string json = session.GetString(key);
            if (!string.IsNullOrEmpty(json))
            {
                result = JsonConvert.DeserializeObject<T>(json);
            }

            return result;
        }
    }
}
