using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using AngleSharp.Html.Dom;
using Xunit;

namespace BancoAlimentar.AlimentaEstaIdeia.Web.IntegrationTests.Helpers
{
    public static class HttpClientExtensions
    {
        public static Task<HttpResponseMessage> SendAsync(
            this HttpClient client,
            IHtmlFormElement form,
            IHtmlElement submitButton)
        {
            return client.SendAsync(form, submitButton, new Dictionary<string, string>());
        }

        public static Task<HttpResponseMessage> SendAsync(
            this HttpClient client,
            IHtmlFormElement form,
            IEnumerable<KeyValuePair<string, string>> formValues)
        {
            var submitElement = Assert.Single(form.QuerySelectorAll("[type=submit]"));
            var submitButton = Assert.IsAssignableFrom<IHtmlElement>(submitElement);

            return client.SendAsync(form, submitButton, formValues);
        }

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
                }else if(control is IHtmlSelectElement)
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
                Content = new StreamContent(submit.Body)
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
