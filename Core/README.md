# Core

The `Core` project is a class library that defines the foundational structures and contracts shared across all layers of the solution. It has **no dependencies** on BLL, DAL, or PL.

---

## Responsibilities

| Folder | Contents |
|--------|----------|
| `Entities/` | Plain C# classes (POCOs) mapped to database tables via EF Core |
| `DTOs/` | Data Transfer Objects for moving data between layers without exposing entities |
| `Configuration/` | Strongly-typed options classes bound to `appsettings.json` |
| *(root)* | Shared enums (`UserRole`, `UserStatus`, `DocumentStatus`, `SubjectStatus`) |

---

## Key Entities

| Entity | Table | Notes |
|--------|-------|-------|
| `User` | `users` | Stores email as hash + AES-256 encrypted ciphertext |
| `Subject` | `subjects` | A course owned by a Lecturer |
| `Document` | `documents` | Metadata for an uploaded file (URL, status, uploader) |
| `DocumentChunk` | `document_chunks` | Text segment of a document; stores a `pgvector` embedding |

---

## Key DTOs

| Namespace | Purpose |
|-----------|---------|
| `DTOs/Auth/` | Login, register, change-password requests & responses |
| `DTOs/Documents/` | Upload, list, chunk view models |
| `DTOs/Subject/` | Subject create/list/detail transfer objects |
| `DTOs/Admin/` | Dashboard stats, user management |
| `DTOs/Common/` | `Result<T>` and `Result` for standardised service responses |

---

## Result Pattern

Services in BLL return `Result` or `Result<T>` instead of throwing exceptions for expected business errors:

```csharp
// Success
return Result<DocumentDto>.Success(dto);

// Failure (no exception thrown to PL)
return Result<DocumentDto>.Failure("File exceeds the allowed size limit.");
```

---

## Architectural Rule

> Core sits at the center. It has **zero** dependencies on BLL, DAL, or PL.  
> All other layers depend on Core.

```
PL  →  Core
BLL →  Core
DAL →  Core
```
