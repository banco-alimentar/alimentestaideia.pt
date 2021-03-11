namespace BancoAlimentar.AlimentaEstaIdeia.Model
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// MultiBank payment system.
    /// </summary>
    public class MultiBankPayment : BasePayment
    {
        /// <summary>
        /// Gets or sets the easypay transaction key.
        /// </summary>
        public string TransactionKey { get; set; }

        /// <summary>
        /// Gets or set the payment status.
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the transcation type.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the message.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the easypay notification id.
        /// </summary>
        public string EasyPayPaymentId { get; set; }
    }
}
