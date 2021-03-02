namespace BancoAlimentar.AlimentaEstaIdeia.Model
{
    using System;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;

    /// <summary>
    /// Base class for the payment process.
    /// </summary>
    public class BasePayment
    {
        /// <summary>
        /// Gets or sets the entity id.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets when the payment was created.
        /// </summary>
        public DateTime Created { get; set; }

        /// <summary>
        /// Gets or sets the donation associated to this payment.
        /// </summary>
        public Donation Donation { get; set; }
    }
}
