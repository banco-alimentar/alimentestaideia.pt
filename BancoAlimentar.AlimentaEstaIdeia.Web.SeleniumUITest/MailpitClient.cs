// <copyright file="MailpitClient.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>

namespace BancoAlimentar.AlimentaEstaIdeia.Web.SeleniumUITest
{
    using System.Diagnostics;
    using System.Net;
    using System.Text;
    using System.Text.Json;
    using System.Text.RegularExpressions;
    using Microsoft.AspNetCore.WebUtilities;

    /// <summary>Reads only email correlated to one unique local browser-test recipient.</summary>
    internal sealed class MailpitClient : IDisposable
    {
        private const string ApiFailure = "Mailpit API request failed; inspect the local catcher configuration.";
        private static readonly TimeSpan MaximumWait = TimeSpan.FromMinutes(2);
        private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);
        private readonly Uri catcher;
        private readonly string recipient;
        private readonly HttpClient http;
        private readonly bool ownsHttp;
        private readonly HashSet<string> captured = new HashSet<string>(StringComparer.Ordinal);

        public MailpitClient(Uri catcher, string recipient, HttpClient? httpClient = null)
        {
            ValidateLocalOrigin(catcher);
            if (!Regex.IsMatch(recipient, @"\Aselenium-[a-f0-9]{32}@example\.test\z", RegexOptions.IgnoreCase, RegexTimeout))
            {
                throw new InvalidOperationException("Mailpit requires a unique selenium GUID recipient at example.test.");
            }

            this.catcher = catcher;
            this.recipient = recipient;
            ownsHttp = httpClient == null;
            // Never follow a catcher redirect to an external host. Injection is for offline handlers only.
            http = httpClient ?? new HttpClient(new HttpClientHandler { AllowAutoRedirect = false, UseProxy = false });
        }

