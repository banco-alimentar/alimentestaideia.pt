// <copyright file="MailpitClientTests.cs" company="Federação Portuguesa dos Bancos Alimentares Contra a Fome">
// Copyright (c) Federação Portuguesa dos Bancos Alimentares Contra a Fome. All rights reserved.
// </copyright>

namespace BancoAlimentar.AlimentaEstaIdeia.Web.SeleniumUITest
{
    using System.Net;
    using System.Text;
    using System.Text.Json;
    using Xunit;

    [Trait("Category", "Offline")]
    public sealed class MailpitClientTests
    {
        private const string Recipient = "selenium-11111111111111111111111111111111@example.test";
        private const string ResetPath = "/Identity/Account/ResetPassword";
        private static readonly Uri Catcher = new Uri("http://127.0.0.1:8025");
        private static readonly Uri Origin = new Uri("http://localhost:5080");
        private static readonly DateTimeOffset Started = DateTimeOffset.UtcNow.AddSeconds(-1);

        [Theory]
        [InlineData("https://mailpit.example.test")]
        [InlineData("ftp://localhost")]
        [InlineData("http://user:secret@localhost:8025")]
        [InlineData("http://localhost:8025/?query=secret")]
        [InlineData("http://localhost:8025/#fragment")]
        [InlineData("http://localhost:8025/api")]
        [InlineData("http://localhost.evil.test:8025")]
        public void Constructor_RejectsUnsafeCatcher(string address)
        {
            Assert.Throws<InvalidOperationException>(() => new MailpitClient(new Uri(address), Recipient));
        }

        [Theory]
        [InlineData("ordinary@example.test")]
        [InlineData("selenium-not-a-guid@example.test")]
        [InlineData("selenium-11111111111111111111111111111111@example.com")]
        public void Constructor_RequiresUniqueTestRecipient(string recipient)
        {
            Assert.Throws<InvalidOperationException>(() => new MailpitClient(Catcher, recipient));
        }

        [Theory]
        [InlineData("http://127.0.0.1:8025")]
        [InlineData("https://localhost:8025/")]
        [InlineData("http://[::1]:8025")]
        public void Constructor_AcceptsLoopbackCatcher(string address)
        {
            using var client = new MailpitClient(new Uri(address), Recipient);
        }

        [Theory]
        [InlineData("<a href='http://localhost:5080/Identity/Account/ResetPassword?code=dGVzdA&amp;returnUrl=%2F'>Reset</a>", "")]
        [InlineData("<a HREF=\"http://localhost:5080/Identity/Account/ResetPassword?code=dGVzdA&#38;returnUrl=%2F\">Reset</a>", "")]
        [InlineData("", "Reset: http://localhost:5080/Identity/Account/ResetPassword?code=dGVzdA")]
        public void ExtractIdentityLink_ReadsEscapedHtmlAndPlainText(string html, string text)
        {
            Uri link = MailpitClient.ExtractIdentityLink(html, text, Origin, ResetPath);
            Assert.True(link.GetLeftPart(UriPartial.Authority) == Origin.GetLeftPart(UriPartial.Authority));
            Assert.True(link.AbsolutePath == ResetPath);
            if (!string.IsNullOrEmpty(html))
            {
                Assert.True(Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(link.Query)["returnUrl"] == "/");
            }
        }

