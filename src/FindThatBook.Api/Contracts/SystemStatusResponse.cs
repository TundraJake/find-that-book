namespace FindThatBook.Api.Contracts;

/// <summary>Response of <c>GET /api/status</c>. <c>Ready</c> is false when required configuration is missing.</summary>
public sealed record SystemStatusResponse(bool Ready, bool GeminiConfigured, IReadOnlyList<string> Problems);
