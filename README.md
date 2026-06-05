# LmsAndAiLearningAssistant

A **Learning Management System** with an integrated AI Learning Assistant built on ASP.NET Core 10.0. Lecturers can upload course documents and students can interact with an AI chatbot (RAG-based) that answers questions strictly based on those documents.

---

## Solution Architecture

This solution follows a strict **3-Tier Architecture** to separate concerns and ensure maintainability:

```
┌─────────────────────────────────────────────────────────────────┐
│  PL — Presentation Layer (ASP.NET Core MVC)                    │
│  Controllers · Views · ViewModels · Hangfire Dashboard         │
└───────────────────────────┬─────────────────────────────────────┘
                            │ depends on (via Interfaces only)
┌───────────────────────────▼─────────────────────────────────────┐
│  BLL — Business Logic Layer (Class Library)                    │
│  Services · Interfaces · Strategies · Result Pattern           │
└───────────────────────────┬─────────────────────────────────────┘
                            │ depends on (via Interfaces only)
┌───────────────────────────▼─────────────────────────────────────┐
│  DAL — Data Access Layer (Class Library)                       │
│  Repositories · Providers · ApplicationDbContext · Migrations  │
└───────────────────────────┬─────────────────────────────────────┘
                            │ depends on
┌───────────────────────────▼─────────────────────────────────────┐
│  Core — Shared Contracts (Class Library)                       │
│  Entities · DTOs · Enums · Configuration Options               │
└─────────────────────────────────────────────────────────────────┘
```

### Layer rules
| Layer | May depend on | Must NOT depend on |
|-------|---------------|--------------------|
| PL | BLL Interfaces, Core | DAL, ApplicationDbContext |
| BLL | DAL Interfaces, Core | ApplicationDbContext directly, PL |
| DAL | Core | BLL, PL |
| Core | *(nothing)* | BLL, DAL, PL |

---

## Project Structure & Documentation

Each project contains its own `README.md` file with details:
- [PL README](PL/README.md) — Controllers, Views, Hangfire, startup configuration
- [BLL README](BLL/README.md) — Services, Interfaces, business rules
- [DAL README](DAL/README.md) — Repositories, Providers, EF Core context
- [Core README](Core/README.md) — Entities, DTOs, enums, configuration options

---

## Key Features

| Feature | Description |
|---------|-------------|
| Role-based Access | Admin, Lecturer, Student roles with cookie authentication |
| Document Upload | Supabase Storage backend, private bucket, GUID filenames |
| RAG AI Chatbot | Select specific documents → AI answers based only on those |
| pgvector Search | Cosine similarity search via PostgreSQL `pgvector` extension |
| Background Jobs | Hangfire processes document chunking + embedding asynchronously |
| Result Pattern | Services return `Result<T>` — no unhandled exceptions in BLL |
| Strategy Pattern | Pluggable document parsers: PDF, DOCX, PPTX, XLSX, TXT |

---

## Security & Keys

- **Email Encryption**: AES-256. The encryption key (exactly 32 bytes) is stored in `appsettings.json` under `Security:EncryptionKey`. Never commit real keys.
- **Password Hashing**: `BCrypt.Net-Next`.
- **Supabase Key**: Service Role Key is used only by backend. Never expose in views, JavaScript, or client-side code.

---

## Environment Setup

### Prerequisites
- .NET 10.0 SDK
- PostgreSQL with `pgvector` extension available
- A [Supabase](https://supabase.com/) project (for file storage)

### Configuration
Open `PL/appsettings.json` and fill in the following sections:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=lms_db;Username=postgres;Password=yourpassword"
  },
  "Security": {
    "EncryptionKey": "your-32-byte-aes-key-here!!!!!"
  },
  "Supabase": {
    "Url": "https://YOUR_PROJECT_REF.supabase.co",
    "ServiceRoleKey": "YOUR_SUPABASE_SERVICE_ROLE_KEY",
    "Bucket": "documents"
  },
  "GeminiSettings": {
    "ApiKey": "YOUR_GEMINI_API_KEY",
    "BaseUrl": "https://generativelanguage.googleapis.com/v1beta/",
    "EmbeddingModel": "text-embedding-004"
  },
  "Upload": {
    "MaxFileSize": 52428800,
    "AllowedMimeTypes": {
      ".pdf":  ["application/pdf"],
      ".docx": ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"],
      ".pptx": ["application/vnd.openxmlformats-officedocument.presentationml.presentation"],
      ".xlsx": ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"],
      ".txt":  ["text/plain"]
    }
  }
}
```

---

## Database Setup & Migrations

The application uses PostgreSQL with the `pgvector` extension for storing AI vector embeddings.

```bash
# From the solution root directory:

# Create a new migration (when you modify Core entities)
dotnet ef migrations add <MigrationName> --project DAL/DAL.csproj --startup-project PL/PL.csproj

# Apply migrations to your database
dotnet ef database update --project DAL/DAL.csproj --startup-project PL/PL.csproj
```

> **Note:** Ensure your PostgreSQL user has privileges to install the `vector` extension, which is required by the `DocumentChunks` table.

---

## Supabase Storage Setup

1. Create a Supabase Storage bucket named `documents`.
2. Keep the bucket **private** (do not make it public).
3. Set the upload size limit to **50 MB**.
4. Allow the following MIME types in bucket policy:
   ```
   application/pdf
   application/msword
   application/vnd.openxmlformats-officedocument.wordprocessingml.document
   application/vnd.ms-powerpoint
   application/vnd.openxmlformats-officedocument.presentationml.presentation
   application/vnd.ms-excel
   application/vnd.openxmlformats-officedocument.spreadsheetml.sheet
   text/plain
   ```

---

## Document Processing Pipeline

When a document is uploaded, it goes through an automated background pipeline:

```
Upload → [Pending] → ChunkingService → [Processing] → EmbeddingService → [Success]
                                                                        ↘ [Failed]
```

| Step | Status | Description |
|------|--------|-------------|
| Upload | `Pending` | File saved to Supabase; metadata written to DB; Hangfire job enqueued |
| Chunking | `Processing` | File downloaded from Supabase, parsed by file-type strategy, split into text chunks |
| Embedding | `Processing` | Each chunk is sent to Gemini Embedding API; vectors stored in `pgvector` |
| Done | `Success` | Document fully indexed and available for AI chat |
| Error | `Failed` | Any step can fail; user can retry from the UI |

**Resumption Logic:** Retry is smart — if chunks already exist, only the embedding step is re-run, avoiding redundant API calls.

**Cleanup Logic:** If Supabase upload succeeds but DB save fails, the uploaded file is automatically deleted to prevent orphan objects.

---

## AI Chatbot (RAG)

The chatbot uses **Retrieval-Augmented Generation (RAG)**:

1. Student selects which documents to include in the AI context (similar to NotebookLM).
2. The student's question is converted to a vector embedding via Gemini API.
3. The system performs a **cosine similarity search** (pgvector) against the selected document chunks.
4. The top 5 most relevant chunks are included in the prompt sent to Gemini.
5. Gemini generates an answer grounded strictly in those course materials.

> If no documents are selected, all indexed documents in the subject are searched. If none match, the AI politely responds that no course materials were found.

---

## Running the Application

```bash
# From the solution root:
dotnet run --project PL/PL.csproj
```

The app starts at `https://localhost:5001` (or the port configured in `PL/Properties/launchSettings.json`).

The Hangfire dashboard is available at `/hangfire` (Admin role required).