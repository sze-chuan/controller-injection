# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a .NET 8 modular monolith demonstration project showing how to structure a system with both direct controller injection and separate worker services. The solution consists of six projects:

- **WebApi.Host** (`src/WebApi.Host/`): Main ASP.NET Core Web API host that orchestrates both modules
- **ModuleA** (`src/ModuleA/`): User management module with `UserController` and `UserService`
- **ModuleB** (`src/ModuleB/`): Order management module that directly injects ModuleA's `UserController`
- **ModuleA.Contracts** (`src/ModuleA.Contracts/`): Shared contracts library containing models and interfaces
- **Worker** (`src/Worker/`): Standalone background service that calls ModuleA via HTTP
- **Shared.Common** (`src/Shared.Common/`): Cross-cutting concerns library for external API clients

## Key Architecture Patterns

### 1. Direct Controller Injection (ModuleB → ModuleA)
- ModuleB's `OrderService` directly injects ModuleA's `UserController`
- Controllers are registered as scoped services in their respective extension methods
- Direct method calls avoid HTTP overhead while maintaining DI container benefits
- Both modules can participate in the same transaction scope

Example from `ModuleB/Services/OrderService.cs:16-18` and `ModuleB/Services/OrderService.cs:26`:
```csharp
public OrderService(UserController userController)
{
    _userController = userController;
}

// Direct controller method call (no HTTP)
var user = await _userController.GetUserByIdDirectAsync(order.UserId);
```

### 2. HTTP Client Pattern (Worker → ModuleA)
- Worker service uses typed HTTP client (`UserApiClient`) to call ModuleA endpoints
- Provides true deployment separation while maintaining type safety via shared contracts
- Uses **Microsoft.Extensions.Http.Resilience** for modern resilience patterns:
  - **Retry**: 3 attempts with exponential backoff and jitter
  - **Circuit Breaker**: Opens after 50% failure ratio with 30s break duration
  - **Timeout**: 60s total request timeout
- Can be scaled independently of the main API

Example from `Worker/Services/UserApiClient.cs:23-51`:
```csharp
public async Task<User?> GetUserByIdAsync(int id, CancellationToken cancellationToken = default)
{
    var response = await _httpClient.GetAsync($"/api/user/{id}", cancellationToken);
    // ... error handling and deserialization
}
```

### 3. Shared Common Library Pattern (ModuleB + Worker → External APIs)
- **Shared.Common** library provides cross-cutting concerns for external API integrations
- Both ModuleB and Worker reference this library to share common HTTP clients
- Includes `WeatherApiClient` for external weather service integration
- Configured with Microsoft.Extensions.Http.Resilience for consistent error handling
- Demonstrates DRY principle for shared external dependencies

Example usage:
```csharp
// In ModuleB/Services/OrderService.cs
public OrderService(UserController userController, IWeatherApiClient weatherApiClient)

// In Worker/Services/WorkerService.cs  
public WorkerService(..., IWeatherApiClient weatherApiClient, ...)
```

## Development Commands

### Build and Run
```bash
# Build the entire solution
dotnet build

# Run the Web API (from solution root)
cd src/WebApi.Host
dotnet run

# Run the Worker Service (separate terminal)
cd src/Worker
dotnet run

# Alternative: run from solution root
dotnet run --project src/WebApi.Host
dotnet run --project src/Worker
```

### Project Structure Commands
```bash
# Restore dependencies
dotnet restore

# Build specific projects
dotnet build src/ModuleA.Contracts
dotnet build src/Shared.Common
dotnet build src/ModuleA
dotnet build src/ModuleB
dotnet build src/WebApi.Host
dotnet build src/Worker

# Clean build artifacts
dotnet clean
```

## Module Registration Patterns

### WebApi.Host Registration
Each module provides an extension method for DI registration:

- **ModuleA**: `services.AddModuleA()` in `ModuleAExtensions.cs:9-15`
  - Registers `IUserService`, `UserService`, and crucially `UserController` as scoped
- **ModuleB**: `services.AddModuleB()` in `ModuleBExtensions.cs:8-16`
  - Registers `IOrderService`, `OrderService`, and shared common services
  - Calls `AddSharedCommon()` to register weather API client

Both are called from `Program.cs:11-12` in the host project.

### Worker Registration
The Worker service registers:
- `WorkerService` as a hosted service
- `UserApiClient` with configured HTTP client, retry policies, and circuit breaker
- `AddSharedCommon()` for weather API client with resilience patterns
- Configuration from `appsettings.json` including API base URL and worker intervals

### Shared Common Registration
The `Shared.Common.Extensions.AddSharedCommon()` method registers:
- `IWeatherApiClient` and `WeatherApiClient` with HTTP client factory
- Microsoft.Extensions.Http.Resilience for retry, circuit breaker, and timeout
- Configured with weather API base URL and resilience settings

## Demo Endpoints

- **User Module**: `/api/user`, `/api/user/{id}`, `POST /api/user`
- **Order Module**: `/api/order`, `/api/order/{id}`, `/api/order/user/{userId}`, `POST /api/order`
- **Demo Endpoint**: `GET /api/order/demo-direct-call` - Shows direct controller injection in action

Access Swagger UI at `https://localhost:7xxx/swagger` when running in development.

The Worker service runs independently and will periodically call the `/api/user` endpoint to demonstrate HTTP-based module communication.

## HttpContext Handling

The project demonstrates HttpContext access in injected scenarios:
- `UserService` uses `IHttpContextAccessor` (see `UserService.cs:16-18`)
- Controllers work when injected because only business logic methods are called
- No HTTP context needed for direct controller-to-controller calls

## Shared Contracts Pattern

The `ModuleA.Contracts` project enables:
- **Type Safety**: Shared models (`User`) ensure consistency between API and Worker
- **Versioning**: Contracts can be versioned independently from implementations  
- **Loose Coupling**: Worker depends only on contracts, not implementation details
- **Deployment Independence**: Worker can be deployed separately while maintaining type safety

## Worker Service Configuration

Key settings in `Worker/appsettings.json`:
- `WorkerIntervalSeconds`: How often the worker runs (default: 30 seconds)
- `ApiBaseUrl`: Base URL for the Web API (default: "https://localhost:7000")
- HTTP client includes Microsoft.Extensions.Http.Resilience for retry, circuit breaker, and timeout patterns

## Target Framework

.NET 8.0 with nullable reference types and implicit usings enabled across all projects.

## Coding patterns

- Use primary constructor when possible