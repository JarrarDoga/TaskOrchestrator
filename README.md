# Task Orchestrator

Real-time collaborative kanban board built with Blazor WebAssembly, ASP.NET Core 10 Minimal API, SignalR, and MariaDB.

---

## Architecture Overview

```
taskOrchestrator/
├── src/
│   ├── TaskOrchestrator.Shared     # C# record DTOs + enums (no logic)
│   ├── TaskOrchestrator.Api        # ASP.NET Core 10 Minimal API + SignalR hub
│   └── TaskOrchestrator.Client     # Blazor WebAssembly SPA
```

### Key decisions

| Concern | Approach |
|---|---|
| API style | Minimal API with vertical slices under `Features/` |
| Real-time | SignalR — clients join per-board groups |
| Persistence | Dapper + raw SQL, no EF Core |
| Conflict resolution | Optimistic concurrency via `Version` column (HTTP 409 on mismatch) |
| Shared contracts | Immutable C# `record` types in `TaskOrchestrator.Shared` |
| Styling | Tailwind CSS (CDN for dev, compile in prod) |

---

## Local Development Setup

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [MariaDB 11.x](https://mariadb.org/download/) running on `localhost:3306`

### 1. Create the database

```sql
CREATE DATABASE task_orchestrator CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
```

Then run the schema from `db/schema.sql`:

```bash
mysql -u root -p task_orchestrator < db/schema.sql
```

### 2. Configure connection string

Edit `src/TaskOrchestrator.Api/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "MariaDb": "Server=localhost;Port=3306;Database=task_orchestrator;User=root;Password=yourpassword;"
  }
}
```

### 3. Configure the client API URL

Edit `src/TaskOrchestrator.Client/wwwroot/appsettings.json`:

```json
{
  "ApiBaseUrl": "http://localhost:5150"
}
```

---

## Running Locally

Open two terminals:

**Terminal 1 — API**
```bash
cd src/TaskOrchestrator.Api
dotnet run
# Listening on http://localhost:5150
```

**Terminal 2 — Client**
```bash
cd src/TaskOrchestrator.Client
dotnet run
# Listening on http://localhost:5173
```

Open `http://localhost:5173` in your browser.

---

## Project References

```
TaskOrchestrator.Api    → TaskOrchestrator.Shared
TaskOrchestrator.Client → TaskOrchestrator.Shared
```

---

## SignalR Hub

Hub URL: `http://localhost:5150/hubs/tasks`

| Client method | Triggered when |
|---|---|
| `TaskCreated(TaskItemDto)` | A new task is created on a board |
| `TaskUpdated(TaskItemDto)` | A task is edited or moved |
| `TaskDeleted({ taskId, boardId })` | A task is deleted |
| `BoardUpdated(BoardDto)` | A board is renamed |

Clients call `JoinBoard(boardId)` on connect and `LeaveBoard(boardId)` on disconnect to scope updates.
