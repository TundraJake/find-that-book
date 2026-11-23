using System.Net;
using System.Text;

namespace FindThatBook.UnitTests.TestDoubles;

/// <summary>Answers each path with a queued sequence of responses (the last one repeats). Records every request path.</summary>
internal sealed class SequenceHttpMessageHandler : HttpMessageHandler
{
    private readonly Dictionary<string, Queue<(HttpStatusCode StatusCode, string Body)>> _sequences = new(StringComparer.Ordinal);

    public List<string> RequestedPaths { get; } = [];

    public SequenceHttpMessageHandler Then(string path, string body, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        if (!_sequences.TryGetValue(path, out var sequence))
        {
            sequence = new Queue<(HttpStatusCode, string)>();
            _sequences[path] = sequence;
        }

        sequence.Enqueue((statusCode, body));
        return this;
    }

    public int CountRequests(string path) => RequestedPaths.Count(requestedPath => requestedPath == path);

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string path = request.RequestUri!.AbsolutePath;
        RequestedPaths.Add(path);

        (HttpStatusCode statusCode, string body) = (HttpStatusCode.NotFound, "{}");
        if (_sequences.TryGetValue(path, out var sequence) && sequence.Count > 0)
        {
            (statusCode, body) = sequence.Count > 1 ? sequence.Dequeue() : sequence.Peek();
        }

        return Task.FromResult(new HttpResponseMessage(statusCode) { Content = new StringContent(body, Encoding.UTF8, "application/json") });
    }
}
