# HabitTracker — Claude Code Guide

## Project Structure
This is a monorepo with two projects:
- C:\HabitTracker — ASP.NET Core 8 backend
- C:\habit-tracker-admin — Next.js 14 admin panel

## Backend (HabitTracker)
- Framework: ASP.NET Core 8 Web API
- ORM: Entity Framework Core 8 (SQL Server)
- Auth: JWT Bearer + Refresh Tokens
- Architecture: Controllers → Services → DbContext (no repository layer)
- All responses use ApiResponse<T> envelope: { success, message, data }
- Turkish error messages throughout
- Rate limiting on all endpoints

## Running Backend
cd C:\HabitTracker
dotnet run
Swagger: http://localhost:8080/swagger

## Running Admin Panel
cd C:\habit-tracker-admin
npm run dev
Admin: http://127.0.0.1:3000/login

## Running with Docker
cd C:\HabitTracker
docker-compose up --build

## Running Tests
cd C:\HabitTracker.Tests
dotnet test

## Database Migrations
cd C:\HabitTracker
dotnet ef migrations add <MigrationName>
dotnet ef database update
Migrations apply automatically on dotnet run.

## Key Conventions
- All new endpoints must use ApiResponse<T>
- All Turkish error messages
- New services must be registered as Scoped in Program.cs
- Every new model needs a migration
- Rate limiting attribute required on all new controllers
- Users can only access their own data (always filter by userId from JWT)

## Environment Variables (appsettings.json)
- ConnectionStrings:DefaultConnection
- Jwt:Key, Jwt:Issuer, Jwt:Audience

## Admin Panel Conventions
- All API calls go through lib/api.ts
- adminApi for admin endpoints, authApi for auth
- All responses: response.data.data for payload
- TODO comments where mock data was replaced
