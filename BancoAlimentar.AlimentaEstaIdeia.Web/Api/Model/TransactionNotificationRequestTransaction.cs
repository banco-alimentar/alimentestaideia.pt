// -----------------------------------------------------------------------
// <copyright file="TransactionNotificationRequestTransaction.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
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
    using Easypay.Rest.Client.Client;
    using Newtonsoft.Json;

    /// <summary>
    /// TransactionNotificationRequestTransaction.
    /// </summary>
    [DataContract(Name = "TransactionNotificationRequest_transaction")]
    public partial class TransactionNotificationRequestTransaction : IValidatableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionNotificationRequestTransaction" /> class.
        /// </summary>
        /// <param name="id">id.</param>
        /// <param name="key">Any value that will help the merchant manage the transaction on his database.</param>
        /// <param name="date">date.</param>
        /// <param name="values">values.</param>
        /// <param name="transferDate">transferDate.</param>
        /// <param name="documentNumber">Unique document number used for Easypay Invoice.</param>
        public TransactionNotificationRequestTransaction(Guid id = default(Guid), string key = default(string), string date = default(string), PaymentSingleTransactionValues values = default(PaymentSingleTransactionValues), DateTime transferDate = default(DateTime), string documentNumber = default(string))
        {
            this.Id = id;
            this.Key = key;
            this.Date = date;
            this.Values = values;
            this.TransferDate = transferDate;
            this.DocumentNumber = documentNumber;
        }

        /// <summary>
        /// Gets or Sets Id.
        /// </summary>
        [DataMember(Name = "id", EmitDefaultValue = false)]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets any value that will help the merchant manage the transaction on his database.
        /// </summary>
        /// <value>Any value that will help the merchant manage the transaction on his database.</value>
        [DataMember(Name = "key", EmitDefaultValue = false)]
        public string Key { get; set; }

        /// <summary>
        /// Gets or Sets Date.
        /// </summary>
        [DataMember(Name = "date", EmitDefaultValue = false)]
        public string Date { get; set; }

        /// <summary>
        /// Gets or Sets Values.
        /// </summary>
        [DataMember(Name = "values", EmitDefaultValue = false)]
        public PaymentSingleTransactionValues Values { get; set; }

        /// <summary>
        /// Gets or Sets TransferDate.
        /// </summary>
        [DataMember(Name = "transfer_date", EmitDefaultValue = false)]
        [JsonConverter(typeof(OpenAPIDateConverter))]
        public DateTime? TransferDate { get; set; }

        /// <summary>
        /// Gets or sets unique document number used for Easypay Invoice.
        /// </summary>
        /// <value>Unique document number used for Easypay Invoice.</value>
        [DataMember(Name = "document_number", EmitDefaultValue = false)]
        public string DocumentNumber { get; set; }

        /// <summary>
        /// Returns the string presentation of the object.
        /// </summary>
        /// <returns>String presentation of the object.</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class TransactionNotificationRequestTransaction {\n");
            sb.Append("  Id: ").Append(Id).Append("\n");
            sb.Append("  Key: ").Append(Key).Append("\n");
            sb.Append("  Date: ").Append(Date).Append("\n");
            sb.Append("  Values: ").Append(Values).Append("\n");
            sb.Append("  TransferDate: ").Append(TransferDate).Append("\n");
            sb.Append("  DocumentNumber: ").Append(DocumentNumber).Append("\n");
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