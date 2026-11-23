using System.Net;
using System.Text;

namespace FindThatBook.UnitTests.TestDoubles;

/// <summary>Returns a canned response and records the last request, so HTTP clients can be tested without a network.</summary>
internal sealed class StubHttpMessageHandler(HttpStatusCode statusCode, string responseBody) : HttpMessageHandler
{
    public HttpRequestMessage? LastRequest { get; private set; }

    public string? LastRequestBody { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        LastRequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        return new HttpResponseMessage(statusCode) { Content = new StringContent(responseBody, Encoding.UTF8, "application/json") };
    }
}
