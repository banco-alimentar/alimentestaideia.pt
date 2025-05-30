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
    /// Chargeback
    /// </summary>
    [DataContract(Name = "Chargeback")]
    public partial class Chargeback : IValidatableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Chargeback" /> class.
        /// </summary>
        /// <param name="id">A unique identifier for the resource. While typically formatted as a UUID (Universally Unique Identifier), it can also be in other formats as defined by the user. This field ensures the resource can be distinctly recognized and referenced..</param>
        /// <param name="createdAt">The timestamp indicating when the resource was created. It is formatted as \&quot;YYYY-MM-DD HH:MM\&quot;..</param>
        /// <param name="message">The message field provides a human-readable explanation of the chargeback reason code, making it easier to understand the cause of the dispute without needing to interpret technical codes. This message clarifies the issue, such as incorrect account information, insufficient funds, or unauthorized transactions, and helps merchants quickly identify the nature of the problem..</param>
        /// <param name="code">The code field provides the specific SEPA or Visa/Masterd code associated with the chargeback. This code identifies the reason for the chargeback, offering insight into why the transaction was disputed by the consumer&#39;s bank or card issuer. SEPA reasons:   - **AC01**:  Incorrect Account Number   - **AC04**:  Closed Account Number   - **AC06**:  Blocked Account   - **AC13**:  Invalid Debitor Account Type   - **AG01**:  Transaction Forbidden   - **AG02**:  Invalid Bank Operation Code   - **AM04**:  Insufficient Funds   - **AM05**:  Duplication   - **BE05**:  Unrecognised Initiating Party   - **CNOR**:  Creditor Bank is Not Registered   - **DNOR**:  Debitor Bank is Not Registered   - **ED05**:  Settlement Failed   - **FF01**:  Invalid File Format   - **MD01**:  No Mandate   - **MD02**:  Missing Mandatory Mandate Information   - **MD06**:  Refund Request by End Customer   - **MD07**:  End Customer Deceased   - **MS02**:  Not Specified Reason Customer Generated   - **MS03**:  Not Specified Reason Agent Generated   - **RC01**:  Bank Identifier Incorrect   - **RR01**:  Missing Debitor Account Or Identification   - **RR02**:  Missing Debitor Name Or Address   - **RR03**:  Missing Creditor Name or Address   - **RR04**:  Regulatory Reason   - **SL01**:  Specific Service Offered By Debitor Agent Visa Reasons:   - **10.1**: EMV Liability Shift Counterfeit Fraud   - **10.2**: EMV Liability Shift Non-Counterfeit Fraud   - **10.3**: Other Fraud, Card-Present Environment   - **10.4**: Other Fraud, Card-Absent Environment   - **10.5**: Visa Fraud Monitoring Program   - **11.1**: Card Recovery Bulletin   - **11.2**: Declined Authorization   - **11.3**: No Authorization   - **12.1**: Late Presentment   - **12.2**: Incorrect Transaction Code   - **12.3**: Incorrect Currency   - **12.4**: Incorrect Account Number   - **12.5**: Incorrect Amount   - **12.6**: Duplicate Processing / Paid By Other Means   - **12.7**: Invalid Data   - **13.1**: Merchandise / Services Not Received   - **13.2**: Cancelled Recurring Transaction   - **13.3**: Not As Described Or Defective Merchandise / Services   - **13.4**: Counterfeit Merchandise   - **13.5**: Misrepresentation   - **13.6**: Credit Not Processed   - **13.7**: Cancelled Merchandise / Services   - **13.8**: Original Credit Transaction Not Accepted   - **13.9**: Non-Receipt of Cash or Load Transaction Value  Mastercard reasons:   - **4837**: No Cardholder Authorization   - **4840**: Fraudulent Processing Of Transactions   - **4849**: Questionable Merchant Activity - Global Merchant Audit Program (GMAP)   - **4849**: Questionable Merchant Activity - Mastercard Rule 3.7 Violation for Coercion Claim   - **4849**: Questionable Merchant Activity - Questionable Merchant Audit Program (QMAP)   - **4863**: Cardholder Does Not Recognize, Potential Fraud   - **4870**: EMV Chip Liability Shift   - **4871**: Chip Liability Shift – Lost / Stolen / Never Received Fraud   - **4807**: Warning Bulletin   - **4808**: Authorization Chargeback - Cardholder-Activated Terminal (CAT) 3 Device   - **4808**: Authorization Chargeback - Expired Chargeback Protection Period   - **4808**: Authorization Chargeback - Multiple Authorization Requests   - **4808**: Authorization Chargeback - Required Authorization Not Obtained   - **4812**: Account Number Not On File   - **4853**: Cardholder Dispute - Credit Posted as a Purchase   - **4853**: Cardholder Dispute - Digital Goods Purchase of $25 or Less   - **4853**: Cardholder Dispute - Goods or Services Not as Described or Defective   - **4853**: Cardholder Dispute - Goods or Services Not Provided   - **4853**: Cardholder Dispute - Issuer Dispute of a Recurring Transaction   - **4853**: Cardholder Dispute - Timeshares   - **4853**: Cardholder Dispute - \&quot;No Show\&quot; Hotel Charge   - **4853**: Cardholder Dispute - Transaction Did Not Complete   - **4853**: Cardholder Dispute - Addendum Dispute   - **4853**: Cardholder Dispute - Cardholder Dispute of a Recurring Transaction   - **4853**: Cardholder Dispute - Counterfeit Goods   - **4853**: Cardholder Dispute - Credit Not Processed   - **4855**: Goods or Services Not Provided   - **4859**: Addendum, No-Show, or ATM Dispute (Europe)   - **4859**: Addendum, No-Show, or ATM Dispute (Non-European Bank)   - **4860**: Credit Not Processed   - **4831**: Incorrect Transaction Amount   - **4834**: POI Error - Charges for Loss, Theft, or Damages   - **4834**: POI Error - Late Presentment   - **4834**: POI Error - Merchant Credit Correcting Error, Cardholder Currency Exchange Loss   - **4834**: POI Error - Transaction Amount Differs   - **4834**: POI Error - Currency Conversion (Dynamic Currency Conversion)   - **4834**: POI Error - Unreasonable Amount (Europe)   - **4834**: POI Error - ATM Disputes   - **4834**: POI Error - Cardholder Debited More than Once   - **4841**: Canceled Recurring or Digital Goods Transactions   - **4842**: Late Presentment (Europe)   - **4842**: Late Presentment (Non-European Banks)   - **4846**: Correct Transaction Currency Code Not Provided / Currency Errors (Non-European Bank)   - **4846**: Correct Transaction Currency Code Not Provided / Currency Error (Europe).</param>
        /// <param name="amount">The monetary amount requested for the transaction. This field is formatted as a double, and will be rounded to two decimal places (e.g., \&quot;100.00\&quot;). The value must be greater than or equal to 0.5..</param>
        /// <param name="capture">capture.</param>
        public Chargeback(string id = default(string), string createdAt = default(string), string message = default(string), string code = default(string), double amount = default(double), RefundScopedCapture capture = default(RefundScopedCapture))
        {
            this.Id = id;
            this.CreatedAt = createdAt;
            this.Message = message;
            this.Code = code;
            this.Amount = amount;
            this.Capture = capture;
        }

        /// <summary>
        /// A unique identifier for the resource. While typically formatted as a UUID (Universally Unique Identifier), it can also be in other formats as defined by the user. This field ensures the resource can be distinctly recognized and referenced.
        /// </summary>
        /// <value>A unique identifier for the resource. While typically formatted as a UUID (Universally Unique Identifier), it can also be in other formats as defined by the user. This field ensures the resource can be distinctly recognized and referenced.</value>
        /*
        <example>c6056234-a3f9-42de-b944-3ed793fcb6bb</example>
        */
        [DataMember(Name = "id", EmitDefaultValue = false)]
        public string Id { get; set; }

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
        /// The message field provides a human-readable explanation of the chargeback reason code, making it easier to understand the cause of the dispute without needing to interpret technical codes. This message clarifies the issue, such as incorrect account information, insufficient funds, or unauthorized transactions, and helps merchants quickly identify the nature of the problem.
        /// </summary>
        /// <value>The message field provides a human-readable explanation of the chargeback reason code, making it easier to understand the cause of the dispute without needing to interpret technical codes. This message clarifies the issue, such as incorrect account information, insufficient funds, or unauthorized transactions, and helps merchants quickly identify the nature of the problem.</value>
        /*
        <example>MD06 - Refund Request by End Customer</example>
        */
        [DataMember(Name = "message", EmitDefaultValue = false)]
        public string Message { get; set; }

        /// <summary>
        /// The code field provides the specific SEPA or Visa/Masterd code associated with the chargeback. This code identifies the reason for the chargeback, offering insight into why the transaction was disputed by the consumer&#39;s bank or card issuer. SEPA reasons:   - **AC01**:  Incorrect Account Number   - **AC04**:  Closed Account Number   - **AC06**:  Blocked Account   - **AC13**:  Invalid Debitor Account Type   - **AG01**:  Transaction Forbidden   - **AG02**:  Invalid Bank Operation Code   - **AM04**:  Insufficient Funds   - **AM05**:  Duplication   - **BE05**:  Unrecognised Initiating Party   - **CNOR**:  Creditor Bank is Not Registered   - **DNOR**:  Debitor Bank is Not Registered   - **ED05**:  Settlement Failed   - **FF01**:  Invalid File Format   - **MD01**:  No Mandate   - **MD02**:  Missing Mandatory Mandate Information   - **MD06**:  Refund Request by End Customer   - **MD07**:  End Customer Deceased   - **MS02**:  Not Specified Reason Customer Generated   - **MS03**:  Not Specified Reason Agent Generated   - **RC01**:  Bank Identifier Incorrect   - **RR01**:  Missing Debitor Account Or Identification   - **RR02**:  Missing Debitor Name Or Address   - **RR03**:  Missing Creditor Name or Address   - **RR04**:  Regulatory Reason   - **SL01**:  Specific Service Offered By Debitor Agent Visa Reasons:   - **10.1**: EMV Liability Shift Counterfeit Fraud   - **10.2**: EMV Liability Shift Non-Counterfeit Fraud   - **10.3**: Other Fraud, Card-Present Environment   - **10.4**: Other Fraud, Card-Absent Environment   - **10.5**: Visa Fraud Monitoring Program   - **11.1**: Card Recovery Bulletin   - **11.2**: Declined Authorization   - **11.3**: No Authorization   - **12.1**: Late Presentment   - **12.2**: Incorrect Transaction Code   - **12.3**: Incorrect Currency   - **12.4**: Incorrect Account Number   - **12.5**: Incorrect Amount   - **12.6**: Duplicate Processing / Paid By Other Means   - **12.7**: Invalid Data   - **13.1**: Merchandise / Services Not Received   - **13.2**: Cancelled Recurring Transaction   - **13.3**: Not As Described Or Defective Merchandise / Services   - **13.4**: Counterfeit Merchandise   - **13.5**: Misrepresentation   - **13.6**: Credit Not Processed   - **13.7**: Cancelled Merchandise / Services   - **13.8**: Original Credit Transaction Not Accepted   - **13.9**: Non-Receipt of Cash or Load Transaction Value  Mastercard reasons:   - **4837**: No Cardholder Authorization   - **4840**: Fraudulent Processing Of Transactions   - **4849**: Questionable Merchant Activity - Global Merchant Audit Program (GMAP)   - **4849**: Questionable Merchant Activity - Mastercard Rule 3.7 Violation for Coercion Claim   - **4849**: Questionable Merchant Activity - Questionable Merchant Audit Program (QMAP)   - **4863**: Cardholder Does Not Recognize, Potential Fraud   - **4870**: EMV Chip Liability Shift   - **4871**: Chip Liability Shift – Lost / Stolen / Never Received Fraud   - **4807**: Warning Bulletin   - **4808**: Authorization Chargeback - Cardholder-Activated Terminal (CAT) 3 Device   - **4808**: Authorization Chargeback - Expired Chargeback Protection Period   - **4808**: Authorization Chargeback - Multiple Authorization Requests   - **4808**: Authorization Chargeback - Required Authorization Not Obtained   - **4812**: Account Number Not On File   - **4853**: Cardholder Dispute - Credit Posted as a Purchase   - **4853**: Cardholder Dispute - Digital Goods Purchase of $25 or Less   - **4853**: Cardholder Dispute - Goods or Services Not as Described or Defective   - **4853**: Cardholder Dispute - Goods or Services Not Provided   - **4853**: Cardholder Dispute - Issuer Dispute of a Recurring Transaction   - **4853**: Cardholder Dispute - Timeshares   - **4853**: Cardholder Dispute - \&quot;No Show\&quot; Hotel Charge   - **4853**: Cardholder Dispute - Transaction Did Not Complete   - **4853**: Cardholder Dispute - Addendum Dispute   - **4853**: Cardholder Dispute - Cardholder Dispute of a Recurring Transaction   - **4853**: Cardholder Dispute - Counterfeit Goods   - **4853**: Cardholder Dispute - Credit Not Processed   - **4855**: Goods or Services Not Provided   - **4859**: Addendum, No-Show, or ATM Dispute (Europe)   - **4859**: Addendum, No-Show, or ATM Dispute (Non-European Bank)   - **4860**: Credit Not Processed   - **4831**: Incorrect Transaction Amount   - **4834**: POI Error - Charges for Loss, Theft, or Damages   - **4834**: POI Error - Late Presentment   - **4834**: POI Error - Merchant Credit Correcting Error, Cardholder Currency Exchange Loss   - **4834**: POI Error - Transaction Amount Differs   - **4834**: POI Error - Currency Conversion (Dynamic Currency Conversion)   - **4834**: POI Error - Unreasonable Amount (Europe)   - **4834**: POI Error - ATM Disputes   - **4834**: POI Error - Cardholder Debited More than Once   - **4841**: Canceled Recurring or Digital Goods Transactions   - **4842**: Late Presentment (Europe)   - **4842**: Late Presentment (Non-European Banks)   - **4846**: Correct Transaction Currency Code Not Provided / Currency Errors (Non-European Bank)   - **4846**: Correct Transaction Currency Code Not Provided / Currency Error (Europe)
        /// </summary>
        /// <value>The code field provides the specific SEPA or Visa/Masterd code associated with the chargeback. This code identifies the reason for the chargeback, offering insight into why the transaction was disputed by the consumer&#39;s bank or card issuer. SEPA reasons:   - **AC01**:  Incorrect Account Number   - **AC04**:  Closed Account Number   - **AC06**:  Blocked Account   - **AC13**:  Invalid Debitor Account Type   - **AG01**:  Transaction Forbidden   - **AG02**:  Invalid Bank Operation Code   - **AM04**:  Insufficient Funds   - **AM05**:  Duplication   - **BE05**:  Unrecognised Initiating Party   - **CNOR**:  Creditor Bank is Not Registered   - **DNOR**:  Debitor Bank is Not Registered   - **ED05**:  Settlement Failed   - **FF01**:  Invalid File Format   - **MD01**:  No Mandate   - **MD02**:  Missing Mandatory Mandate Information   - **MD06**:  Refund Request by End Customer   - **MD07**:  End Customer Deceased   - **MS02**:  Not Specified Reason Customer Generated   - **MS03**:  Not Specified Reason Agent Generated   - **RC01**:  Bank Identifier Incorrect   - **RR01**:  Missing Debitor Account Or Identification   - **RR02**:  Missing Debitor Name Or Address   - **RR03**:  Missing Creditor Name or Address   - **RR04**:  Regulatory Reason   - **SL01**:  Specific Service Offered By Debitor Agent Visa Reasons:   - **10.1**: EMV Liability Shift Counterfeit Fraud   - **10.2**: EMV Liability Shift Non-Counterfeit Fraud   - **10.3**: Other Fraud, Card-Present Environment   - **10.4**: Other Fraud, Card-Absent Environment   - **10.5**: Visa Fraud Monitoring Program   - **11.1**: Card Recovery Bulletin   - **11.2**: Declined Authorization   - **11.3**: No Authorization   - **12.1**: Late Presentment   - **12.2**: Incorrect Transaction Code   - **12.3**: Incorrect Currency   - **12.4**: Incorrect Account Number   - **12.5**: Incorrect Amount   - **12.6**: Duplicate Processing / Paid By Other Means   - **12.7**: Invalid Data   - **13.1**: Merchandise / Services Not Received   - **13.2**: Cancelled Recurring Transaction   - **13.3**: Not As Described Or Defective Merchandise / Services   - **13.4**: Counterfeit Merchandise   - **13.5**: Misrepresentation   - **13.6**: Credit Not Processed   - **13.7**: Cancelled Merchandise / Services   - **13.8**: Original Credit Transaction Not Accepted   - **13.9**: Non-Receipt of Cash or Load Transaction Value  Mastercard reasons:   - **4837**: No Cardholder Authorization   - **4840**: Fraudulent Processing Of Transactions   - **4849**: Questionable Merchant Activity - Global Merchant Audit Program (GMAP)   - **4849**: Questionable Merchant Activity - Mastercard Rule 3.7 Violation for Coercion Claim   - **4849**: Questionable Merchant Activity - Questionable Merchant Audit Program (QMAP)   - **4863**: Cardholder Does Not Recognize, Potential Fraud   - **4870**: EMV Chip Liability Shift   - **4871**: Chip Liability Shift – Lost / Stolen / Never Received Fraud   - **4807**: Warning Bulletin   - **4808**: Authorization Chargeback - Cardholder-Activated Terminal (CAT) 3 Device   - **4808**: Authorization Chargeback - Expired Chargeback Protection Period   - **4808**: Authorization Chargeback - Multiple Authorization Requests   - **4808**: Authorization Chargeback - Required Authorization Not Obtained   - **4812**: Account Number Not On File   - **4853**: Cardholder Dispute - Credit Posted as a Purchase   - **4853**: Cardholder Dispute - Digital Goods Purchase of $25 or Less   - **4853**: Cardholder Dispute - Goods or Services Not as Described or Defective   - **4853**: Cardholder Dispute - Goods or Services Not Provided   - **4853**: Cardholder Dispute - Issuer Dispute of a Recurring Transaction   - **4853**: Cardholder Dispute - Timeshares   - **4853**: Cardholder Dispute - \&quot;No Show\&quot; Hotel Charge   - **4853**: Cardholder Dispute - Transaction Did Not Complete   - **4853**: Cardholder Dispute - Addendum Dispute   - **4853**: Cardholder Dispute - Cardholder Dispute of a Recurring Transaction   - **4853**: Cardholder Dispute - Counterfeit Goods   - **4853**: Cardholder Dispute - Credit Not Processed   - **4855**: Goods or Services Not Provided   - **4859**: Addendum, No-Show, or ATM Dispute (Europe)   - **4859**: Addendum, No-Show, or ATM Dispute (Non-European Bank)   - **4860**: Credit Not Processed   - **4831**: Incorrect Transaction Amount   - **4834**: POI Error - Charges for Loss, Theft, or Damages   - **4834**: POI Error - Late Presentment   - **4834**: POI Error - Merchant Credit Correcting Error, Cardholder Currency Exchange Loss   - **4834**: POI Error - Transaction Amount Differs   - **4834**: POI Error - Currency Conversion (Dynamic Currency Conversion)   - **4834**: POI Error - Unreasonable Amount (Europe)   - **4834**: POI Error - ATM Disputes   - **4834**: POI Error - Cardholder Debited More than Once   - **4841**: Canceled Recurring or Digital Goods Transactions   - **4842**: Late Presentment (Europe)   - **4842**: Late Presentment (Non-European Banks)   - **4846**: Correct Transaction Currency Code Not Provided / Currency Errors (Non-European Bank)   - **4846**: Correct Transaction Currency Code Not Provided / Currency Error (Europe)</value>
        /*
        <example>MD06</example>
        */
        [DataMember(Name = "code", EmitDefaultValue = false)]
        public string Code { get; set; }

        /// <summary>
        /// The monetary amount requested for the transaction. This field is formatted as a double, and will be rounded to two decimal places (e.g., \&quot;100.00\&quot;). The value must be greater than or equal to 0.5.
        /// </summary>
        /// <value>The monetary amount requested for the transaction. This field is formatted as a double, and will be rounded to two decimal places (e.g., \&quot;100.00\&quot;). The value must be greater than or equal to 0.5.</value>
        /*
        <example>15.32</example>
        */
        [DataMember(Name = "amount", EmitDefaultValue = false)]
        public double Amount { get; set; }

        /// <summary>
        /// Gets or Sets Capture
        /// </summary>
        [DataMember(Name = "capture", EmitDefaultValue = false)]
        public RefundScopedCapture Capture { get; set; }

        /// <summary>
        /// Returns the string presentation of the object
        /// </summary>
        /// <returns>String presentation of the object</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("class Chargeback {\n");
            sb.Append("  Id: ").Append(Id).Append("\n");
            sb.Append("  CreatedAt: ").Append(CreatedAt).Append("\n");
            sb.Append("  Message: ").Append(Message).Append("\n");
            sb.Append("  Code: ").Append(Code).Append("\n");
            sb.Append("  Amount: ").Append(Amount).Append("\n");
            sb.Append("  Capture: ").Append(Capture).Append("\n");
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
            // Amount (double) minimum
            if (this.Amount < (double)0.5)
            {
                yield return new ValidationResult("Invalid value for Amount, must be a value greater than or equal to 0.5.", new[] { "Amount" });
            }

            yield break;
        }
    }

}
