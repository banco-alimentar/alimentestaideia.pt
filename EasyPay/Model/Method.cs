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
    /// Method
    /// </summary>
    [DataContract(Name = "Method")]
    public partial class Method : IValidatableObject
    {
        /// <summary>
        /// Indicates the type of credit card used for the payment (e.g., \&quot;Visa\&quot;, \&quot;MasterCard\&quot;). This field is only applicable for credit card transactions.
        /// </summary>
        /// <value>Indicates the type of credit card used for the payment (e.g., \&quot;Visa\&quot;, \&quot;MasterCard\&quot;). This field is only applicable for credit card transactions.</value>
        [JsonConverter(typeof(StringEnumConverter))]
        public enum CardTypeEnum
        {
            /// <summary>
            /// Enum VISA for value: VISA
            /// </summary>
            [EnumMember(Value = "VISA")]
            VISA = 1,

            /// <summary>
            /// Enum MasterCard for value: MasterCard
            /// </summary>
            [EnumMember(Value = "MasterCard")]
            MasterCard = 2
        }


        /// <summary>
        /// Indicates the type of credit card used for the payment (e.g., \&quot;Visa\&quot;, \&quot;MasterCard\&quot;). This field is only applicable for credit card transactions.
        /// </summary>
        /// <value>Indicates the type of credit card used for the payment (e.g., \&quot;Visa\&quot;, \&quot;MasterCard\&quot;). This field is only applicable for credit card transactions.</value>
        [DataMember(Name = "card_type", EmitDefaultValue = false)]
        public CardTypeEnum? CardType { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="Method" /> class.
        /// </summary>
        [JsonConstructorAttribute]
        protected Method() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="Method" /> class.
        /// </summary>
        /// <param name="type">type (required).</param>
        /// <param name="status">status (required).</param>
        /// <param name="sddMandate">sddMandate.</param>
        /// <param name="cardType">Indicates the type of credit card used for the payment (e.g., \&quot;Visa\&quot;, \&quot;MasterCard\&quot;). This field is only applicable for credit card transactions..</param>
        /// <param name="lastFour">The last four digits of the credit card number used for the payment. This field is only applicable for credit card transactions..</param>
        /// <param name="expirationDate">expirationDate.</param>
        /// <param name="url">The URL where the merchant should redirect the user to complete Credit Card Payments. This field is only applicable and available for credit card payment transactions..</param>
        /// <param name="alias">A unique identifier for the user within the MBWay system, used to identify the user in the MBWay SDK. This field is only applicable and available for MBWay transactions..</param>
        /// <param name="entity">The identifier for the Multibanco entity to which the payment should be routed. This field specifies the entity code required for processing the payment through the Multibanco network, ensuring that the payment is directed to the correct recipient..</param>
        /// <param name="reference">The unique payment reference number issued for the specific Multibanco entity. This field is used by the end user to make a payment at an ATM or through home banking. It ensures that the payment is correctly attributed to the intended transaction and recipient..</param>
        /// <param name="iban">The International Bank Account Number (IBAN) of the debtor&#39;s account. This field is used to uniquely identify the debtor&#39;s bank account across international borders, ensuring accurate and efficient processing of SEPA Direct Debit transactions. The IBAN is a standardized format that includes the country code, check digits, bank code, and account number..</param>
        public Method(string type = default(string), string status = default(string), SddMandate sddMandate = default(SddMandate), CardTypeEnum? cardType = default(CardTypeEnum?), string lastFour = default(string), string expirationDate = default(string), string url = default(string), string alias = default(string), string entity = default(string), string reference = default(string), string iban = default(string))
        {
            // to ensure "type" is required (not null)
            if (type == null)
            {
                throw new ArgumentNullException("type is a required property for Method and cannot be null");
            }
            this.Type = type;
            // to ensure "status" is required (not null)
            if (status == null)
            {
                throw new ArgumentNullException("status is a required property for Method and cannot be null");
            }
            this.Status = status;
            this.SddMandate = sddMandate;
            this.CardType = cardType;
            this.LastFour = lastFour;
            this.ExpirationDate = expirationDate;
            this.Url = url;
            this.Alias = alias;
            this.Entity = entity;
            this.Reference = reference;
            this.Iban = iban;
        }

        /// <summary>
        /// Gets or Sets Type
        /// </summary>
        [DataMember(Name = "type", IsRequired = true, EmitDefaultValue = true)]
        public string Type { get; set; }

        /// <summary>
        /// Gets or Sets Status
        /// </summary>
        [DataMember(Name = "status", IsRequired = true, EmitDefaultValue = true)]
        public string Status { get; set; }

        /// <summary>
        /// Gets or Sets SddMandate
        /// </summary>
        [DataMember(Name = "sdd_mandate", EmitDefaultValue = false)]
        public SddMandate SddMandate { get; set; }

        /// <summary>
        /// The last four digits of the credit card number used for the payment. This field is only applicable for credit card transactions.
        /// </summary>
        /// <value>The last four digits of the credit card number used for the payment. This field is only applicable for credit card transactions.</value>
        /*
        <example>1234</example>
        */
        [DataMember(Name = "last_four", EmitDefaultValue = false)]
        public string LastFour { get; set; }

        /// <summary>
        /// Gets or Sets ExpirationDate
        /// </summary>
        [DataMember(Name = "expiration_date", EmitDefaultValue = false)]
        public string ExpirationDate { get; set; }

        /// <summary>
        /// The URL where the merchant should redirect the user to complete Credit Card Payments. This field is only applicable and available for credit card payment transactions.
        /// </summary>
        /// <value>The URL where the merchant should redirect the user to complete Credit Card Payments. This field is only applicable and available for credit card payment transactions.</value>
        /*
        <example>https://cc.easypay.pt/v3/public/card-details/cec55ab6-bda3-4ab3-af22-04d2ad2c046a</example>
        */
        [DataMember(Name = "url", EmitDefaultValue = false)]
        public string Url { get; set; }

        /// <summary>
        /// A unique identifier for the user within the MBWay system, used to identify the user in the MBWay SDK. This field is only applicable and available for MBWay transactions.
        /// </summary>
        /// <value>A unique identifier for the user within the MBWay system, used to identify the user in the MBWay SDK. This field is only applicable and available for MBWay transactions.</value>
        [DataMember(Name = "alias", EmitDefaultValue = false)]
        [Obsolete]
        public string Alias { get; set; }

        /// <summary>
        /// The identifier for the Multibanco entity to which the payment should be routed. This field specifies the entity code required for processing the payment through the Multibanco network, ensuring that the payment is directed to the correct recipient.
        /// </summary>
        /// <value>The identifier for the Multibanco entity to which the payment should be routed. This field specifies the entity code required for processing the payment through the Multibanco network, ensuring that the payment is directed to the correct recipient.</value>
        /*
        <example>11683</example>
        */
        [DataMember(Name = "entity", EmitDefaultValue = false)]
        public string Entity { get; set; }

        /// <summary>
        /// The unique payment reference number issued for the specific Multibanco entity. This field is used by the end user to make a payment at an ATM or through home banking. It ensures that the payment is correctly attributed to the intended transaction and recipient.
        /// </summary>
        /// <value>The unique payment reference number issued for the specific Multibanco entity. This field is used by the end user to make a payment at an ATM or through home banking. It ensures that the payment is correctly attributed to the intended transaction and recipient.</value>
        /*
        <example>665876931</example>
        */
        [DataMember(Name = "reference", EmitDefaultValue = false)]
        public string Reference { get; set; }

        /// <summary>
        /// The International Bank Account Number (IBAN) of the debtor&#39;s account. This field is used to uniquely identify the debtor&#39;s bank account across international borders, ensuring accurate and efficient processing of SEPA Direct Debit transactions. The IBAN is a standardized format that includes the country code, check digits, bank code, and account number.
        /// </summary>
        /// <value>The International Bank Account Number (IBAN) of the debtor&#39;s account. This field is used to uniquely identify the debtor&#39;s bank account across international borders, ensuring accurate and efficient processing of SEPA Direct Debit transactions. The IBAN is a standardized format that includes the country code, check digits, bank code, and account number.</value>
        /*
        <example>PT50000747199140461443823</example>
        */
        [DataMember(Name = "iban", EmitDefaultValue = false)]
        public string Iban { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("class Method {\n");
            sb.Append("  Type: ").Append(Type).Append("\n");
            sb.Append("  Status: ").Append(Status).Append("\n");
            sb.Append("  SddMandate: ").Append(SddMandate).Append("\n");
            sb.Append("  CardType: ").Append(CardType).Append("\n");
            sb.Append("  LastFour: ").Append(LastFour).Append("\n");
            sb.Append("  ExpirationDate: ").Append(ExpirationDate).Append("\n");
            sb.Append("  Url: ").Append(Url).Append("\n");
            sb.Append("  Alias: ").Append(Alias).Append("\n");
            sb.Append("  Entity: ").Append(Entity).Append("\n");
            sb.Append("  Reference: ").Append(Reference).Append("\n");
            sb.Append("  Iban: ").Append(Iban).Append("\n");
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
            // Entity (string) maxLength
            if (this.Entity != null && this.Entity.Length > 5)
            {
                yield return new ValidationResult("Invalid value for Entity, length must be less than 5.", new[] { "Entity" });
            }

            // Entity (string) minLength
            if (this.Entity != null && this.Entity.Length < 5)
            {
                yield return new ValidationResult("Invalid value for Entity, length must be greater than 5.", new[] { "Entity" });
            }

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

            // Iban (string) maxLength
            if (this.Iban != null && this.Iban.Length > 34)
            {
                yield return new ValidationResult("Invalid value for Iban, length must be less than 34.", new[] { "Iban" });
            }

            yield break;
        }
    }

}
