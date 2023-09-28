// -----------------------------------------------------------------------
// <copyright file="GuidConverter.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.JsonConverter
{
    using System;
    using Newtonsoft.Json;

    /// <summary>
    /// Default Guid converter.
    /// </summary>
    public class GuidConverter : Newtonsoft.Json.JsonConverter
    {
        /// <inheritdoc/>
        public override bool CanConvert(Type objectType)
        {
            return typeof(Guid) == objectType;
        }

        /// <summary>
        /// Read the guid from the json value.
        /// </summary>
        /// <param name="reader">A reference to the reader.</param>
        /// <param name="objectType">Object type.</param>
        /// <param name="existingValue">Existing value.</param>
        /// <param name="serializer">Serializer.</param>
        /// <returns>The Guid.</returns>
        /// <exception cref="ArgumentException">Possible excepion.</exception>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            switch (reader.TokenType)
            {
                case JsonToken.Null:
                    return Guid.Empty;
                case JsonToken.String:
                    string str = reader.Value as string;
                    if (string.IsNullOrEmpty(str))
                    {
                        return Guid.Empty;
                    }
                    else
                    {
                        return new Guid(str);
                    }

                default:
                    throw new ArgumentException("Invalid token type");
            }
        }

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (Guid.Empty.Equals(value))
            {
                writer.WriteValue(string.Empty);
            }
            else
            {
                writer.WriteValue((Guid)value);
            }
        }
    }
}
