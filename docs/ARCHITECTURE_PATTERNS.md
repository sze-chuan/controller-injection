# Inter-Module Communication Patterns in Modular Monolith

This document analyzes different approaches for communication between modules in our modular monolith architecture, specifically focusing on how ModuleB can interact with ModuleA while respecting architectural constraints.

## Table of Contents
- [Current Implementation](#current-implementation)
- [Architectural Constraints](#architectural-constraints)
- [Alternative Patterns](#alternative-patterns)
- [Pattern Comparison](#pattern-comparison)
- [Recommendations](#recommendations)
- [Migration Considerations](#migration-considerations)

## Current Implementation

### Direct Controller Injection Pattern

**Location**: `src/ModuleB/Services/OrderService.cs:7`

The current implementation uses direct controller injection where:

```csharp
public class OrderService(UserController userController, IWeatherApiClient weatherApiClient) : IOrderService
{
    // ModuleB directly injects ModuleA's UserController
    public async Task<Order?> GetOrderByIdAsync(int id)
    {
        var order = _orders.FirstOrDefault(o => o.Id == id);
        if (order != null)
        {
            // Direct method call to controller
            var user = await userController.GetUserByIdDirectAsync(order.UserId);
            // ... rest of logic
        }
        return order;
    }
}
```

**Key Components**:
- ModuleA's `UserController` is registered as a scoped service (`ModuleAExtensions.cs:12`)
- Special "Direct" methods bypass HTTP pipeline (`UserController.cs:41-52`)
- ModuleB directly depends on ModuleA's controller assembly

**Issues with Current Approach**:
1. **Tight Coupling**: ModuleB directly depends on ModuleA's controller implementation
2. **Controller Misuse**: Controllers are meant for HTTP handling, not business logic
3. **HttpContext Issues**: Controllers expect HTTP context which may not exist in direct calls
4. **Testing Complexity**: Hard to mock and test controller dependencies
5. **Architectural Violation**: Breaks clean architecture principles

## Architectural Constraints

Based on the current system design, the following constraint applies:

> **ModuleB cannot depend on any service in ModuleA except for the controller if needed.**

This constraint significantly influences our choice of communication patterns, as it eliminates service-to-service communication options.

## Alternative Patterns

### 1. Internal HTTP Client Pattern ⭐ (Recommended)

**Concept**: ModuleB uses an HTTP client to make actual HTTP requests to ModuleA's endpoints within the same process.

```csharp
// ModuleB implementation
public class InternalUserApiClient
{
    private readonly HttpClient _httpClient;
    
    public InternalUserApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    
    public async Task<User?> GetUserByIdAsync(int id)
    {
        var response = await _httpClient.GetAsync($"/api/user/{id}");
        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<User>(json);
        }
        return null;
    }
}

// Registration in ModuleB
services.AddHttpClient<InternalUserApiClient>(client =>
{
    client.BaseAddress = new Uri(configuration["InternalApiBaseUrl"]!);
})
.AddStandardResilienceHandler(); // Same resilience patterns as Worker service
```

**Pros**:
- ✅ True module isolation - no code dependencies
- ✅ Full HTTP semantics with status codes and headers
- ✅ Can leverage existing resilience patterns (retry, circuit breaker)
- ✅ Easy testing with HTTP mocking
- ✅ Migration-ready for microservices
- ✅ Consistent with existing Worker pattern
- ✅ Respects architectural constraints

**Cons**:
- ❌ Performance overhead from HTTP serialization/deserialization
- ❌ Goes through full network stack even for internal calls
- ❌ Cannot participate in same database transaction
- ❌ Requires configuration management for base URLs

### 2. Controller Interface Abstraction

**Concept**: Create an interface that abstracts the controller methods needed by external modules.

```csharp
// In ModuleA.Contracts
public interface IUserController
{
    Task<User?> GetUserByIdDirectAsync(int id);
    Task<List<User>> GetAllUsersDirectAsync();
}

// ModuleA implementation
public class UserController : ControllerBase, IUserController
{
    // HTTP endpoints...
    
    // Interface methods for internal calls
    public async Task<User?> GetUserByIdDirectAsync(int id) { /* ... */ }
}

// ModuleB usage
public class OrderService(IUserController userController) : IOrderService
{
    // Now depends on interface instead of concrete controller
}
```

**Pros**:
- ✅ Adds abstraction layer while keeping existing pattern
- ✅ Easier testing through interface mocking
- ✅ Fast performance (direct method calls)
- ✅ Can participate in same transaction scope
- ✅ Respects architectural constraints

**Cons**:
- ❌ Still couples ModuleB to ModuleA's interface contract
- ❌ Controllers still handle both HTTP and business concerns
- ❌ Potential HttpContext issues remain

### 3. Message Bus/Mediator Pattern

**Concept**: Use an in-process message bus (like MediatR) for decoupled communication.

```csharp
// Shared command/query contracts
public record GetUserByIdQuery(int Id) : IRequest<User?>;

// ModuleA handler
public class GetUserByIdHandler : IRequestHandler<GetUserByIdQuery, User?>
{
    public async Task<User?> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        // Handle user retrieval logic
    }
}

// ModuleB usage
public class OrderService(IMediator mediator) : IOrderService
{
    public async Task<Order?> GetOrderByIdAsync(int id)
    {
        var user = await mediator.Send(new GetUserByIdQuery(order.UserId));
        // ... rest of logic
    }
}
```

**Pros**:
- ✅ True decoupling - no direct dependencies
- ✅ Excellent for complex workflows
- ✅ Built-in cross-cutting concerns (logging, validation)
- ✅ Easy testing with mediator mocking
- ✅ Supports both commands and queries

**Cons**:
- ❌ Adds complexity with additional abstraction layer
- ❌ Requires MediatR dependency across modules
- ❌ Learning curve for team members
- ❌ Can make code flow harder to follow

### 4. Facade Service Pattern

**Concept**: Create a facade service in ModuleA that ModuleB can depend on, wrapping all internal coordination.

```csharp
// ModuleA facade
public interface IModuleAFacade
{
    Task<User?> GetUserByIdAsync(int id);
    Task<List<User>> GetAllUsersAsync();
}

public class ModuleAFacade(IUserService userService) : IModuleAFacade
{
    public async Task<User?> GetUserByIdAsync(int id)
    {
        var contractUser = await userService.GetUserByIdAsync(id);
        // Convert and return
        return contractUser != null ? 
            new User { Id = contractUser.Id, Name = contractUser.Name, Email = contractUser.Email } : 
            null;
    }
}

// ModuleB usage
public class OrderService(IModuleAFacade moduleAFacade) : IOrderService
```

**Pros**:
- ✅ Clean separation of public vs internal APIs
- ✅ Fast performance (direct method calls)
- ✅ Can participate in same transaction scope
- ✅ Easier testing with facade mocking

**Cons**:
- ❌ Violates the constraint (depends on ModuleA service)
- ❌ Additional layer to maintain
- ❌ Still creates code coupling between modules

### 5. Improved Current Pattern

**Concept**: Keep the direct controller injection but improve it with proper abstractions and error handling.

```csharp
// Enhanced controller with better error handling
public class UserController : ControllerBase
{
    // ... HTTP endpoints
    
    public async Task<User?> GetUserByIdDirectAsync(int id)
    {
        try 
        {
            var contractUser = await userService.GetUserByIdAsync(id);
            if (contractUser == null) return null;
            
            return new User 
            { 
                Id = contractUser.Id, 
                Name = contractUser.Name, 
                Email = contractUser.Email, 
                CreatedAt = contractUser.CreatedAt 
            };
        }
        catch (Exception ex)
        {
            // Proper logging
            logger.LogError(ex, "Error getting user {UserId} via direct call", id);
            return null;
        }
    }
}
```

**Pros**:
- ✅ Minimal changes to existing codebase
- ✅ Fast performance (direct method calls)
- ✅ Can participate in same transaction scope
- ✅ Respects architectural constraints

**Cons**:
- ❌ Maintains tight coupling between modules
- ❌ Controllers still serve dual purposes
- ❌ Limited long-term architectural benefits

## Pattern Comparison

| Aspect | Current Pattern | Internal HTTP Client | Controller Interface | MediatR | Facade Service | Improved Current |
|--------|----------------|---------------------|---------------------|---------|---------------|-----------------|
| **Performance** | Fast | Slow (HTTP overhead) | Fast | Fast | Fast | Fast |
| **Coupling** | Tight | Loose | Medium | Loose | Medium | Tight |
| **Testing** | Hard | Easy (HTTP mocks) | Medium | Easy | Medium | Hard |
| **Transactions** | Same scope | Separate requests | Same scope | Same scope | Same scope | Same scope |
| **Migration Ready** | No | Yes | No | Partially | No | No |
| **Resilience** | Direct exceptions | HTTP patterns | Direct exceptions | Custom | Direct exceptions | Direct exceptions |
| **Complexity** | Low | Medium | Low | High | Medium | Low |
| **Constraint Compliance** | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ |

## Recommendations

### For Current System (Short Term)
**Recommended**: **Controller Interface Abstraction** (#2)
- Minimal disruption to existing code
- Adds proper abstraction layer
- Improves testability
- Respects all constraints

### For Future Architecture (Long Term)
**Recommended**: **Internal HTTP Client Pattern** (#1)
- Provides maximum flexibility for future evolution
- Enables gradual migration to microservices
- Leverages existing resilience patterns from Worker service
- True module isolation

### For Complex Workflows
**Consider**: **Message Bus/Mediator Pattern** (#3)
- When you need complex cross-module workflows
- When you want to add cross-cutting concerns (validation, logging)
- When eventual consistency is acceptable

## Migration Considerations

### Path 1: Gradual HTTP Migration
1. Implement Internal HTTP Client alongside current pattern
2. Gradually move high-volume endpoints to HTTP pattern
3. Add resilience patterns (circuit breaker, retry)
4. Eventually remove direct controller dependencies

### Path 2: Interface First
1. Extract interfaces from current controller methods
2. Add proper error handling and logging
3. Improve testing coverage with interface mocks
4. Later decide on HTTP vs current pattern per use case

### Path 3: Hybrid Approach
1. Keep direct controller injection for transactional operations
2. Use Internal HTTP Client for read-only operations
3. Add MediatR for complex workflows
4. Document when to use each pattern

## Conclusion

The choice of inter-module communication pattern depends on your priorities:

- **Performance Critical**: Stick with improved current pattern or controller interface
- **Future Microservices**: Implement Internal HTTP Client pattern
- **Maximum Testability**: Use Controller Interface or MediatR
- **Complex Workflows**: Consider MediatR pattern

All patterns respect the architectural constraint that ModuleB cannot depend on ModuleA's internal services, while providing different trade-offs between performance, coupling, and future flexibility.