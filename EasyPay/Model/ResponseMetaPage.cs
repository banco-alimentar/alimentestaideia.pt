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
    /// An object containing information about the pagination of pages within the collection.
    /// </summary>
    [DataContract(Name = "ResponseMeta_page")]
    public partial class ResponseMetaPage : IValidatableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResponseMetaPage" /> class.
        /// </summary>
        [JsonConstructorAttribute]
        protected ResponseMetaPage() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="ResponseMetaPage" /> class.
        /// </summary>
        /// <param name="current">The current page number being viewed. (required).</param>
        /// <param name="total">The total number of pages available in the collection. (required).</param>
        public ResponseMetaPage(int current = default(int), int total = default(int))
        {
            this.Current = current;
            this.Total = total;
        }

        /// <summary>
        /// The current page number being viewed.
        /// </summary>
        /// <value>The current page number being viewed.</value>
        [DataMember(Name = "current", IsRequired = true, EmitDefaultValue = true)]
        public int Current { get; set; }

        /// <summary>
        /// The total number of pages available in the collection.
        /// </summary>
        /// <value>The total number of pages available in the collection.</value>
        [DataMember(Name = "total", IsRequired = true, EmitDefaultValue = true)]
        public int Total { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("class ResponseMetaPage {\n");
            sb.Append("  Current: ").Append(Current).Append("\n");
            sb.Append("  Total: ").Append(Total).Append("\n");
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
