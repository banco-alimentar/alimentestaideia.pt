// -----------------------------------------------------------------------
// <copyright file="CancelInvoice.cshtml.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Areas.Admin.Pages
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Model;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Localization;

    /// <summary>
    /// Admin action to cancel invoices and list canceled invoices.
    /// </summary>
    public class CancelInvoiceModel : PageModel
    {
        private const int MaxInvoiceNumberLength = 128;
        private const int MaxCancelReasonLength = 2000;

        private readonly ApplicationDbContext dbContext;
        private readonly IStringLocalizer localizer;

        /// <summary>
        /// Initializes a new instance of the <see cref="CancelInvoiceModel"/> class.
        /// </summary>
        /// <param name="dbContext">Application database context.</param>
        /// <param name="localizerFactory">String localizer factory.</param>
        public CancelInvoiceModel(
            ApplicationDbContext dbContext,
            IStringLocalizerFactory localizerFactory)
        {
            this.dbContext = dbContext;
            this.localizer = localizerFactory.Create(
                "BancoAlimentar.AlimentaEstaIdeia.Web.Resources.Areas.Admin.Pages.CancelInvoice",
                Assembly.GetExecutingAssembly().GetName().Name);
        }

        /// <summary>
        /// Gets or sets the invoice number to look up.
        /// </summary>
        [BindProperty]
        [Display(Name = "InvoiceNumber")]
        [StringLength(MaxInvoiceNumberLength)]
        public string InvoiceNumber { get; set; }

        /// <summary>
        /// Gets or sets the cancellation reason.
        /// </summary>
        [BindProperty]
        [Display(Name = "CancelReason")]
        [StringLength(MaxCancelReasonLength)]
        public string CancelReason { get; set; }

        /// <summary>
        /// Gets or sets the selected invoice identifier for cancellation.
        /// </summary>
        [BindProperty]
        public int? SelectedInvoiceId { get; set; }

        /// <summary>
        /// Gets or sets the success status message.
        /// </summary>
        [TempData]
        public string StatusMessage { get; set; }

        /// <summary>
        /// Gets or sets the error status message.
        /// </summary>
        [TempData]
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the invoice found for cancellation.
        /// </summary>
        public InvoiceLookupViewModel SelectedInvoice { get; set; }

        /// <summary>
        /// Gets or sets canceled invoices for the list section.
        /// </summary>
        public IList<CanceledInvoiceViewModel> CanceledInvoices { get; set; } = new List<CanceledInvoiceViewModel>();

        /// <summary>
        /// Execute get operation.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task OnGetAsync()
        {
            await this.LoadCanceledInvoicesAsync();
        }

        /// <summary>
        /// Looks up an invoice by number.
        /// </summary>
        /// <returns>The current page.</returns>
        public async Task<IActionResult> OnPostLookupAsync()
        {
            await this.LoadCanceledInvoicesAsync();

            string normalizedNumber = NormalizeInvoiceNumber(this.InvoiceNumber);
            if (string.IsNullOrEmpty(normalizedNumber))
            {
                this.ModelState.AddModelError(nameof(this.InvoiceNumber), this.localizer["InvoiceNumberRequired"]);
                return this.Page();
            }

            List<InvoiceLookupViewModel> matches = await this.FindInvoicesByNumberAsync(normalizedNumber);
            if (matches.Count == 0)
            {
                this.ErrorMessage = this.localizer["InvoiceNotFound", normalizedNumber];
                this.SelectedInvoice = null;
                return this.Page();
            }

            if (matches.Count > 1)
            {
                this.ErrorMessage = this.localizer["MultipleInvoicesFound", normalizedNumber];
                this.SelectedInvoice = null;
                return this.Page();
            }

            this.SelectedInvoice = matches[0];
            this.SelectedInvoiceId = this.SelectedInvoice.Id;
            this.InvoiceNumber = this.SelectedInvoice.Number;

            if (this.SelectedInvoice.IsCanceled)
            {
                this.ErrorMessage = this.localizer["InvoiceAlreadyCanceled", this.SelectedInvoice.Number];
            }

            return this.Page();
        }

        /// <summary>
        /// Cancels the selected invoice.
        /// </summary>
        /// <returns>The current page.</returns>
        public async Task<IActionResult> OnPostCancelAsync()
        {
            await this.LoadCanceledInvoicesAsync();

            string reason = NormalizeCancelReason(this.CancelReason);
            if (string.IsNullOrEmpty(reason))
            {
                this.ModelState.AddModelError(nameof(this.CancelReason), this.localizer["CancelReasonRequired"]);
            }

            if (!this.SelectedInvoiceId.HasValue)
            {
                this.ModelState.AddModelError(string.Empty, this.localizer["InvoiceNotSelected"]);
            }

            if (!this.ModelState.IsValid)
            {
                if (this.SelectedInvoiceId.HasValue)
                {
                    this.SelectedInvoice = await this.FindInvoiceByIdAsync(this.SelectedInvoiceId.Value);
                }

                return this.Page();
            }

            Invoice invoice = await this.dbContext.Invoices
                .FirstOrDefaultAsync(item => item.Id == this.SelectedInvoiceId.Value);

            if (invoice == null)
            {
                this.ErrorMessage = this.localizer["InvoiceNotFoundGeneric"];
                this.SelectedInvoice = null;
                return this.Page();
            }

            if (invoice.IsCanceled)
            {
                this.ErrorMessage = this.localizer["InvoiceAlreadyCanceled", invoice.Number];
                this.SelectedInvoice = await this.MapInvoiceLookupAsync(invoice);
                return this.Page();
            }

            invoice.IsCanceled = true;
            invoice.InternalMessage = AppendInternalMessage(invoice.InternalMessage, reason);
            await this.dbContext.SaveChangesAsync();

            this.StatusMessage = this.localizer["InvoiceCanceledSuccess", invoice.Number];
            this.InvoiceNumber = null;
            this.CancelReason = null;
            this.SelectedInvoiceId = null;
            this.SelectedInvoice = null;

            await this.LoadCanceledInvoicesAsync();
            return this.Page();
        }

        private static string NormalizeInvoiceNumber(string invoiceNumber)
        {
            if (string.IsNullOrWhiteSpace(invoiceNumber))
            {
                return null;
            }

            string trimmed = invoiceNumber.Trim();
            if (trimmed.Length > MaxInvoiceNumberLength)
            {
                trimmed = trimmed.Substring(0, MaxInvoiceNumberLength);
            }

            return trimmed;
        }

        private static string NormalizeCancelReason(string cancelReason)
        {
            if (string.IsNullOrWhiteSpace(cancelReason))
            {
                return null;
            }

            string trimmed = cancelReason.Trim();
            if (trimmed.Length > MaxCancelReasonLength)
            {
                trimmed = trimmed.Substring(0, MaxCancelReasonLength);
            }

            return trimmed;
        }

        private static string AppendInternalMessage(string existingMessage, string reason)
        {
            if (string.IsNullOrWhiteSpace(existingMessage))
            {
                return reason;
            }

            return existingMessage.TrimEnd() + Environment.NewLine + reason;
        }

        private async Task LoadCanceledInvoicesAsync()
        {
            this.CanceledInvoices = await this.dbContext.Invoices
                .AsNoTracking()
                .Where(invoice => invoice.IsCanceled)
                .OrderByDescending(invoice => invoice.Created)
                .Select(invoice => new CanceledInvoiceViewModel
                {
                    Id = invoice.Id,
                    Number = invoice.Number,
                    Created = invoice.Created,
                    InternalMessage = invoice.InternalMessage,
                    UserEmail = invoice.User.Email,
                })
                .ToListAsync();
        }

        private async Task<List<InvoiceLookupViewModel>> FindInvoicesByNumberAsync(string invoiceNumber)
        {
            return await this.dbContext.Invoices
                .AsNoTracking()
                .Where(invoice => invoice.Number == invoiceNumber)
                .Select(invoice => new InvoiceLookupViewModel
                {
                    Id = invoice.Id,
                    Number = invoice.Number,
                    Created = invoice.Created,
                    IsCanceled = invoice.IsCanceled,
                    InternalMessage = invoice.InternalMessage,
                    UserEmail = invoice.User.Email,
                })
                .ToListAsync();
        }

        private async Task<InvoiceLookupViewModel> FindInvoiceByIdAsync(int invoiceId)
        {
            return await this.dbContext.Invoices
                .AsNoTracking()
                .Where(invoice => invoice.Id == invoiceId)
                .Select(invoice => new InvoiceLookupViewModel
                {
                    Id = invoice.Id,
                    Number = invoice.Number,
                    Created = invoice.Created,
                    IsCanceled = invoice.IsCanceled,
                    InternalMessage = invoice.InternalMessage,
                    UserEmail = invoice.User.Email,
                })
                .FirstOrDefaultAsync();
        }

        private Task<InvoiceLookupViewModel> MapInvoiceLookupAsync(Invoice invoice)
        {
            return Task.FromResult(new InvoiceLookupViewModel
            {
                Id = invoice.Id,
                Number = invoice.Number,
                Created = invoice.Created,
                IsCanceled = invoice.IsCanceled,
                InternalMessage = invoice.InternalMessage,
                UserEmail = invoice.User?.Email,
            });
        }

        /// <summary>
        /// Invoice details shown before cancellation.
        /// </summary>
        public class InvoiceLookupViewModel
        {
            /// <summary>
            /// Gets or sets the invoice id.
            /// </summary>
            public int Id { get; set; }

            /// <summary>
            /// Gets or sets the invoice number.
            /// </summary>
            public string Number { get; set; }

            /// <summary>
            /// Gets or sets when the invoice was created.
            /// </summary>
            public DateTime Created { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether the invoice is canceled.
            /// </summary>
            public bool IsCanceled { get; set; }

            /// <summary>
            /// Gets or sets the internal message.
            /// </summary>
            public string InternalMessage { get; set; }

            /// <summary>
            /// Gets or sets the user email.
            /// </summary>
            public string UserEmail { get; set; }
        }

        /// <summary>
        /// Canceled invoice row for the list section.
        /// </summary>
        public class CanceledInvoiceViewModel
        {
            /// <summary>
            /// Gets or sets the invoice id.
            /// </summary>
            public int Id { get; set; }

            /// <summary>
            /// Gets or sets the invoice number.
            /// </summary>
            public string Number { get; set; }

            /// <summary>
            /// Gets or sets when the invoice was created.
            /// </summary>
            public DateTime Created { get; set; }

            /// <summary>
            /// Gets or sets the cancellation reason stored in internal message.
            /// </summary>
            public string InternalMessage { get; set; }

            /// <summary>
            /// Gets or sets the user email.
            /// </summary>
            public string UserEmail { get; set; }
        }
    }
}
