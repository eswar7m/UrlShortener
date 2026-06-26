using System.Text;

namespace UrlShortener.Api.Services;

public class Base62Encoder : IBase62Encoder
{
    private const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

    /// <summary>
    /// Encodes a numeric identifier into a Base62 string.
    /// </summary>
    public string Encode(long value)
    {
        if (value == 0)
        {
            return Alphabet[0].ToString();
        }
        var result = new StringBuilder();
        while (value > 0)
        {
            result.Insert(0, Alphabet[(int)(value % 62)]);
            value /= 62;
        }
        return result.ToString();
    }
}
