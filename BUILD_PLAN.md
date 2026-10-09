# Tobacco SaaS — Build Plan

**Product:** Multi-Tenant Sales, Distribution & Marketing Management SaaS
**Source of truth:** `Multi-Tenant_Sales_Distribution_Marketing_SaaS_Requirements_Specification.pdf` (v1.0)
**Governing rules:** `AGENTS.md` (Generic Software Development Rules, 42 sections)
**Status:** Planning baseline + Foundation (build steps 01–04) in progress

---

## 1. Product Definition (one line)

A single platform where a platform operator onboards independent tenant companies; each tenant models its own organization, sales geography, distributors, outlets and products; its field force (CSRs) executes daily routes with offline-capable visits; orders, targets, marketing campaigns, inventory and collections are tracked against that geography; and every role — from CEO to CSR — sees exactly the slice of data its scope allows, under a premium light/dark theme it can toggle.

Reference market: **Bangladesh (BDT / poisha minor units)**, internationally portable.

---

## 2. Locked Technical Decisions

| Concern | Decision |
|---------|----------|
| Backend | ASP.NET Core **Web API** (.NET 8, LTS) |
| Architecture | **Clean Architecture** (explicitly requested by developer → permitted by AGENTS.md §3) |
| Frontend | **React** SPA (Vite + TypeScript) |
| Database | **PostgreSQL** (Supabase-hosted) via Npgsql / EF Core |
| Access pattern | EF Core for CRUD; parameterized SQL / functions for complex listing, reporting, aggregation |
| Auth | JWT access token + rotating refresh token; server-side permission + scope checks |
| Money | `bigint` minor units (poisha). Never floating point (spec §3.5, §18.1) |
| IDs | `uuid v4` (safe for offline client generation — spec §18.1) |
| Time | `timestamptz` UTC storage; tenant timezone at render |
| Soft delete | `deleted_at timestamp NULL`; partial unique indexes `WHERE deleted_at IS NULL` |

> Mapping to the AGENTS.md generic rules: Controllers stay thin (§5), business logic lives in the Application layer (§6), data access in Infrastructure (§8), DTOs separate from entities (§22), validation split input/business (§15), tenant/scope enforced server-side (§17), centralized exception handling (§19), DI everywhere (§13).

**Clean Architecture layer → responsibility (adapted from AGENTS.md §4/§39):**

```
TobaccoSaaS.Api            → HTTP/endpoints only            (Presentation)
   ↓
TobaccoSaaS.Application    → use cases, business rules, DTOs, interfaces
   ↓
TobaccoSaaS.Domain         → entities, enums, domain invariants (no dependencies)
   ↑
TobaccoSaaS.Infrastructure → EF Core, repositories, UoW, storage, providers (implements Application interfaces)
```

`Application` and `Infrastructure` communicate **only** through interfaces declared in `Application`. `Api` wires the DI container.

---

## 3. Solution / Repository Layout

```
TOBACCO SAAAS/
├── AGENTS.md
├── BUILD_PLAN.md
├── README.md
├── TobaccoSaaS.sln
├── Directory.Build.props            # shared TFM, nullable, langversion
├── .editorconfig / .gitignore
├── .env.example                     # secrets template (never committed real values)
├── docker-compose.yml               # postgres + api (local, optional)
├── .github/workflows/ci.yml         # build + test + isolation gate
├── src/
│   ├── TobaccoSaaS.Domain/          # Entities, Enums, Common
│   ├── TobaccoSaaS.Application/     # Common, Interfaces, DTOs, Services, Mapping, Validation
│   ├── TobaccoSaaS.Infrastructure/  # Data (DbContext, Configurations, Migrations), Repositories, Services
│   └── TobaccoSaaS.Api/             # Controllers, Middleware, Program.cs, appsettings
├── tests/
│   ├── TobaccoSaaS.UnitTests/
│   └── TobaccoSaaS.IntegrationTests/
└── web/                             # React SPA
    ├── src/{app,features,components,lib,styles}
    └── package.json
```

---

## 4. Non-Negotiable Invariants (from spec Ch. 3–5)

