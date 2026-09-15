# Development guide

## Prerequisites

- .NET SDK 9.0
- SQL Server for production-like work (optional on a Mac)
- `dotnet ef` (this repository includes a local tool; run `dotnet tool restore`)

On macOS, Development uses **SQLite** by default so the site can run without Docker or SQL Server.

Production and team servers still use SQL Server. Switch later by setting `Database:Provider` to `SqlServer` and configuring `ConnectionStrings:DefaultConnection`.

Optional SQL Server via Docker:

```bash
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=Your_local_dev_password1" \
  -p 1433:1433 --name topland-sql -d mcr.microsoft.com/mssql/server:2022-latest
```

## Restore and build

From the repository root:

```bash
dotnet restore
dotnet build
```

## Configure secrets

Do not put real passwords in source files. From `src/ToplandERP.Web`:

```bash
dotnet user-secrets set "Seed:SuperAdmin:Password" "YOUR_LOCAL_ADMIN_PASSWORD"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=ToplandERP_Dev;User Id=sa;Password=YOUR_LOCAL_SQL_PASSWORD;TrustServerCertificate=True;Encrypt=True;MultipleActiveResultSets=True"
```

Equivalent environment variables:

- `Seed__SuperAdmin__Password`
- `ConnectionStrings__DefaultConnection`

`appsettings.Development.json.example` is a template only.

## Migrations

Create a migration after model changes:

```bash
dotnet tool restore
dotnet ef migrations add MigrationName \
  --project src/ToplandERP.Infrastructure \
  --startup-project src/ToplandERP.Web \
  --output-dir Data/Migrations
```

Apply migrations:

```bash
dotnet ef database update \
  --project src/ToplandERP.Infrastructure \
  --startup-project src/ToplandERP.Web
```

The web host also applies pending migrations at startup outside the `Testing` environment.

## Run the application

From the repository root:

```bash
cd /Applications/XAMPP/xamppfiles/htdocs/ToplandERP
dotnet run --project src/ToplandERP.Web --launch-profile http
```

HTTP: `http://localhost:5025`

Development on a Mac uses SQLite automatically. You do not need SQL Server or `dotnet ef database update` for local login.

- Username: `admin`
- Password: set locally in gitignored `appsettings.Development.json` or via user secrets

The existing local Development settings file is not committed. Keep it on this machine so current login continues to work. New clones should copy `appsettings.Development.json.example` to `appsettings.Development.json` and set the SuperAdmin password locally.

Re-running the app does not create duplicate companies, roles, or the admin user.

## Tests

```bash
dotnet test
```

- Unit tests cover domain access rules, company service filtering, and EF query filters.
- Integration tests host the web app with EF InMemory so they do not require SQL Server.

## Health

- Anonymous JSON: `GET /health`
- Authenticated page: `/Health`

## File uploads

Upload UI is not implemented yet. The storage foundation writes to `App_Data/uploads` and only allows `.jpg`, `.jpeg`, `.png`, `.webp`, and `.pdf`.
