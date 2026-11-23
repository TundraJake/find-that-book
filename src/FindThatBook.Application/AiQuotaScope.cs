namespace FindThatBook.Application;

/// <summary>Which provider quota ran out.</summary>
public enum AiQuotaScope
{
    /// <summary>Requests-per-minute (or tokens-per-minute): clears within about a minute.</summary>
    PerMinute,

    /// <summary>Requests-per-day: clears at the provider's daily reset (midnight Pacific for Gemini).</summary>
    Daily,
}
