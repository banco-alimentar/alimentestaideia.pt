// -----------------------------------------------------------------------
// <copyright file="LoopbackOnlyMiddleware.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Hosting
{
    using System.Net;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// Rejects requests that do not come from the local machine. The tool has no login and shows donor data.
    /// </summary>
    public sealed class LoopbackOnlyMiddleware
    {
        private readonly RequestDelegate next;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoopbackOnlyMiddleware"/> class.
        /// </summary>
        /// <param name="next">Next middleware.</param>
        public LoopbackOnlyMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        /// <summary>
        /// Handles a request.
        /// </summary>
        /// <param name="context">HTTP context.</param>
        /// <returns>A task.</returns>
        public Task InvokeAsync(HttpContext context)
        {
            if (!IsLocal(context.Connection.RemoteIpAddress))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return context.Response.WriteAsync("CleanProdHarden only accepts requests from the local machine.");
            }

            return this.next(context);
        }

        /// <summary>
        /// Gets a value indicating whether an address is the local machine.
        /// </summary>
        /// <param name="address">Remote address. Null for in-process test servers.</param>
        /// <returns>True when local.</returns>
        internal static bool IsLocal(IPAddress? address)
        {
            if (address == null)
            {
                return true;
            }

            return IPAddress.IsLoopback(address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address);
        }
    }
}
