// -----------------------------------------------------------------------
// <copyright file="KeyNames.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Telemetry
{
    /// <summary>
    /// Name of the key for the telemetry.
    /// </summary>
    public class KeyNames
    {
        /// <summary>
        /// Key used for the current user.
        /// </summary>
        public const string CurrentUserKey = "__currentUserKey";

        /// <summary>
        /// Public Session id key name.
        /// </summary>
        public const string SessionIdKey = "SessionId";

        /// <summary>
        /// The name of the key in the HttpItems dictionary.
        /// </summary>
        public const string GenericNotificationKey = "__GenericNotificationKey";

        /// <summary>
        /// The name of the key in the HttpItems dictionary.
        /// </summary>
        public const string PaymentNotificationKey = "__PaymentNotificationKey";

        /// <summary>
        /// The name of the donation id key in the HttpItems dictionary.
        /// </summary>
        public const string DonationIdKey = "__DonationIdKey";

        /// <summary>
        /// Key used to persist the donation id.
        /// </summary>
        public const string DonationSessionKey = "_donationSessionKey";

        /// <summary>
        /// Key used to indicate that the donation has been completed.
        /// </summary>
        public const string DonationCompletedKey = "_donationCompleted";

        /// <summary>
        /// Name of the property in AI.
        /// </summary>
        public const string PropertyKey = "DonationSessionId";

        /// <summary>
        /// Id of the tenant.
        /// </summary>
        public const string TenantId = "TenantId";

        /// <summary>
        /// Name of the tenant.
        /// </summary>
        public const string TenantName = "TenantName";
    }
}
