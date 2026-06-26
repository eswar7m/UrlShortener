namespace UrlShortener.Api.Services;

public interface IBase62Encoder
{
    /// <summary>
    /// Encodes a numeric identifier into a Base62 string.
    /// </summary>
    string Encode(long value);
}
