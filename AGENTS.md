
## Code Style
Use Clean Architecture and SOLID principles.
Controllers must be thin, business logic belongs to Services.
Use Dependency Injection via constructor injection only.
Do not inject Scoped services into Singleton services.
Use PascalCase for classes, methods, and properties.
Use camelCase for local variables and parameters.
Use async/await for all I/O operations (DB, API, Redis, File).
Never use .Result or .Wait() on async methods.
Use DTOs for Request/Response, never expose Entities directly.
Use FluentValidation for request validation.
Use Global Exception Middleware for error handling.
Use Serilog for structured logging.
Log important business events and unexpected errors only.
Use EF Core migrations for database changes.
Use CancellationToken in APIs and background services.
Keep configuration in appsettings.* and Environment Variables.
Write Unit Tests for business logic and Integration Tests for APIs.
## Command
dotnet restore dotnet build dotnet run dotnet test dotnet ef migrations add InitialCreate dotnet ef database update docker compose up -d docker compose down