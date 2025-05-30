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
    /// InlineObject12
    /// </summary>
    [DataContract(Name = "inline_object_12")]
    public partial class InlineObject12 : IValidatableObject
    {

        /// <summary>
        /// Gets or Sets Status
        /// </summary>
        [DataMember(Name = "status", IsRequired = true, EmitDefaultValue = true)]
        public CaptureStatus Status { get; set; }

        /// <summary>
        /// Gets or Sets PaymentType
        /// </summary>
        [DataMember(Name = "payment_type", EmitDefaultValue = false)]
        public PaymentTypes? PaymentType { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="InlineObject12" /> class.
        /// </summary>
        [JsonConstructorAttribute]
        protected InlineObject12() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="InlineObject12" /> class.
        /// </summary>
        /// <param name="id">A unique identifier for the resource. While typically formatted as a UUID (Universally Unique Identifier), it can also be in other formats as defined by the user. This field ensures the resource can be distinctly recognized and referenced. (required).</param>
        /// <param name="status">status (required).</param>
        /// <param name="descriptive">A text field that describes the transaction as it will appear on the end user&#39;s account statement. This is typically used to provide clear, recognizable information about the payment, such as \&quot;Payment of Invoice Nº 1982652\&quot; or \&quot;Ticket for Queen\&quot;. (required).</param>
        /// <param name="transactionKey">A customizable text field for users to input their own identifier for the resource. This can be any string that helps the user uniquely identify or reference the resource in their own system..</param>
        /// <param name="captureDate">The date when the action should be executed. This field specifies the exact day for capturing the transaction, formatted as \&quot;YYYY-MM-DD\&quot; (e.g., \&quot;2024-06-30\&quot;). It is optional and defaults to the current date if not specified..</param>
        /// <param name="account">account.</param>
        /// <param name="splits">splits.</param>
        /// <param name="paymentId">A unique identifier for the resource. While typically formatted as a UUID (Universally Unique Identifier), it can also be in other formats as defined by the user. This field ensures the resource can be distinctly recognized and referenced..</param>
        /// <param name="paymentType">paymentType.</param>
        /// <param name="value">The monetary amount requested for the transaction. This field is formatted as a double, and will be rounded to two decimal places (e.g., \&quot;100.00\&quot;). The value must be greater than or equal to 0.5..</param>
        /// <param name="force3ds">A boolean field indicating whether to enforce 3D Secure (3DS) authentication for the transaction. If set to true, 3DS authentication will be required, adding an additional layer of security by verifying the cardholder&#39;s identity during the transaction process. If set to false, 3DS authentication will not be enforced. This field helps enhance security and reduce fraud in online payments..</param>
        /// <param name="createdAt">The timestamp indicating when the resource was created. It is formatted as \&quot;YYYY-MM-DD HH:MM\&quot;..</param>
        /// <param name="updatedAt">The timestamp indicating when the resource was updated. It is formatted as \&quot;YYYY-MM-DD HH:MM\&quot;..</param>
        /// <param name="refunds">refunds.</param>
        public InlineObject12(string id = default(string), CaptureStatus status = default(CaptureStatus), string descriptive = default(string), string transactionKey = default(string), DateOnly captureDate = default(DateOnly), CaptureAccount account = default(CaptureAccount), Collection<CaptureSplit> splits = default(Collection<CaptureSplit>), string paymentId = default(string), PaymentTypes? paymentType = default(PaymentTypes?), double value = default(double), bool force3ds = default(bool), string createdAt = default(string), string updatedAt = default(string), Collection<Refund> refunds = default(Collection<Refund>))
        {
            // to ensure "id" is required (not null)
            if (id == null)
            {
                throw new ArgumentNullException("id is a required property for InlineObject12 and cannot be null");
            }
            this.Id = id;
            this.Status = status;
            // to ensure "descriptive" is required (not null)
            if (descriptive == null)
            {
                throw new ArgumentNullException("descriptive is a required property for InlineObject12 and cannot be null");
            }
            this.Descriptive = descriptive;
            this.TransactionKey = transactionKey;
            this.CaptureDate = captureDate;
            this.Account = account;
            this.Splits = splits;
            this.PaymentId = paymentId;
            this.PaymentType = paymentType;
            this.Value = value;
            this.Force3ds = force3ds;
            this.CreatedAt = createdAt;
            this.UpdatedAt = updatedAt;
            this.Refunds = refunds;
        }

        /// <summary>
        /// A unique identifier for the resource. While typically formatted as a UUID (Universally Unique Identifier), it can also be in other formats as defined by the user. This field ensures the resource can be distinctly recognized and referenced.
        /// </summary>
        /// <value>A unique identifier for the resource. While typically formatted as a UUID (Universally Unique Identifier), it can also be in other formats as defined by the user. This field ensures the resource can be distinctly recognized and referenced.</value>
        /*
        <example>c6056234-a3f9-42de-b944-3ed793fcb6bb</example>
        */
        [DataMember(Name = "id", IsRequired = true, EmitDefaultValue = true)]
        public string Id { get; set; }

        /// <summary>
        /// A text field that describes the transaction as it will appear on the end user&#39;s account statement. This is typically used to provide clear, recognizable information about the payment, such as \&quot;Payment of Invoice Nº 1982652\&quot; or \&quot;Ticket for Queen\&quot;.
        /// </summary>
        /// <value>A text field that describes the transaction as it will appear on the end user&#39;s account statement. This is typically used to provide clear, recognizable information about the payment, such as \&quot;Payment of Invoice Nº 1982652\&quot; or \&quot;Ticket for Queen\&quot;.</value>
        /*
        <example>Payment of Invoice Nº 1982652</example>
        */
        [DataMember(Name = "descriptive", IsRequired = true, EmitDefaultValue = true)]
        public string Descriptive { get; set; }

        /// <summary>
        /// A customizable text field for users to input their own identifier for the resource. This can be any string that helps the user uniquely identify or reference the resource in their own system.
        /// </summary>
        /// <value>A customizable text field for users to input their own identifier for the resource. This can be any string that helps the user uniquely identify or reference the resource in their own system.</value>
        /*
        <example>01J1PKR2RPHJNJQGFWGDYXY0KM</example>
        */
        [DataMember(Name = "transaction_key", EmitDefaultValue = false)]
        public string TransactionKey { get; set; }

        /// <summary>
        /// The date when the action should be executed. This field specifies the exact day for capturing the transaction, formatted as \&quot;YYYY-MM-DD\&quot; (e.g., \&quot;2024-06-30\&quot;). It is optional and defaults to the current date if not specified.
        /// </summary>
        /// <value>The date when the action should be executed. This field specifies the exact day for capturing the transaction, formatted as \&quot;YYYY-MM-DD\&quot; (e.g., \&quot;2024-06-30\&quot;). It is optional and defaults to the current date if not specified.</value>
        [DataMember(Name = "capture_date", EmitDefaultValue = false)]
        public DateOnly CaptureDate { get; set; }

        /// <summary>
        /// Gets or Sets Account
        /// </summary>
        [DataMember(Name = "account", EmitDefaultValue = false)]
        public CaptureAccount Account { get; set; }

        /// <summary>
        /// Gets or Sets Splits
        /// </summary>
        [DataMember(Name = "splits", EmitDefaultValue = false)]
        public Collection<CaptureSplit> Splits { get; set; }

        /// <summary>
        /// A unique identifier for the resource. While typically formatted as a UUID (Universally Unique Identifier), it can also be in other formats as defined by the user. This field ensures the resource can be distinctly recognized and referenced.
        /// </summary>
        /// <value>A unique identifier for the resource. While typically formatted as a UUID (Universally Unique Identifier), it can also be in other formats as defined by the user. This field ensures the resource can be distinctly recognized and referenced.</value>
        /*
        <example>c6056234-a3f9-42de-b944-3ed793fcb6bb</example>
        */
        [DataMember(Name = "payment_id", EmitDefaultValue = false)]
        public string PaymentId { get; set; }

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
        /// A boolean field indicating whether to enforce 3D Secure (3DS) authentication for the transaction. If set to true, 3DS authentication will be required, adding an additional layer of security by verifying the cardholder&#39;s identity during the transaction process. If set to false, 3DS authentication will not be enforced. This field helps enhance security and reduce fraud in online payments.
        /// </summary>
        /// <value>A boolean field indicating whether to enforce 3D Secure (3DS) authentication for the transaction. If set to true, 3DS authentication will be required, adding an additional layer of security by verifying the cardholder&#39;s identity during the transaction process. If set to false, 3DS authentication will not be enforced. This field helps enhance security and reduce fraud in online payments.</value>
        [DataMember(Name = "force_3ds", EmitDefaultValue = true)]
        public bool Force3ds { get; set; }

        /// <summary>
        /// The timestamp indicating when the resource was created. It is formatted as \&quot;YYYY-MM-DD HH:MM\&quot;.
        /// </summary>
        /// <value>The timestamp indicating when the resource was created. It is formatted as \&quot;YYYY-MM-DD HH:MM\&quot;.</value>
        /*
        <example>2006-01-02 15:04</example>
        */
        [DataMember(Name = "created_at", EmitDefaultValue = false)]
        public string CreatedAt { get; set; }

        /// <summary>
        /// The timestamp indicating when the resource was updated. It is formatted as \&quot;YYYY-MM-DD HH:MM\&quot;.
        /// </summary>
        /// <value>The timestamp indicating when the resource was updated. It is formatted as \&quot;YYYY-MM-DD HH:MM\&quot;.</value>
        /*
        <example>2006-01-02 15:04</example>
        */
        [DataMember(Name = "updated_at", EmitDefaultValue = false)]
        public string UpdatedAt { get; set; }

        /// <summary>
        /// Gets or Sets Refunds
        /// </summary>
        [DataMember(Name = "refunds", EmitDefaultValue = false)]
        public Collection<Refund> Refunds { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("class InlineObject12 {\n");
            sb.Append("  Id: ").Append(Id).Append("\n");
            sb.Append("  Status: ").Append(Status).Append("\n");
            sb.Append("  Descriptive: ").Append(Descriptive).Append("\n");
            sb.Append("  TransactionKey: ").Append(TransactionKey).Append("\n");
            sb.Append("  CaptureDate: ").Append(CaptureDate).Append("\n");
            sb.Append("  Account: ").Append(Account).Append("\n");
            sb.Append("  Splits: ").Append(Splits).Append("\n");
            sb.Append("  PaymentId: ").Append(PaymentId).Append("\n");
            sb.Append("  PaymentType: ").Append(PaymentType).Append("\n");
            sb.Append("  Value: ").Append(Value).Append("\n");
            sb.Append("  Force3ds: ").Append(Force3ds).Append("\n");
            sb.Append("  CreatedAt: ").Append(CreatedAt).Append("\n");
            sb.Append("  UpdatedAt: ").Append(UpdatedAt).Append("\n");
            sb.Append("  Refunds: ").Append(Refunds).Append("\n");
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
            // Descriptive (string) maxLength
            if (this.Descriptive != null && this.Descriptive.Length > 255)
            {
                yield return new ValidationResult("Invalid value for Descriptive, length must be less than 255.", new[] { "Descriptive" });
            }

            // TransactionKey (string) maxLength
            if (this.TransactionKey != null && this.TransactionKey.Length > 255)
            {
                yield return new ValidationResult("Invalid value for TransactionKey, length must be less than 255.", new[] { "TransactionKey" });
            }

            // Value (double) minimum
            if (this.Value < (double)0.5)
            {
                yield return new ValidationResult("Invalid value for Value, must be a value greater than or equal to 0.5.", new[] { "Value" });
            }

            yield break;
        }
    }

}
