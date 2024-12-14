// -----------------------------------------------------------------------
// <copyright file="TenantHttpNonHttpContext.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Sas.Core.Http
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Claims;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Http.Features;

    /// <summary>
    /// Represent a non-HTTP context for the tenant.
    /// </summary>
    public class TenantHttpNonHttpContext : HttpContext
    {
        private InMemmoryFeatureCollection featureCollection = new InMemmoryFeatureCollection();

        /// <inheritdoc/>
        public override IFeatureCollection Features => this.featureCollection;

        /// <inheritdoc/>
        public override HttpRequest Request => throw new NotImplementedException();

        /// <inheritdoc/>
        public override HttpResponse Response => throw new NotImplementedException();

        /// <inheritdoc/>
        public override ConnectionInfo Connection => throw new NotImplementedException();

        /// <inheritdoc/>
        public override WebSocketManager WebSockets => throw new NotImplementedException();

        /// <inheritdoc/>
        public override ClaimsPrincipal User { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        /// <inheritdoc/>
        public override IDictionary<object, object?> Items { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        /// <inheritdoc/>
        public override IServiceProvider RequestServices { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        /// <inheritdoc/>
        public override CancellationToken RequestAborted { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        /// <inheritdoc/>
        public override string TraceIdentifier { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        /// <inheritdoc/>
        public override ISession Session { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        /// <inheritdoc/>
        public override void Abort()
        {
        }
    }
}
