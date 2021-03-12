namespace BancoAlimentar.AlimentaEstaIdeia.Model
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Represent a credit card payment.
    /// </summary>
    public class CreditCardPayment : EasyPayBaseClass
    {
        /// <summary>
        /// Gets or sets the url that the user need to be redirected to complete the payment.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Gets or sets the transaction requestes payment monetary value.
        /// </summary>
        public float Requested { get; set; }

        /// <summary>
        /// Gets or sets the transaction payed payment monetary value.
        /// </summary>
        public float Paid { get; set; }

        /// <summary>
        /// Gets or sets the transaction fixed feee value.
        /// </summary>
        public float FixedFee { get; set; }

        /// <summary>
        /// Gets or sets the transaction variable fee value.
        /// </summary>
        public float VariableFee { get; set; }

        /// <summary>
        /// Gets or sets the transaction tax value.
        /// </summary>
        public float Tax { get; set; }

        /// <summary>
        /// Gets or sets the transaction transfered monetary value.
        /// </summary>
        public float Transfer { get; set; }

    }
}
