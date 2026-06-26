# UrlShortener

A lightweight URL shortening service built with ASP.NET Core Minimal API and EF Core. Create short links, redirect to the original URL, and track click counts — all backed by SQLite.

## Features

- **Shorten URLs** — `POST /api/shorten` with validation and deduplication
- **Redirect** — `GET /{shortCode}` returns a `302 Found` to the original URL
- **Click tracking** — increments `click_count` on each redirect
- **URL normalization** — trims whitespace and removes trailing slashes from paths
- **Deduplication** — submitting the same long URL returns the existing short link
- **Base62 short codes** — generated from the database row ID (e.g. `1` → `1`, `62` → `10`)

## Tech stack

| Component | Technology |
|-----------|------------|
| Runtime | .NET 10 (`net10.0`) |
| API | ASP.NET Core Minimal API |
| ORM | EF Core 10 + SQLite |
| Validation | Built-in `AddValidation()` + DataAnnotations |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

Verify your installation:

```bash
dotnet --version
```

## Getting started

### Clone and run

```bash
git clone <repository-url>
cd UrlShortener
dotnet run --project src/UrlShortener.Api/UrlShortener.Api.csproj --launch-profile http
```

The API listens on **http://localhost:5280**.

The SQLite database file (`urlshortener.db`) is created automatically in the project directory on first startup.

### Build

```bash
dotnet build UrlShortener.sln
```

## Configuration

Settings are in `src/UrlShortener.Api/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=urlshortener.db"
  },
  "UrlShortener": {
    "BaseUrl": "http://localhost:5280"
  }
}
```

| Setting | Description |
|---------|-------------|
| `ConnectionStrings:DefaultConnection` | SQLite connection string. The database file is created on startup. |
| `UrlShortener:BaseUrl` | Public base URL used when building short links in API responses. Should match the host/port the app runs on. |

Override values per environment in `appsettings.Development.json` or via environment variables using the standard ASP.NET Core configuration pattern.

## API reference

### Shorten a URL

```http
POST /api/shorten
Content-Type: application/json

{
  "longUrl": "https://example.com/very/long/path"
}
```

**Validation:** `longUrl` is required and must be a valid URL (`[Required]`, `[Url]`).

**Responses:**

| Status | Condition | Body |
|--------|-----------|------|
| `201 Created` | New URL mapping created | `{ "shortUrl": "http://localhost:5280/1" }` |
| `200 OK` | URL already exists (deduplicated) | `{ "shortUrl": "http://localhost:5280/1" }` |
| `400 Bad Request` | Validation failed | Problem details |

**Example (PowerShell):**

```powershell
Invoke-RestMethod -Uri "http://localhost:5280/api/shorten" `
  -Method POST `
  -ContentType "application/json" `
  -Body '{"longUrl": "https://example.com/very/long/path"}'
```

**Example (curl):**

```bash
curl -X POST http://localhost:5280/api/shorten \
  -H "Content-Type: application/json" \
  -d '{"longUrl": "https://example.com/very/long/path"}'
```

### Redirect to original URL

```http
GET /{shortCode}
```

**Responses:**

| Status | Condition |
|--------|-----------|
| `302 Found` | Short code found — redirects to the original URL and increments click count |
| `404 Not Found` | Short code does not exist |

**Example:**

```bash
curl -I http://localhost:5280/1
```

Reserved single-segment paths (`api`, `openapi`) are blocked so they do not collide with API and documentation routes.

## Database schema

Table: `url_mappings`

| Column | Type | Notes |
|--------|------|-------|
| `id` | INTEGER PK | SQLite `AUTOINCREMENT` |
| `long_url` | TEXT NOT NULL | Original URL (indexed for dedup) |
| `short_code` | TEXT NOT NULL UNIQUE | Base62-encoded ID (indexed) |
| `click_count` | INTEGER NOT NULL DEFAULT 0 | Incremented on each redirect |
| `created_at` | TEXT / DateTime | UTC, set on insert |
