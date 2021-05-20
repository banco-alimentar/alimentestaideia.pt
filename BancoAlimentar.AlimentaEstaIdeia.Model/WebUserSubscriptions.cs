// -----------------------------------------------------------------------
// <copyright file="WebUserSubscriptions.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Model
{
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;

    /// <summary>
    /// Table for the many to many relationship between <see cref="WebUser"/> and <see cref="Subscription"/>.
    /// </summary>
    public class WebUserSubscriptions
    {
        /// <summary>
        /// Gets or sets the table Id.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the user.
        /// </summary>
        public WebUser User { get; set; }

        /// <summary>
        /// Gets or sets the subscription.
        /// </summary>
        public Subscription Subscription { get; set; }
    }
}
