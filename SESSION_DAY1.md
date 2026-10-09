# Tobacco SaaS — Session Export (Day 1)

Date: **Thu 2026-10-08** | Platform: Windows (win32), PowerShell 5.1 | Workspace: `G:\.NET SAAS\TOBACCO SAAAS`

> This document is the Day-1 session snapshot so a new context can be resumed quickly.
> Secrets are NOT duplicated here — they live in the root `.env` (gitignored) and in dotnet user-secrets (see "Secrets").

---

## 1. Objective

Build the multi-tenant sales / distribution / marketing SaaS (tobacco/FMCG) from
`Multi-Tenant_Sales_Distribution_Marketing_SaaS_Requirements_Specification.pdf`,
following `BUILD_PLAN.md` (Foundation steps 01–04 done). 17 modules, 54+ tables.

## 2. Locked Stack (user-chosen)

| Area | Choice |
|---|---|
| API | ASP.NET Core Web API (net8.0), Clean Architecture |
| UI | React 19.3.0 SPA, **JSX/JS (no TypeScript)**, Vite, Tailwind v4, shadcn/ui, react-query, react-hook-form + zod, Sonner |
| DB | PostgreSQL on **Supabase** (via session pooler, see §4) |
| Auth | Custom JWT access tokens + rotating refresh tokens, permission policies |
| Deploy | SPA → **Vercel** (`vercel.json`); API not yet deployed |

Money = bigint minor units · UUID PKs · timestamptz UTC · soft delete via `deleted_at`.

## 3. Project Layout

```
TobaccoSaaS.slnx
├── src/TobaccoSaaS.Domain          Entities, enums, PermissionCatalog, Common
├── src/TobaccoSaaS.Application     Services, business logic, validation, documents
├── src/TobaccoSaaS.Infrastructure  DbContext, migrations, GenericRepository, UnitOfWork, seed
├── src/TobaccoSaaS.Api             Controllers, middleware, Program.cs, JWT
├── tests/                          (empty)
└── web/                            React SPA (JSX)
```

Dependency order: Api → Application / Infrastructure → Contracts (ViewModels,DTOs) → Domain.

## 4. Database (Supabase) — where the connection string lives

- Project ref: `zrnlzpvxtwleryvuyjni` · Region: **ap-northeast-1 (Tokyo)** · Database `postgres`
- **Working connection** (session pooler, IPv4 — the direct host `db.zrnlzpvxtwleryvuyjni.supabase.co`
  is IPv6-only and unreachable from this machine):
  `Host=aws-0-ap-northeast-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.zrnlzpvxtwleryvuyjni;Ssl Mode=Require;Trust Server Certificate=true`
- All secrets (DB password, JWT signing key, seed admin password) are in **two places**:
  1. Root **`.env`** (project workspace, gitignored):
     `ConnectionStrings__DefaultConnection`, `Jwt__SigningKey`, `Seed__PlatformAdminPassword`
     (double-underscore keys map to `ConnectionStrings:DefaultConnection` etc. in config).
  2. **dotnet user-secrets** (UserSecretsId `tobaccosaas-api-9f3c1a2b`):
     `%APPDATA%\Microsoft\UserSecrets\tobaccosaas-api-9f3c1a2b\secrets.json`
- **`.env` loading:** `Program.cs` `LoadDotEnvFile()` walks up from the working dir to find the
  nearest `.env`, sets process env vars (real env vars win), then `builder.Configuration
  .AddEnvironmentVariables()` binds them. **Verified** — API booted + platform login succeeded with
  user-secrets moved aside.
- Migration `20261008205707_InitialCreate` applied. 14 tables in `public`: audit_logs,
  feature_flags, permissions, plans, role_permissions, roles, subscriptions, tenant_features,
  tenant_settings, tenants, user_roles, user_scopes, user_sessions, users.

## 5. Verified accounts

| Account | Email | Password |
|---|---|---|
| Platform Admin | `platform.admin@tobaccosaas.local` | in `.env` / user-secrets (`Seed__PlatformAdminPassword`) |
| Tenant Admin (Demo Distributor, slug `demo-distributor-01`) | `demo.admin@example.com` | temp password shown at provisioning (password change on first login) |

Tenant admin scope: `tenantId=a887868d-0bf6-4000-9a2a-a8ffe79e7267`, `isPlatformScope=false`, role `TENANT_ADMIN`, 91 permissions.

## 6. Day-1 Session Log (what was done)

1. Backend foundation scaffolded & built (Domain/Application/Infrastructure/Api/Contracts, Auth,
   RBAC two-level: platform + per-company role matrix, tenant provisioning, seeding, Swagger, Serilog,
   centralized `ExceptionHandlingMiddleware`).
2. Supabase connection established via **session pooler** (direct host IPv6-blocked). `dotnet ef database
   update` applied the initial migration → 14 tables verified in Supabase.
