# Business Logic Layer (BLL)

The `BLL` project is a class library that contains all the core business logic of the application.

## Responsibilities
- **Services**: Classes that orchestrate data flow between the Presentation Layer (PL) and the Data Access Layer (DAL).
- **Interfaces**: Contracts defining the services to be injected into the PL controllers.
- **Validation & Logic**: Any business rules, validation logic (other than simple data annotations), and data manipulation occur here.

## Key Components
- `AuthService`: Handles user authentication, password hashing using `BCrypt.Net-Next`, and email encryption logic.

## Usage
- The Presentation Layer depends on the BLL.
- The BLL depends on the Data Access Layer (`DAL`) and `Core`.
- Services defined here should be registered in the Dependency Injection container in `PL/Program.cs`.
