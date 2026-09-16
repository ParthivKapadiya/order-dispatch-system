# Topland ERP Architecture

## System purpose

Topland ERP is a cloud-deployable web application for approximately 20+ employees across three companies. Its confirmed purpose is to replace a paper-based **order management and dispatch** workflow. Accounting remains in separate software. Inventory, HR, payroll, manufacturing, BOM, quality, and maintenance are out of scope.

The three companies share one application and one database, with logical isolation by `CompanyId`.

## Technology stack

| Area | Choice |
| --- | --- |
| Backend | ASP.NET Core 9, C# |
| UI | ASP.NET Core MVC, Razor, Bootstrap 5 |
| Data | SQL Server (production), SQLite (local Development), EF Core |
| Auth | ASP.NET Core Identity, cookie authentication |
| Tests | xUnit, FluentAssertions, EF InMemory |

## Project structure

```
ToplandERP/
├── src/
│   ├── ToplandERP.Domain/
│   ├── ToplandERP.Application/
│   ├── ToplandERP.Infrastructure/
│   └── ToplandERP.Web/
├── tests/
│   ├── ToplandERP.UnitTests/
│   └── ToplandERP.IntegrationTests/
└── docs/
```

## Layer responsibilities

**Domain** holds entities, enums, constants, and company-access rules. It has no ASP.NET or EF UI dependencies.

**Application** holds use cases, DTOs, validators, and abstractions such as `IApplicationDbContext`, `ICurrentUser`, `IFileStorage`, and `INotificationService`.

**Infrastructure** holds EF Core, Identity persistence, seeding, SQL Server configuration, and local file storage.

**Web** holds MVC, Razor, authentication UI, claims factory, current-user resolution, middleware, and composition in `Program.cs`.

Dependency direction:

```
Web → Application → Domain
Web → Infrastructure → Application → Domain
```

## Authentication architecture

- Users are ASP.NET Core Identity accounts (`ApplicationUser`).
- Passwords are hashed by Identity. Plaintext passwords are never stored.
- Authentication uses secure cookies.
- Roles: `SuperAdmin`, `CompanyAdmin`, `SalesEmployee`, `DispatchUser`.
- Company membership is stored on the user (`CompanyId`) and copied into claims at sign-in.
- SuperAdmin has no company assignment and can access all companies.
- Browser-supplied `CompanyId`, `UserId`, or role values are never trusted. Server-side identity is the source of truth.

Authorization policies are registered in `AddWebServices()`. A fallback policy requires an authenticated user. Login, error, and `/health` remain anonymous.

## Multi-company architecture

Every company-scoped business entity implements `ICompanyScoped` and has `CompanyId`.

`ApplicationDbContext` applies a global query filter:

- SuperAdmin: no company restriction
- Other users: `CompanyId` must match the authenticated user's company
- Unauthenticated requests: no company-scoped rows
- Seeding and migrations: `SystemCurrentUser` bypasses the filter

Application services must still check access explicitly (see `CompanyService` and `CompanyAccess`) so isolation is not only a UI concern.

## Database strategy

- SQL Server in production and production-like environments
- SQLite in local Development when `Database:Provider` is `Sqlite` (macOS default, so the app can run without Docker)
- Fluent API entity configurations
- UTC timestamps on `BaseEntity`
- Restrict delete from `Company` to dependent records
- Cascade delete for order items and dispatch documents
- Migrations live in `ToplandERP.Infrastructure/Data/Migrations`
- Connection strings and seed passwords come from configuration, user secrets, or environment variables

Core tables are created now so later phases can add columns and behavior without inventing extra ERP modules. Monetary, inventory, and workflow-approval fields are intentionally incomplete until those phases are specified.

## Security principles

- HTTPS and HSTS in non-development environments
- HttpOnly authentication and anti-forgery cookies
- Secure password rules and account lockout
- Generic login failure messages
- Production error pages without stack traces
- File storage foundation rejects path traversal and unlisted extensions
- No production secrets in source control

## Health and operations

- `/health` returns ASP.NET health checks, including EF Core database connectivity
- `/Health` is an authenticated status page
- Logging uses the built-in ASP.NET Core providers

## Implemented phases

0. Foundation
1. Customer, product, transporter, payment-condition, and employee masters
2. Order capture (status starts as Order Received)
3. Order modification requests/approvals and dispatch workflow with document upload
4. Internal in-app notifications (database-backed ERP inbox; no WhatsApp/SMS/email)

## Future development phases

These are planned, not implemented:

5. Operational reports
6. External customer messaging (WhatsApp/SMS/email) only if later confirmed — must not replace the in-app inbox

Deferred decisions (do not invent business rules until confirmed):

- Exact order numbering scheme
- Full order status workflow beyond the three UI badges already reserved
- Modification-request statuses and who can approve them
- Whether order prices belong in this system or remain in accounting software
- Dispatch document types and photo requirements
- Whether SuperAdmin should belong to a company in some environments
