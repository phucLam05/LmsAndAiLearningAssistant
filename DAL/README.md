# Data Access Layer (DAL)

The `DAL` project is a class library responsible for all database interactions.

## Responsibilities
- **Database Context**: Contains the `ApplicationDbContext` which inherits from `DbContext` (Entity Framework Core). This file defines the tables and relationships (using Fluent API).
- **Repositories**: Implements the Repository pattern. Repositories abstract the data source so that the Business Logic Layer doesn't need to know whether the data comes from a database or an API.
- **Interfaces**: Contracts for the repositories to ensure loose coupling.
- **Migrations**: Stores EF Core migration scripts that track changes to the database schema over time.

## Key Configurations
- **PostgreSQL**: The project uses `Npgsql.EntityFrameworkCore.PostgreSQL`.
- **Pgvector**: For AI-powered vector similarity search, `Pgvector.EntityFrameworkCore` is installed, and the `ApplicationDbContext` enables the `vector` extension.

## Running Migrations
When entities in the `Core` project are modified, a new migration should be created.
Run this from the solution root:
```bash
dotnet ef migrations add <MigrationName> --project DAL\DAL.csproj --startup-project PL\PL.csproj
dotnet ef database update --project DAL\DAL.csproj --startup-project PL\PL.csproj
```
