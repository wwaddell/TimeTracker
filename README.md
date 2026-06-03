# TimeTracker

A customizable, multi-tenant time tracking application. Users log activity quickly
(the only always-required field is a **note**); organizations configure what extra
fields are collected, per role.

## Architecture

| Project | Purpose |
| --- | --- |
| `src/TimeTracker.Domain` | POCO entities + enums (no infrastructure dependencies) |
| `src/TimeTracker.Infrastructure` | EF Core 10 `DbContext`, entity configurations, migrations |
| `src/TimeTracker.Contracts` | DTOs shared by the API and clients (web + future MAUI) |
| `src/TimeTracker.Api` | ASP.NET Core Web API (JWT bearer, Entra External ID) — *stub* |
| `src/TimeTracker.Web` | Blazor WebAssembly client (MSAL) — *stub* |
| `tests/TimeTracker.Tests` | Unit/integration tests |

**Authentication** is handled by Microsoft Entra External ID (Google + Apple social,
Okta/enterprise via custom OIDC) and is consumed as standard OIDC/JWT, keeping the
provider swappable. **Authorization** (organizations, per-org roles, role→entry-format
mapping) lives in this database.

**Configurable fields** use an EAV model: core columns on `t_time_entry`, plus
org/role-defined fields defined in `t_time_entry_field` and stored as key-value rows
in `t_time_entry_attribute`. Tables follow the house convention: domain tables `t_*`,
lookup tables `t_type_*`.

## Prerequisites

- .NET SDK **10.0.300** (pinned in `global.json`)
- SQL Server LocalDB for local development

## Database

The design-time connection defaults to LocalDB
(`Server=(localdb)\MSSQLLocalDB;Database=TimeTracker;...`). Override with the
`TIMETRACKER_CONNECTION` environment variable, or the `TimeTracker` connection
string when hosted.

```pwsh
# Apply migrations to the local database
dotnet ef database update -p src/TimeTracker.Infrastructure -s src/TimeTracker.Infrastructure

# Add a new migration
dotnet ef migrations add <Name> -p src/TimeTracker.Infrastructure -s src/TimeTracker.Infrastructure -o Persistence/Migrations
```

## Status

Iteration 1 complete: solution structure, domain model, EF Core context + initial
migration applied to LocalDB. UI, API endpoints, auth wiring, and the MAUI app are
upcoming iterations.
