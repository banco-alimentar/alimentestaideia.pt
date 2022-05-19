// -----------------------------------------------------------------------
// <copyright file="DonationTelemetryMiddleware.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.Telemetry;

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

/// <summary>
/// Telemetry middleware.
/// </summary>
public class DonationTelemetryMiddleware
{
    private readonly RequestDelegate next;

    /// <summary>
    /// Initializes a new instance of the <see cref="DonationTelemetryMiddleware"/> class.
    /// </summary>
    /// <param name="next">Next request delegate.</param>
    public DonationTelemetryMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    /// <summary>
    /// Invoke the middleware.
    /// </summary>
    /// <param name="httpContext">Http Context.</param>
    /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
    public Task Invoke(HttpContext httpContext)
    {
        string donationId = httpContext.Session.GetString(KeyNames.DonationSessionKey);
        if (!string.IsNullOrEmpty(donationId))
        {
            httpContext.Items.Add(KeyNames.DonationSessionKey, new Guid(donationId));
        }

        if (httpContext.Session.IsAvailable)
        {
            httpContext.Items.Add(KeyNames.SessionIdKey, httpContext.Session.Id);
        }

        httpContext.Response.Headers.Add("Request-Id", Activity.Current.RootId);

        return next(httpContext);
    }
}
