# HabitTracker

A full-stack habit tracking application with an ASP.NET Core 8 REST API and a Next.js admin dashboard.

[![Backend](https://github.com/<your-username>/HabitTracker/actions/workflows/backend.yml/badge.svg)](https://github.com/<your-username>/HabitTracker/actions/workflows/backend.yml)
[![Admin](https://github.com/<your-username>/HabitTracker/actions/workflows/admin.yml/badge.svg)](https://github.com/<your-username>/HabitTracker/actions/workflows/admin.yml)
[![Docker](https://github.com/<your-username>/HabitTracker/actions/workflows/docker.yml/badge.svg)](https://github.com/<your-username>/HabitTracker/actions/workflows/docker.yml)

## Tech Stack

- **Backend:** ASP.NET Core 8, Entity Framework Core 8, SQL Server
- **Auth:** JWT Bearer + Refresh Tokens, BCrypt
- **Admin UI:** Next.js 14, TypeScript
- **Testing:** xUnit, Moq, FluentAssertions, EF Core SQLite (in-memory)
- **Infrastructure:** Docker, Docker Compose

## Run Locally

### Backend API

```bash
cd HabitTracker
dotnet run
```

API: http://localhost:8080  
Swagger: http://localhost:8080/swagger

### Admin Dashboard

```bash
cd habit-tracker-admin
npm install
npm run dev
```

Dashboard: http://localhost:3000

## Docker

Build and start both the API and SQL Server together:

```bash
docker-compose up --build
```

Stop containers:

```bash
docker-compose down
```

Remove containers and database volume:

```bash
docker-compose down -v
```

## Tests

```bash
dotnet test HabitTracker.Tests/HabitTracker.Tests.csproj --verbosity normal
```

22 tests across auth, habit CRUD, and streak calculation.
