using System.Net;
using FindThatBook.Application.Auditing;
using FindThatBook.Infrastructure.Auditing;
using Microsoft.Extensions.Options;

namespace FindThatBook.Api.Auditing;

/// <summary>
/// Works out who is calling: client IP and approximate location. Location comes only from Cloudflare's visitor location headers
/// (no third-party lookups); anything missing is "Not available". Proxy headers are read only when
/// <see cref="SearchAuditOptions.TrustProxyHeaders"/> is on, because a client can forge them.
/// </summary>
public sealed class ClientContextResolver(IOptions<SearchAuditOptions> options)
{
    internal const string CloudflareConnectingIpHeader = "cf-connecting-ip";
    internal const string DigitalOceanConnectingIpHeader = "do-connecting-ip";
    internal const string ForwardedForHeader = "X-Forwarded-For";
    internal const string CountryHeader = "cf-ipcountry";
    internal const string RegionHeader = "cf-region";
    internal const string RegionCodeHeader = "cf-region-code";
    internal const string CityHeader = "cf-ipcity";
    internal const string TimezoneHeader = "cf-timezone";
    private const string CloudflareSource = "cloudflare";
    private const int MaximumHeaderValueLength = 100;
    private const int MaximumUserAgentLength = 300;
    /// <summary>Cloudflare country codes that mean "no country": XX unknown, T1 Tor.</summary>
    private static readonly HashSet<string> _unknownCountryCodes = new(StringComparer.OrdinalIgnoreCase) { "XX", "T1" };

    private readonly SearchAuditOptions _options = options.Value;

    public string ResolveIpAddress(HttpContext httpContext)
    {
        if (_options.TrustProxyHeaders)
        {
            string? forwarded = ReadHeader(httpContext, CloudflareConnectingIpHeader)
                ?? ReadHeader(httpContext, DigitalOceanConnectingIpHeader)
                ?? ReadHeader(httpContext, ForwardedForHeader)?.Split(',')[0].Trim();
            if (IPAddress.TryParse(forwarded, out IPAddress? forwardedAddress))
            {
                return Normalize(forwardedAddress);
            }
        }

        return httpContext.Connection.RemoteIpAddress is { } remoteAddress ? Normalize(remoteAddress) : ClientLocation.NotAvailable;
    }

    public ClientLocation ResolveLocation(HttpContext httpContext)
    {
        if (!_options.TrustProxyHeaders)
        {
            return ClientLocation.Unknown;
        }

        string? country = ReadHeader(httpContext, CountryHeader);
        if (country is not null && _unknownCountryCodes.Contains(country))
        {
            country = null;
        }

        string? region = ReadHeader(httpContext, RegionHeader) ?? ReadHeader(httpContext, RegionCodeHeader);
        string? city = ReadHeader(httpContext, CityHeader);
        string? timezone = ReadHeader(httpContext, TimezoneHeader);
        if (country is null && region is null && city is null && timezone is null)
        {
            return ClientLocation.Unknown;
        }

        return new ClientLocation(
            country ?? ClientLocation.NotAvailable,
            region ?? ClientLocation.NotAvailable,
            city ?? ClientLocation.NotAvailable,
            timezone ?? ClientLocation.NotAvailable,
            CloudflareSource);
    }

    public static string ResolveUserAgent(HttpContext httpContext) =>
        Truncate(httpContext.Request.Headers.UserAgent.ToString(), MaximumUserAgentLength) is { Length: > 0 } userAgent ? userAgent : ClientLocation.NotAvailable;

    private static string? ReadHeader(HttpContext httpContext, string name)
    {
        string value = httpContext.Request.Headers[name].ToString().Trim();
        return value.Length == 0 ? null : Truncate(value, MaximumHeaderValueLength);
    }

    private static string Normalize(IPAddress address) => (address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address).ToString();

    private static string Truncate(string value, int maximumLength) => value.Length > maximumLength ? value[..maximumLength] : value;
}