        [Theory]
        // Short scenario labels keep Azure test names distinct when xUnit truncates long URLs.
        [InlineData("lookalike-host", "http://localhost.evil.test:5080/Identity/Account/ResetPassword?code=dGVzdA")]
        [InlineData("different-host", "http://127.0.0.1:5080/Identity/Account/ResetPassword?code=dGVzdA")]
        [InlineData("different-port", "http://localhost:5081/Identity/Account/ResetPassword?code=dGVzdA")]
        [InlineData("different-scheme", "https://localhost:5080/Identity/Account/ResetPassword?code=dGVzdA")]
        [InlineData("userinfo", "http://user:secret@localhost:5080/Identity/Account/ResetPassword?code=dGVzdA")]
        [InlineData("wrong-route", "http://localhost:5080/Identity/Account/ConfirmEmail?code=dGVzdA&userId=test")]
        [InlineData("extra-path", "http://localhost:5080/Identity/Account/ResetPassword/extra?code=dGVzdA")]
        [InlineData("missing-token", "http://localhost:5080/Identity/Account/ResetPassword")]
        [InlineData("empty-token", "http://localhost:5080/Identity/Account/ResetPassword?code=")]
        [InlineData("invalid-base64", "http://localhost:5080/Identity/Account/ResetPassword?code=%25%25%25")]
        [InlineData("short-base64", "http://localhost:5080/Identity/Account/ResetPassword?code=a")]
        [InlineData("duplicate-token", "http://localhost:5080/Identity/Account/ResetPassword?code=dGVzdA&code=b3RoZXI")]
        [InlineData("invalid-utf8", "http://localhost:5080/Identity/Account/ResetPassword?code=_w")]
        [InlineData("fragment", "http://localhost:5080/Identity/Account/ResetPassword?code=dGVzdA#fragment")]
        [InlineData("script-url", "javascript:alert('secret')")]
        public void ExtractIdentityLink_RejectsUnsafeOrMalformedLinksWithoutDisclosingThem(string scenario, string link)
        {
            var error = Assert.Throws<InvalidOperationException>(() =>
                MailpitClient.ExtractIdentityLink("<a href=\"" + link + "\">Reset</a>", "", Origin, ResetPath));
            Assert.True(error.Message == "Email did not contain exactly one safe Identity link.",
                scenario + ": unsafe links must be rejected without disclosing their content.");
            Assert.Null(error.InnerException);
        }

        [Fact]
        public void ExtractIdentityLink_RequiresConfirmationUserIdAndRejectsAmbiguity()
        {
            const string path = "/Identity/Account/ConfirmEmail";
            Assert.Throws<InvalidOperationException>(() => MailpitClient.ExtractIdentityLink(
                "", Origin + "Identity/Account/ConfirmEmail?code=dGVzdA", Origin, path));
            Uri confirmed = MailpitClient.ExtractIdentityLink("",
                Origin + "Identity/Account/ConfirmEmail?code=dGVzdA&userId=owned", Origin, path);
            Assert.True(confirmed.AbsolutePath == path);
            Assert.Throws<InvalidOperationException>(() => MailpitClient.ExtractIdentityLink("",
                Origin + "Identity/Account/ResetPassword?code=dGVzdA " +
                Origin + "Identity/Account/ResetPassword?code=b3RoZXI", Origin, ResetPath));
        }

        [Fact]
        public void ExtractIdentityLink_DoesNotTreatDataHrefOrEncodedQuotesAsAttributes()
        {
            Assert.Throws<InvalidOperationException>(() => MailpitClient.ExtractIdentityLink(
                "<a data-href='http://localhost:5080/Identity/Account/ResetPassword?code=dGVzdA' href='https://example.test/'>Reset</a>",
                "", Origin, ResetPath));
            Assert.Throws<InvalidOperationException>(() => MailpitClient.ExtractIdentityLink(
                "<a href='https://example.test/&#39; href=&#39;http://localhost:5080/Identity/Account/ResetPassword?code=dGVzdA'>Reset</a>",
                "", Origin, ResetPath));
        }

        [Fact]
        public async Task Poll_RejectsMessageDetailWithDifferentRecipient()
        {
            using var http = new HttpClient(new StubHandler(request => request.RequestUri!.AbsolutePath == "/api/v1/search"
                ? Summary(Message("owned", Recipient, Started.AddSeconds(1)))
                : Detail("owned", "other@example.test")));
            using var client = new MailpitClient(Catcher, Recipient, http);
            var error = await Assert.ThrowsAsync<InvalidOperationException>(() => client.WaitForIdentityLinkAsync(
                new HashSet<string>(), Started, Origin, ResetPath, TimeSpan.FromSeconds(1)));
            Assert.Equal("Mailpit message identity did not match the requested test email.", error.Message);
        }

