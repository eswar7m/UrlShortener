using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using UrlShortener.Api.Data;
using UrlShortener.Api.Models;

namespace UrlShortener.Api.Services;

public class UrlShortenerOptions
{
    public const string SectionName = "UrlShortener";
    public string BaseUrl { get; set; } = "http://localhost:5280";
}

public class UrlShortenerService(
    UrlShortenerDbContext dbContext,
    IBase62Encoder base62Encoder,
    IOptions<UrlShortenerOptions> options) : IUrlShortenerService
{
    private readonly UrlShortenerOptions _options = options.Value;

    /// <summary>
    /// Creates or returns an existing short URL for the given long URL.
    /// </summary>
    public async Task<(CreateUrlResponse Response, bool Created)> ShortenAsync(string longUrl, CancellationToken cancellationToken = default)
    {
        var normalizedUrl = NormalizeUrl(longUrl);
        var existing = await dbContext.UrlMappings
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.LongUrl == normalizedUrl, cancellationToken);
        if (existing is not null)
        {
            return (new CreateUrlResponse { ShortUrl = BuildShortUrl(existing.ShortCode) }, false);
        }
        var mapping = new UrlMapping
        {
            LongUrl = normalizedUrl,
            ShortCode = $"pending-{Guid.NewGuid():N}",
            ClickCount = 0,
            CreatedAt = DateTime.UtcNow
        };
        dbContext.UrlMappings.Add(mapping);
        await dbContext.SaveChangesAsync(cancellationToken);
        mapping.ShortCode = base62Encoder.Encode(mapping.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (new CreateUrlResponse { ShortUrl = BuildShortUrl(mapping.ShortCode) }, true);
    }

    /// <summary>
    /// Resolves a short code to its long URL and records a click.
    /// </summary>
    public async Task<string?> ResolveAndTrackClickAsync(string shortCode, CancellationToken cancellationToken = default)
    {
        var mapping = await dbContext.UrlMappings
            .FirstOrDefaultAsync(m => m.ShortCode == shortCode, cancellationToken);
        if (mapping is null)
        {
            return null;
        }
        mapping.ClickCount++;
        await dbContext.SaveChangesAsync(cancellationToken);
        return mapping.LongUrl;
    }

    /// <summary>
    /// Normalizes a URL by trimming whitespace and removing trailing slashes from the path.
    /// </summary>
    private static string NormalizeUrl(string url)
    {
        url = url.Trim();
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return url;
        }
        var path = uri.AbsolutePath;
        if (path.Length <= 1 || !path.EndsWith('/'))
        {
            return uri.ToString();
        }
        var builder = new UriBuilder(uri) { Path = path.TrimEnd('/') };
        return builder.Uri.ToString();
    }

    /// <summary>
    /// Builds the public short URL from a short code and configured base URL.
    /// </summary>
    private string BuildShortUrl(string shortCode)
    {
        return $"{_options.BaseUrl.TrimEnd('/')}/{shortCode}";
    }
}
