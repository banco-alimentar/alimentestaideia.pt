// -----------------------------------------------------------------------
// <copyright file="TenantTelemetryChannel.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Telemetry
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel;

    /// <summary>
    /// Sas Tenant telemetry channel.
    /// </summary>
    public class TenantTelemetryChannel : ITelemetryChannel, IAsyncFlushable, ITelemetryModule
    {
        private ITelemetryChannel innerChannel;

        /// <summary>
        /// Initializes a new instance of the <see cref="TenantTelemetryChannel"/> class.
        /// </summary>
        public TenantTelemetryChannel(ITelemetryChannel innertTelemetryChannel)
        {
            this.innerChannel = innertTelemetryChannel;
            this.DeveloperMode = false;
            this.EndpointAddress = this.innerChannel.EndpointAddress;
        }

        /// <inheritdoc/>
        public bool? DeveloperMode { get; set; }

        /// <inheritdoc/>
        public string EndpointAddress { get; set; }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.innerChannel.Dispose();
        }

        /// <inheritdoc/>
        public void Flush()
        {
            this.innerChannel.Flush();
        }

        /// <inheritdoc/>
        public Task<bool> FlushAsync(CancellationToken cancellationToken)
        {
            if (this.innerChannel is IAsyncFlushable)
            {
                return ((IAsyncFlushable)this.innerChannel).FlushAsync(cancellationToken);
            }
            else
            {
                return Task.FromResult(true);
            }
        }

        /// <inheritdoc/>
        public void Initialize(TelemetryConfiguration configuration)
        {
            if (this.innerChannel is ITelemetryModule)
            {
                ((ITelemetryModule)this.innerChannel).Initialize(configuration);
            }
        }

        /// <inheritdoc/>
        public void Send(ITelemetry item)
        {
            this.innerChannel.Send(item);
        }
    }
}
