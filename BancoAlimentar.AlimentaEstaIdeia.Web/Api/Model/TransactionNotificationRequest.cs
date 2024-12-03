// -----------------------------------------------------------------------
// <copyright file="TransactionNotificationRequest.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
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
    using Easypay.Rest.Client.Model;

    /// <summary>
    /// TransactionNotificationRequest.
    /// </summary>
    [DataContract(Name = "TransactionNotificationRequest")]
    public partial class TransactionNotificationRequest : IValidatableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionNotificationRequest" /> class.
        /// </summary>
        /// <param name="account">account.</param>
        /// <param name="id">id.</param>
        /// <param name="value">value.</param>
        /// <param name="currency">currency (default to &quot;EUR&quot;).</param>
        /// <param name="key">Merchant identification key.</param>
        /// <param name="expirationTime">expirationTime.</param>
        /// <param name="customer">customer.</param>
        /// <param name="method">method.</param>
        /// <param name="transaction">transaction.</param>
        public TransactionNotificationRequest(AuthorisationNotificationRequestAuthorisation account = default(AuthorisationNotificationRequestAuthorisation), Guid id = default(Guid), double value = default(double), string currency = "EUR", string key = default(string), string expirationTime = default(string), Customer customer = default(Customer), string method = default(string), TransactionNotificationRequestTransaction transaction = default(TransactionNotificationRequestTransaction))
        {
            this.Account = account;
            this.Id = id;
            this.Value = value;
            this.Currency = currency ?? "EUR";
            this.Key = key;
            this.ExpirationTime = expirationTime;
            this.Customer = customer;
            this.Method = method;
            this.Transaction = transaction;
        }

        /// <summary>
        /// Gets or Sets Account.
        /// </summary>
        [DataMember(Name = "account", EmitDefaultValue = false)]
        public AuthorisationNotificationRequestAuthorisation Account { get; set; }

        /// <summary>
        /// Gets or Sets Id.
        /// </summary>
        [DataMember(Name = "id", EmitDefaultValue = false)]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or Sets Value.
        /// </summary>
        [DataMember(Name = "value", EmitDefaultValue = false)]
        public double Value { get; set; }

        /// <summary>
        /// Gets or Sets Currency.
        /// </summary>
        [DataMember(Name = "currency", EmitDefaultValue = false)]
        public string Currency { get; set; }

        /// <summary>
        /// Gets or sets merchant identification key.
        /// </summary>
        /// <value>Merchant identification key.</value>
        [DataMember(Name = "key", EmitDefaultValue = false)]
        public string Key { get; set; }

        /// <summary>
        /// Gets or Sets ExpirationTime.
        /// </summary>
        [DataMember(Name = "expiration_time", EmitDefaultValue = false)]
        public string ExpirationTime { get; set; }

        /// <summary>
        /// Gets or Sets Customer.
        /// </summary>
        [DataMember(Name = "customer", EmitDefaultValue = false)]
        public Customer Customer { get; set; }

        /// <summary>
        /// Gets or Sets Method.
        /// </summary>
        [DataMember(Name = "method", EmitDefaultValue = false)]
        public string Method { get; set; }

        /// <summary>
        /// Gets or Sets Transaction.
        /// </summary>
        [DataMember(Name = "transaction", EmitDefaultValue = false)]
        public TransactionNotificationRequestTransaction Transaction { get; set; }

        /// <summary>
        /// Returns the string presentation of the object.
        /// </summary>
        /// <returns>String presentation of the object.</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("class TransactionNotificationRequest {\n");
            sb.Append("  Account: ").Append(Account).Append("\n");
            sb.Append("  Id: ").Append(Id).Append("\n");
            sb.Append("  Value: ").Append(Value).Append("\n");
            sb.Append("  Currency: ").Append(Currency).Append("\n");
            sb.Append("  Key: ").Append(Key).Append("\n");
            sb.Append("  ExpirationTime: ").Append(ExpirationTime).Append("\n");
            sb.Append("  Customer: ").Append(Customer).Append("\n");
            sb.Append("  Method: ").Append(Method).Append("\n");
            sb.Append("  Transaction: ").Append(Transaction).Append("\n");
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
