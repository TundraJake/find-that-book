using System.Text.Json;

namespace FindThatBook.Infrastructure.OpenLibrary;

/// <summary>Classifies exceptions from Open Library calls.</summary>
internal static class OpenLibraryFailures
{
    /// <summary>Open Library, network, timeout, or parsing failures; a cancellation requested by the caller is not one.</summary>
    public static bool IsOpenLibraryFailure(Exception exception, CancellationToken cancellationToken) =>
        exception switch
        {
            OpenLibraryException or JsonException or HttpRequestException => true,
            TaskCanceledException => !cancellationToken.IsCancellationRequested,
            _ => false,
        };

    public static string DescribeReason(Exception exception) =>
        exception is OpenLibraryException openLibraryException ? openLibraryException.Reason : exception.Message;
}
