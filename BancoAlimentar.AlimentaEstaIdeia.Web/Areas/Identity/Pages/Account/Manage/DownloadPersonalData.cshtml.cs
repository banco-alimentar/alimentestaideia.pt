// -----------------------------------------------------------------------
// <copyright file="DownloadPersonalData.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Identity.Pages.Account.Manage
{
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Text;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Model.Identity;
    using BancoAlimentar.AlimentaEstaIdeia.Repository;
    using BancoAlimentar.AlimentaEstaIdeia.Web.JsonConverter.PersonalData;
    using BancoAlimentar.AlimentaEstaIdeia.Web.Pages;
    using Microsoft.ApplicationInsights;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Localization;
    using Microsoft.FeatureManagement;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Class for downloading your personal data from the website.
    /// </summary>
    public class DownloadPersonalDataModel : PageModel
    {
        private readonly UserManager<WebUser> userManager;
        private readonly IUnitOfWork context;
        private readonly TelemetryClient telemetryClient;
        private readonly IViewRenderService renderService;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IConfiguration configuration;
        private readonly IStringLocalizerFactory stringLocalizerFactory;
        private readonly IFeatureManager featureManager;
        private readonly IWebHostEnvironment env;

        /// <summary>
        /// Initializes a new instance of the <see cref="DownloadPersonalDataModel"/> class.
        /// </summary>
        /// <param name="userManager">User Manager.</param>
        /// <param name="context">Unit of work.</param>
        /// <param name="telemetryClient">Telemetry client.</param>
        /// <param name="renderService">Render service.</param>
        /// <param name="webHostEnvironment">Web host environment.</param>
        /// <param name="configuration">Configuration.</param>
        /// <param name="stringLocalizerFactory">Localizer factory.</param>
        /// <param name="featureManager">Feature manager.</param>
        /// <param name="env">Web hosting environment.</param>
        public DownloadPersonalDataModel(
            UserManager<WebUser> userManager,
            IUnitOfWork context,
            TelemetryClient telemetryClient,
            IViewRenderService renderService,
            IWebHostEnvironment webHostEnvironment,
            IConfiguration configuration,
            IStringLocalizerFactory stringLocalizerFactory,
            IFeatureManager featureManager,
            IWebHostEnvironment env)
        {
            this.userManager = userManager;
            this.context = context;
            this.telemetryClient = telemetryClient;
            this.renderService = renderService;
            this.webHostEnvironment = webHostEnvironment;
            this.configuration = configuration;
            this.stringLocalizerFactory = stringLocalizerFactory;
            this.featureManager = featureManager;
            this.env = env;
        }

        /// <summary>
        /// Execute the post operation.
        /// </summary>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<IActionResult> OnPostAsync()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{userManager.GetUserId(User)}'.");
            }

            Dictionary<string, string> extraProperties = new ();
            extraProperties.Add("UserId", user.Id);
            telemetryClient.TrackEvent("DownloadPersonalData", extraProperties);

            JsonSerializer serializer = new ();
            serializer.Formatting = Formatting.Indented;
            serializer.Converters.Add(new GenericPersonalDataConverter<WebUser>());
            serializer.Converters.Add(new GenericPersonalDataConverter<Donation>());
            serializer.Converters.Add(new GenericPersonalDataConverter<DonorAddress>());

            JObject downloadData = new ()
            {
                ["User"] = JObject.FromObject(user, serializer),
                ["Donations"] = JArray.FromObject(RemoveReferenceLoops(context.Donation.GetUserDonation(user.Id)), serializer),
            };
            var invoices = context.Invoice.GetAllInvoicesFromUserId(user.Id);

            using MemoryStream ms = new ();
            using (ZipArchive archive = new (ms, ZipArchiveMode.Create, true))
            {
                var logins = await userManager.GetLoginsAsync(user);
                foreach (var l in logins)
                {
                    downloadData[$"Login-{l.ProviderDisplayName}"] = JObject.FromObject(l);
                }

                ZipArchiveEntry personalDataZipEntry = archive.CreateEntry("PersonalData.json");
                using (Stream peronsalDataWriter = personalDataZipEntry.Open())
                {
                    using StreamWriter streamWriter = new StreamWriter(peronsalDataWriter, Encoding.UTF8);
                    streamWriter.Write(JsonConvert.SerializeObject(downloadData, Formatting.Indented));
                }

                GenerateInvoiceModel generateInvoiceModel = new GenerateInvoiceModel(
                       this.context,
                       this.renderService,
                       this.webHostEnvironment,
                       this.configuration,
                       this.stringLocalizerFactory,
                       this.featureManager,
                       this.env)
                {
                    WillGenerateInvoice = false,
                };

                foreach (var item in invoices)
                {
                    var pdfFile = await generateInvoiceModel.RenderInternal(item.Donation.PublicId.ToString());
                    if (pdfFile.PdfFile != null)
                    {
                        ZipArchiveEntry pdfZipEntry = archive.CreateEntry($"{pdfFile.Invoice.Number.Replace("/", "-")}.pdf");
                        using Stream fileStream = pdfZipEntry.Open();
                        await pdfFile.PdfFile.CopyToAsync(fileStream);
                    }
                }
            }

            ms.Position = 0;
            Response.Headers.Add("Content-Disposition", "attachment; filename=PersonalData.zip");
            return new FileContentResult(ms.GetBuffer(), "application/x-zip-compressed");
        }

        private static List<Donation> RemoveReferenceLoops(List<Donation> items)
        {
            foreach (var item in items)
            {
                foreach (var donationItem in item.DonationItems)
                {
                    donationItem.Donation = null;
                }

                foreach (var payment in item.Payments)
                {
                    payment.Donation = null;
                }
            }

            return items;
        }
    }
}
