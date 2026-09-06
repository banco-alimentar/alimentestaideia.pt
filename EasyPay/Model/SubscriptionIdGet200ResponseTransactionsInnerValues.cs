/*
 * Easypay Payments API
 *
 * Transaction values returned by the subscription detail endpoint.
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
    /// Monetary values for a subscription transaction.
    /// </summary>
    [DataContract(Name = "_subscription__id__get_200_response_transactions_inner_values")]
    public partial class SubscriptionIdGet200ResponseTransactionsInnerValues : IValidatableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriptionIdGet200ResponseTransactionsInnerValues" /> class.
        /// </summary>
        /// <param name="requested">Requested amount.</param>
        /// <param name="paid">Paid amount.</param>
        /// <param name="fixedFee">Fixed fee.</param>
        /// <param name="variableFee">Variable fee.</param>
        /// <param name="tax">Tax.</param>
        /// <param name="transfer">Transferred amount.</param>
        public SubscriptionIdGet200ResponseTransactionsInnerValues(
            decimal requested = default,
            decimal paid = default,
            decimal fixedFee = default,
            decimal variableFee = default,
            decimal tax = default,
            decimal transfer = default)
        {
            this.Requested = requested;
            this.Paid = paid;
            this.FixedFee = fixedFee;
            this.VariableFee = variableFee;
            this.Tax = tax;
            this.Transfer = transfer;
        }

        /// <summary>
        /// Gets or sets the requested amount.
        /// </summary>
        [DataMember(Name = "requested", EmitDefaultValue = false)]
        public decimal Requested { get; set; }

        /// <summary>
        /// Gets or sets the paid amount.
        /// </summary>
        [DataMember(Name = "paid", EmitDefaultValue = false)]
        public decimal Paid { get; set; }

        /// <summary>
        /// Gets or sets the fixed fee.
        /// </summary>
        [DataMember(Name = "fixed_fee", EmitDefaultValue = false)]
        public decimal FixedFee { get; set; }

        /// <summary>
        /// Gets or sets the variable fee.
        /// </summary>
        [DataMember(Name = "variable_fee", EmitDefaultValue = false)]
        public decimal VariableFee { get; set; }

        /// <summary>
        /// Gets or sets the tax.
        /// </summary>
        [DataMember(Name = "tax", EmitDefaultValue = false)]
        public decimal Tax { get; set; }

        /// <summary>
        /// Gets or sets the transferred amount.
        /// </summary>
        [DataMember(Name = "transfer", EmitDefaultValue = false)]
        public decimal Transfer { get; set; }

        /// <inheritdoc />
        public override string ToString()
        {
            var builder = new StringBuilder();
            builder.Append("class SubscriptionIdGet200ResponseTransactionsInnerValues {\n");
            builder.Append("  Requested: ").Append(this.Requested).Append("\n");
            builder.Append("  Paid: ").Append(this.Paid).Append("\n");
            builder.Append("  FixedFee: ").Append(this.FixedFee).Append("\n");
            builder.Append("  VariableFee: ").Append(this.VariableFee).Append("\n");
            builder.Append("  Tax: ").Append(this.Tax).Append("\n");
            builder.Append("  Transfer: ").Append(this.Transfer).Append("\n");
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
