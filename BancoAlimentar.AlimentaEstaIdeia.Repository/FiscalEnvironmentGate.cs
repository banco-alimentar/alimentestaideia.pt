// -----------------------------------------------------------------------
// <copyright file="FiscalEnvironmentGate.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository
{
    using System;
    using System.Globalization;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Prevents a deployment from issuing fiscal documents with a series that is not valid for
    /// its configured environment.
    /// </summary>
    public class FiscalEnvironmentGate : IFiscalEnvironmentGate
    {
        private const string PreProductionSeriesModeKey = "Fiscal:EnvironmentGate:PreProductionSeriesMode";
        private const string ProductionSigningReadyKey = "Fiscal:EnvironmentGate:ProductionSigningReady";
        private const string ProductionEnvironmentName = "Production";
        private const string StagingEnvironmentName = "Staging";
        private const string DevelopmentEnvironmentName = "Development";
        private const string TestingEnvironmentName = "Testing";
        private const string TestSeriesPrefix = "AA";

        private readonly IConfiguration configuration;
        private readonly IWebHostEnvironment webHostEnvironment;

        /// <summary>
        /// Initializes a new instance of the <see cref="FiscalEnvironmentGate"/> class.
        /// </summary>
        /// <param name="configuration">The application configuration.</param>
        /// <param name="webHostEnvironment">The current web hosting environment.</param>
        public FiscalEnvironmentGate(IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
        {
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.webHostEnvironment = webHostEnvironment ?? throw new ArgumentNullException(nameof(webHostEnvironment));
        }

        /// <inheritdoc />
        public void EnsureIssuanceAllowed(
            FiscalIssuerProfile fiscalIssuerProfile,
            FiscalDocumentSeries fiscalDocumentSeries)
        {
            if (fiscalIssuerProfile == null)
            {
                throw new ArgumentNullException(nameof(fiscalIssuerProfile));
            }

            if (fiscalDocumentSeries == null)
            {
                throw new ArgumentNullException(nameof(fiscalDocumentSeries));
            }

            string environmentName = this.webHostEnvironment.EnvironmentName;
            if (string.Equals(environmentName, DevelopmentEnvironmentName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(environmentName, TestingEnvironmentName, StringComparison.OrdinalIgnoreCase))
            {
                this.EnsureTestSeries(fiscalDocumentSeries);
                return;
            }

            if (string.Equals(environmentName, ProductionEnvironmentName, StringComparison.OrdinalIgnoreCase))
            {
                this.EnsureProductionConfiguration(fiscalIssuerProfile, fiscalDocumentSeries);
                return;
            }

            if (string.Equals(environmentName, StagingEnvironmentName, StringComparison.OrdinalIgnoreCase))
            {
                this.EnsurePreProductionConfiguration(fiscalIssuerProfile, fiscalDocumentSeries);
                return;
            }

            throw new InvalidOperationException(
                $"Fiscal issuance is blocked because environment '{environmentName ?? "(unset)"}' has no configured fiscal series policy.");
        }

        private void EnsurePreProductionConfiguration(
            FiscalIssuerProfile fiscalIssuerProfile,
            FiscalDocumentSeries fiscalDocumentSeries)
        {
            string preProductionSeriesMode = this.configuration[PreProductionSeriesModeKey];
            if (string.Equals(preProductionSeriesMode, "Test", StringComparison.OrdinalIgnoreCase))
            {
                this.EnsureTestSeries(fiscalDocumentSeries);
                return;
            }

            if (string.Equals(preProductionSeriesMode, "Production", StringComparison.OrdinalIgnoreCase))
            {
                this.EnsureProductionConfiguration(fiscalIssuerProfile, fiscalDocumentSeries);
                return;
            }

            throw new InvalidOperationException(
                "Fiscal issuance in Staging requires Fiscal:EnvironmentGate:PreProductionSeriesMode to be Test or Production.");
        }

        private void EnsureProductionConfiguration(
            FiscalIssuerProfile fiscalIssuerProfile,
            FiscalDocumentSeries fiscalDocumentSeries)
        {
            if (this.IsTestSeries(fiscalDocumentSeries))
            {
                throw new InvalidOperationException("Production fiscal issuance cannot use an AT test series.");
            }

            if (!this.HasNonZeroCertificateNumber(fiscalIssuerProfile.SoftwareCertificateNumber))
            {
                throw new InvalidOperationException("Production fiscal issuance requires a non-zero software certificate number.");
            }

            if (!bool.TryParse(this.configuration[ProductionSigningReadyKey], out bool productionSigningReady) ||
                !productionSigningReady)
            {
                throw new InvalidOperationException("Production fiscal issuance requires the production signing provider to be ready.");
            }
        }

        private void EnsureTestSeries(FiscalDocumentSeries fiscalDocumentSeries)
        {
            if (!this.IsTestSeries(fiscalDocumentSeries))
            {
                throw new InvalidOperationException("Non-production fiscal issuance requires an AT test series.");
            }
        }

        private bool IsTestSeries(FiscalDocumentSeries fiscalDocumentSeries)
        {
            return fiscalDocumentSeries.AtcudValidationCode?.StartsWith(TestSeriesPrefix, StringComparison.OrdinalIgnoreCase) == true;
        }

        private bool HasNonZeroCertificateNumber(string softwareCertificateNumber)
        {
            return long.TryParse(
                softwareCertificateNumber,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out long certificateNumber) && certificateNumber > 0;
        }
    }
}