        public async Task<HashSet<string>> CaptureBaselineAsync(CancellationToken cancellationToken = default)
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(TimeSpan.FromSeconds(10));
            var messages = await SearchAsync(deadline.Token);
            return messages.Select(message => message.Id).ToHashSet(StringComparer.Ordinal);
        }

        public async Task<Uri> WaitForIdentityLinkAsync(
            ISet<string> baseline,
            DateTimeOffset requestedAt,
            Uri origin,
            string expectedPath,
            TimeSpan timeout,
            CancellationToken cancellationToken = default)
        {
            ValidateLocalOrigin(origin);
            using var deadline = CreateDeadline(timeout, cancellationToken);
            try
            {
                while (true)
                {
                    var messages = await FindNewAsync(baseline, requestedAt, deadline.Token);
                    foreach (var message in messages)
                    {
                        using var detail = await GetJsonAsync("/api/v1/message/" + Uri.EscapeDataString(message.Id), deadline.Token);
                        var root = detail.RootElement;
                        if (ReadString(root, "ID") != message.Id || !IsRecipient(root))
                        {
                            throw new InvalidOperationException("Mailpit message identity did not match the requested test email.");
                        }

                        return ExtractIdentityLink(ReadString(root, "HTML"), ReadString(root, "Text"), origin, expectedPath);
                    }

                    await Task.Delay(TimeSpan.FromMilliseconds(250), deadline.Token);
                }
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException("Timed out waiting for the expected test email.");
            }
        }

        public async Task<(string Subject, string Html, string Text)> WaitForMessageAsync(
            ISet<string> baseline,
            DateTimeOffset requestedAt,
            string expectedSubject,
            TimeSpan timeout,
            CancellationToken cancellationToken = default)
        {
            using var deadline = CreateDeadline(timeout, cancellationToken);
            try
            {
                while (true)
                {
                    foreach (var message in await FindNewAsync(baseline, requestedAt, deadline.Token))
                    {
                        using var detail = await GetJsonAsync("/api/v1/message/" + Uri.EscapeDataString(message.Id), deadline.Token);
                        var root = detail.RootElement;
                        if (ReadString(root, "ID") != message.Id || !IsRecipient(root))
                        {
                            throw new InvalidOperationException("Mailpit message identity did not match the requested test email.");
                        }

                        string subject = ReadString(root, "Subject");
                        if (subject == expectedSubject)
                        {
                            return (subject, ReadString(root, "HTML"), ReadString(root, "Text"));
                        }
                    }

                    await Task.Delay(TimeSpan.FromMilliseconds(250), deadline.Token);
                }
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException("Timed out waiting for the expected test email.");
            }
        }

        public async Task AssertNoMessageAsync(
            ISet<string> baseline,
            DateTimeOffset requestedAt,
            TimeSpan observationWindow,
            CancellationToken cancellationToken = default)
        {
            // Keep request cancellation distinct from a successfully completed observation window.
            ValidateTimeout(observationWindow);
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(observationWindow + TimeSpan.FromSeconds(10));
            var elapsed = Stopwatch.StartNew();
            do
            {
                if ((await FindNewAsync(baseline, requestedAt, deadline.Token)).Count != 0)
                {
                    throw new InvalidOperationException("Unexpected email for the unique test recipient.");
                }

                TimeSpan remaining = observationWindow - elapsed.Elapsed;
                if (remaining <= TimeSpan.Zero)
                {
                    break;
                }

                await Task.Delay(remaining < TimeSpan.FromMilliseconds(250) ? remaining : TimeSpan.FromMilliseconds(250), deadline.Token);
            }
            while (true);
        }

        public async Task CleanupAsync(CancellationToken cancellationToken = default)
        {
            if (captured.Count == 0)
            {
                return; // Mailpit interprets an empty IDs array as DELETE ALL.
            }

            string[] ids = captured.OrderBy(id => id, StringComparer.Ordinal).ToArray();
            using var request = new HttpRequestMessage(HttpMethod.Delete, new Uri(catcher, "/api/v1/messages"))
            {
                Content = new StringContent(JsonSerializer.Serialize(new { IDs = ids }), Encoding.UTF8, "application/json"),
            };
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(TimeSpan.FromSeconds(10));
            using var response = await SendAsync(request, deadline.Token);
            captured.ExceptWith(ids);
        }

        public void Dispose()
        {
            if (ownsHttp)
            {
                http.Dispose();
            }
        }

        internal static Uri ExtractIdentityLink(string html, string text, Uri origin, string expectedPath)
        {
            const string failure = "Email did not contain exactly one safe Identity link.";
            ValidateLocalOrigin(origin);
            if (expectedPath != "/Identity/Account/ResetPassword" && expectedPath != "/Identity/Account/ConfirmEmail")
            {
                throw new InvalidOperationException(failure);
            }

            try
            {
                // Parse attributes before decoding entities: encoded quotes must never become attribute delimiters.
                var candidates = Regex.Matches(html, @"<a\b[^>]*?\shref\s*=\s*(?:""(?<url>[^""]*)""|'(?<url>[^']*)'|(?<url>[^\s>]+))", RegexOptions.IgnoreCase, RegexTimeout)
                    .Select(match => WebUtility.HtmlDecode(match.Groups["url"].Value))
                    .Concat(Regex.Matches(text, @"https?://[^\s<>""']+", RegexOptions.IgnoreCase, RegexTimeout).Select(match => WebUtility.HtmlDecode(match.Value)))
                    .Distinct(StringComparer.Ordinal);
                var safe = new List<Uri>();
                foreach (string candidate in candidates)
                {
                    if (!Uri.TryCreate(candidate, UriKind.Absolute, out var link)
                        || !string.IsNullOrEmpty(link.UserInfo) || !string.IsNullOrEmpty(link.Fragment)
                        || !string.Equals(link.Scheme, origin.Scheme, StringComparison.OrdinalIgnoreCase)
                        || !string.Equals(link.IdnHost, origin.IdnHost, StringComparison.OrdinalIgnoreCase)
                        || link.Port != origin.Port || link.AbsolutePath != expectedPath)
                    {
                        continue;
                    }

                    var query = QueryHelpers.ParseQuery(link.Query);
                    if (!query.TryGetValue("code", out var codes) || codes.Count != 1
                        || !Regex.IsMatch(codes[0] ?? "", @"\A[A-Za-z0-9_-]+\z", RegexOptions.None, RegexTimeout))
                    {
                        continue;
                    }

                    byte[] bytes;
                    try
                    {
                        bytes = WebEncoders.Base64UrlDecode(codes[0]!);
                        if (string.IsNullOrWhiteSpace(new UTF8Encoding(false, true).GetString(bytes)))
                        {
                            continue;
                        }
                    }
                    catch (Exception exception) when (exception is FormatException || exception is DecoderFallbackException)
                    {
                        continue;
                    }

                    if (expectedPath.EndsWith("ConfirmEmail", StringComparison.Ordinal)
                        && (!query.TryGetValue("userId", out var userIds) || userIds.Count != 1 || string.IsNullOrWhiteSpace(userIds[0])))
                    {
                        continue;
                    }

                    safe.Add(link);
                }

                if (safe.Count == 1)
                {
                    return safe[0];
                }
            }
            catch (Exception exception) when (exception is ArgumentException || exception is RegexMatchTimeoutException)
            {
                // Neither parser errors nor exception chains may contain message text or a token.
            }

            throw new InvalidOperationException(failure);
        }

        private static void ValidateLocalOrigin(Uri origin)
        {
            if (!origin.IsAbsoluteUri || !origin.IsLoopback
                || (origin.Scheme != Uri.UriSchemeHttp && origin.Scheme != Uri.UriSchemeHttps)
                || origin.UserInfo.Length != 0 || origin.Query.Length != 0 || origin.Fragment.Length != 0
                || origin.AbsolutePath != "/")
            {
                throw new InvalidOperationException("Email tests require a loopback HTTP(S) origin without credentials, path, query, or fragment.");
            }
        }

        private static CancellationTokenSource CreateDeadline(TimeSpan timeout, CancellationToken cancellationToken)
        {
            ValidateTimeout(timeout);
            var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(timeout);
            return deadline;
        }

        private static void ValidateTimeout(TimeSpan timeout)
        {
            if (timeout <= TimeSpan.Zero || timeout > MaximumWait)
            {
                throw new InvalidOperationException("Mailpit polling requires a positive timeout of at most two minutes.");
            }
        }

        private async Task<List<MessageSummary>> FindNewAsync(ISet<string> baseline, DateTimeOffset requestedAt, CancellationToken cancellationToken)
        {
            var messages = (await SearchAsync(cancellationToken))
                .Where(message => !baseline.Contains(message.Id) && message.Created >= requestedAt).ToList();
            captured.UnionWith(messages.Select(message => message.Id));
            return messages;
        }

        private async Task<List<MessageSummary>> SearchAsync(CancellationToken cancellationToken)
        {
            var messages = new List<MessageSummary>();
            for (int start = 0; start < 10000; start += 100)
            {
                using var json = await GetJsonAsync("/api/v1/search?query=" + Uri.EscapeDataString("to:" + recipient) + "&start=" + start + "&limit=100", cancellationToken);
                var root = json.RootElement;
                if (root.ValueKind != JsonValueKind.Object
                    || !root.TryGetProperty("messages", out var rows) || rows.ValueKind != JsonValueKind.Array
                    || !root.TryGetProperty("messages_count", out var count) || count.ValueKind != JsonValueKind.Number
                    || !count.TryGetInt32(out int total) || total < 0)
                {
                    throw new InvalidOperationException("Mailpit returned an invalid response.");
                }

                foreach (var row in rows.EnumerateArray().Where(IsRecipient))
                {
                    string id = ReadString(row, "ID");
                    if (!Regex.IsMatch(id, @"\A[A-Za-z0-9_-]+\z", RegexOptions.None, RegexTimeout)
                        || id.Equals("latest", StringComparison.OrdinalIgnoreCase)
                        || !DateTimeOffset.TryParse(ReadString(row, "Created"), System.Globalization.CultureInfo.InvariantCulture,
                            System.Globalization.DateTimeStyles.RoundtripKind, out var created))
                    {
                        throw new InvalidOperationException("Mailpit returned an invalid response.");
                    }

                    messages.Add(new MessageSummary(id, created));
                }

                if (start + rows.GetArrayLength() >= total)
                {
                    return messages;
                }

                if (rows.GetArrayLength() == 0)
                {
                    break;
                }
            }

            throw new InvalidOperationException("Mailpit recipient search exceeded its bounded page limit.");
        }

        private bool IsRecipient(JsonElement message) =>
            message.ValueKind == JsonValueKind.Object && message.TryGetProperty("To", out var addresses) && addresses.ValueKind == JsonValueKind.Array
            && addresses.EnumerateArray().Any(address => ReadString(address, "Address").Equals(recipient, StringComparison.OrdinalIgnoreCase));

        private static string ReadString(JsonElement element, string property) =>
            element.ValueKind == JsonValueKind.Object && element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString() ?? "" : "";

        private async Task<JsonDocument> GetJsonAsync(string path, CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(catcher, path));
            using var response = await SendAsync(request, cancellationToken);
            try
            {
                using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            }
            catch (JsonException)
            {
                throw new InvalidOperationException("Mailpit returned an invalid response.");
            }
            catch (HttpRequestException)
            {
                throw new InvalidOperationException(ApiFailure);
            }
        }

        private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var response = await http.SendAsync(request, cancellationToken);
                Uri? actual = response.RequestMessage?.RequestUri;
                if (!response.IsSuccessStatusCode || (actual != null && actual != request.RequestUri))
                {
                    response.Dispose();
                    throw new InvalidOperationException(ApiFailure);
                }

                return response;
            }
            catch (HttpRequestException)
            {
                throw new InvalidOperationException(ApiFailure);
            }
        }

        private sealed record MessageSummary(string Id, DateTimeOffset Created);
    }
}
