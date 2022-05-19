// -----------------------------------------------------------------------
// <copyright file="HtmlHelpers.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Testing.Common;

using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Html.Dom;
using AngleSharp.Io;

/// <summary>
/// Class for html helpers.
/// </summary>
public class HtmlHelpers
{
    /// <summary>
    /// Gets the <see cref="IHtmlDocument"/> from the <see cref="HttpResponseMessage"/> object.
    /// </summary>
    /// <param name="response">A reference to the <see cref="HttpResponseMessage"/> object.</param>
    /// <returns>A reference to the <see cref="IHtmlDocument"/>.</returns>
    public static async Task<IHtmlDocument> GetDocumentAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        var document = await BrowsingContext.New()
            .OpenAsync(ResponseFactory, CancellationToken.None);
        return (IHtmlDocument)document;

        void ResponseFactory(VirtualResponse htmlResponse)
        {
            htmlResponse
                .Address(response.RequestMessage.RequestUri)
                .Status(response.StatusCode);

            MapHeaders(response.Headers);
            MapHeaders(response.Content.Headers);

            htmlResponse.Content(content);

            void MapHeaders(HttpHeaders headers)
            {
                foreach (var header in headers)
                {
                    foreach (var value in header.Value)
                    {
                        htmlResponse.Header(header.Key, value);
                    }
                }
            }
        }
    }
}
