// -----------------------------------------------------------------------
// <copyright file="Modelo25.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages.Exports
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using BancoAlimentar.AlimentaEstaIdeia.Web;
    using ClosedXML.Excel;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Localization;

    /// <summary>
    /// Export Modelo 25 invoice data for a selected year as Excel.
    /// </summary>
    public class Modelo25Model : PageModel
    {
        private const string InvoiceDownloadPath = "/Identity/Account/Manage/GenerateInvoice?publicDonationId=";

        private readonly ApplicationDbContext context;
        private readonly IStringLocalizer<AdminSharedResources> localizer;

        /// <summary>
        /// Initializes a new instance of the <see cref="Modelo25Model"/> class.
        /// </summary>
        /// <param name="context">Application database context.</param>
        /// <param name="localizer">Admin shared localizer.</param>
        public Modelo25Model(ApplicationDbContext context, IStringLocalizer<AdminSharedResources> localizer)
        {
            this.context = context;
            this.localizer = localizer;
        }

        /// <summary>
        /// Gets or sets the selected year.
        /// </summary>
        [BindProperty]
        public int? Year { get; set; }

        /// <summary>
        /// Gets the years available for export.
        /// </summary>
        public IList<SelectListItem> YearOptions { get; private set; } = new List<SelectListItem>();

        /// <summary>
        /// Load years for the export form.
        /// </summary>
        public async Task OnGetAsync()
        {
            await this.LoadYearOptionsAsync();
        }

        /// <summary>
        /// Export Modelo 25 data for the selected year.
        /// </summary>
        /// <returns>Excel file download or the page with validation errors.</returns>
        public async Task<IActionResult> OnPostExportAsync()
        {
            if (!this.Year.HasValue)
            {
                this.ModelState.AddModelError(nameof(this.Year), this.localizer["SelectYearToExport"]);
                await this.LoadYearOptionsAsync();
                return this.Page();
            }

            List<Modelo25Row> rows = await this.QueryRowsAsync(this.Year.Value);
            byte[] fileBytes = BuildExcelFile(rows, this.BuildInvoiceBaseUrl());
            string fileName = $"modelo25-{this.Year.Value}.xlsx";

            return this.File(
                fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        private static byte[] BuildExcelFile(IReadOnlyList<Modelo25Row> rows, string invoiceBaseUrl)
        {
            using XLWorkbook workbook = new XLWorkbook();
            IXLWorksheet worksheet = workbook.Worksheets.Add("Modelo25");

            worksheet.Cell(1, 1).Value = "Data";
            worksheet.Cell(1, 2).Value = "Valor";
            worksheet.Cell(1, 3).Value = "NIF";
            worksheet.Cell(1, 4).Value = "ReciboNr";
            worksheet.Cell(1, 5).Value = "Recibo";
            worksheet.Range(1, 1, 1, 5).Style.Font.Bold = true;

            int rowIndex = 2;
            foreach (Modelo25Row row in rows)
            {
                worksheet.Cell(rowIndex, 1).Value = row.DonationDate;
                worksheet.Cell(rowIndex, 1).Style.DateFormat.Format = "yyyy-MM-dd";
                worksheet.Cell(rowIndex, 2).Value = row.DonationAmount;
                worksheet.Cell(rowIndex, 3).Value = row.Nif ?? string.Empty;
                worksheet.Cell(rowIndex, 4).Value = row.InvoiceNumber ?? string.Empty;

                string receiptUrl = invoiceBaseUrl + row.PublicId.ToString("D", CultureInfo.InvariantCulture);
                worksheet.Cell(rowIndex, 5).Value = receiptUrl;
                worksheet.Cell(rowIndex, 5).SetHyperlink(new XLHyperlink(receiptUrl));

                rowIndex++;
            }

            worksheet.Columns().AdjustToContents();

            using MemoryStream stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        private async Task LoadYearOptionsAsync()
        {
            List<int> years = await this.context.Invoices
                .AsNoTracking()
                .Where(i => !i.IsCanceled && i.Donation != null)
                .Select(i => i.Donation.DonationDate.Year)
                .Distinct()
                .OrderByDescending(year => year)
                .ToListAsync();

            if (years.Count == 0)
            {
                years.Add(DateTime.UtcNow.Year);
            }

            this.YearOptions = years
                .Select(year => new SelectListItem
                {
                    Value = year.ToString(CultureInfo.InvariantCulture),
                    Text = year.ToString(CultureInfo.InvariantCulture),
                    Selected = this.Year.HasValue && year == this.Year.Value,
                })
                .ToList();
        }

        private async Task<List<Modelo25Row>> QueryRowsAsync(int year)
        {
            return await this.context.Invoices
                .AsNoTracking()
                .Where(i => !i.IsCanceled
                    && i.Donation != null
                    && i.Donation.DonationDate.Year == year)
                .OrderBy(i => i.Donation.DonationDate)
                .ThenBy(i => i.Number)
                .Select(i => new Modelo25Row
                {
                    DonationDate = i.Donation.DonationDate,
                    DonationAmount = i.Donation.DonationAmount,
                    Nif = i.Donation.Nif,
                    InvoiceNumber = i.Number,
                    PublicId = i.Donation.PublicId,
                })
                .ToListAsync();
        }

        private string BuildInvoiceBaseUrl()
        {
            return $"{this.Request.Scheme}://{this.Request.Host.Value}{InvoiceDownloadPath}";
        }

        private sealed class Modelo25Row
        {
            public DateTime DonationDate { get; set; }

            public double DonationAmount { get; set; }

            public string? Nif { get; set; }

            public string? InvoiceNumber { get; set; }

            public Guid PublicId { get; set; }
        }
    }
}
