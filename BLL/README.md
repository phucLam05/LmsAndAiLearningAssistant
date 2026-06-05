# Business Logic Layer (BLL)

The `BLL` project is a class library containing all business rules, orchestration, and service logic. It sits between the Presentation Layer and the Data Access Layer, communicating with each exclusively through interfaces.

---

## Responsibilities

| Folder | Contents |
|--------|----------|
| `Services/` | Concrete service implementations |
| `Interfaces/` | Service contracts consumed by PL controllers |
| `Strategies/DocumentParsing/` | Pluggable document parsers (Strategy pattern) |
| `DependencyInjection.cs` | Extension method `AddBusinessLogicLayer()` for `Program.cs` |

---

## Services

| Service | Interface | Responsibility |
|---------|-----------|----------------|
| `AuthService` | `IAuthService` | Login validation, password hashing (BCrypt), email encryption/decryption |
| `UserService` | `IUserService` | User profile management, role-based access |
| `SubjectService` | `ISubjectService` | Subject CRUD, lecturer ownership checks |
| `DocumentService` | `IDocumentService` | Upload validation, Supabase coordination, Hangfire job enqueuing |
| `ChunkingService` | `IChunkingService` | Background job: download → parse → chunk → save (Hangfire) |
| `DocumentEmbeddingService` | `IEmbeddingService` | Background job: chunk → embed via Gemini → save pgvector (Hangfire) |
| `ChatService` | `IChatService` | RAG chatbot: embed query → similarity search → Gemini generation |
| `AdminService` | `IAdminService` | User management, role changes, dashboard statistics |
| `EmailService` | `IEmailService` | Email sending via SMTP |

---

## Architectural Patterns

### Repository Pattern (Strict 3-tier compliance)
BLL services depend **only on DAL interfaces** — never on `ApplicationDbContext` directly:

```csharp
// ✅ Correct — through interface
public AdminService(IUserRepository userRepository, IDocumentRepository documentRepository, ...)

// ❌ Wrong — bypasses DAL abstraction
public AdminService(ApplicationDbContext dbContext, ...)
```

### Result Pattern
Services return `Result` or `Result<T>` instead of throwing exceptions for predictable failures:

```csharp
// In DocumentService
var subject = await _subjectRepository.GetByIdAsync(uploadDto.SubjectId);
if (subject == null)
    return Result<DocumentDto>.Failure("Subject does not exist.");

return Result<DocumentDto>.Success(MapDocument(document));
```

### Strategy Pattern (Document Parsing)
`ChunkingService` uses `IDocumentParser` to dynamically select the correct parser:

| Parser | Handles |
|--------|---------|
| `PdfParser` | `.pdf` files via PdfPig |
| `WordParser` | `.docx` files via DocumentFormat.OpenXml |
| `PowerPointParser` | `.pptx` files via DocumentFormat.OpenXml |
| `FallbackTextParser` | `.txt`, `.csv`, and unknown types |

### Resumption Logic (Idempotent Background Jobs)
If a background job fails partway through:
- **Retry from UI** → checks `HasChunksAsync()` first
- If chunks exist → skips Chunking, re-runs only Embedding
- If no chunks → runs full pipeline from scratch

This avoids redundant API calls and prevents unnecessary Gemini usage costs.

---

## RAG Chatbot Flow

```
Student query
    │
    ▼
IGeminiEmbeddingProvider.GetEmbeddingAsync()     ← Gemini Embedding API
    │
    ▼
IDocumentChunkRepository.SearchSimilarChunksAsync()  ← pgvector cosine distance
    │  (filtered by selected document IDs if user chose specific docs)
    ▼
Top 5 matching chunks → build prompt
    │
    ▼
Gemini generateContent API
    │
    ▼
Answer returned to student
```

---

## Architectural Rule

> BLL depends on DAL **interfaces** and Core only.  
> BLL must **never** reference `ApplicationDbContext`, `DAL.Data`, or any EF Core `DbSet<T>` directly.  
> PL depends on BLL **interfaces** only.
