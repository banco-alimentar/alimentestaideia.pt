// -----------------------------------------------------------------------
// <copyright file="LoopbackOnlyMiddlewareTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Tests
{
    using System.Net;
    using System.Threading.Tasks;
    using BancoAlimentar.AlimentaEstaIdeia.Tools.CleanProdHarden.Hosting;
    using Microsoft.AspNetCore.Http;
    using Xunit;

    /// <summary>
    /// Tests that the tool only answers requests from the local machine.
    /// </summary>
    public class LoopbackOnlyMiddlewareTests
    {
        /// <summary>
        /// Loopback addresses, including IPv4-mapped loopback on dual-mode sockets, are local.
        /// </summary>
        [Theory]
        [InlineData("127.0.0.1", true)]
        [InlineData("::1", true)]
        [InlineData("::ffff:127.0.0.1", true)]
        [InlineData("192.168.1.20", false)]
        [InlineData("10.0.0.5", false)]
        [InlineData("::ffff:10.0.0.5", false)]
        [InlineData("fe80::1", false)]
        public void IsLocalAcceptsOnlyLoopback(string address, bool expected)
        {
            Assert.Equal(expected, LoopbackOnlyMiddleware.IsLocal(IPAddress.Parse(address)));
        }

        /// <summary>
        /// Remote requests get 403 and never reach the app.
        /// </summary>
        [Fact]
        public async Task RemoteRequestIsForbidden()
        {
            bool reachedApp = false;
            var middleware = new LoopbackOnlyMiddleware(_ =>
            {
                reachedApp = true;
                return Task.CompletedTask;
            });
            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = IPAddress.Parse("192.168.1.20");

            await middleware.InvokeAsync(context);

            Assert.False(reachedApp);
            Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        }

        /// <summary>
        /// Local requests reach the app.
        /// </summary>
        [Fact]
        public async Task LocalRequestReachesApp()
        {
            bool reachedApp = false;
            var middleware = new LoopbackOnlyMiddleware(_ =>
            {
                reachedApp = true;
                return Task.CompletedTask;
            });
            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = IPAddress.Loopback;

            await middleware.InvokeAsync(context);

            Assert.True(reachedApp);
        }
    }
}
