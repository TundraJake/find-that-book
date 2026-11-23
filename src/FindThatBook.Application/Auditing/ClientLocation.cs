namespace FindThatBook.Application.Auditing;

/// <summary>Approximate visitor location derived from the client IP. Unknown parts are <see cref="NotAvailable"/>, never null.</summary>
public sealed record ClientLocation(string Country, string Region, string City, string Timezone, string Source)
{
    public const string NotAvailable = "Not available";
    public const string NoSource = "none";

    public static ClientLocation Unknown { get; } = new(NotAvailable, NotAvailable, NotAvailable, NotAvailable, NoSource);
}
