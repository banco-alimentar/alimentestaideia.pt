// -----------------------------------------------------------------------
// <copyright file="WebUserConverter.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.JsonConverter.PersonalData
{
    using System;
    using System.Linq;
    using Microsoft.AspNetCore.Identity;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Serialize instances into json by only showing personal data.
    /// </summary>
    public class GenericPersonalDataConverter<T> : JsonConverter
    {
        /// <inheritdoc/>
        public override bool CanConvert(Type objectType)
        {
            if (objectType == typeof(T))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <inheritdoc/>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return null;
        }

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value != null)
            {
                var personalDataProps = typeof(T).GetProperties().Where(
                            prop => Attribute.IsDefined(prop, typeof(PersonalDataAttribute)));
                JObject fullObject = JObject.FromObject(value);
                foreach (var item in fullObject.Properties().ToList())
                {
                    var found = personalDataProps.Where(p => p.Name == item.Name).FirstOrDefault();
                    if (found == null)
                    {
                        fullObject.Remove(item.Name);
                    }
                }

                fullObject.WriteTo(writer);
            }
        }
    }
}
