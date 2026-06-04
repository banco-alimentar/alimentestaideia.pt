// -----------------------------------------------------------------------
// <copyright file="EasyPayWebhookVerificationOptions.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Services.EasyPay
{
    /// <summary>
    /// Configuration for Easypay webhook verification.
    /// </summary>
    public class EasyPayWebhookVerificationOptions
    {
        /// <summary>
        /// Configuration section name.
        /// </summary>
        public const string SectionName = "Easypay:Webhooks";

        /// <summary>
        /// Gets or sets the verification mode: <c>Api</c> (production) or <c>IntegrationTest</c> (in-memory tests).
        /// </summary>
        public string Mode { get; set; } = "Api";
    }
}
