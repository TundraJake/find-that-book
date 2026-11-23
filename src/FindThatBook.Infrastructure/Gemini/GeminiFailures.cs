using System.Text.Json;

namespace FindThatBook.Infrastructure.Gemini;

/// <summary>Classifies exceptions from a Gemini call.</summary>
internal static class GeminiFailures
{
    /// <summary>Gemini, network, timeout, or parsing failures; a cancellation requested by the caller is not one.</summary>
    public static bool IsGeminiFailure(Exception exception, CancellationToken cancellationToken) =>
        exception switch
        {
            GeminiException or JsonException or HttpRequestException => true,
            TaskCanceledException => !cancellationToken.IsCancellationRequested,
            _ => false,
        };

    public static string DescribeReason(Exception exception) =>
        exception is GeminiException geminiException ? geminiException.Reason : exception.Message;
}
