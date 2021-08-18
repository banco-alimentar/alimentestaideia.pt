// -----------------------------------------------------------------------
// <copyright file="WebUserConverter.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.JsonConverter.PersonalData
{
    using System;
    using System.Collections;
    using System.Linq;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
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
                JObject target = new JObject();
                foreach (var p in personalDataProps)
                {
                    object serializeValue = p.GetValue(value);
                    if (serializeValue != null)
                    {
                        try
                        {
                            if (typeof(IEnumerable).IsAssignableFrom(serializeValue.GetType()))
                            {
                                target[p.Name] = JArray.FromObject(serializeValue, serializer);
                            }
                            else
                            {
                                target[p.Name] = JObject.FromObject(serializeValue, serializer);
                            }
                        }
                        catch (Exception ex)
                        {
                            target[p.Name] = new JValue(serializeValue);
                        }
                    }
                }

                target.WriteTo(writer);
            }
        }
    }
}
