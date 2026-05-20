# Task Orchestrator

![.NET](https://img.shields.io/badge/.NET_10-512BD4?style=flat&logo=dotnet&logoColor=white)
![Blazor](https://img.shields.io/badge/Blazor_WASM-512BD4?style=flat&logo=blazor&logoColor=white)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL_17-4169E1?style=flat&logo=postgresql&logoColor=white)
![SignalR](https://img.shields.io/badge/SignalR-real--time-blue?style=flat)
![Auth0](https://img.shields.io/badge/Auth0-EB5424?style=flat&logo=auth0&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-2496ED?style=flat&logo=docker&logoColor=white)
![Deployed on Vercel](https://img.shields.io/badge/Client-Vercel-000000?style=flat&logo=vercel&logoColor=white)

A real-time collaborative Kanban board application. Multiple users can manage tasks simultaneously with live presence indicators, optimistic UI updates, and conflict resolution — all without a page refresh.

**Live site:** https://task-orchestrator-phi.vercel.app

---

## Overview

Task Orchestrator is a full-stack project management tool built for teams that need live collaboration. Cards move across columns in real time, presence avatars show who is on the board, and every change is reflected instantly across all connected clients via SignalR. The backend is a self-hosted .NET 10 minimal API running on a VPS behind Traefik; the frontend is a Blazor WebAssembly SPA deployed on Vercel.

---

## Features

### Boards and Columns
- Create and manage multiple boards per workspace
- Add, rename, recolor, reorder, and delete columns
- Column positions persist across sessions with full real-time sync

### Cards
- Create cards with title, description, and priority (Low / Medium / High / Critical)
- Drag-and-drop cards across columns with optimistic local state
- Edit card details in a slide-in drawer without leaving the board
- Delete cards with a two-step inline confirmation
- Per-card activity timeline showing who did what and when

### File Attachments
- Upload files directly to cards (drag, click, or paste from clipboard)
- Authenticated downloads — attachments served via Bearer token injection; direct URL access is blocked
- File type icons for images, PDFs, documents, and archives

### Real-time Collaboration
- SignalR hub scoped per board — all connected clients receive card, column, presence, and activity events instantly
- Live presence avatars showing who is currently on the board (profile photo or initial fallback)
- Connection state indicator (live / reconnecting / offline)
- Optimistic concurrency — card edits carry a `Version` token; server returns HTTP 409 on conflict with the current server snapshot so the user can review before retrying

### Board Access Management
- Invite code generation, copy, and revocation
- Member list with roles (Owner, Member), last-seen timestamps, and profile avatars
- Board owners can remove members or transfer ownership

### Authentication and Email
- Auth0 OIDC with Google social login
- JWT bearer tokens validated on all API endpoints; board membership enforced server-side on every request
- Transactional emails via Resend (invite notifications, account events)

---

## Tech Stack

| Layer | Technology |
|---|---|
| Backend | .NET 10 Minimal API (ASP.NET Core) |
| Frontend | Blazor WebAssembly |
| Shared types | C# class library (DTOs and enums) |
| Real-time | ASP.NET Core SignalR |
| Database | PostgreSQL 17 |
| Data access | Dapper (no EF Core) |
| Auth | Auth0 (SPA + custom API audience) |
| Email | Resend |
| Reverse proxy | Traefik (on VPS) |
| Containers | Docker / GHCR |
| Frontend hosting | Vercel |
| CI/CD | GitHub Actions |

---

## Architecture

```
TaskOrchestrator/
├── src/
│   ├── TaskOrchestrator.Shared/    # Shared C# record DTOs and enums (no logic)
│   ├── TaskOrchestrator.Api/       # ASP.NET Core 10 Minimal API + SignalR hub
│   └── TaskOrchestrator.Client/    # Blazor WebAssembly SPA
├── db/
│   └── migrations/                 # SQL migration scripts
└── .github/
    └── workflows/
        ├── ci.yml                  # Lint, build, test, Docker build validation
        └── deploy.yml              # Push API image to GHCR + deploy API to VPS + client to Vercel
```

The API and client are deployed independently. The Blazor WASM bundle is a static artifact — Vercel serves it globally and it connects to the self-hosted API at runtime. SignalR long-polling and WebSocket connections go directly to the API; the API container is attached to the Coolify Docker network and routed by Traefik on the VPS.

---

## Deployment

The application runs as two independent services:

| Service | Platform | Notes |
|---|---|---|
| API | Self-hosted VPS (Coolify-managed Docker + Traefik) | Image published to GHCR; container runs via Docker Compose on VPS and is connected to the Coolify network for Traefik routing |
| Client | [Vercel](https://vercel.com) | Static Blazor WASM output; `appsettings.json` points to the VPS API URL |

### API environment variables (production)

| Variable | Description |
|---|---|
| `ConnectionStrings__Postgres` | PostgreSQL connection string |
| `Auth0__Domain` | Auth0 tenant domain |
| `Auth0__Audience` | Custom API audience identifier |
| `Resend__ApiKey` | Resend API key |
| `Resend__FromEmail` | Sender email for invite/account messages |
| `Resend__FromName` | Sender display name |
| `InviteEmail__ClientBaseUrl` | Public client URL used in invite links |

---

## CI/CD

GitHub Actions uses two main workflows:

**`ci.yml`** — triggered on every push and pull request:
1. Restore dependencies and build all projects
2. Run the test suite
3. Build the Docker image for `TaskOrchestrator.Api` (validation only)

**`deploy.yml`** — triggered on pushes to `main`:
1. Build, test, and verify
2. Build and push the API image to GHCR
3. SSH into the VPS, update `docker-compose.yml`, pull the new API image, and restart services
4. Connect the API container to the Coolify Docker network for Traefik routing
5. Deploy the Blazor WASM client to Vercel using the Vercel CLI

Secrets (`GHCR_TOKEN`, `VPS_SSH_KEY`, `VERCEL_TOKEN`, etc.) are stored in GitHub repository secrets.

---

## License

MIT
