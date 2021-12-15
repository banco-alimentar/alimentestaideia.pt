// -----------------------------------------------------------------------
// <copyright file="NifApiValidator.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.Validation
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Configuration;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Validate if a Nif is valid or not.
    /// </summary>
    public class NifApiValidator
    {
        private static readonly HttpClient Client = new HttpClient();
        private readonly string key;
        private readonly IMemoryCache memoryCache;
        private readonly TelemetryClient telemetryClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="NifApiValidator"/> class.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        /// <param name="memoryCache">Memory cache.</param>
        /// <param name="telemetryClient">Telemetry client.</param>
        public NifApiValidator(
            IConfiguration configuration,
            IMemoryCache memoryCache,
            TelemetryClient telemetryClient)
        {
            this.key = configuration["NifApi"];
            this.memoryCache = memoryCache;
            this.telemetryClient = telemetryClient;
        }

        /// <summary>
        /// Checks if the nif is valid or not.
        /// </summary>
        /// <param name="value">The nif to validate.</param>
        /// <returns>True if the nif is valid, false otherwise.</returns>
        public async Task<bool> IsValidNif(string value)
        {
            bool result = false;
            if (this.memoryCache.TryGetValue(value, out bool resultCached))
            {
                result = resultCached;
            }
            else
            {
                try
                {
                    HttpResponseMessage response = await Client.GetAsync($"https://www.nif.pt/?json=1&q={value}&key={this.key}");
                    if (response.IsSuccessStatusCode)
                    {
                        JObject json = JObject.Parse(await response.Content.ReadAsStringAsync());
                        result = json["nif_validation"].Value<bool>();
                        this.memoryCache.Set(value, result);
                    }
                }
                catch (Exception ex)
                {
                    this.telemetryClient.TrackException(ex, new Dictionary<string, string>() { { "Nif", value } });
                    result = NifValidation.ValidateNif(value);
                }
            }

            return result;
        }
    }
}
