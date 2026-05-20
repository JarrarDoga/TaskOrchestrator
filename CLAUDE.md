@~/.claude/CLAUDE.md

## Project Goal

TaskOrchestrator is a Kanban board app with a Blazor WASM frontend (hosted on Vercel) and a .NET 10 minimal API backend (self-hosted on Hetzner VPS via Docker + Traefik). Auth is handled by Auth0 (SPA + custom API audience), data by PostgreSQL with Dapper, real-time by SignalR, emails by Resend.

## Stack

- **Frontend**: Blazor WASM → Vercel (`src/TaskOrchestrator.Client/`)
- **API**: .NET 10 minimal APIs → VPS Docker (`src/TaskOrchestrator.Api/`)
- **Shared**: DTOs + enums (`src/TaskOrchestrator.Shared/`)
- **DB**: PostgreSQL 17 — migrations at `db/migrations/` (applied manually on VPS)
- **Auth**: Auth0 domain `dev-1a2cx08ksfvxqxy0.us.auth0.com`, API audience `https://api.taskorchestrator.io`

## Key Infrastructure

- VPS: `5.161.194.50` — `ssh -i ~/.ssh/coolify_automation root@5.161.194.50`
- API at `/opt/taskorchestrator/` with docker-compose; Traefik routes `taskorchestrator.jdoga.works`
- Migrations NOT auto-applied on deploy — run them manually after schema changes

## Conventions

- Minimal API endpoints via `Map*Endpoints()` extension methods per feature folder
- Repositories use Dapper — no EF Core
- JWT claims: `sub` mapped via `NameClaimType`; `IUserContext` reads from `HttpContext.User`
- CORS `AllowedOrigins` must include `https://task-orchestrator-phi.vercel.app` in production config
- Production errors: `UseExceptionHandler` placed **after** `UseCors` so CORS headers are present on 500 responses
