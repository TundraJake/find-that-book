namespace FindThatBook.Api;

/// <summary>Source-generated log messages emitted while the app starts.</summary>
internal static partial class StartupLog
{
    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Gemini API key is not configured; searches will be rejected. Set it with: dotnet user-secrets set \"Gemini:ApiKey\" \"<key>\" --project src/FindThatBook.Api")]
    public static partial void GeminiNotConfigured(ILogger logger);
}
