# FMCG — Multi-Tenant Sales, Distribution & Marketing SaaS

A multi-tenant sales, distribution & marketing SaaS for the tobacco / FMCG
route-to-market industry (14 modules, 54-table domain). Built as a clean
architecture with a separate React SPA frontend, targeting PostgreSQL.

## Stack

- **Backend:** ASP.NET Core 8 Web API, Clean Architecture (6 projects)
- **Frontend:** React 19 (Vite + TypeScript), Tailwind CSS v4, shadcn/ui
- **Database:** PostgreSQL (Supabase), EF Core 8 + Dapper for complex queries
- **Auth:** Custom JWT (access + rotating refresh tokens), dynamic role/permission policies, BCrypt password hashing

## Solution Layout

```text
TobaccoSaaS.slnx
├── src
│   ├── TobaccoSaaS.Domain          Domain entities, enums, permission catalog
│   ├── TobaccoSaaS.Application     Application services, business logic, DTOs
│   ├── TobaccoSaaS.Infrastructure  EF Core, repositories, JWT, seeding, migrations
│   └── TobaccoSaaS.Api             REST API (/api/v1), middleware, auth policies
├── tests
│   ├── TobaccoSaaS.UnitTests       Unit tests
│   └── TobaccoSaaS.IntegrationTests   Integration tests
└── web                             React SPA
```

## Getting Started

Prerequisites: .NET 8 SDK, Node 20+, PostgreSQL (local or Supabase).

### Backend

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=<host>;Database=postgres;Username=<user>;Password=<password>;"
dotnet user-secrets set "Jwt:SigningKey" "<64+ char random key>"
dotnet ef database update --project src/TobaccoSaaS.Infrastructure --startup-project src/TobaccoSaaS.Api
dotnet run --project src/TobaccoSaaS.Api
```

The API boots on `http://localhost:5085` with Swagger at `/swagger`. Seeding is
idempotent and inserts permissions, feature flags, plans, platform roles, and a
platform admin (credentials from `Seed:PlatformAdminPassword`).

> Secrets (connection string, JWT signing key, seed password) are read from
> .NET user-secrets / environment variables. Never commit them. See `.gitignore`.

### Frontend

```bash
cd web
npm install
cp .env.example .env        # set VITE_API_BASE_URL if needed
npm run dev                 # http://localhost:5173
```

## API Conventions

- Base path: `/api/v1`
- Responses: `ApiResponse<T>` envelope `{ success, data, error }`
- Pagination: server-side only, request `{ page, pageSize }`
- Auth: `Authorization: Bearer <accessToken>`; expired access tokens are
  refreshed via a rotating refresh token (sliding rotation, session revocation
  on reuse/logout)

## Build & Test

```bash
dotnet build TobaccoSaaS.slnx
cd web && npm run build && npm run lint
```