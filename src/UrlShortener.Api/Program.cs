using Microsoft.EntityFrameworkCore;
using UrlShortener.Api.Data;
using UrlShortener.Api.Models;
using UrlShortener.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddValidation();
builder.Services.Configure<UrlShortenerOptions>(builder.Configuration.GetSection(UrlShortenerOptions.SectionName));
builder.Services.AddDbContext<UrlShortenerDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddSingleton<IBase62Encoder, Base62Encoder>();
builder.Services.AddScoped<IUrlShortenerService, UrlShortenerService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<UrlShortenerDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}

app.MapPost("/api/shorten", async (CreateUrlRequest request, IUrlShortenerService urlShortenerService, CancellationToken cancellationToken) =>
{
    var (response, created) = await urlShortenerService.ShortenAsync(request.LongUrl, cancellationToken);
    return created
        ? Results.Created(response.ShortUrl, response)
        : Results.Ok(response);
})
.WithName("ShortenUrl");

app.MapGet("/{shortCode}", async (string shortCode, IUrlShortenerService urlShortenerService, CancellationToken cancellationToken) =>
{
    if (IsReservedPath(shortCode))
    {
        return Results.NotFound();
    }
    var longUrl = await urlShortenerService.ResolveAndTrackClickAsync(shortCode, cancellationToken);
    if (longUrl is null)
    {
        return Results.Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Not Found",
            detail: $"No URL mapping exists for short code '{shortCode}'.");
    }
    return Results.Redirect(longUrl, permanent: false);
})
.WithName("RedirectToLongUrl");

app.Run();

static bool IsReservedPath(string shortCode) =>
    shortCode.Equals("api", StringComparison.OrdinalIgnoreCase)
    || shortCode.Equals("openapi", StringComparison.OrdinalIgnoreCase);
