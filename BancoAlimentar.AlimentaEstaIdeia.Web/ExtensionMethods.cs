// -----------------------------------------------------------------------
// <copyright file="ExtensionMethods.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web
{
    using System;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Http.Features;

    public static class ExtensionMethods
    {
        public static Uri GetRequestOriginalRawUri(this HttpRequest request)
        {
            IHttpRequestFeature requestFeature = request.HttpContext.Features.Get<IHttpRequestFeature>();
            Uri result = null;

            Uri.TryCreate(requestFeature.RawTarget, UriKind.Absolute, out result);

            return result;
        }
    }
}
