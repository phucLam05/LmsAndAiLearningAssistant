# LmsAndAiLearningAssistant

## Architecture
This solution is organized as a three-layer MVC architecture:

- **PL** (Presentation Layer): ASP.NET Core MVC project (`net10.0`) hosting controllers and views.
- **BAL** (Business Logic Layer): class library (`net48`) for services.
- **DAL** (Data Access Layer): class library (`net48`) for repositories.
- **Core**: shared models/contracts (`net48`).

## Project References
- **PL** references **BAL** and **Core**.
- **BAL** referencing **DAL** and **Core**
- **DAL** referencing **Core**

## Folder Structure
### PL
- `Models/` - Data and business logic layer
- `Views/` - Razor views for displaying data to the user
- `Controllers` - The intermediary between Model and View
### BAL
- `Interfaces/` – BLL service interfaces
- `Services/` – BLL service implementations

### DAL
- `Interfaces/` – DAL repository interfaces
- `Repositories/` – DAL repository implementations

### Core
