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
    /// PaymentCapture
    /// </summary>
    [DataContract(Name = "Payment_Capture")]
    public partial class PaymentCapture : IValidatableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PaymentCapture" /> class.
        /// </summary>
        /// <param name="transactionKey">transactionKey.</param>
        /// <param name="captureDate">captureDate.</param>
        /// <param name="account">account.</param>
        /// <param name="status">status.</param>
        /// <param name="splits">splits.</param>
        /// <param name="paymentType">paymentType.</param>
        /// <param name="paymentId">paymentId.</param>
        /// <param name="id">id.</param>
        /// <param name="descriptive">descriptive.</param>
        /// <param name="value">value.</param>
        /// <param name="force3ds">Whether or not you want to force the 3DS authentication. (default to false).</param>
        public PaymentCapture(string transactionKey = default(string), string captureDate = default(string), SubscriptionPost201ResponseCustomer account = default(SubscriptionPost201ResponseCustomer), string status = default(string), Collection<CaptureSplit> splits = default(Collection<CaptureSplit>), string paymentType = default(string), string paymentId = default(string), string id = default(string), string descriptive = default(string), double value = default(double), bool force3ds = false)
        {
            this.TransactionKey = transactionKey;
            this.CaptureDate = captureDate;
            this.Account = account;
            this.Status = status;
            this.Splits = splits;
            this.PaymentType = paymentType;
            this.PaymentId = paymentId;
            this.Id = id;
            this.Descriptive = descriptive;
            this.Value = value;
            this.Force3ds = force3ds;
        }

        /// <summary>
        /// Gets or Sets TransactionKey
        /// </summary>
        [DataMember(Name = "transaction_key", EmitDefaultValue = false)]
        public string TransactionKey { get; set; }

        /// <summary>
        /// Gets or Sets CaptureDate
        /// </summary>
        [DataMember(Name = "capture_date", EmitDefaultValue = false)]
        public string CaptureDate { get; set; }

        /// <summary>
        /// Gets or Sets Account
        /// </summary>
        [DataMember(Name = "account", EmitDefaultValue = false)]
        public SubscriptionPost201ResponseCustomer Account { get; set; }

        /// <summary>
        /// Gets or Sets Status
        /// </summary>
        [DataMember(Name = "status", EmitDefaultValue = false)]
        public string Status { get; set; }

        /// <summary>
        /// Gets or Sets Splits
        /// </summary>
        [DataMember(Name = "splits", EmitDefaultValue = false)]
        public Collection<CaptureSplit> Splits { get; set; }

        /// <summary>
        /// Gets or Sets PaymentType
        /// </summary>
        [DataMember(Name = "payment_type", EmitDefaultValue = false)]
        public string PaymentType { get; set; }

        /// <summary>
        /// Gets or Sets PaymentId
        /// </summary>
        [DataMember(Name = "payment_id", EmitDefaultValue = false)]
        public string PaymentId { get; set; }

        /// <summary>
        /// Gets or Sets Id
        /// </summary>
        [DataMember(Name = "id", EmitDefaultValue = false)]
        public string Id { get; set; }

        /// <summary>
        /// Gets or Sets Descriptive
        /// </summary>
        [DataMember(Name = "descriptive", EmitDefaultValue = false)]
        public string Descriptive { get; set; }

        /// <summary>
        /// Gets or Sets Value
        /// </summary>
        [DataMember(Name = "value", EmitDefaultValue = false)]
        public double Value { get; set; }

        /// <summary>
        /// Whether or not you want to force the 3DS authentication.
        /// </summary>
        /// <value>Whether or not you want to force the 3DS authentication.</value>
        [DataMember(Name = "force_3ds", EmitDefaultValue = true)]
        public bool Force3ds { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("class PaymentCapture {\n");
            sb.Append("  TransactionKey: ").Append(TransactionKey).Append("\n");
            sb.Append("  CaptureDate: ").Append(CaptureDate).Append("\n");
            sb.Append("  Account: ").Append(Account).Append("\n");
            sb.Append("  Status: ").Append(Status).Append("\n");
            sb.Append("  Splits: ").Append(Splits).Append("\n");
            sb.Append("  PaymentType: ").Append(PaymentType).Append("\n");
            sb.Append("  PaymentId: ").Append(PaymentId).Append("\n");
            sb.Append("  Id: ").Append(Id).Append("\n");
            sb.Append("  Descriptive: ").Append(Descriptive).Append("\n");
            sb.Append("  Value: ").Append(Value).Append("\n");
            sb.Append("  Force3ds: ").Append(Force3ds).Append("\n");
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
