using System.Net;
using System.Text;

namespace FindThatBook.UnitTests.TestDoubles;

/// <summary>Answers requests by path (query string ignored); unknown paths return 404. Records every request URI.</summary>
internal sealed class RoutingHttpMessageHandler : HttpMessageHandler
{
    private readonly Dictionary<string, (HttpStatusCode StatusCode, string Body)> _routes = new(StringComparer.Ordinal);

    public List<Uri> RequestedUris { get; } = [];

    public RoutingHttpMessageHandler Route(string path, string body, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _routes[path] = (statusCode, body);
        return this;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Uri uri = request.RequestUri!;
        lock (RequestedUris)
        {
            RequestedUris.Add(uri);
        }

        (HttpStatusCode statusCode, string body) = _routes.TryGetValue(uri.AbsolutePath, out var route) ? route : (HttpStatusCode.NotFound, "{}");
        return Task.FromResult(new HttpResponseMessage(statusCode) { Content = new StringContent(body, Encoding.UTF8, "application/json") });
    }
}
