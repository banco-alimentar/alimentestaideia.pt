// -----------------------------------------------------------------------
// <copyright file="PaymentSingleTransactionValues.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
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
    /// PaymentSingleTransactionValues.
    /// </summary>
    [DataContract(Name = "Payment_Single_Transaction_values")]
    public partial class PaymentSingleTransactionValues : IValidatableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PaymentSingleTransactionValues" /> class.
        /// </summary>
        /// <param name="requested">requested.</param>
        /// <param name="paid">paid.</param>
        /// <param name="fixedFee">fixedFee.</param>
        /// <param name="variableFee">variableFee.</param>
        /// <param name="tax">tax.</param>
        /// <param name="transfer">transfer.</param>
        public PaymentSingleTransactionValues(double requested = default(double), double paid = default(double), double fixedFee = default(double), double variableFee = default(double), double tax = default(double), double transfer = default(double))
        {
            this.Requested = requested;
            this.Paid = paid;
            this.FixedFee = fixedFee;
            this.VariableFee = variableFee;
            this.Tax = tax;
            this.Transfer = transfer;
        }

        /// <summary>
        /// Gets or Sets Requested.
        /// </summary>
        [DataMember(Name = "requested", EmitDefaultValue = false)]
        public double Requested { get; set; }

        /// <summary>
        /// Gets or Sets Paid.
        /// </summary>
        [DataMember(Name = "paid", EmitDefaultValue = false)]
        public double Paid { get; set; }

        /// <summary>
        /// Gets or Sets FixedFee.
        /// </summary>
        [DataMember(Name = "fixed_fee", EmitDefaultValue = false)]
        public double FixedFee { get; set; }

        /// <summary>
        /// Gets or Sets VariableFee.
        /// </summary>
        [DataMember(Name = "variable_fee", EmitDefaultValue = false)]
        public double VariableFee { get; set; }

        /// <summary>
        /// Gets or Sets Tax.
        /// </summary>
        [DataMember(Name = "tax", EmitDefaultValue = false)]
        public double Tax { get; set; }

        /// <summary>
        /// Gets or Sets Transfer.
        /// </summary>
        [DataMember(Name = "transfer", EmitDefaultValue = false)]
        public double Transfer { get; set; }

        /// <summary>
        /// Returns the string presentation of the object.
        /// </summary>
        /// <returns>String presentation of the object.</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class PaymentSingleTransactionValues {\n");
            sb.Append("  Requested: ").Append(Requested).Append("\n");
            sb.Append("  Paid: ").Append(Paid).Append("\n");
            sb.Append("  FixedFee: ").Append(FixedFee).Append("\n");
            sb.Append("  VariableFee: ").Append(VariableFee).Append("\n");
            sb.Append("  Tax: ").Append(Tax).Append("\n");
            sb.Append("  Transfer: ").Append(Transfer).Append("\n");
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