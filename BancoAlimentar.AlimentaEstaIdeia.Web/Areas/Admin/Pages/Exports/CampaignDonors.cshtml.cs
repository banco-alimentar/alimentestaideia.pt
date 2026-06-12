// -----------------------------------------------------------------------
// <copyright file="CampaignDonors.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages.Exports
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Web;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Localization;

    /// <summary>
    /// Export donors for a selected campaign as CSV.
    /// </summary>
    public class CampaignDonorsModel : PageModel
    {
        private readonly ApplicationDbContext context;
        private readonly IStringLocalizer<AdminSharedResources> localizer;

        /// <summary>
        /// Initializes a new instance of the <see cref="CampaignDonorsModel"/> class.
        /// </summary>
        /// <param name="context">Application database context.</param>
        /// <param name="localizer">Admin shared localizer.</param>
        public CampaignDonorsModel(ApplicationDbContext context, IStringLocalizer<AdminSharedResources> localizer)
        {
            this.context = context;
            this.localizer = localizer;
        }

        /// <summary>
        /// Gets or sets the selected campaign id.
        /// </summary>
        [BindProperty]
        public int? CampaignId { get; set; }

        /// <summary>
        /// Gets the campaigns available for export.
        /// </summary>
        public IList<SelectListItem> CampaignOptions { get; private set; } = new List<SelectListItem>();

        /// <summary>
        /// Load campaigns for the export form.
        /// </summary>
        public async Task OnGetAsync()
        {
            await this.LoadCampaignOptionsAsync();
        }

        /// <summary>
        /// Export donors for the selected campaign.
        /// </summary>
        /// <returns>CSV file download or the page with validation errors.</returns>
        public async Task<IActionResult> OnPostExportAsync()
        {
            if (!this.CampaignId.HasValue)
            {
                this.ModelState.AddModelError(nameof(this.CampaignId), this.localizer["SelectCampaignToExport"]);
                await this.LoadCampaignOptionsAsync();
                return this.Page();
            }

            Campaign? campaign = await this.context.Campaigns
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == this.CampaignId.Value);
            if (campaign == null)
            {
                return this.NotFound();
            }

            List<DonorExportRow> donors = await this.QueryDonorsAsync(this.CampaignId.Value);
            byte[] fileBytes = this.BuildCsv(donors);
            string fileName = $"campaign-{SanitizeFileName(campaign.Number)}-donors.csv";

            return this.File(fileBytes, "text/csv; charset=utf-8", fileName);
        }

        private static string CsvEscape(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            if (value.Contains('"', StringComparison.Ordinal)
                || value.Contains(',', StringComparison.Ordinal)
                || value.Contains('\r', StringComparison.Ordinal)
                || value.Contains('\n', StringComparison.Ordinal))
            {
                return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
            }

            return value;
        }

        private static string FormatCampaignLabel(Campaign campaign)
        {
            return $"{campaign.Number} ({campaign.Start:yyyy-MM-dd} - {campaign.End:yyyy-MM-dd})";
        }

        private static string SanitizeFileName(string campaignNumber)
        {
            if (string.IsNullOrWhiteSpace(campaignNumber))
            {
                return "unknown";
            }

            char[] invalidChars = System.IO.Path.GetInvalidFileNameChars();
            StringBuilder builder = new StringBuilder(campaignNumber.Length);
            foreach (char character in campaignNumber)
            {
                builder.Append(invalidChars.Contains(character) ? '-' : character);
            }

            return builder.ToString();
        }

        private async Task LoadCampaignOptionsAsync()
        {
            List<Campaign> campaigns = await this.context.Campaigns
                .AsNoTracking()
                .OrderByDescending(c => c.Start)
                .ThenByDescending(c => c.Id)
                .ToListAsync();

            this.CampaignOptions = campaigns
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(CultureInfo.InvariantCulture),
                    Text = FormatCampaignLabel(c),
                    Selected = this.CampaignId.HasValue && c.Id == this.CampaignId.Value,
                })
                .ToList();
        }

        private async Task<List<DonorExportRow>> QueryDonorsAsync(int campaignId)
        {
            List<Donation> donations = await this.context.Donations
                .AsNoTracking()
                .Include(d => d.User)
                .Where(d => d.CampaignId == campaignId
                    && (d.PaymentStatus == PaymentStatus.Payed || d.PaymentStatus == PaymentStatus.NotPayed)
                    && d.User != null)
                .ToListAsync();

            return donations
                .GroupBy(d => d.User.Email, StringComparer.OrdinalIgnoreCase)
                .Select(g => new DonorExportRow
                {
                    FullName = g.First().User.FullName,
                    Email = g.First().User.Email,
                })
                .OrderBy(d => d.FullName)
                .ThenBy(d => d.Email)
                .ToList();
        }

        private byte[] BuildCsv(IReadOnlyList<DonorExportRow> donors)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(string.Join(",", this.localizer["FullName"], this.localizer["Email"]));
            foreach (DonorExportRow donor in donors)
            {
                builder.Append(CsvEscape(donor.FullName));
                builder.Append(',');
                builder.AppendLine(CsvEscape(donor.Email));
            }

            return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(builder.ToString())).ToArray();
        }

        private sealed class DonorExportRow
        {
            public string? FullName { get; set; }

            public string? Email { get; set; }
        }
    }
}
