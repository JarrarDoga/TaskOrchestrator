# Task Orchestrator

![.NET](https://img.shields.io/badge/.NET_10-512BD4?style=flat&logo=dotnet&logoColor=white)
![Blazor](https://img.shields.io/badge/Blazor_WASM-512BD4?style=flat&logo=blazor&logoColor=white)
![SignalR](https://img.shields.io/badge/SignalR-real--time-blue?style=flat)
![MariaDB](https://img.shields.io/badge/MariaDB-003545?style=flat&logo=mariadb&logoColor=white)
![Auth0](https://img.shields.io/badge/Auth0-EB5424?style=flat&logo=auth0&logoColor=white)
![Deployed on Railway](https://img.shields.io/badge/API-Railway-0B0D0E?style=flat&logo=railway&logoColor=white)
![Deployed on Vercel](https://img.shields.io/badge/Client-Vercel-000000?style=flat&logo=vercel&logoColor=white)

A real-time collaborative Kanban board application. Multiple users can manage tasks simultaneously with live presence indicators, optimistic UI updates, and conflict resolution — all without a page refresh.

**Live site:** https://task-orchestrator-phi.vercel.app

---

## Features

### Boards and Columns
- Create and manage multiple boards per workspace
- Add, rename, recolor, reorder, and delete columns
- Column positions persist across sessions with full real-time sync
- "Add column" inline form directly on the board canvas

### Cards
- Create cards with title, description, and priority (Low / Medium / High / Critical)
- Drag-and-drop cards across columns with optimistic local state updates
- Edit card details in a slide-in drawer without leaving the board
- Delete cards with a two-step inline confirmation
- Priority badges with color coding (Critical, High, Medium, Low)
- Per-card activity timeline showing who did what and when

### File Attachments
- Upload files directly to cards (drag, click, or paste from clipboard)
- Authenticated downloads — attachments served with Bearer token injection so direct URL access is blocked
- File type icons for images, PDFs, documents, archives

### Real-time Collaboration
- SignalR hub scoped per board — all connected clients receive card, column, presence, and activity events instantly
- Live presence avatars in the top bar showing who is currently on the board (profile photo or initial fallback)
- Hover tooltip on each avatar showing the user's display name
- Connection state indicator (live / reconnecting / offline)
- Optimistic concurrency — card edits carry a `Version` token; server returns HTTP 409 on conflict with the current server snapshot so the user can review before retrying

### Board Access Management
- Invite code generation, copy, and revocation from the Home page
- Member list with roles (Owner, Member), last-seen timestamps, and profile avatars
- Board owners can kick members or transfer ownership

### Authentication
- Auth0 OIDC with Google social login
- JWT bearer tokens on all API endpoints
- Board membership enforced server-side on every request

---

## Architecture

```
taskOrchestrator/
├── src/
│   ├── TaskOrchestrator.Shared     # Shared C# record DTOs and enums (no logic)
│   ├── TaskOrchestrator.Api        # ASP.NET Core 10 Minimal API + SignalR hub
│   └── TaskOrchestrator.Client     # Blazor WebAssembly SPA
```

### Key decisions

| Concern | Approach |
|---|---|
| API style | Minimal API with vertical slices under `Features/` |
| Real-time | SignalR — clients join per-board groups, server broadcasts typed events |
| Persistence | Dapper + raw SQL against MariaDB, no EF Core |
| Conflict resolution | Optimistic concurrency via `Version` column (HTTP 409 on mismatch) |
| Shared contracts | Immutable C# `record` types in `TaskOrchestrator.Shared` |
| UI state | `BoardStateService` holds local board state; applies SignalR events and optimistic mutations |
| Styling | Tailwind CSS with custom dark design system (Manrope + Inter, Material Symbols) |

### SignalR events (client receives)

| Event | Payload |
|---|---|
| `CardCreated` | `CardDto` |
| `CardUpdated` | `CardDto` |
| `CardDeleted` | `{ cardId }` |
| `AttachmentAdded` | `{ cardId, attachment }` |
| `ActivityAppended` | `ActivityEventDto` |
| `PresenceChanged` | `BoardPresenceSnapshot` |
| `ColumnCreated` | `ColumnDto` |
| `ColumnUpdated` | `ColumnDto` |
| `ColumnDeleted` | `{ columnId }` |
| `MemberKicked` | `{ kickedUserId }` |

Clients call `JoinBoard(boardId, userId, displayName, avatarUrl)` on connect and `LeaveBoard(boardId)` on disconnect.

---

## Local Development

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [MariaDB 11.x](https://mariadb.org/download/) running locally
- An [Auth0](https://auth0.com) application (Single Page Application + API)

### 1. Database

```sql
CREATE DATABASE task_orchestrator CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
```

```bash
mysql -u root -p task_orchestrator < db/schema.sql
```

### 2. API configuration

Edit `src/TaskOrchestrator.Api/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "MariaDb": "Server=localhost;Port=3306;Database=task_orchestrator;User=root;Password=YOUR_LOCAL_PASSWORD;"
  },
  "Auth0": {
    "Domain": "YOUR_AUTH0_DOMAIN",
    "Audience": "YOUR_AUTH0_API_IDENTIFIER"
  },
  "AllowedOrigins": ["http://localhost:5035"]
}
```

### 3. Client configuration

Edit `src/TaskOrchestrator.Client/wwwroot/appsettings.json`:

```json
{
  "ApiBaseUrl": "http://localhost:5150",
  "Auth0": {
    "Authority": "https://YOUR_AUTH0_DOMAIN",
    "ClientId": "YOUR_AUTH0_CLIENT_ID",
    "Audience": "YOUR_AUTH0_API_IDENTIFIER"
  }
}
```

### 4. Run

```bash
# Terminal 1 — API
cd src/TaskOrchestrator.Api
dotnet run
# Listening on http://localhost:5150

# Terminal 2 — Client
cd src/TaskOrchestrator.Client
dotnet run
# Listening on http://localhost:5035
```

---

## Deployment

The live application is deployed as two independent services:

| Service | Platform | Notes |
|---|---|---|
| API | [Railway](https://railway.app) | Connection string and Auth0 config via environment variables |
| Client | [Vercel](https://vercel.com) | Static Blazor WASM output; `appsettings.json` points to Railway API URL |
