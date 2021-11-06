﻿// -----------------------------------------------------------------------
// <copyright file="SubscriptionStatus.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Model
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Represent the <see cref="Subscription"/> current status.
    /// </summary>
    public enum SubscriptionStatus
    {
        /// <summary>
        /// The subscription is created on the EasyPay API.
        /// </summary>
        Created,

        /// <summary>
        /// The subscription has a payment method enabled.
        /// </summary>
        Capture,

        /// <summary>
        /// The subscription is active.
        /// </summary>
        Active,

        /// <summary>
        /// The subscription is not active.
        /// </summary>
        Inactive,

        /// <summary>
        /// The subscription has an error.
        /// </summary>
        Error,

        /// <summary>
        /// The subscription is expired.
        /// </summary>
        Expired,
    }
}
