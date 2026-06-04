// -----------------------------------------------------------------------
// <copyright file="DonationReportGenerationState.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.Reporting
{
    using System.Threading;

    /// <summary>
    /// Tracks whether a background donation report generation is in progress.
    /// </summary>
    public class DonationReportGenerationState
    {
        private int running;

        /// <summary>
        /// Gets a value indicating whether generation is currently running.
        /// </summary>
        public bool IsRunning => Volatile.Read(ref this.running) == 1;

        /// <summary>
        /// Attempts to mark generation as started.
        /// </summary>
        /// <returns><c>true</c> if this call started the run; otherwise <c>false</c>.</returns>
        public bool TryStart()
        {
            return Interlocked.CompareExchange(ref this.running, 1, 0) == 0;
        }

        /// <summary>
        /// Marks generation as finished.
        /// </summary>
        public void Complete()
        {
            Interlocked.Exchange(ref this.running, 0);
        }
    }
}
