# Core

The `Core` project is a class library that defines the foundational structures and contracts shared across all other layers in the solution.

## Responsibilities
- **Entities**: Plain Old CLR Objects (POCOs) that map to database tables via Entity Framework Core. Examples include `User`, `Document`, and `DocumentChunk`.
- **DTOs (Data Transfer Objects)**: Objects used to transfer data between layers (e.g., from controllers to views, or services to controllers) without exposing database entities directly.
  - *Result Pattern*: Includes `Result` and `Result<T>` classes to standardize service responses.
- **Configuration & Options**: Strongly typed configuration classes like `UploadOptions` that map to `appsettings.json`.
- **Enums & Constants**: Shared enums like `DocumentProcessingStatus` to track the RAG pipeline progress.

## Key Features
- **AI Integration Support**: Contains `DocumentChunk` which uses the `Pgvector.Vector` type (from the `Pgvector` NuGet package) to store vector embeddings for semantic search capabilities.

## Usage
- This project has no dependencies on BLL or DAL. It sits at the center of the architecture.
- Both the DAL, BLL, and PL reference the Core project.
