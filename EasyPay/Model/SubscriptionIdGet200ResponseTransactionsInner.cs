/*
 * Easypay Payments API
 *
 * Transaction records returned by the subscription detail endpoint.
 */

using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using Newtonsoft.Json;

namespace Easypay.Rest.Client.Model
{
    /// <summary>
    /// A transaction returned with an Easypay subscription.
    /// </summary>
    [DataContract(Name = "_subscription__id__get_200_response_transactions_inner")]
    public partial class SubscriptionIdGet200ResponseTransactionsInner : IValidatableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionIdGet200ResponseTransactionsInner" /> class.
        /// </summary>
        /// <param name="id">Transaction identifier.</param>
        /// <param name="key">Merchant key.</param>
        /// <param name="createdAt">Creation timestamp.</param>
        /// <param name="date">Transaction timestamp.</param>
        /// <param name="values">Transaction monetary values.</param>
        /// <param name="transferDate">Transfer timestamp.</param>
        /// <param name="transferBatch">Transfer batch.</param>
        /// <param name="method">Payment method.</param>
        /// <param name="documentNumber">Document number.</param>
        /// <param name="descriptive">Description.</param>
        public SubscriptionIdGet200ResponseTransactionsInner(
            string id = default,
            string key = default,
            string createdAt = default,
            string date = default,
            SubscriptionIdGet200ResponseTransactionsInnerValues values = default,
            string transferDate = default,
            string transferBatch = default,
            string method = default,
            string documentNumber = default,
            string descriptive = default)
        {
            this.Id = id;
            this.Key = key;
            this.CreatedAt = createdAt;
            this.Date = date;
            this.Values = values;
            this.TransferDate = transferDate;
            this.TransferBatch = transferBatch;
            this.Method = method;
            this.DocumentNumber = documentNumber;
            this.Descriptive = descriptive;
        }

        /// <summary>
        /// Gets or sets the transaction identifier.
        /// </summary>
        [DataMember(Name = "id", EmitDefaultValue = false)]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the merchant key.
        /// </summary>
        [DataMember(Name = "key", EmitDefaultValue = false)]
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the creation timestamp.
        /// </summary>
        [DataMember(Name = "created_at", EmitDefaultValue = false)]
        public string CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the transaction timestamp.
        /// </summary>
        [DataMember(Name = "date", EmitDefaultValue = false)]
        public string Date { get; set; }

        /// <summary>
        /// Gets or sets the transaction monetary values.
        /// </summary>
        [DataMember(Name = "values", EmitDefaultValue = false)]
        public SubscriptionIdGet200ResponseTransactionsInnerValues Values { get; set; }

        /// <summary>
        /// Gets or sets the transfer timestamp.
        /// </summary>
        [DataMember(Name = "transfer_date", EmitDefaultValue = false)]
        public string TransferDate { get; set; }

        /// <summary>
        /// Gets or sets the transfer batch.
        /// </summary>
        [DataMember(Name = "transfer_batch", EmitDefaultValue = false)]
        public string TransferBatch { get; set; }

        /// <summary>
        /// Gets or sets the payment method.
        /// </summary>
        [DataMember(Name = "method", EmitDefaultValue = false)]
        public string Method { get; set; }

        /// <summary>
        /// Gets or sets the document number.
        /// </summary>
        [DataMember(Name = "document_number", EmitDefaultValue = false)]
        public string DocumentNumber { get; set; }

        /// <summary>
        /// Gets or sets the transaction description.
        /// </summary>
        [DataMember(Name = "descriptive", EmitDefaultValue = false)]
        public string Descriptive { get; set; }

        /// <inheritdoc />
        public override string ToString()
        {
            var builder = new StringBuilder();
            builder.Append("class SubscriptionIdGet200ResponseTransactionsInner {\n");
            builder.Append("  Id: ").Append(this.Id).Append("\n");
            builder.Append("  Key: ").Append(this.Key).Append("\n");
            builder.Append("  CreatedAt: ").Append(this.CreatedAt).Append("\n");
            builder.Append("  Date: ").Append(this.Date).Append("\n");
            builder.Append("  Values: ").Append(this.Values).Append("\n");
            builder.Append("  TransferDate: ").Append(this.TransferDate).Append("\n");
            builder.Append("  TransferBatch: ").Append(this.TransferBatch).Append("\n");
            builder.Append("  Method: ").Append(this.Method).Append("\n");
            builder.Append("  DocumentNumber: ").Append(this.DocumentNumber).Append("\n");
            builder.Append("  Descriptive: ").Append(this.Descriptive).Append("\n");
            builder.Append("}\n");
            return builder.ToString();
        }

        /// <summary>
        /// Gets the JSON representation of this instance.
        /// </summary>
        /// <returns>JSON representation.</returns>
        public virtual string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        /// <inheritdoc />
        IEnumerable<ValidationResult> IValidatableObject.Validate(ValidationContext validationContext)
        {
            yield break;
        }
    }
}
