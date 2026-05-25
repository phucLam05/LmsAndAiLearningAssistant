# Presentation Layer (PL)

The `PL` project is an ASP.NET Core 10.0 MVC application. It serves as the main entry point of the LmsAndAiLearningAssistant solution.

## Responsibilities
- **Controllers**: Handle incoming HTTP requests, interact with the Business Logic Layer (BLL), and return responses or views.
- **Views**: Razor views (`.cshtml`) that provide the user interface.
- **Models**: View Models specifically designed for data transfer between Controllers and Views.
- **Configuration**: Contains `appsettings.json` for database connection strings and security keys.
- **Dependency Injection**: Wires up services from the BLL and repositories from the DAL in `Program.cs`.

## Key Files
- `Program.cs`: Application startup, DI container registration, middleware pipeline configuration.
- `appsettings.json`: Stores configuration variables such as `ConnectionStrings:DefaultConnection` and `Security:EncryptionKey`.

## Getting Started
To run this project:
1. Ensure the PostgreSQL database is configured correctly in `appsettings.json`.
2. Run the project using Visual Studio, Rider, or `dotnet run` in this directory.
