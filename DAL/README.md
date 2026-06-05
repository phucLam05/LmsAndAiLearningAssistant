# Data Access Layer (DAL)

The `DAL` project is a class library responsible for all data persistence and external infrastructure interactions. It depends only on `Core`.

---

## Responsibilities

| Folder | Contents |
|--------|----------|
| `Data/` | `ApplicationDbContext` (EF Core DbContext), `AuditInterceptor` |
| `Repositories/` | Concrete EF Core implementations of repository interfaces |
| `Interfaces/` | Repository and Provider contracts consumed by BLL |
| `Providers/` | External infrastructure clients (Supabase Storage, Gemini Embedding) |
| `Migrations/` | EF Core migration history files |
| `DependencyInjection.cs` | Extension method `AddDataAccessLayer()` for `Program.cs` |

---

## Repositories

All repositories implement their corresponding interface from `DAL/Interfaces/` and use `ApplicationDbContext` internally. The BLL never accesses `ApplicationDbContext` directly — it always goes through an interface.

| Repository | Interface | Responsibility |
|------------|-----------|----------------|
| `UserRepository` | `IUserRepository` | User CRUD, email hash lookup, role management |
| `SubjectRepository` | `ISubjectRepository` | Subject CRUD, lecturer's subject list |
| `DocumentRepository` | `IDocumentRepository` | Document metadata, status updates, all-docs admin view |
| `DocumentChunkRepository` | `IDocumentChunkRepository` | Chunk insert/update/delete, pgvector cosine similarity search |

### Notable repository methods

- **`IDocumentChunkRepository.SearchSimilarChunksAsync`** — performs a pgvector cosine distance search filtered by subject and optionally by selected document IDs. Used by `ChatService` for RAG retrieval.
- **`IDocumentRepository.GetAllWithDetailsAsync`** — returns all documents across all subjects with `Subject` and `Uploader` navigation properties eagerly loaded. Used by `AdminService`.
- **`IDocumentChunkRepository.CountAllAsync`** — returns the total chunk count across the entire database. Used by `AdminService` for the dashboard stats widget.

---

## Providers

| Provider | Interface | Responsibility |
|----------|-----------|----------------|
| `SupabaseStorageProvider` | `ISupabaseStorageProvider` | Upload, download, delete, signed URL for Supabase Storage |
| `GeminiEmbeddingProvider` | `IGeminiEmbeddingProvider` | Calls Gemini `text-embedding-004` API and returns `float[]` vectors |

---

## Database Context

`ApplicationDbContext` configures:
- `pgvector` extension enablement on model creation
- Fluent API mappings for all entities (snake_case column names, indexes, foreign keys)
- `AuditInterceptor` to automatically set `CreatedAt` / `UpdatedAt` on save

---

## Running Migrations

From the solution root directory:

```bash
# Add a new migration after modifying Core entities
dotnet ef migrations add <MigrationName> --project DAL/DAL.csproj --startup-project PL/PL.csproj

# Apply pending migrations to the database
dotnet ef database update --project DAL/DAL.csproj --startup-project PL/PL.csproj
```

---

## Architectural Rule

> DAL depends only on `Core`.  
> BLL communicates with DAL **exclusively through interfaces** (`IUserRepository`, `IDocumentRepository`, etc.) — never through `ApplicationDbContext` directly.
