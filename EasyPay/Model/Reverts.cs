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
    /// Reverts refers to the process of reversing a previously made split payment. This means that the amounts that were divided and allocated to different recipients or accounts are returned to the original payer or redistributed as per the new instructions.
    /// </summary>
    [DataContract(Name = "Reverts")]
    public partial class Reverts : IValidatableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Reverts" /> class.
        /// </summary>
        /// <param name="id">A unique identifier for the resource. While typically formatted as a UUID (Universally Unique Identifier), it can also be in other formats as defined by the user. This field ensures the resource can be distinctly recognized and referenced..</param>
        /// <param name="value">The monetary amount requested for the transaction. This field is formatted as a double, and will be rounded to two decimal places (e.g., \&quot;100.00\&quot;). The value must be greater than or equal to 0.5..</param>
        /// <param name="marginValue">The monetary amount designated as the margin in a split payment. This field is formatted as a double, and will be rounded to two decimal places (e.g., \&quot;10.00\&quot;). The value must be less than the total value of the split. This specifies the portion of the funds that should be routed to the margin account..</param>
        public Reverts(string id = default(string), double value = default(double), double marginValue = default(double))
        {
            this.Id = id;
            this.Value = value;
            this.MarginValue = marginValue;
        }

        /// <summary>
        /// A unique identifier for the resource. While typically formatted as a UUID (Universally Unique Identifier), it can also be in other formats as defined by the user. This field ensures the resource can be distinctly recognized and referenced.
        /// </summary>
        /// <value>A unique identifier for the resource. While typically formatted as a UUID (Universally Unique Identifier), it can also be in other formats as defined by the user. This field ensures the resource can be distinctly recognized and referenced.</value>
        /*
        <example>c6056234-a3f9-42de-b944-3ed793fcb6bb</example>
        */
        [DataMember(Name = "id", EmitDefaultValue = false)]
        public string Id { get; set; }

        /// <summary>
        /// The monetary amount requested for the transaction. This field is formatted as a double, and will be rounded to two decimal places (e.g., \&quot;100.00\&quot;). The value must be greater than or equal to 0.5.
        /// </summary>
        /// <value>The monetary amount requested for the transaction. This field is formatted as a double, and will be rounded to two decimal places (e.g., \&quot;100.00\&quot;). The value must be greater than or equal to 0.5.</value>
        /*
        <example>15.32</example>
        */
        [DataMember(Name = "value", EmitDefaultValue = false)]
        public double Value { get; set; }

        /// <summary>
        /// The monetary amount designated as the margin in a split payment. This field is formatted as a double, and will be rounded to two decimal places (e.g., \&quot;10.00\&quot;). The value must be less than the total value of the split. This specifies the portion of the funds that should be routed to the margin account.
        /// </summary>
        /// <value>The monetary amount designated as the margin in a split payment. This field is formatted as a double, and will be rounded to two decimal places (e.g., \&quot;10.00\&quot;). The value must be less than the total value of the split. This specifies the portion of the funds that should be routed to the margin account.</value>
        [DataMember(Name = "margin_value", EmitDefaultValue = false)]
        public double MarginValue { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("class Reverts {\n");
            sb.Append("  Id: ").Append(Id).Append("\n");
            sb.Append("  Value: ").Append(Value).Append("\n");
            sb.Append("  MarginValue: ").Append(MarginValue).Append("\n");
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
            // Value (double) minimum
            if (this.Value < (double)0.5)
            {
                yield return new ValidationResult("Invalid value for Value, must be a value greater than or equal to 0.5.", new[] { "Value" });
            }

            yield break;
        }
    }

}
