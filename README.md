# LmsAndAiLearningAssistant

Welcome to the **LmsAndAiLearningAssistant** project! This repository contains a full-stack solution utilizing ASP.NET Core and Entity Framework Core, integrated with AI capabilities (Gemini LLM) and PostgreSQL `pgvector`.

## Solution Architecture
This solution is organized as a multi-tier architecture to separate concerns and ensure maintainability:
- **PL (Presentation Layer)**: ASP.NET Core MVC project hosting controllers and views.
- **BLL (Business Logic Layer)**: Class library for services (Authentication, Business Logic, Encryption). 
  - *Result Pattern*: Services return `Result` or `Result<T>` instead of throwing exceptions for expected business logic errors.
  - *Strategy Pattern*: Document parsing is implemented using the Strategy pattern (`BLL/Strategies/DocumentParsing`) to dynamically support different file types.
- **DAL (Data Access Layer)**: Class library for repositories and database context (`Entity Framework Core`). Repositories are split by domain entity (e.g., `DocumentRepository`, `DocumentChunkRepository`).
- **Core**: Shared models/contracts, Data Transfer Objects, and Entities.

## Project Structure & Documentation
Each project contains its own `README.md` file detailing its specific responsibility and structure:
- [PL README](PL/README.md) - Learn about the frontend, controllers, and startup configurations.
- [BLL README](BLL/README.md) - Learn about the business logic and services.
- [DAL README](DAL/README.md) - Learn about database context, repositories, and entity mappings.
- [Core README](Core/README.md) - Learn about shared entities and DTOs.

## Security & Keys
- **Encryption Key**: The system uses AES-256 for encrypting sensitive data like emails. The encryption key must be exactly 32 bytes and is stored in `PL/appsettings.json` under `Security:EncryptionKey`.
- **Password Hashing**: Passwords are mathematically hashed using `BCrypt.Net-Next`.

## Environment Setup
### Prerequisites
- .NET 10.0 SDK
- PostgreSQL (running locally or accessible remotely)

### Configuration
1. Open `PL/appsettings.json`.
2. Configure your local database credentials inside `ConnectionStrings:DefaultConnection`. Example:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Host=localhost;Port=5432;Database=yourdatabase;Username=yourusername;Password=yourpassword"
   }
   ```

## Database Setup & Migration
The application uses PostgreSQL with the `pgvector` extension for storing and querying AI embeddings.

To apply migrations and update your database schema:
1. Open a terminal in the root directory.
2. Run the following commands:
   ```bash
   dotnet ef database update --project DAL\DAL.csproj --startup-project PL\PL.csproj
   ```

*Note: Ensure your PostgreSQL user has the necessary privileges to install the `vector` extension, as it is required by the `DocumentChunk` table.*
## Document Upload With Supabase Storage
Uploaded learning documents are stored in Supabase Storage, while only metadata is stored in the application database.

### Supabase Bucket
1. Create a Supabase Storage bucket named `documents`.
2. Keep the bucket private. Do not make it public.
3. Configure the bucket upload limit to 50MB.
4. Allow these common file families in the bucket policy when Supabase asks for MIME restrictions:
   ```text
   application/pdf
   application/msword
   application/vnd.openxmlformats-officedocument.wordprocessingml.document
   application/vnd.ms-powerpoint
   application/vnd.openxmlformats-officedocument.presentationml.presentation
   application/vnd.ms-excel
   application/vnd.openxmlformats-officedocument.spreadsheetml.sheet
   text/plain
   text/csv
   ```

### Supabase Configuration
Do not commit real keys. Configure these values in `PL/appsettings.json`, `PL/appsettings.Development.json`, or environment variables:

```json
"Supabase": {
  "Url": "https://YOUR_PROJECT_REF.supabase.co",
  "ServiceRoleKey": "YOUR_SUPABASE_SERVICE_ROLE_KEY",
  "Bucket": "documents"
},
"Upload": {
  "MaxFileSize": 52428800,
  "AllowedMimeTypes": {
    ".pdf": [ "application/pdf" ],
    ".docx": [ "application/vnd.openxmlformats-officedocument.wordprocessingml.document" ]
  }
}
```

The service role key is used only by backend services. It must never be exposed in views, JavaScript, or client-side code.

### Upload Flow
1. Log in to the MVC application.
2. Open `/Document`.
3. Choose a supported source file on `/Document/Upload`.
4. Submit the form.
5. The original file is uploaded to the private Supabase `documents` bucket using a GUID-based storage filename.
6. Metadata is saved to the `Documents` table with initial status `Uploaded` (0).
7. A Hangfire background job is enqueued from the Business Logic Layer (`DocumentService`) to parse and chunk the document (`ChunkingService`).
8. After chunking, a continuation job (`EmbeddingService`) creates vector embeddings using the Gemini API.
9. The document status transitions sequentially: `Uploaded` -> `Chunking` -> `Chunked` -> `Embedding` -> `Indexed` -> `Failed`.

**Resumption Logic:** If a document fails during chunking or embedding, the user can hit "Retry" in the UI. The request is validated by the BLL to ensure the user owns the document, and the pipeline is smart enough to check the current status and only resume work from where it failed, avoiding redundant processing and API costs.

*(Note: Background job enqueuing and ownership validation are strictly handled in the BLL to maintain a clean three-tier architecture.)*

If the Supabase upload succeeds but database save fails, the backend attempts to delete the uploaded object to avoid orphan files.

## AI Integration & Embeddings
This project utilizes the Gemini API to process documents and generate **Embeddings** for semantic search.

### What are Embeddings?
**Embedding is the process of transforming text (characters) into arrays of numbers (vectors).** - Instead of comparing each letter individually, AI and computers will use these vectors to understand the "semantic meaning" of the text.

- In this project, when a new document is created, the system will call the Gemini API to hash and encode the text into vectors.
- We then store these vectors in a PostgreSQL database via the `pgvector` extension to support intelligent search features later.