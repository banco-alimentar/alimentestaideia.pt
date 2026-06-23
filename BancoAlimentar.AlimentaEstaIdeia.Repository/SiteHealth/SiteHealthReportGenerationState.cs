// -----------------------------------------------------------------------
// <copyright file="SiteHealthReportGenerationState.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.SiteHealth
{
    using System;
    using System.Threading;

    /// <summary>
    /// Tracks in-process site health report generation progress.
    /// </summary>
    public class SiteHealthReportGenerationState
    {
        private int running;
        private int progress;
        private string statusMessage = string.Empty;
        private string errorMessage;
        private DateTime? startedAtUtc;
        private DateTime? completedAtUtc;
        private DateTime? reportGeneratedAtUtc;

        /// <summary>
        /// Gets a value indicating whether generation is running.
        /// </summary>
        public bool IsRunning => Volatile.Read(ref this.running) == 1;

        /// <summary>
        /// Gets the current progress (0-100).
        /// </summary>
        public int Progress => Volatile.Read(ref this.progress);

        /// <summary>
        /// Gets the current status message.
        /// </summary>
        public string StatusMessage => this.statusMessage ?? string.Empty;

        /// <summary>
        /// Gets the last error message.
        /// </summary>
        public string ErrorMessage => this.errorMessage;

        /// <summary>
        /// Gets when the current/last run started.
        /// </summary>
        public DateTime? StartedAtUtc => this.startedAtUtc;

        /// <summary>
        /// Gets when the current/last run completed.
        /// </summary>
        public DateTime? CompletedAtUtc => this.completedAtUtc;

        /// <summary>
        /// Gets when the last successful report was generated.
        /// </summary>
        public DateTime? ReportGeneratedAtUtc => this.reportGeneratedAtUtc;

        /// <summary>
        /// Attempts to mark generation as started.
        /// </summary>
        /// <returns><c>true</c> if this call started the run.</returns>
        public bool TryStart()
        {
            if (Interlocked.CompareExchange(ref this.running, 1, 0) != 0)
            {
                return false;
            }

            Volatile.Write(ref this.progress, 0);
            this.statusMessage = "Starting…";
            this.errorMessage = null;
            this.startedAtUtc = DateTime.UtcNow;
            this.completedAtUtc = null;
            return true;
        }

        /// <summary>
        /// Updates progress for the current run.
        /// </summary>
        /// <param name="progress">Progress 0-100.</param>
        /// <param name="statusMessage">Human-readable step.</param>
        public void SetProgress(int progress, string statusMessage)
        {
            if (!this.IsRunning)
            {
                return;
            }

            Volatile.Write(ref this.progress, Math.Clamp(progress, 0, 100));
            this.statusMessage = statusMessage ?? string.Empty;
        }

        /// <summary>
        /// Marks generation as successfully completed.
        /// </summary>
        /// <param name="reportGeneratedAtUtc">Report timestamp.</param>
        public void CompleteSuccess(DateTime reportGeneratedAtUtc)
        {
            this.reportGeneratedAtUtc = reportGeneratedAtUtc;
            this.errorMessage = null;
            Volatile.Write(ref this.progress, 100);
            this.statusMessage = "Complete";
            this.completedAtUtc = DateTime.UtcNow;
            Interlocked.Exchange(ref this.running, 0);
        }

        /// <summary>
        /// Marks generation as failed.
        /// </summary>
        /// <param name="errorMessage">Failure message.</param>
        public void CompleteFailure(string errorMessage)
        {
            this.errorMessage = errorMessage;
            this.statusMessage = "Failed";
            this.completedAtUtc = DateTime.UtcNow;
            Interlocked.Exchange(ref this.running, 0);
        }

        /// <summary>
        /// Creates a status DTO from current state.
        /// </summary>
        /// <returns>Status snapshot.</returns>
        public SiteHealthReportGenerationStatus ToStatus()
        {
            return new SiteHealthReportGenerationStatus
            {
                IsRunning = this.IsRunning,
                Progress = this.Progress,
                StatusMessage = this.StatusMessage,
                StartedAtUtc = this.startedAtUtc,
                CompletedAtUtc = this.completedAtUtc,
                ErrorMessage = this.errorMessage,
                ReportGeneratedAtUtc = this.reportGeneratedAtUtc,
            };
        }
    }
}
