# LmsAndAiLearningAssistant

Welcome to the **LmsAndAiLearningAssistant** project! This repository contains a full-stack solution utilizing ASP.NET Core and Entity Framework Core, integrated with AI capabilities (Gemini LLM) and PostgreSQL `pgvector`.

## Solution Architecture
This solution is organized as a multi-tier architecture to separate concerns and ensure maintainability:
- **PL (Presentation Layer)**: ASP.NET Core MVC project hosting controllers and views.
- **BLL (Business Logic Layer)**: Class library for services (Authentication, Business Logic, Encryption).
- **DAL (Data Access Layer)**: Class library for repositories and database context (`Entity Framework Core`).
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
4. Allow these MIME types:
   ```text
   application/pdf
   application/vnd.openxmlformats-officedocument.wordprocessingml.document
   application/vnd.openxmlformats-officedocument.presentationml.presentation
   ```

### Supabase Configuration
Do not commit real keys. Configure these values in `PL/appsettings.json`, `PL/appsettings.Development.json`, or environment variables:

```json
"Supabase": {
  "Url": "YOUR_SUPABASE_URL",
  "ServiceRoleKey": "YOUR_SUPABASE_SERVICE_ROLE_KEY",
  "Bucket": "documents"
}
```

The service role key is used only by backend services. It must never be exposed in views, JavaScript, or client-side code.

### Upload Flow
1. Log in to the MVC application.
2. Open `/Document`.
3. Choose a PDF, DOCX, or PPTX file on `/Document/Upload`.
4. Submit the form.
5. The original file is uploaded to the private Supabase `documents` bucket using a GUID-based storage filename.
6. Metadata is saved to the `Documents` table with initial status `uploaded`.

If the Supabase upload succeeds but database save fails, the backend attempts to delete the uploaded object to avoid orphan files.

## AI Integration & Embeddings
This project utilizes the Gemini API to process documents and generate **Embeddings** for semantic search.

  
### What are Embeddings?
**Embedding là quá trình biến đổi văn bản (chữ) thành các mảng con số (vector).** - Thay vì so sánh từng chữ cái, AI và máy tính sẽ sử dụng các vector này để hiểu "ý nghĩa ngữ nghĩa" của đoạn văn.
- Trong dự án này, khi có một tài liệu mới, hệ thống sẽ gọi Gemini API để băm nhỏ và mã hóa văn bản thành vector.
- Sau đó, chúng ta lưu trữ các vector này vào database PostgreSQL thông qua extension `pgvector` để phục vụ cho các tính năng tìm kiếm thông minh sau này.
