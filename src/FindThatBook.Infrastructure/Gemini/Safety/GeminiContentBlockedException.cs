using FindThatBook.Application;

namespace FindThatBook.Infrastructure.Gemini.Safety;

/// <summary>Gemini blocked the prompt or response, or our output checks found unsafe content. Surfaces as <see cref="SearchRejectedException"/>.</summary>
internal sealed class GeminiContentBlockedException(string reason) : Exception($"Gemini content blocked: {reason}")
{
    public string Reason { get; } = reason;

    public SearchRejectedException ToApplicationException() => new(Reason, this);
}
