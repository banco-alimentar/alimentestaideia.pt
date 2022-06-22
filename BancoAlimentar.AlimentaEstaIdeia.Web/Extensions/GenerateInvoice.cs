// -----------------------------------------------------------------------
// <copyright file="GenerateInvoice.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Extensions
{
    using System.IO;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Repository.Validation;
    using BancoAlimentar.AlimentaEstaIdeia.Sas.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Pages.Account.Manage;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Pages;
    using Microsoft.ApplicationInsights;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Localization;
    using Microsoft.FeatureManagement;

    /// <summary>
    /// Class that generates invoices.
    /// </summary>
    public class GenerateInvoice
    {
        private readonly IUnitOfWork context;
        private readonly IViewRenderService renderService;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IConfiguration configuration;
        private readonly IStringLocalizerFactory stringLocalizerFactory;
        private readonly IFeatureManager featureManager;
        private readonly IWebHostEnvironment env;
        private readonly NifApiValidator nifApiValidator;
        private readonly TelemetryClient telemetryClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="GenerateInvoice"/> class.
        /// </summary>
        public GenerateInvoice(
            IUnitOfWork context,
            IViewRenderService renderService,
            IWebHostEnvironment webHostEnvironment,
            IConfiguration configuration,
            IStringLocalizerFactory stringLocalizerFactory,
            IFeatureManager featureManager,
            IWebHostEnvironment env,
            NifApiValidator nifApiValidator,
            TelemetryClient telemetryClient)
        {
            this.context = context;
            this.renderService = renderService;
            this.webHostEnvironment = webHostEnvironment;
            this.configuration = configuration;
            this.stringLocalizerFactory = stringLocalizerFactory;
            this.featureManager = featureManager;
            this.env = env;
            this.nifApiValidator = nifApiValidator;
            this.telemetryClient = telemetryClient;
        }

        /// <summary>
        /// Creates an invoice.
        /// </summary>
        /// <param name="publicId">Donation public id.</param>
        /// <param name="tenant">Current tenant.</param>
        /// <returns>(Invoice invoice, Stream pdfFile).</returns>
        public async Task<(Invoice Invoice, Stream PdfFile)> GeneratePDFInvoiceAsync(
            string publicId,
            Tenant tenant)
        {
            GenerateInvoiceModel generateInvoiceModel = new GenerateInvoiceModel(
                            this.context,
                            this.renderService,
                            this.webHostEnvironment,
                            this.configuration,
                            this.stringLocalizerFactory,
                            this.featureManager,
                            this.env,
                            this.nifApiValidator,
                            this.telemetryClient);

            return await generateInvoiceModel.GenerateInvoiceInternalAsync(publicId, tenant);
        }
    }
}
