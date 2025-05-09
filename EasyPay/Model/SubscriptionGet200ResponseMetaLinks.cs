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
    /// SubscriptionGet200ResponseMetaLinks
    /// </summary>
    [DataContract(Name = "_subscription_get_200_response_meta_links")]
    public partial class SubscriptionGet200ResponseMetaLinks : IValidatableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionGet200ResponseMetaLinks" /> class.
        /// </summary>
        /// <param name="first">first.</param>
        /// <param name="prev">prev.</param>
        /// <param name="next">next.</param>
        /// <param name="last">last.</param>
        public SubscriptionGet200ResponseMetaLinks(string first = default(string), string prev = default(string), string next = default(string), string last = default(string))
        {
            this.First = first;
            this.Prev = prev;
            this.Next = next;
            this.Last = last;
        }

        /// <summary>
        /// Gets or Sets First
        /// </summary>
        [DataMember(Name = "first", EmitDefaultValue = false)]
        public string First { get; set; }

        /// <summary>
        /// Gets or Sets Prev
        /// </summary>
        [DataMember(Name = "prev", EmitDefaultValue = false)]
        public string Prev { get; set; }

        /// <summary>
        /// Gets or Sets Next
        /// </summary>
        [DataMember(Name = "next", EmitDefaultValue = false)]
        public string Next { get; set; }

        /// <summary>
        /// Gets or Sets Last
        /// </summary>
        [DataMember(Name = "last", EmitDefaultValue = false)]
        public string Last { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("class SubscriptionGet200ResponseMetaLinks {\n");
            sb.Append("  First: ").Append(First).Append("\n");
            sb.Append("  Prev: ").Append(Prev).Append("\n");
            sb.Append("  Next: ").Append(Next).Append("\n");
            sb.Append("  Last: ").Append(Last).Append("\n");
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
            yield break;
        }
    }

}
