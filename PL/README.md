# Presentation Layer (PL)

The `PL` project is an **ASP.NET Core 10.0 MVC** web application. It is the main entry point of the system, handling HTTP requests, rendering user interfaces (Views), and managing authentication.

## Folder Structure

- **`Controllers/`**: Receives user requests and routes them to the appropriate business services. Controllers only depend on **BLL Interfaces** and never interact with the database directly.
- **`Views/`**: Contains the UI templates using the Razor syntax (`.cshtml`), organized by controller.
- **`Models/`**: Contains ViewModels used to safely pass data between Controllers and Views.
- **`wwwroot/`**: Stores static assets such as CSS, JavaScript, fonts, and images.
- **`Properties/`**: Contains development server configurations (e.g., `launchSettings.json`).
- **(root files)**: Includes `Program.cs` for configuring Dependency Injection, the Hangfire Dashboard, Cookie Authentication, and the middleware pipeline.

## Role-based Routing
Upon successful login, the application redirects users based on their roles:
- **Admin**: Redirected to the analytics Dashboard.
- **Lecturer**: Redirected to the subject management page.
- **Student**: Redirected to the list of enrolled subjects.

## Running the Application
```bash
# Ensure appsettings.json is properly configured.
# Run this command from the solution root directory:
dotnet run --project PL/PL.csproj
```
- Default development URL: `https://localhost:5001`.
- Hangfire Dashboard (Requires Admin account): `https://localhost:5001/hangfire`.
