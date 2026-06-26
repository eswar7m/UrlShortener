using Microsoft.EntityFrameworkCore;
using UrlShortener.Api.Models;

namespace UrlShortener.Api.Data;

public class UrlShortenerDbContext(DbContextOptions<UrlShortenerDbContext> options) : DbContext(options)
{
    public DbSet<UrlMapping> UrlMappings => Set<UrlMapping>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UrlMapping>(entity =>
        {
            entity.ToTable("url_mappings");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.LongUrl).HasColumnName("long_url").IsRequired();
            entity.Property(e => e.ShortCode).HasColumnName("short_code").IsRequired();
            entity.HasIndex(e => e.ShortCode).IsUnique();
            entity.HasIndex(e => e.LongUrl);
            entity.Property(e => e.ClickCount).HasColumnName("click_count").HasDefaultValue(0);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        });
    }
}
