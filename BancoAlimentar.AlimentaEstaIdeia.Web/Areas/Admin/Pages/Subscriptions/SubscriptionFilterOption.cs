// -----------------------------------------------------------------------
// <copyright file="SubscriptionFilterOption.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages.Subscriptions
{
    /// <summary>
    /// Filter dropdown option with result count.
    /// </summary>
    public sealed class SubscriptionFilterOption
    {
        /// <summary>
        /// Gets or sets the filter value.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Gets or sets the display label without count.
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// Gets or sets the number of subscriptions matching this filter.
        /// </summary>
        public int Count { get; set; }
    }
}
