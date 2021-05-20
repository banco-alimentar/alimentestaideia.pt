// -----------------------------------------------------------------------
// <copyright file="DevelopingFeatureFlags.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Features
{
    /// <summary>
    /// This enum represent the feature flag enabled so far.
    /// </summary>
    public enum DevelopingFeatureFlags
    {
        /// <summary>
        /// Enable or disable the subscription flow in the donation process.
        /// </summary>
        SubscriptionDonation,

        /// <summary>
        /// Enable or disable the subscription in the admin section.
        /// </summary>
        SubscriptionAdmin,

        /// <summary>
        /// Eanble or disable the subscription payment system.
        /// </summary>
        SubscriptionPayements,
    }
}
