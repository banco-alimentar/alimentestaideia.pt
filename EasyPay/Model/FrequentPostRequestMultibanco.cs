/*
 * Easypay Payments API
 *
 * <a href='https://www.easypay.pt/en/legal-terms-and-conditions/' class='item'>Terms conditions and legal terms</a><br><a href='https://www.easypay.pt/en/privacy-and-data-protection-policy/' class='item'>Privacy Policy</a>
 *
 * The version of the OpenAPI document: 2.0
 * Contact: tec@easypay.pt
 * Generated by: https://github.com/openapitools/openapi-generator.git
 */


using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System.ComponentModel.DataAnnotations;
using OpenAPIDateConverter = Easypay.Rest.Client.Client.OpenAPIDateConverter;

namespace Easypay.Rest.Client.Model
{
    /// <summary>
    /// Configuration of the Multibanco payment
    /// </summary>
    [DataContract(Name = "_frequent_post_request_multibanco")]
    public partial class FrequentPostRequestMultibanco : IValidatableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FrequentPostRequestMultibanco" /> class.
        /// </summary>
        [JsonConstructorAttribute]
        protected FrequentPostRequestMultibanco() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="FrequentPostRequestMultibanco" /> class.
        /// </summary>
        /// <param name="reference">This field controls which reference is going to be created. It&#39;s only available if you have an exclusive Multibanco entity. (required).</param>
        public FrequentPostRequestMultibanco(string reference = default(string))
        {
            // to ensure "reference" is required (not null)
            if (reference == null)
            {
                throw new ArgumentNullException("reference is a required property for FrequentPostRequestMultibanco and cannot be null");
            }
            this.Reference = reference;
        }

        /// <summary>
        /// This field controls which reference is going to be created. It&#39;s only available if you have an exclusive Multibanco entity.
        /// </summary>
        /// <value>This field controls which reference is going to be created. It&#39;s only available if you have an exclusive Multibanco entity.</value>
        /*
        <example>505237431</example>
        */
        [DataMember(Name = "reference", IsRequired = true, EmitDefaultValue = true)]
        public string Reference { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("class FrequentPostRequestMultibanco {\n");
            sb.Append("  Reference: ").Append(Reference).Append("\n");
            sb.Append("}\n");
            return sb.ToString();
        }

        /// <summary>
        /// Returns the JSON string presentation of the object
        /// </summary>
        /// <returns>JSON string presentation of the object</returns>
        public virtual string ToJson()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented);
        }

        /// <summary>
        /// To validate all properties of the instance
        /// </summary>
        /// <param name="validationContext">Validation context</param>
        /// <returns>Validation Result</returns>
        IEnumerable<ValidationResult> IValidatableObject.Validate(ValidationContext validationContext)
        {
            // Reference (string) maxLength
            if (this.Reference != null && this.Reference.Length > 9)
            {
                yield return new ValidationResult("Invalid value for Reference, length must be less than 9.", new[] { "Reference" });
            }

            // Reference (string) minLength
            if (this.Reference != null && this.Reference.Length < 9)
            {
                yield return new ValidationResult("Invalid value for Reference, length must be greater than 9.", new[] { "Reference" });
            }

            if (this.Reference != null)
            {
                // Reference (string) pattern
                Regex regexReference = new Regex(@"^[0-9]*$", RegexOptions.CultureInvariant);
                if (!regexReference.Match(this.Reference).Success)
                {
                    yield return new System.ComponentModel.DataAnnotations.ValidationResult("Invalid value for Reference, must match a pattern of " + regexReference, new[] { "Reference" });
                }
            }

            yield break;
        }
    }

}
