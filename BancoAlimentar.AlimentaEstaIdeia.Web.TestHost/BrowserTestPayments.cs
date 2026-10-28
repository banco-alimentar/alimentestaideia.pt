// <copyright file="BrowserTestPayments.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>

namespace BancoAlimentar.AlimentaEstaIdeia.Web.TestHost
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// Loopback Easypay simulator used by the real SDK and production webhook verifier.
    /// Registered only by the test host, never by application startup.
    /// </summary>
    public sealed class BrowserTestPayments : IStartupFilter
    {
        private readonly ConcurrentDictionary<Guid, SimulatedPayment> payments = new ConcurrentDictionary<Guid, SimulatedPayment>();
        private int rejectedPayPalRequests;

        /// <summary>Gets how many attempted PayPal calls were blocked before any network request.</summary>
        public int RejectedPayPalRequests => Volatile.Read(ref this.rejectedPayPalRequests);

        /// <summary>Rejects unsupported PayPal operations at the transport boundary without network access.</summary>
        /// <returns>A failed local transport operation.</returns>
        public Task<PayPalHttp.HttpResponse> RejectPayPalAsync()
        {
            Interlocked.Increment(ref this.rejectedPayPalRequests);
            return Task.FromException<PayPalHttp.HttpResponse>(new InvalidOperationException("PayPal is disabled in the isolated browser host."));
        }

        /// <summary>Marks an existing simulated provider payment paid without changing application data.</summary>
        /// <param name="paymentId">Identifier returned by the simulated checkout.</param>
        public void MarkPaid(Guid paymentId)
        {
            if (!this.payments.TryGetValue(paymentId, out var payment))
            {
                throw new InvalidOperationException("Only payments created by this host's provider can be completed.");
            }

            this.payments[paymentId] = payment with { Paid = true };
        }

        /// <inheritdoc/>
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                app.Use(async (context, remaining) =>
                {
                    if (!context.Request.Path.StartsWithSegments("/2.0", out var providerPath))
                    {
                        await remaining();
                        return;
                    }

                    if (providerPath == "/single" && HttpMethods.IsPost(context.Request.Method))
                    {
                        using var document = await JsonDocument.ParseAsync(context.Request.Body);
                        var request = document.RootElement;
                        if (!request.TryGetProperty("key", out var key) || !Guid.TryParse(key.GetString(), out _)
                            || !request.TryGetProperty("value", out var value) || value.GetDouble() <= 0
                            || !request.TryGetProperty("method", out var method) || method.GetString() != "mbw")
                        {
                            context.Response.StatusCode = StatusCodes.Status400BadRequest;
                            return;
                        }

                        var payment = new SimulatedPayment(Guid.NewGuid(), key.GetString(), value.GetDouble(), false);
                        this.payments[payment.Id] = payment;
                        context.Response.StatusCode = StatusCodes.Status201Created;
                        await context.Response.WriteAsJsonAsync(new
                        {
                            status = "ok",
                            message = new[] { "Local simulated payment created" },
                            id = payment.Id,
                            method = new { type = "mbw", status = "pending", alias = "browser-test" },
                            customer = new { id = "browser-test-customer" },
                        });
                        return;
                    }

                    if (providerPath == "/single" && HttpMethods.IsGet(context.Request.Method))
                    {
                        await context.Response.WriteAsJsonAsync(new
                        {
                            data = this.payments.Values.Select(payment => payment.Response()).ToArray(),
                            meta = new { page = new { current = 1, total = 1 } },
                        });
                        return;
                    }

                    if (HttpMethods.IsGet(context.Request.Method)
                        && providerPath.StartsWithSegments("/single", out var suffix)
                        && Guid.TryParse(suffix.Value.TrimStart('/'), out Guid id)
                        && this.payments.TryGetValue(id, out var found))
                    {
                        await context.Response.WriteAsJsonAsync(found.Response());
                        return;
                    }

                    // Unknown provider operations fail locally; never proxy or fall back to a provider.
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                });
                next(app);
            };
        }

        private sealed record SimulatedPayment(Guid Id, string Key, double Amount, bool Paid)
        {
            public object Response() => new
            {
                id = this.Id,
                key = this.Key,
                value = this.Amount,
                method = new { type = "mbw", status = this.Paid ? "paid" : "pending" },
            };
        }
    }
}
