# Topland ERP

Topland ERP is a secure, multi-company web application for **order management and dispatch workflow**. It replaces the current paper-based process used by:

- Gravis India Private Limited
- Jeeko Agritech LLP
- Shree Agency

This repository currently contains **Phase 0**: a production-ready application foundation. Order, dispatch, notification, and reporting modules are not implemented yet.

## What Phase 0 includes

- ASP.NET Core MVC solution with Clean Architecture layering
- SQL Server + Entity Framework Core (SQLite is used automatically for local Development on a Mac)
- ASP.NET Core Identity with role and company isolation foundations
- Seeded companies and roles
- Development SuperAdmin seed
- Responsive corporate UI shell
- Health checks, logging, and global error handling
- Unit and integration test projects

## What is out of scope

Inventory, accounting, HR, manufacturing, WhatsApp/SMS, reports, and full order/dispatch CRUD are intentionally not part of this phase.

## Quick start

See [docs/DEVELOPMENT.md](docs/DEVELOPMENT.md) for prerequisites, database setup, migrations, seeding, and tests.

```bash
cd /Applications/XAMPP/xamppfiles/htdocs/ToplandERP
dotnet restore
dotnet tool restore
dotnet run --project src/ToplandERP.Web --launch-profile http
```

Open `http://localhost:5025`

- Username: `admin`
- Password: set locally in gitignored `appsettings.Development.json` or via user secrets

## Architecture

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).
