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
        ├── ci.yml                  # Build, test, Docker image push to GHCR
        └── deploy.yml              # Deploy API to VPS + client to Vercel on push to main
```

The API and client are deployed independently. The Blazor WASM bundle is a static artifact — Vercel serves it globally and it connects to the self-hosted API at runtime. SignalR long-polling and WebSocket connections go directly to the API; Traefik handles TLS termination and routing on the VPS.

---

## Local Development

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org) (for Vercel CLI, optional)
- [Docker](https://www.docker.com) and Docker Compose
- An [Auth0](https://auth0.com) tenant with an SPA application and a custom API configured
- A [Resend](https://resend.com) API key

### 1. Start PostgreSQL

```bash
docker compose up -d db
```

### 2. Apply database migrations

```bash
# Run the SQL scripts in db/migrations/ in order against your local PostgreSQL instance
psql -U postgres -d taskorch -f db/migrations/001_initial.sql
# ... repeat for subsequent migrations
```

### 3. Configure the API

Create `src/TaskOrchestrator.Api/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Database=taskorch;Username=postgres;Password=postgres"
  },
  "Auth0": {
    "Domain": "your-tenant.auth0.com",
    "Audience": "https://your-api-audience"
  },
  "Resend": {
    "ApiKey": "re_..."
  }
}
```

### 4. Configure the client

Edit `src/TaskOrchestrator.Client/wwwroot/appsettings.Development.json`:

```json
{
  "ApiBaseUrl": "https://localhost:5001",
  "Auth0": {
    "Domain": "your-tenant.auth0.com",
    "ClientId": "your-spa-client-id",
    "Audience": "https://your-api-audience"
  }
}
```

### 5. Run

```bash
# Terminal 1 — API
dotnet run --project src/TaskOrchestrator.Api

# Terminal 2 — Client
dotnet run --project src/TaskOrchestrator.Client
```

The client will be available at `https://localhost:5002` and the API at `https://localhost:5001`.

---

## Deployment

The application runs as two independent services:

| Service | Platform | Notes |
|---|---|---|
| API | Self-hosted VPS (Docker + Traefik) | Image published to GHCR; Traefik handles TLS and routing |
| Client | [Vercel](https://vercel.com) | Static Blazor WASM output; `appsettings.json` points to the VPS API URL |

### API environment variables (production)

| Variable | Description |
|---|---|
| `ConnectionStrings__Default` | PostgreSQL connection string |
| `Auth0__Domain` | Auth0 tenant domain |
| `Auth0__Audience` | Custom API audience identifier |
| `Resend__ApiKey` | Resend API key |

---

## CI/CD

GitHub Actions runs two workflows on push to `main`:

**`ci.yml`** — triggered on every push and pull request:
1. Restore dependencies and build all projects
2. Run the test suite
3. Build the Docker image for `TaskOrchestrator.Api`
4. Push the image to GitHub Container Registry (GHCR)

**`deploy.yml`** — triggered after `ci.yml` succeeds on `main`:
1. SSH into the VPS and pull the new image from GHCR
2. Restart the API container via Docker Compose
3. Deploy the Blazor WASM client to Vercel using the Vercel CLI

Secrets (`GHCR_TOKEN`, `VPS_SSH_KEY`, `VERCEL_TOKEN`, etc.) are stored in GitHub repository secrets.

---

## License

MIT
