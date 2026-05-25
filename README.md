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
