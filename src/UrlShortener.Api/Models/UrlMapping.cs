namespace UrlShortener.Api.Models;

public class UrlMapping
{
    public int Id { get; set; }
    public required string LongUrl { get; set; }
    public required string ShortCode { get; set; }
    public int ClickCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
