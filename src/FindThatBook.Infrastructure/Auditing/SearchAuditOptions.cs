using System.ComponentModel.DataAnnotations;

namespace FindThatBook.Infrastructure.Auditing;

/// <summary>Search audit log settings, bound from the <c>SearchAudit</c> configuration section.</summary>
public sealed class SearchAuditOptions
{
    public const string SectionName = "SearchAudit";

    public bool Enabled { get; set; } = true;

    /// <summary>Folder for the daily JSON Lines files; relative paths resolve against the app's content root.</summary>
    [Required]
    public string Directory { get; set; } = string.Empty;

    /// <summary>Files are named <c>{FileNamePrefix}-{yyyy-MM-dd}.jsonl</c> (UTC date).</summary>
    [Required]
    public string FileNamePrefix { get; set; } = string.Empty;

    /// <summary>Entries waiting to be written; when full, new entries are dropped (and a warning logged) rather than slowing searches.</summary>
    [Range(1, 100000)]
    public int QueueCapacity { get; set; }

    /// <summary>
    /// Read the client IP and location from proxy headers (Cloudflare <c>cf-*</c>, DigitalOcean <c>do-connecting-ip</c>, <c>X-Forwarded-For</c>).
    /// Only enable behind those proxies: anyone reaching the app directly can forge these headers.
    /// </summary>
    public bool TrustProxyHeaders { get; set; }
}
