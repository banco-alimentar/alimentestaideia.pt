namespace BancoAlimentar.AlimentaEstaIdeia.Model
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// PayPal payment system.
    /// </summary>
    public class PayPalPayment : BasePayment
    {
        /// <summary>
        /// Gets or sets the PayPal Payment Id.
        /// </summary>
        public string PayPalPaymentId { get; set; }

        /// <summary>
        /// Gets or sets the Payer Id.
        /// </summary>
        public string PayerId { get; set; }

        /// <summary>
        /// Gets or sets the PayPal token.
        /// </summary>
        public string Token { get; set; }
    }
}
