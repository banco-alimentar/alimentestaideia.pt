// -----------------------------------------------------------------------
// <copyright file="SaftExporterController.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Api
{
    using System.IO;
    using System.Xml.Serialization;
    using BancoAlimentar.AlimentaEstaIdeia.Common.Services;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.Compliance.Tax;
    using Humanizer.DateTimeHumanizeStrategy;
    using Microsoft.ApplicationInsights;
    using Microsoft.AspNetCore.Mvc;

    /// <summary>
    /// SAFT-PT Web API that return the <see cref="BancoAlimentar.AlimentaEstaIdeia.Repository.Compliance.Tax.AuditFile"/> as XML.
    /// </summary>
    [Route("api/saftexporter")]
    [ApiController]
    public class SaftExporterController : ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly TelemetryClient telemetryClient;
        private readonly IAppVersionService appVersionService;

        /// <summary>
        /// Initializes a new instance of the <see cref="SaftExporterController"/> class.
        /// </summary>
        /// <param name="context">DbContext.</param>
        /// <param name="telemetryClient">TelemetryClient.</param>
        /// <param name="appVersionService">App Version Service.</param>
        public SaftExporterController(
            ApplicationDbContext context,
            TelemetryClient telemetryClient,
            IAppVersionService appVersionService)
        {
            this.context = context;
            this.telemetryClient = telemetryClient;
            this.appVersionService = appVersionService;
        }

        /// <summary>
        /// Execute the GET operation.
        /// </summary>
        /// <returns>The Action Result.</returns>
        public IActionResult OnGet()
        {
            SaftDocumentGenerator generator = new SaftDocumentGenerator(
                this.context,
                this.telemetryClient,
                this.appVersionService);

            XmlSerializer xmlSerializer = new XmlSerializer(typeof(AuditFile));

            using StringWriter writer = new StringWriter();

            xmlSerializer.Serialize(writer, generator.GeneateDocument());
            return this.Content(writer.ToString(), "application/xml");
        }
    }
}