1. **Server-derived tenant context.** Tenant comes from the authenticated session/token, never from the client. Client-supplied tenant IDs are ignored and logged as suspicion (§3.3.1).
2. **Central query scope.** Every tenant-owned query is filtered by `tenant_id` at the data-access layer, never only in the service/UI.
3. **Database constraints.** `tenant_id NOT NULL` + index leading on `tenant_id` on every tenant table; composite `(tenant_id, id)` FK path where practical.
4. **Scope-aware reads.** Geographic scope further restricts rows; exports apply the *identical* predicate.
5. **File isolation.** Tenant-prefixed keys; short-lived signed URLs re-authorized per request.
6. **Audit on access.** Cross-module/admin access auditable; audit append-only (app DB role has no UPDATE/DELETE on `audit_logs`).

**Four-part authorization:** user ↔ position ↔ role(permission bundle) ↔ geographic scope. These are separate; conflating them is forbidden. Effective permissions = union of role permissions. One active scope node per user (platform roles have none).

---

## 5. The 17 Domain Modules (ownership boundaries — spec §3.4)

Identity · Tenancy · Organization · Geography · Products · Distribution · Outlets · FieldForce · Sales · Targets · Marketing · Inventory · Finance · Reports · Notifications · Workflow · Audit

Each module owns its tables and must not own another module's concern (e.g. FieldForce fires events to Sales; Sales raises events to Finance).

---

## 6. Database Design (54 tables — spec Ch. 18–19)

**Conventions:** snake_case; plural tables; join tables `a_b`; `ix_`/`fk_`/`uq_` prefixes; uuid PK `gen_random_uuid()`; `timestamptz`; `bigint` money; JSONB only for settings/snapshots; standard audit column block on every table; soft delete with partial indexes.

**Table inventory by module:**

| Module | Tables |
|--------|--------|
| Platform & Tenancy | tenants, plans, subscriptions, feature_flags, tenant_features, tenant_settings |
| Identity & Access | users, employees, roles, permissions, role_permissions, user_roles, user_scopes |
| Organization | org_units, positions, employee_positions |
| Geography | geo_reference, geo_nodes |
| Distribution & Outlets | distributors, distributor_users, outlets, outlet_assignments |
| Products | categories, brands, products, skus, price_lists, price_list_items |
| Field Force | route_plans, route_stops, visits, visit_activities, attendance |
| Sales | sales_orders, sales_order_items, sales_transactions, order_status_history |
| Targets | targets, target_allocations, achievement_snapshots |
| Marketing | campaigns, campaign_executions, posm_assets, competitor_records |
| Inventory & Finance | stock_movements, distributor_stock, outlet_stock, invoices, payments |
| Workflow & System | approval_workflows, approval_requests, notifications, audit_logs, attachments |

Column-level DDL is contractual (spec Ch. 19) — names must not be renamed, merged or dropped.

---

## 7. API Standards (spec Ch. 20)

- Versioned base path: `/api/v1/*`
- Consistent response envelope:
  - success: `{ "data": ..., "meta": { "page", "pageSize", "total" } }`
  - error: `{ "error": { "code", "message", "details"? } }` (never leaks internals — AGENTS.md §19)
- Pagination/sort/filter **server-side only**; hard row caps on exports.
- Permission code per endpoint (`module.action`), enforced by a policy/authorization handler.
- Every write audited; every export audited (spec §13.3, §21.2).

Default permission catalog = Appendix B (seed + authorization test oracle). Role templates = Table 4.1. Both live as seed data so tests read the same catalog as production (no drift).

---

## 8. Frontend (React SPA)

- Vite + TypeScript + React Router; TanStack Query for server state; a small form layer with client validation.
- Feature-folder structure mirroring the 17 modules.
- **Dual theme** (light `#f5f8fc` / dark `#0a1628` family) driven by CSS custom properties (design tokens, spec §16). Single always-visible toggle; per-user persistence; OS preference default; **no hard-coded colors in components**.
- CSR mobile shell: bottom-tab, 44px targets, offline-aware (service worker + outbox) — later phase.
- Every list is server-paginated; every screen implements five states: loading, empty, error, no-permission, offline (spec §17).
- SPA must never hold secrets or make authorization decisions (AGENTS.md §24).

---

## 9. Security, Testing & DevOps Envelope