        [Fact]
        public async Task Poll_KeepsPollingUntilNewMessageArrives()
        {
            int polls = 0;
            using var http = new HttpClient(new StubHandler(request => request.RequestUri!.AbsolutePath == "/api/v1/search"
                ? (++polls == 1 ? Summary() : Summary(Message("owned", Recipient, Started.AddSeconds(1))))
                : Detail("owned", Recipient)));
            using var client = new MailpitClient(Catcher, Recipient, http);
            Uri link = await client.WaitForIdentityLinkAsync(new HashSet<string>(), Started, Origin, ResetPath, TimeSpan.FromSeconds(3));
            Assert.True(link.AbsolutePath == ResetPath);
            Assert.Equal(2, polls);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(121000)]
        public async Task Poll_RejectsInvalidWaitBounds(int milliseconds)
        {
            using var http = new HttpClient(new StubHandler(_ => throw new InvalidOperationException("Unexpected HTTP call.")));
            using var client = new MailpitClient(Catcher, Recipient, http);
            await Assert.ThrowsAsync<InvalidOperationException>(() => client.WaitForIdentityLinkAsync(
                new HashSet<string>(), Started, Origin, ResetPath, TimeSpan.FromMilliseconds(milliseconds)));
            await Assert.ThrowsAsync<InvalidOperationException>(() => client.AssertNoMessageAsync(
                new HashSet<string>(), Started, TimeSpan.FromMilliseconds(milliseconds)));
        }

