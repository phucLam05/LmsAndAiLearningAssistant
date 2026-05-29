# Presentation Layer (PL)

The `PL` project is an ASP.NET Core 10.0 MVC application. It serves as the main entry point of the LmsAndAiLearningAssistant solution.

## Responsibilities
- **Controllers**: Handle incoming HTTP requests, interact with the Business Logic Layer (BLL), and return responses or views.
- **Views**: Razor views (`.cshtml`) that provide the user interface.
- **Models**: View Models specifically designed for data transfer between Controllers and Views.
- **Configuration**: Contains `appsettings.json` for database connection strings and security keys.
- **Dependency Injection**: Wires up services from the BLL and repositories from the DAL in `Program.cs`.

### Controllers
- **AuthController**: Handles user authentication, registration, login/logout using cookie-based authentication.
- **DocumentController**: Manages file uploads, listing user documents, and triggering background jobs via Hangfire. Now leverages the `Result` pattern from the BLL to gracefully handle errors without throwing HTTP 500 exceptions.

### Dependencies
- **Hangfire**: Used for background job processing (document chunking and embedding). Resumption logic ensures that if a background job fails, retrying from the UI will smartly pick up from the point of failure.

## Key Files
- `Program.cs`: Application startup, DI container registration, Options binding (`UploadOptions`), middleware pipeline configuration, and Hangfire setup for background jobs.
- `appsettings.json`: Stores configuration variables such as `ConnectionStrings:DefaultConnection`, `Security:EncryptionKey`, and `Upload` block (for MaxFileSize and AllowedMimeTypes).

## Background Jobs (Hangfire)
- The Presentation Layer integrates **Hangfire** backed by PostgreSQL to process long-running tasks asynchronously (like document chunking). The Hangfire dashboard is mapped to `/hangfire`.

## Getting Started
To run this project:
1. Ensure the PostgreSQL database is configured correctly in `appsettings.json`.
2. Run the project using Visual Studio, Rider, or `dotnet run` in this directory.