3. API runs at `http://127.0.0.1:5085`; full chain **Supabase → seed → JWT login → tenant provisioning**
   verified end-to-end (platform admin login, tenant `Demo Distributor` created, tenant admin login OK).
4. Git initialized → repo `https://github.com/montaherul/FMCG.git` (branch `main`; identity
   montaherul / c233210@ugrad.iiuc.ac.bd). Root `.gitignore` + `README.md` written. Commits pushed:
   - `first commit` (121 files)
   - `frontend: jsx/js, react 19.3, vercel deployable`
5. Frontend scaffolded, then **converted from TSX/TS to JSX/JS**; React/React-DOM bumped to **19.3.0**;
   `web/vercel.json` added (framework `vite`, output `dist`, SPA rewrite); `package.json` `build` = `vite build` (no tsc).
6. SPA screens built: skeleton (login/dashboard/not-found), app layout + protected routes + auth store,
   **Tenants** screen (list + pagination + provision form + suspend/activate, permission-gated), then
   **Roles** screen (role list, permission matrix grouped by module, create role, save permissions) and
   **Users** screen (user table, create user with optional password + roles, inline role assignment).
7. Backend addition to support Roles UI: **`GET /Roles/{id}/permissions`** (returns the role's permission
   codes) — added to `IRbacService` / `RbacService.GetRolePermissionsAsync` / `RolesController`.
8. Secrets unified into root **`.env`** + `.env` loader in `Program.cs` (walks up, real env wins).
   Verified **API boots and logs in using `.env` only** (user-secrets temporarily moved aside, then restored).
9. Frontend verification: `npm run build` + `npm run lint` pass (2 benign warnings: `buttonVariants`
   export in button.jsx, `useTheme` export in theme-provider.jsx). Dev-server smoke test of the JSX
   conversion was inconclusive (aborted) — build/lint are the trusted checks.

## 7. Current State (in-flight)

- **Done/verified:** DB live, seeding, platform + tenant login, Tenants/Roles/Users SPA screens (build+lint),
  `.env` secrets loading. RBAC backend endpoint added and compiled.
- **Not yet verified at runtime:** the new `/Roles/{id}/permissions` endpoint + the new Roles/Users screens
  against a live API session (API currently running on `:5085`, PID logged in startup output).
- Tests projects (`tests/`) empty — no automated tests written yet.

## 8. Next Steps (suggested)

1. Smoke-test RBAC end-to-end (tenant admin login → `/Users`, `/Roles`, `/Roles/{id}/permissions`,
   `/permissions`, `POST /Users`, `PUT /Roles/{id}/permissions`).
2. Commit + push the current work (Roles/Users screens, rbac API module, new backend endpoint,
   `.env` loader in Program.cs). **Root `.env` stays gitignored.**
3. Re-run the Vite dev-server smoke test (`cmd.exe /c npm run dev`) to confirm the JSX app serves.
4. Next backend module (Organization / Geography / Product per BUILD_PLAN order) or write UnitTests/IntegrationTests.
5. Optionally deploy the API somewhere and set `web/.env` `VITE_API_BASE_URL` to the hosted URL for Vercel.

## 9. Useful Commands

```powershell
# Build (whole solution)
dotnet build TobaccoSaaS.slnx -nologo -clp:NoSummary

# Run API locally
dotnet run --project src\TobaccoSaaS.Api --no-build --urls http://127.0.0.1:5085

# Migrations
dotnet ef migrations add <Name> --project src\TobaccoSaaS.Infrastructure --startup-project src\TobaccoSaaS.Api
dotnet ef database update --project src\TobaccoSaaS.Infrastructure --startup-project src\TobaccoSaaS.Api

# Backend secrets
dotnet user-secrets list --project src\TobaccoSaaS.Api

# Frontend
cd web; npm run build; npm run lint; npm run dev

# Git
git add -A; git commit -m "..."; git push
```

## 10. Key Files

- `BUILD_PLAN.md` — roadmap, DoD, build order, §14 deviation log
- `AGENTS.md` — engineering rules (root template + customized project copy)
- `src\TobaccoSaaS.Api\Program.cs` — bootstrap incl. `LoadDotEnvFile()`
- `src\TobaccoSaaS.Application\Features\Rbac\{IRbacService,RbacService,RbacDtos}.cs` — RBAC contract
- `src\TobaccoSaaS.Api\Controllers\{RolesController,UsersController}.cs` — RBAC endpoints
- `src\TobaccoSaaS.Domain\Common\PermissionCatalog.cs` — permission modules/actions
- `web\src\pages\{tenants,roles,users}-page.jsx` — SPA admin screens
- `web\src\lib\api\{client,auth,tenants,rbac,types}.js` — API client + typed helpers
- `web\src\App.jsx`, `web\src\components\app-layout.jsx`, `web\src\store\auth.js` — routing/nav/session
- `web\vercel.json`, `web\.env`, `web\.env.example` — deploy + local env

---

*End of Day-1 session export.*