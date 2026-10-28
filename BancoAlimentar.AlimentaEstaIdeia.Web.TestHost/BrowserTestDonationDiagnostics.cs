// <copyright file="BrowserTestDonationDiagnostics.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>

namespace BancoAlimentar.AlimentaEstaIdeia.Web.TestHost
{
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc.Filters;

    /// <summary>Records only invalid donation field names, never submitted values or security links.</summary>
    internal sealed class BrowserTestDonationDiagnostics : IAsyncPageFilter
    {
        public string Fields { get; private set; } = string.Empty;

        public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context) => Task.CompletedTask;

        public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
        {
            await next();
            if (context.HttpContext.Request.Path == "/Donation")
            {
                this.Fields = string.Join(", ", context.ModelState.Where(pair => pair.Value.Errors.Count > 0).Select(pair => pair.Key));
            }
        }
    }
}
