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
    /// PaymentFrequentBase
    /// </summary>
    [DataContract(Name = "Payment_Frequent_Base")]
    public partial class PaymentFrequentBase : IValidatableObject
    {

        /// <summary>
        /// Gets or Sets Currency
        /// </summary>
        [DataMember(Name = "currency", EmitDefaultValue = false)]
        public Currency? Currency { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="PaymentFrequentBase" /> class.
        /// </summary>
        [JsonConstructorAttribute]
        protected PaymentFrequentBase() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="PaymentFrequentBase" /> class.
        /// </summary>
        /// <param name="id">id.</param>
        /// <param name="descriptive">This will appear in the bank statement/mbway application.</param>
        /// <param name="value">Value will be rounded to 2 decimals (required).</param>
        /// <param name="expirationTime">Optional.</param>
        /// <param name="currency">currency.</param>
        /// <param name="customer">customer.</param>
        /// <param name="key">Merchant identification key.</param>
        /// <param name="maxValue">Value will be rounded to 2 decimals.</param>
        /// <param name="minValue">Value will be rounded to 2 decimals.</param>
        /// <param name="unlimitedPayments">Transactions will be unlimited, max or min value will be refreshed on each payment (default to true).</param>
        public PaymentFrequentBase(Guid id = default(Guid), string descriptive = default(string), double value = default(double), string expirationTime = default(string), Currency? currency = default(Currency?), Customer customer = default(Customer), string key = default(string), double maxValue = default(double), double minValue = default(double), bool unlimitedPayments = true)
        {
            this.Value = value;
            this.Id = id;
            this.Descriptive = descriptive;
            this.ExpirationTime = expirationTime;
            this.Currency = currency;
            this.Customer = customer;
            this.Key = key;
            this.MaxValue = maxValue;
            this.MinValue = minValue;
            this.UnlimitedPayments = unlimitedPayments;
        }

        /// <summary>
        /// Gets or Sets Id
        /// </summary>
        /*
        <example>4c67e74b-a256-4e0a-965d-97bf5d01bd50</example>
        */
        [DataMember(Name = "id", EmitDefaultValue = false)]
        public Guid Id { get; set; }

        /// <summary>
        /// This will appear in the bank statement/mbway application
        /// </summary>
        /// <value>This will appear in the bank statement/mbway application</value>
        /*
        <example>Descriptive Example</example>
        */
        [DataMember(Name = "descriptive", EmitDefaultValue = false)]
        public string Descriptive { get; set; }

        /// <summary>
        /// Value will be rounded to 2 decimals
        /// </summary>
        /// <value>Value will be rounded to 2 decimals</value>
        /*
        <example>17.5</example>
        */
        [DataMember(Name = "value", IsRequired = true, EmitDefaultValue = true)]
        public double Value { get; set; }

        /// <summary>
        /// Optional
        /// </summary>
        /// <value>Optional</value>
        /*
        <example>2017-12-12 16:05</example>
        */
        [DataMember(Name = "expiration_time", EmitDefaultValue = false)]
        public string ExpirationTime { get; set; }

        /// <summary>
        /// Gets or Sets Customer
        /// </summary>
        [DataMember(Name = "customer", EmitDefaultValue = false)]
        public Customer Customer { get; set; }

        /// <summary>
        /// Merchant identification key
        /// </summary>
        /// <value>Merchant identification key</value>
        /*
        <example>Example Key</example>
        */
        [DataMember(Name = "key", EmitDefaultValue = false)]
        public string Key { get; set; }

        /// <summary>
        /// Value will be rounded to 2 decimals
        /// </summary>
        /// <value>Value will be rounded to 2 decimals</value>
        /*
        <example>20</example>
        */
        [DataMember(Name = "max_value", EmitDefaultValue = false)]
        public double MaxValue { get; set; }

        /// <summary>
        /// Value will be rounded to 2 decimals
        /// </summary>
        /// <value>Value will be rounded to 2 decimals</value>
        /*
        <example>2</example>
        */
        [DataMember(Name = "min_value", EmitDefaultValue = false)]
        public double MinValue { get; set; }

        /// <summary>
        /// Transactions will be unlimited, max or min value will be refreshed on each payment
        /// </summary>
        /// <value>Transactions will be unlimited, max or min value will be refreshed on each payment</value>
        /*
        <example>false</example>
        */
        [DataMember(Name = "unlimited_payments", EmitDefaultValue = true)]
        public bool UnlimitedPayments { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("class PaymentFrequentBase {\n");
            sb.Append("  Id: ").Append(Id).Append("\n");
            sb.Append("  Descriptive: ").Append(Descriptive).Append("\n");
            sb.Append("  Value: ").Append(Value).Append("\n");
            sb.Append("  ExpirationTime: ").Append(ExpirationTime).Append("\n");
            sb.Append("  Currency: ").Append(Currency).Append("\n");
            sb.Append("  Customer: ").Append(Customer).Append("\n");
            sb.Append("  Key: ").Append(Key).Append("\n");
            sb.Append("  MaxValue: ").Append(MaxValue).Append("\n");
            sb.Append("  MinValue: ").Append(MinValue).Append("\n");
            sb.Append("  UnlimitedPayments: ").Append(UnlimitedPayments).Append("\n");
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

            // Value (double) minimum
            if (this.Value < (double)0.5)
            {
                yield return new ValidationResult("Invalid value for Value, must be a value greater than or equal to 0.5.", new[] { "Value" });
            }

            // Key (string) maxLength
            if (this.Key != null && this.Key.Length > 50)
            {
                yield return new ValidationResult("Invalid value for Key, length must be less than 50.", new[] { "Key" });
            }

            // MaxValue (double) minimum
            if (this.MaxValue < (double)0)
            {
                yield return new ValidationResult("Invalid value for MaxValue, must be a value greater than or equal to 0.", new[] { "MaxValue" });
            }

            // MinValue (double) minimum
            if (this.MinValue < (double)0)
            {
                yield return new ValidationResult("Invalid value for MinValue, must be a value greater than or equal to 0.", new[] { "MinValue" });
            }

            yield break;
        }
    }

}
