// -----------------------------------------------------------------------
// <copyright file="SaftDocumentGenerator.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Repository.Compliance.Tax
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Xml.Serialization;
    using BancoAlimentar.AlimentaEstaIdeia.Common.Services;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using Microsoft.ApplicationInsights;
    using Microsoft.Extensions.Caching.Memory;
    using Polly;

    /// <summary>
    /// Generate and xml file in compliance with the SAFT-PT format.
    /// </summary>
    public class SaftDocumentGenerator
    {
        private readonly ApplicationDbContext context;
        private readonly TelemetryClient telemetryClient;
        private readonly IAppVersionService appVersionService;

        /// <summary>
        /// Initializes a new instance of the <see cref="SaftDocumentGenerator"/> class.
        /// </summary>
        /// <param name="context">DbContext.</param>
        /// <param name="telemetryClient">TelemetryClient.</param>
        /// <param name="appVersionService">Application Version Service.</param>
        public SaftDocumentGenerator(
            ApplicationDbContext context,
            TelemetryClient telemetryClient,
            IAppVersionService appVersionService)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
            this.telemetryClient = telemetryClient ?? throw new ArgumentNullException(nameof(telemetryClient));
            this.appVersionService = appVersionService;
        }

        /// <summary>
        /// Generate the AuditFile.
        /// </summary>
        /// <returns>A reference to the Audit File.</returns>
        public AuditFile GeneateDocument()
        {
            AuditFile result = new AuditFile();
            result.Header = this.CreateHeader();
            result.MasterFiles = new AuditFileMasterFiles();
            result.SourceDocuments = new SourceDocuments();
            result.GeneralLedgerEntries = new GeneralLedgerEntries();
            return result;
        }

        private Header CreateHeader()
        {
            Header result = new Header();
            result.StartDate = DateTime.Now;
            result.DateCreated = DateTime.Now;
            result.FiscalYear = DateTime.Now.Year.ToString();
            result.AuditFileVersion = this.appVersionService.Version;
            result.SoftwareCertificateNumber = "1235";
            result.ProductVersion = this.appVersionService.Version;
            result.Website = "https://dev.alimentestaideia.pt/";
            return result;
        }
    }
}