        [Theory]
        [InlineData("null")]
        [InlineData("[]")]
        [InlineData("{}")]
        public async Task Api_RejectsInvalidResponseShape(string response)
        {
            using var http = new HttpClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(response),
            }));
            using var client = new MailpitClient(Catcher, Recipient, http);
            var error = await Assert.ThrowsAsync<InvalidOperationException>(() => client.CaptureBaselineAsync());
            Assert.Equal("Mailpit returned an invalid response.", error.Message);
        }

        [Fact]
        public async Task Poll_FiltersRecipientBaselineAndRequestTimeBeforeReadingDetails()
        {
            var details = new List<string>();
            using var http = new HttpClient(new StubHandler(request =>
            {
                if (request.RequestUri!.AbsolutePath == "/api/v1/search")
                {
                    Assert.Contains(Uri.EscapeDataString("to:" + Recipient), request.RequestUri.Query);
                    return Summary(
                        Message("other", "other@example.test", Started.AddSeconds(1)),
                        Message("baseline", Recipient, Started.AddSeconds(1)),
                        Message("old", Recipient, Started.AddMinutes(-1)),
                        Message("owned", Recipient.ToUpperInvariant(), Started.AddSeconds(1)));
                }

                details.Add(request.RequestUri.AbsolutePath);
                return Detail("owned", Recipient);
            }));
            using var client = new MailpitClient(Catcher, Recipient, http);
            Uri link = await client.WaitForIdentityLinkAsync(new HashSet<string> { "baseline" }, Started,
                Origin, ResetPath, TimeSpan.FromSeconds(1));
            Assert.True(link.AbsolutePath == ResetPath);
            Assert.Equal(new[] { "/api/v1/message/owned" }, details);
        }

        [Fact]
        public async Task Baseline_PaginatesAndRetainsOnlyExactRecipients()
        {
            int requests = 0;
            using var http = new HttpClient(new StubHandler(request =>
            {
                requests++;
                bool secondPage = request.RequestUri!.Query.Contains("start=100");
                object[] rows = secondPage
                    ? new[] { Message("last", Recipient, Started) }
                    : Enumerable.Range(0, 100).Select(i => Message("other" + i, "other@example.test", Started)).ToArray();
                return Json(new { messages = rows, messages_count = 101 });
            }));
            using var client = new MailpitClient(Catcher, Recipient, http);
            var baseline = await client.CaptureBaselineAsync();
            Assert.Equal(new[] { "last" }, baseline);
            Assert.Equal(2, requests);
        }

        [Fact]
        public async Task Cleanup_DeletesOnlyCapturedIdsAndNeverSendsEmptyDelete()
        {
            var deleted = new List<string>();
            using var http = new HttpClient(new StubHandler(async (request, cancellation) =>
            {
                if (request.Method == HttpMethod.Delete)
                {
                    Assert.Equal("/api/v1/messages", request.RequestUri!.AbsolutePath);
                    using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellation));
                    deleted.AddRange(body.RootElement.GetProperty("IDs").EnumerateArray().Select(id => id.GetString()!));
                    return new HttpResponseMessage(HttpStatusCode.OK);
                }

                return request.RequestUri!.AbsolutePath == "/api/v1/search"
                    ? Summary(Message("baseline", Recipient, Started.AddSeconds(1)), Message("owned", Recipient, Started.AddSeconds(1)), Message("other", "other@example.test", Started))
                    : Detail("owned", Recipient);
            }));
            using var client = new MailpitClient(Catcher, Recipient, http);
            await client.CleanupAsync();
            Assert.Empty(deleted);
            await client.WaitForIdentityLinkAsync(new HashSet<string> { "baseline" }, Started, Origin, ResetPath, TimeSpan.FromSeconds(1));
            await client.CleanupAsync();
            await client.CleanupAsync();
            Assert.Equal(new[] { "owned" }, deleted);
        }

        [Fact]
        public async Task NoMessage_ObservesWholeWindowAndIgnoresOtherRecipients()
        {
            int polls = 0;
            using var http = new HttpClient(new StubHandler(_ =>
            {
                polls++;
                return Summary(Message("other", "other@example.test", Started));
            }));
            using var client = new MailpitClient(Catcher, Recipient, http);
            var elapsed = System.Diagnostics.Stopwatch.StartNew();
            await client.AssertNoMessageAsync(new HashSet<string>(), Started, TimeSpan.FromMilliseconds(60));
            Assert.True(polls >= 1 && elapsed.Elapsed >= TimeSpan.FromMilliseconds(60),
                "No-email observation must last for the whole bounded window.");
        }

        [Fact]
        public async Task NoMessage_FailsForOwnedMessageWithoutReadingBody()
        {
            using var http = new HttpClient(new StubHandler(request =>
            {
                Assert.Equal("/api/v1/search", request.RequestUri!.AbsolutePath);
                return Summary(Message("owned", Recipient, Started.AddSeconds(1)));
            }));
            using var client = new MailpitClient(Catcher, Recipient, http);
            var error = await Assert.ThrowsAsync<InvalidOperationException>(() => client.AssertNoMessageAsync(
                new HashSet<string>(), Started, TimeSpan.FromSeconds(1)));
            Assert.Equal("Unexpected email for the unique test recipient.", error.Message);
        }

        [Fact]
        public async Task Poll_TimesOutAndHonorsCancellation()
        {
            using var http = new HttpClient(new StubHandler(_ => Summary()));
            using var client = new MailpitClient(Catcher, Recipient, http);
            var error = await Assert.ThrowsAsync<TimeoutException>(() => client.WaitForIdentityLinkAsync(
                new HashSet<string>(), Started, Origin, ResetPath, TimeSpan.FromMilliseconds(30)));
            Assert.Equal("Timed out waiting for the expected test email.", error.Message);
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.CaptureBaselineAsync(cancellation.Token));
        }

        [Fact]
        public async Task Poll_CancelsHangingHttpWithinItsDeadline()
        {
            using var http = new HttpClient(new StubHandler(async (_, cancellation) =>
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellation);
                return Summary();
            }));
            using var client = new MailpitClient(Catcher, Recipient, http);
            await Assert.ThrowsAsync<TimeoutException>(() => client.WaitForIdentityLinkAsync(
                new HashSet<string>(), Started, Origin, ResetPath, TimeSpan.FromMilliseconds(30)));
        }

        [Theory]
        [InlineData(302)]
        [InlineData(500)]
        public async Task Api_RejectsRedirectsAndErrorsWithoutEchoingResponse(int status)
        {
            using var http = new HttpClient(new StubHandler(_ => new HttpResponseMessage((HttpStatusCode)status)
            {
                Content = new StringContent("secret message body"),
                Headers = { Location = new Uri("https://example.test/secret") },
            }));
            using var client = new MailpitClient(Catcher, Recipient, http);
            var error = await Assert.ThrowsAsync<InvalidOperationException>(() => client.CaptureBaselineAsync());
            Assert.Equal("Mailpit API request failed; inspect the local catcher configuration.", error.Message);
            Assert.Null(error.InnerException);
        }

        [Fact]
        public async Task Api_SanitizesMalformedJsonAndTransportExceptions()
        {
            using var malformed = new HttpClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("secret malformed message"),
            }));
            using var malformedClient = new MailpitClient(Catcher, Recipient, malformed);
            var jsonError = await Assert.ThrowsAsync<InvalidOperationException>(() => malformedClient.CaptureBaselineAsync());
            Assert.Equal("Mailpit returned an invalid response.", jsonError.Message);
            Assert.Null(jsonError.InnerException);
            using var broken = new HttpClient(new StubHandler(_ => throw new HttpRequestException("secret reset URL")));
            using var brokenClient = new MailpitClient(Catcher, Recipient, broken);
            var transportError = await Assert.ThrowsAsync<InvalidOperationException>(() => brokenClient.CaptureBaselineAsync());
            Assert.Equal("Mailpit API request failed; inspect the local catcher configuration.", transportError.Message);
            Assert.Null(transportError.InnerException);
        }

        private static object Message(string id, string recipient, DateTimeOffset created) =>
            new { ID = id, To = new[] { new { Address = recipient } }, Created = created };

        [Fact]
        public async Task MessagePoll_ReadsOnlyNewRecipientMessageWithExpectedSubject()
        {
            int polls = 0;
            using var http = new HttpClient(new StubHandler(request => request.RequestUri!.AbsolutePath == "/api/v1/search"
                ? Summary(Message("old", Recipient, Started.AddSeconds(1)), Message(++polls == 1 ? "other-subject" : "new", Recipient, Started.AddSeconds(1)))
                : Json(new
                {
                    ID = request.RequestUri!.AbsolutePath.Split('/').Last(),
                    To = new[] { new { Address = Recipient } },
                    Subject = polls == 1 ? "Different email" : "Payment confirmed",
                    HTML = "<p>Campanha</p>",
                    Text = "Campanha",
                })));
            using var client = new MailpitClient(Catcher, Recipient, http);
            var message = await client.WaitForMessageAsync(new HashSet<string> { "old" }, Started,
                "Payment confirmed", TimeSpan.FromSeconds(2));
            Assert.Equal("Payment confirmed", message.Subject);
            Assert.Equal("<p>Campanha</p>", message.Html);
            Assert.Equal("Campanha", message.Text);
            Assert.True(polls >= 2);
        }

        [Theory]
        [InlineData("different-id", Recipient)]
        [InlineData("owned", "different@example.test")]
        public async Task MessagePoll_RejectsUncorrelatedDetail(string id, string recipient)
        {
            using var http = new HttpClient(new StubHandler(request => request.RequestUri!.AbsolutePath == "/api/v1/search"
                ? Summary(Message("owned", Recipient, Started.AddSeconds(1)))
                : Detail(id, recipient)));
            using var client = new MailpitClient(Catcher, Recipient, http);
            var error = await Assert.ThrowsAsync<InvalidOperationException>(() => client.WaitForMessageAsync(
                new HashSet<string>(), Started, "Payment confirmed", TimeSpan.FromSeconds(1)));
            Assert.Equal("Mailpit message identity did not match the requested test email.", error.Message);
        }

        private static HttpResponseMessage Summary(params object[] messages) =>
            Json(new { messages, messages_count = messages.Length });

        private static HttpResponseMessage Detail(string id, string recipient) => Json(new
        {
            ID = id,
            To = new[] { new { Address = recipient } },
            HTML = "<a href='http://localhost:5080/Identity/Account/ResetPassword?code=dGVzdA'>Reset</a>",
            Text = "",
        });

        private static HttpResponseMessage Json(object value) => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json"),
        };

        private sealed class StubHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond;

            public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
                : this((request, _) => Task.FromResult(respond(request)))
            {
            }

            public StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond)
            {
                this.respond = respond;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
                respond(request, cancellationToken);
        }
    }
}
