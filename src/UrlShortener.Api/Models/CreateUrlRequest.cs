using System.ComponentModel.DataAnnotations;

namespace UrlShortener.Api.Models;

public class CreateUrlRequest
{
    [Required]
    [Url]
    public string LongUrl { get; set; } = string.Empty;
}