**Security (spec Ch. 21):** Argon2id/bcrypt hashing; lockout + backoff; session-fixation protection; MFA-ready; TLS/HSTS; CSRF for cookie sessions; per-IP/token rate limits (stricter on auth + sync); signed short-lived file URLs; secrets from env/secret manager; dependency scanning; append-only audit.

**Testing (spec Ch. 22):**
- Unit (pricing, cascade math, scope subtree, metric formulas)
- Integration (repos, migrations up/down)
- API/contract (auth, permissions, validation, pagination, idempotency)
- **Tenant isolation suite — zero tolerance, blocks merge**
- UI smoke (nightly), load smoke (weekly)

**DevOps (spec Ch. 24):** containers; CI stages Build → Integration → **Isolation (blocking)** → Package → Staging → Canary → Prod; expand-only migrations; structured logs with request/tenant/trace ids; PII never logged; daily backup + PITR; RPO 15 min / RTO 2 h.

---

## 10. Master Build Order (spec §23.2) — 28 steps

Dependency chain: identity → organization → geography → outlets → routes → visits → orders → everything → dashboards.

| # | Deliverable | Depends | Gate |
|---|-------------|---------|------|
| 01 | Foundation: repo, CI, env, migrations tooling | – | CI green on hello-world deploy |
| 02 | Multi-tenancy: tenants, settings, feature flags | 01 | Provision tenant E2E |
| 03 | Authentication: users, sessions, tokens, reset, lockout | 02 | Auth suite green |
| 04 | RBAC: roles, permissions, bindings, guards | 03 | Authorization matrix green |
| 05 | Organization: units, positions, assignments | 04 | Org CRUD + cycle tests |
| 06 | Employees + profile lifecycle | 05 | Employee suite green |
| 07 | Geography: reference + sales nodes, scope ladder | 05 | Subtree perf test |
| 08 | Distributors + portal users | 07 | Isolation suite green |
| 09 | Outlets + assignments + import | 08 | Import dry-run E2E |
| 10 | Products: catalog + price lists | 02 | Price resolution tests |
| 11 | Field force: plans, stops, visits core | 09 | Visit lifecycle E2E |
| 12 | Route day patterns + conflict rules | 11 | Plan conflict tests |
| 13 | Mobile visit flow + offline sync engine | 11 | Sync adversarial suite |
| 14 | Sales orders + state machine + approval hooks | 10, 11 | Order E2E + workflow |
| 15 | Transactions + performance rollups | 14 | Metric formula tests |
| 16 | Targets + allocation cascade | 15 | Cascade sum validation |
| 17 | Marketing core: campaigns, POSM | 16 | Campaign state E2E |
| 18 | Trade execution + competitor capture | 13, 17 | Visit-linked execution |
| 19 | Inventory visibility + movements | 14 | Negative-stock rejection |
| 20 | Invoices, payments, outstanding | 15 | Aging + credit tests |
| 21 | Dashboards per Ch. 14 | 15–20 | p95 latency target |
| 22 | Report engine + 16 catalog reports + exports | 21 | Export scope audit |
| 23 | Notifications: events, channels, preferences | 14–20 | Delivery retry tests |
| 24 | Offline hardening + conflict UX | 13 | Field test script pass |
| 25 | SaaS billing: plans, subscription states, limits | 02 | Limit enforcement tests |
| 26 | Security hardening pass + dependency scan | all | Threat checklist closed |
| 27 | Performance: indexes, rollups, cache, partitioning | 21 | NFR suite green |
| 28 | Production cutover: IaC, backups, monitors, runbooks | 26, 27 | Restore drill pass |

---

## 11. Definition of Done (per module — spec §23.1)

1. Reversible, indexed DB migration.
2. Service + endpoints per API catalog with validation.
3. Authorization enforced: permission + tenant + scope on every path.
4. Tenant isolation verified by the parameterized suite.
5. Frontend screens with all five UI states.
6. Server-side pagination, sorting, filtering, search on every list.
7. Export inherits scope; export audited.
8. Audit events emitted for every mutation.
9. Notifications wired for the module's events.
10. Unit + integration + API + authorization + isolation tests green.
11. Module README updated (entities, endpoints, rules, flags).
12. Feature flag honored (module disable-safe).

