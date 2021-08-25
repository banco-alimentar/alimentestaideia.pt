// -----------------------------------------------------------------------
// <copyright file="HttpClientExtensions.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace BancoAlimentar.AlimentaEstaIdeia.Web.IntegrationTests.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;
    using AngleSharp.Html.Dom;
    using Xunit;

    /// <summary>
    /// Http Client extension methods.
    /// </summary>
    public static class HttpClientExtensions
    {
        /// <summary>
        /// Sends a html form.
        /// </summary>
        /// <param name="client">A reference the <see cref="HttpClient"/>.</param>
        /// <param name="form">The form to submit.</param>
        /// <param name="submitButton">The submit button.</param>
        /// <returns>The <see cref="HttpResponseMessage"/> object.</returns>
        public static Task<HttpResponseMessage> SendAsync(
            this HttpClient client,
            IHtmlFormElement form,
            IHtmlElement submitButton)
        {
            return client.SendAsync(form, submitButton, new Dictionary<string, string>());
        }

        /// <summary>
        /// Sends a html form.
        /// </summary>
        /// <param name="client">A reference the <see cref="HttpClient"/>.</param>
        /// <param name="form">The form to submit.</param>
        /// <param name="formValues">Forms values to submit.</param>
        /// <returns>The <see cref="HttpResponseMessage"/> object.</returns>
        public static Task<HttpResponseMessage> SendAsync(
            this HttpClient client,
            IHtmlFormElement form,
            IEnumerable<KeyValuePair<string, string>> formValues)
        {
            var submitElement = Assert.Single(form.QuerySelectorAll("[type=submit]"));
            var submitButton = Assert.IsAssignableFrom<IHtmlElement>(submitElement);

            return client.SendAsync(form, submitButton, formValues);
        }

        /// <summary>
        /// Sends a html form.
        /// </summary>
        /// <param name="client">A reference the <see cref="HttpClient"/>.</param>
        /// <param name="form">The form to submit.</param>
        /// <param name="submitButton">The submit button.</param>
        /// <param name="formValues">Forms values to submit.</param>
        /// <returns>The <see cref="HttpResponseMessage"/> object.</returns>
        public static Task<HttpResponseMessage> SendAsync(
            this HttpClient client,
            IHtmlFormElement form,
            IHtmlElement submitButton,
            IEnumerable<KeyValuePair<string, string>> formValues)
        {
            foreach (var kvp in formValues)
            {
                var control = form[kvp.Key];
                if (control is IHtmlInputElement element1 && element1.Type == "checkbox")
                {
                    var element = Assert.IsAssignableFrom<IHtmlInputElement>(control);
                    element.IsChecked = bool.Parse(kvp.Value);
                }
                else if (control is IHtmlInputElement)
                {
                    var element = Assert.IsAssignableFrom<IHtmlInputElement>(control);
                    element.Value = kvp.Value;
                }
                else if (control is IHtmlSelectElement)
                {
                    var element = Assert.IsAssignableFrom<IHtmlSelectElement>(control);
                    element.Value = kvp.Value;
                }
                else
                {
                    throw new InvalidOperationException($"Parsing failed for {control.TextContent}");
                }
            }

            var submit = form.GetSubmission(submitButton);
            var target = (Uri)submit.Target;
            if (submitButton.HasAttribute("formaction"))
            {
                var formaction = submitButton.GetAttribute("formaction");
                target = new Uri(formaction, UriKind.Relative);
            }

            var submission = new HttpRequestMessage(new HttpMethod(submit.Method.ToString()), target)
            {
                Content = new StreamContent(submit.Body),
            };

            foreach (var header in submit.Headers)
            {
                submission.Headers.TryAddWithoutValidation(header.Key, header.Value);
                submission.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            return client.SendAsync(submission);
        }
    }
}
