# Presentation Layer (PL)

The `PL` project is an **ASP.NET Core 10.0 MVC** application. It serves as the entry point of the LmsAndAiLearningAssistant solution, handling all HTTP requests and rendering views for the user interface.

---

## Responsibilities

| Folder | Contents |
|--------|----------|
| `Controllers/` | HTTP request handlers — interact with BLL via injected interfaces only |
| `Views/` | Razor `.cshtml` views organized by controller |
| `Models/` | View models for data binding between Controllers and Views |
| `wwwroot/` | Static assets (CSS, JavaScript, fonts, images) |
| `Properties/` | `launchSettings.json` for dev server configuration |

---

## Controllers

| Controller | Accessible by | Responsibility |
|------------|--------------|----------------|
| `AuthController` | Anonymous | Login, logout, registration |
| `HomeController` | All authenticated | Root redirect based on role |
| `SubjectController` | Lecturer, Admin | Subject CRUD, document listing, AI chat interface |
| `DocumentController` | Lecturer, Admin | Upload, delete, retry processing, download, chunk viewer |
| `AdminController` | Admin only | Dashboard stats, user management, document oversight |
| `LecturerController` | Lecturer | Lecturer-specific utilities (portal redirect removed) |

> **Architecture rule:** Controllers must only inject **BLL interfaces** (e.g., `IDocumentService`, `ISubjectService`). Direct use of `ApplicationDbContext`, DAL repositories, or provider classes in Controllers is **not permitted**.

---

## Role-based Routing

| Role | Login redirect |
|------|---------------|
| Admin | `Admin/Index` (Dashboard) |
| Lecturer | `Subject/MySubjects` (Subject list) |
| Student | `Subject/Index` (Subject list) |

---

## Key Files

| File | Purpose |
|------|---------|
| `Program.cs` | Application startup: DI wiring (`AddDataAccessLayer`, `AddBusinessLogicLayer`), cookie auth, Hangfire, middleware pipeline |
| `appsettings.json` | Connection strings, security keys, Supabase config, Gemini API key, upload options |
| `DbSeeder.cs` | Startup utility that seeds initial Admin user if none exists |

---

## Dependency Injection

Services are registered via extension methods in `Program.cs`:

```csharp
builder.Services.AddDataAccessLayer(builder.Configuration);  // DAL repositories + providers
builder.Services.AddBusinessLogicLayer();                     // BLL services
```

This keeps DI configuration organized and each layer self-contained.

---

## Background Jobs (Hangfire)

Hangfire is backed by PostgreSQL and processes:
- **`ChunkingService.ProcessFileChunkingAsync`** — triggered when a document is uploaded
- **`DocumentEmbeddingService.ProcessEmbeddingsAsync`** — continuation job after chunking completes

The Hangfire dashboard is available at `/hangfire` (requires Admin role).

---

## Authentication

Cookie-based authentication with roles (`Admin`, `Lecturer`, `Student`). Session expires after 30 days (sliding). Unauthorized access is redirected to `/Auth/Login`.

---

## Getting Started

```bash
# Ensure appsettings.json is configured (DB, Supabase, Gemini keys)
# Then from the solution root:
dotnet run --project PL/PL.csproj
```

Default dev URL: `https://localhost:5001` (see `launchSettings.json`).
