namespace FindThatBook.UnitTests.TestDoubles;

/// <summary>
/// Wraps <see cref="RoutingHttpMessageHandler"/> and makes matching requests fail the way an <c>HttpClient.Timeout</c> does:
/// a <see cref="TaskCanceledException"/> with an inner <see cref="TimeoutException"/>, while the caller's token stays uncancelled.
/// </summary>
internal sealed class TimeoutInjectingHandler(RoutingHttpMessageHandler routes, Func<Uri, bool> timesOut) : DelegatingHandler(routes)
{
    public RoutingHttpMessageHandler Routes { get; } = routes;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (timesOut(request.RequestUri!))
        {
            lock (Routes.RequestedUris)
            {
                Routes.RequestedUris.Add(request.RequestUri!);
            }

            throw new TaskCanceledException("The request was canceled due to the configured HttpClient.Timeout.", new TimeoutException());
        }

        return base.SendAsync(request, cancellationToken);
    }
}
