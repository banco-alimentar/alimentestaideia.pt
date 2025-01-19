// -----------------------------------------------------------------------
// <copyright file="AuthorisationNotificationRequestAuthorisation.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Api.Model
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;
    using System.Text;

    /// <summary>
    /// AuthorisationNotificationRequestAuthorisation.
    /// </summary>
    [DataContract(Name = "AuthorisationNotificationRequest_authorisation")]
    public partial class AuthorisationNotificationRequestAuthorisation : IValidatableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AuthorisationNotificationRequestAuthorisation" /> class.
        /// </summary>
        /// <param name="id">id.</param>
        public AuthorisationNotificationRequestAuthorisation(Guid id = default(Guid))
        {
            this.Id = id;
        }

        /// <summary>
        /// Gets or Sets Id.
        /// </summary>
        [DataMember(Name = "id", EmitDefaultValue = false)]
        public Guid Id { get; set; }

        /// <summary>
        /// Returns the string presentation of the object.
        /// </summary>
        /// <returns>String presentation of the object.</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class AuthorisationNotificationRequestAuthorisation {\n");
            sb.Append("  Id: ").Append(Id).Append("\n");
            sb.Append("}\n");
            return sb.ToString();
        }

        /// <summary>
        /// Returns the JSON string presentation of the object.
        /// </summary>
        /// <returns>JSON string presentation of the object.</returns>
        public virtual string ToJson()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented);
        }

        /// <summary>
        /// To validate all properties of the instance.
        /// </summary>
        /// <param name="validationContext">Validation context.</param>
        /// <returns>Validation Result.</returns>
        IEnumerable<System.ComponentModel.DataAnnotations.ValidationResult> IValidatableObject.Validate(ValidationContext validationContext)
        {
            yield break;
        }
    }
}