**Discipline:** inspect before writing; shared-infra changes get a blast-radius list + regression run; one vertical module per pass; never silently change business rules (status vocabularies, metric formulas, permission codes are contracts).

---

## 12. Foundation Delivery — Build Steps 01–04 (this pass)

### Step 01 — Foundation
- [x] Solution + 4 backend projects (Clean Architecture) + 2 test projects
- [x] `Directory.Build.props`, `.editorconfig`, `.gitignore`
- [x] EF Core + Npgsql wiring, `ApplicationDbContext`, base entity
- [x] Generic repository + UnitOfWork abstractions (justified: reusable generic CRUD, spec §8/§36)
- [x] Central exception middleware + standardized API envelope
- [x] Config via `appsettings.*` + env; `.env.example`; no secrets in repo
- [x] Swagger, health checks, CORS for SPA
- [x] GitHub Actions CI; `docker-compose.yml` (postgres + api)
- [x] React SPA scaffold (Vite + TS) with dual-theme tokens + auth shell

### Step 02 — Multi-tenancy
- [ ] Entities: Tenant, Plan, Subscription, FeatureFlag, TenantFeature, TenantSetting
- [ ] EF configurations + initial migration
- [ ] Tenant provisioning, settings, feature-flag services + endpoints
- [ ] Seed: plans (FREE/STARTER/BUSINESS/ENTERPRISE) + feature flag catalog

### Step 03 — Authentication
- [ ] User entity + credential store; bcrypt hashing
- [ ] JWT access + rotating refresh token; login/logout/refresh
- [ ] Lockout with backoff; signed single-use password reset; MFA-ready model
- [ ] Auth middleware + `/api/v1/auth/*` endpoints; auth events audited

### Step 04 — RBAC
- [ ] Roles, permissions, role_permissions, user_roles, user_scopes
- [ ] Permission catalog seed (Appendix B) + role templates (Table 4.1)
- [ ] Permission-based authorization handler/policy; scope resolver
- [ ] RBAC services + endpoints; authorization matrix tests

**Out of this pass (later steps):** employees, organization, geography, distributors, outlets, products, field force, sales, targets, marketing, inventory, finance, dashboards, reports, notifications, offline sync, billing, production hardening.

---

## 13. Deferred Scope (spec Appendix D)

Warehouse ERP (bins/batches/expiry) · native mobile apps (PWA is v1) · route optimization/AI · scheme/trade-offer engine · locales beyond en/bn · tenant custom branding/domains · data-warehouse/BI extracts. The schema keeps extension columns (e.g. `batch_ref`) so nothing here needs a migration shock later.

---

## 14. How AGENTS.md Is Honored

- **§3 Architecture preservation** — Clean Architecture requested explicitly by the developer; documented here.
- **§5 thin endpoints / §6 application logic / §8 data access** — layer boundaries above.
- **§10–11 reuse over duplication** — one generic repository/service where genuinely generic; dedicated services only for real workflows.
- **§15 validation split**, **§17 tenant/scope server-side**, **§19 centralized errors**, **§21 config/secrets**, **§22 DTOs**, **§29–30 tests + real verification**, **§31 migration care** — enforced per module and in the DoD.
- **§37 no invented structure/APIs/tables** — every table, endpoint and permission code comes from the spec; none are fabricated.
- **§30/§42.13 never fabricate build/test results** — each pass reports exactly what was built, run and left unverified.

### Documented deviations

- **Build step 05 (Organization) — `employees` table included early.** The spec groups `employees` under Identity/master-data (§19.3, Ch. 6), but `employee_positions` carries a FK to `employees` (§19.4). Rather than create `employee_positions` without its referenced table, the spec's `employees` DDL, `Employee` entity, EF configuration and migration were delivered together with the Organization step. This keeps the migration runnable and FK-valid; no invented columns or tables were added. The Employees module (API/CRUD) remains a later step.
- **Spec-contractual DDL applied in step 05.** The Organization tables add `CHECK` constraints and spec-named partial indexes (`ix_positions_unit ... WHERE is_active`, `ix_emp_pos_employee`, `ix_org_units_tenant_parent`) exactly as the spec DDL defines, even though build steps 01–04 did not retrofit checks/filters onto earlier tables. Earlier tables were intentionally left untouched (no unrelated changes).

---

*End of BUILD_PLAN.md*
