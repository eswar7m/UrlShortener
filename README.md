# URL Shortener Service

## Overview

A lightweight URL shortening service built with ASP.NET Core Minimal API and EF Core. Create short links, redirect to the original URL, and track click counts.

Clients submit a long URL via `POST /api/shorten` and receive a short link. When someone visits that link (`GET /{shortCode}`), the service looks up the original URL, records a click, and issues an HTTP redirect. URLs are normalized (trimmed and de-slashed), identical long URLs reuse the same short link, and short codes are Base62-encoded from the database row ID.

## Why this project

URL shorteners are a practical way to learn backend fundamentals: HTTP semantics (redirects, status codes), input validation, persistence, and idempotent create behavior. This project keeps the scope focused while demonstrating patterns you would use in production services:

- **Minimal API endpoints** with clear request/response contracts
- **Service layer** separating HTTP concerns from business logic
- **EF Core** for schema management and queries
- **Deduplication** so the same long URL always maps to one short link
- **Click tracking** for basic usage analytics

It works well as a reference implementation, a starting point for a portfolio project, or a sandbox for experimenting with caching, auth, and scaling strategies.

## System architecture

Requests enter through the Minimal API in `Program.cs`, which handles routing and validation. Business logic lives in `UrlShortenerService`, and all URL mappings are stored in SQLite through EF Core.

**Shortening a URL (`POST /api/shorten`):** The API validates the request body, then passes the long URL to the service. The service normalizes the URL and checks whether that URL already exists in the database. If it does, the existing short link is returned with `200 OK`. If not, a new row is saved, its ID is converted to a Base62 short code, and the new short link is returned with `201 Created`.

**Redirecting (`GET /{shortCode}`):** The API receives the short code from the URL path and asks the service to look it up. If no matching record exists, the client gets `404 Not Found`. If a match is found, the service increments the click count, saves the update, and the API sends the client a `302 Found` redirect to the original long URL.

**Components:**

| Layer | Responsibility |
|-------|----------------|
| **Minimal API** (`Program.cs`) | Routing, validation, HTTP status codes, reserved-path checks |
| **UrlShortenerService** | Normalization, deduplication, Base62 encoding, click tracking |
| **Base62Encoder** | Converts numeric row IDs to compact alphanumeric short codes |
| **UrlShortenerDbContext** | EF Core persistence for `url_mappings` |

## Tech stack

| Component | Technology |
|-----------|------------|
| Runtime | .NET 10 (`net10.0`) |
| API | ASP.NET Core Minimal API |
| ORM | EF Core 10 |
| Database | SQLite |
| Validation | Built-in `AddValidation()` + DataAnnotations |

> **Note:** SQLite is used for **development and local demonstration** only. It requires no external database server and keeps setup friction low. For production workloads, swap the EF Core provider to PostgreSQL or SQL Server and use connection pooling, backups, and appropriate indexing.

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

## How to run locally

### Prerequisites

Install the [.NET 10 SDK](https://dotnet.microsoft.com/download) and verify it:

```bash
dotnet --version
```

### Clone, build, and run

```bash
git clone <repository-url>
cd UrlShortener
dotnet build UrlShortener.sln
dotnet run --project src/UrlShortener.Api/UrlShortener.Api.csproj --launch-profile http
```

The API listens on **http://localhost:5280**. The SQLite database file (`urlshortener.db`) is created automatically in the project directory on first startup.

### Configuration

Settings live in `src/UrlShortener.Api/appsettings.json`:

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

## Future enhancements

| Enhancement | Why it matters |
|-------------|----------------|
| **Redis cache for redirects** | Hot short codes can be served from memory to reduce database load on high-traffic links |
| **Rate limiting & abuse protection** | Prevent spam, brute-force enumeration, and redirect abuse on public endpoints |
| **API keys / authentication** | Restrict who can create links while keeping redirects public |
| **Custom short aliases** | Let trusted users choose memorable codes (e.g. `/docs`) with collision checks |
| **Link expiration & max clicks** | Time-bound or usage-capped links for campaigns, trials, and security-sensitive shares |
| **Analytics API** | Expose click trends, referrers, and geo data (via middleware or a dedicated read model) |
| **Bulk import / export** | Migrate or back up thousands of mappings for marketing or ops workflows |
| **Health checks & structured logging** | `/health` endpoints, OpenTelemetry traces, and metrics for production observability |
| **Idempotent shorten with client keys** | Safe retries from clients using `Idempotency-Key` headers without duplicate rows |