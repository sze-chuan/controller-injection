# Controller Injection Demo - Modular Monolith

This solution demonstrates how to directly call controller methods between modules in a .NET modular monolith architecture without making HTTP requests.

## Architecture

- **WebApi.Host**: Main web API project that hosts both modules
- **ModuleA**: Contains user management functionality with `UserController`
- **ModuleB**: Contains order management functionality that needs user data from Module A

## Key Demonstration Points

### 1. Direct Controller Injection
Module B directly injects Module A's `UserController` and calls its methods without HTTP:

```csharp
// In ModuleB/Services/OrderService.cs
public OrderService(UserController userController)
{
    _userController = userController;
}

// Direct method call (no HTTP)
var user = await _userController.GetUserByIdDirectAsync(order.UserId);
```

### 2. Dependency Injection Solution
Both controllers are registered as services in the DI container:

```csharp
// ModuleA/ModuleAExtensions.cs
services.AddScoped<UserController>();

// This allows Module B to inject UserController directly
```

### 3. HTTP Context Handling
Controllers work fine when injected directly because:
- Only business logic methods are called (not HTTP action methods)
- Dependencies are properly resolved by the DI container
- No HTTP context is needed for business operations

## API Endpoints

### Module A (Users)
- `GET /api/user` - Get all users
- `GET /api/user/{id}` - Get user by ID
- `POST /api/user` - Create new user

### Module B (Orders)
- `GET /api/order` - Get all orders
- `GET /api/order/{id}` - Get order by ID
- `GET /api/order/user/{userId}` - Get orders by user ID
- `POST /api/order` - Create new order
- `GET /api/order/demo-direct-call` - **Demo endpoint showing direct controller call**

## Running the Application

```bash
cd src/WebApi.Host
dotnet run
```

Visit `https://localhost:7xxx/swagger` to test the APIs.

## Key Observations

1. **No HTTP Overhead**: Module B calls Module A's controller methods directly
2. **Proper DI**: All dependencies are resolved correctly by the DI container
3. **Clean Separation**: Modules remain loosely coupled through interfaces
4. **Performance**: Direct method calls are much faster than HTTP requests
5. **Transaction Scope**: Both modules can participate in the same transaction scope

## Test the Demo

Call the demo endpoint: `GET /api/order/demo-direct-call`

This endpoint demonstrates Module B directly calling Module A's controller method and returning the results, proving that controller injection works without HTTP context issues.