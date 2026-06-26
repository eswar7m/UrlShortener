using UrlShortener.Api.Models;

namespace UrlShortener.Api.Services;

public interface IUrlShortenerService
{
    /// <summary>
    /// Creates or returns an existing short URL for the given long URL.
    /// </summary>
    Task<(CreateUrlResponse Response, bool Created)> ShortenAsync(string longUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves a short code to its long URL and records a click.
    /// </summary>
    Task<string?> ResolveAndTrackClickAsync(string shortCode, CancellationToken cancellationToken = default);
}
