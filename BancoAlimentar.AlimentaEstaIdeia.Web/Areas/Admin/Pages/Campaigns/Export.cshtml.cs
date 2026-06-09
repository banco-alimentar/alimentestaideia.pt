// -----------------------------------------------------------------------
// <copyright file="Export.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages.Campaigns
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Web;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Localization;

    /// <summary>
    /// Export a campaign.
    /// </summary>
    public class ExportModel : PageModel
    {
        private readonly ApplicationDbContext context;
        private readonly IStringLocalizer<AdminSharedResources> localizer;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExportModel"/> class.
        /// </summary>
        /// <param name="context">Application Db Context.</param>
        /// <param name="localizer">Admin shared localizer.</param>
        public ExportModel(ApplicationDbContext context, IStringLocalizer<AdminSharedResources> localizer)
        {
            this.context = context;
            this.localizer = localizer;
        }

        /// <summary>
        /// Get the export page.
        /// </summary>
        /// <param name="id">Campaign id.</param>
        public IActionResult OnGet(int id)
        {
            Campaign? campaign = this.context.Campaigns.Find(id);
            if (campaign != null)
            {
                List<string> allEmails = this.context.Donations
                    .Include(p => p.User)
                    .Where(p => p.DonationDate <= campaign.End && p.DonationDate >= campaign.Start)
                    .Select(p => p.User.Email)
                    .ToList();

                MemoryStream ms = new MemoryStream();

                using (StreamWriter writer = new StreamWriter(ms, Encoding.UTF8))
                {
                    writer.WriteLine(this.localizer["Emails"]);
                    foreach (string email in allEmails)
                    {
                        writer.WriteLine(email);
                    }
                }

                return new FileContentResult(ms.ToArray(), "text/csv")
                {
                    FileDownloadName = $"{campaign.Number}.csv",
                };
            }
            else
            {
                return this.NotFound();
            }
        }
    }
}
