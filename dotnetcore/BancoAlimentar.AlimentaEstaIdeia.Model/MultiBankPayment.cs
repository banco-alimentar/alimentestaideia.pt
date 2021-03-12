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
    public class MultiBankPayment : EasyPayBaseClass
    {
        /// <summary>
        /// Gets or sets the transcation type.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the message.
        /// </summary>
        public string Message { get; set; }
    }
}
