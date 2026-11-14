// -----------------------------------------------------------------------
// <copyright file="DonationExperimentService.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Services.Donations
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Security.Cryptography;
    using Microsoft.ApplicationInsights;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Assigns a stable donation-page variant for the lifetime of the visitor session.
    /// </summary>
    public sealed class DonationExperimentService : IDonationExperimentService
    {
        private const string SessionKey = "Donation2ExperimentVariant";
        private const string Donation2Variant = "Donation2";
        private const string LegacyVariant = "Donation";
        private readonly IOptionsMonitor<DonationExperimentOptions> options;
        private readonly TelemetryClient telemetryClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="DonationExperimentService"/> class.
        /// </summary>
        /// <param name="options">Experiment configuration.</param>
        /// <param name="telemetryClient">Application telemetry client.</param>
        public DonationExperimentService(
            IOptionsMonitor<DonationExperimentOptions> options,
            TelemetryClient telemetryClient)
        {
            this.options = options;
            this.telemetryClient = telemetryClient;
        }

        /// <inheritdoc/>
        public bool ShouldUseDonation2(HttpContext httpContext)
        {
            int trafficPercentage = Math.Clamp(this.options.CurrentValue.TrafficPercentage, 0, 100);
            if (trafficPercentage == 0)
            {
                return false;
            }

            string assignedVariant = httpContext.Session.GetString(SessionKey);
            if (!string.IsNullOrEmpty(assignedVariant))
            {
                return string.Equals(assignedVariant, Donation2Variant, StringComparison.Ordinal);
            }

            bool useDonation2 = trafficPercentage == 100
                || RandomNumberGenerator.GetInt32(100) < trafficPercentage;
            assignedVariant = useDonation2 ? Donation2Variant : LegacyVariant;
            httpContext.Session.SetString(SessionKey, assignedVariant);
            this.telemetryClient.TrackEvent(
                "DonationExperienceAssigned",
                new Dictionary<string, string>
                {
                    { "Variant", assignedVariant },
                    { "TrafficPercentage", trafficPercentage.ToString(CultureInfo.InvariantCulture) },
                    { "SessionId", httpContext.Session.Id },
                });
            return useDonation2;
        }
    }
}