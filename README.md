# Claim Request System

A centralized system that supports the creation of claims and reduces paperwork for FPT Software staff.

## Quick Links
- **API Documentation**: [Swagger UI](https://claim-request-system.azurewebsites.net/swagger/index.html)
- **Live Demo**: [Frontend Application](https://crs24.vercel.app/)
- **Documentation**: [Presentation Slides](Document/Demo/FinalDemoNET04.pptx)

## Table of Contents
- [Quick Links](#quick-links)
- [Getting Started](#getting-started)
  - [Prerequisites](#prerequisites)
  - [Database Setup](#database-setup)
  - [Building the Solution](#building-the-solution)
- [Architecture](#architecture)
  - [Data Access Layer Architecture](#data-access-layer-architecture)
  - [Design Patterns](#design-patterns)
    - [Unit of Work Pattern](#unitofwork-pattern-overview)
    - [Dependency Injection Pattern](#dependency-injection-pattern)
    - [Singleton Pattern](#singleton-pattern)
    - [Builder Pattern](#builder-pattern)
    - [Middleware Pattern](#middleware-pattern)
    - [Strategy Pattern](#strategy-pattern)
- [Features](#features)
  - [Authentication](#authentication)
  - [Claim Management](#claim-management)
  - [Staff Management](#staff-management)
- [Testing](#testing)
  - [Unit Test Results](#unit-test-results)

## Getting Started

### Prerequisites
- .NET 8.0 SDK
- PostgreSQL
- Docker (optional)

### Database Setup

1. Install Entity Framework tools
```bash
dotnet tool install --global dotnet-ef
```

2. Create new migration
```bash
dotnet ef migrations add InitialCreate --project ClaimRequest.Data
```

3. Apply migrations
```bash
dotnet ef database update --project ClaimRequest.Data
```

### Building the Solution

1. Clone the repository
```bash
git clone https://github.com/your-repo/claim-request-system.git
```

2. Build using .NET CLI
```bash
dotnet build
```

3. Using Docker
```bash
docker-compose build
docker-compose up
```

## Architecture

![Design Pattern](Document/Architecture/DesignPattern.png)

### Data Access Layer Architecture

#### UnitOfWork Pattern Overview
![Database Context](Document/Architecture/DbContext.svg)

#### Architecture Layers
- **Client Layer**: Application code using the UnitOfWork
- **Business Layer**: UnitOfWork and Repository implementations
- **Data Layer**: Entity Framework DbContext and Database

#### Core Components
- **UnitOfWork<TContext>**: Manages transaction lifecycle and repository creation
- **GenericRepository<T>**: Type-safe data access operations
- **DbContext**: Entity Framework database context
- **Database**: Underlying PostgreSQL database

#### Transaction Management

##### Automatic Transaction Handling
```csharp
public class ClaimService
{
    private readonly IUnitOfWork _unitOfWork;

    public async Task<bool> ProcessClaim(Claim claim)
    {
        return await _unitOfWork.ProcessInTransactionAsync(async () =>
        {
            var claimRepo = _unitOfWork.GetRepository<Claim>();
            await claimRepo.AddAsync(claim);
            return true;
        });
    }
}
```

##### Manual Transaction Control
- Begin transaction: `BeginTransactionAsync()`
- Commit changes: `CommitAsync()`
- Rollback changes: `RollbackAsync()`

#### Key Features
- Lazy repository initialization
- Automatic transaction management
- Change tracking and validation
- Exception handling with rollback
- Repository pattern implementation


### Dependency Injection Pattern
![Dependency Injection Pattern](Document/Architecture/DependencyInjectionPattern.svg)

#### Implementation Details
- Services are registered with scoped lifetime
- Constructor injection used in BaseService for:
  - IUnitOfWork
  - ILogger
  - IMapper
  - IHttpContextAccessor
- Benefits:
  - Loose coupling between components
  - Easy to swap implementations
  - Better testability through mocking

```plantuml
@startuml
participant Program
participant ServiceCollection
participant Container
participant BaseService
participant Services

Program -> ServiceCollection : AddScoped<IClaimService, ClaimService>()
Program -> ServiceCollection : AddScoped<IStaffService, StaffService>()
Program -> ServiceCollection : AddScoped<IUnitOfWork, UnitOfWork>()
ServiceCollection -> Container : BuildServiceProvider()
Container --> BaseService : Inject Dependencies
note right of BaseService
  Constructor injection:
  - IUnitOfWork
  - ILogger
  - IMapper
  - IHttpContextAccessor
end note
BaseService -> Services : Provide dependencies
@enduml
```

### Singleton Pattern
![Singleton Pattern](Document/Architecture/SingletonPattern.svg)

#### Implementation Details
- Utility services registered as singletons:
  - JwtUtil for token management
  - OtpUtil for OTP operations
- Characteristics:
  - Single instance shared across application
  - Thread-safe access to shared resources
  - Used for stateless utility services

```plantuml
@startuml
participant Program
participant ServiceCollection
participant JwtUtil
participant OtpUtil

Program -> ServiceCollection : AddSingleton<JwtUtil>()
Program -> ServiceCollection : AddSingleton<OtpUtil>()
note right of JwtUtil : Single instance for\nentire application
note right of OtpUtil : Single instance for\nentire application
JwtUtil --> Program : Use same instance
OtpUtil --> Program : Use same instance
@enduml
```

### Builder Pattern

#### Implementation Details
- Used for fluent database context configuration
- Step-by-step construction of DbContext:
  1. Configure database provider
  2. Set retry policies
  3. Configure options
  4. Build final context
- Benefits:
  - Clear separation of construction steps
  - Fluent interface for configuration
  - Complex object creation made simple

```plantuml
@startuml
participant Program
participant DbContextOptionsBuilder
participant DbContext

Program -> DbContextOptionsBuilder : UseNpgsql()
Program -> DbContextOptionsBuilder : EnableRetryOnFailure()
DbContextOptionsBuilder -> DbContextOptionsBuilder : Configure options
DbContextOptionsBuilder -> DbContext : Build DbContext
note right of DbContextOptionsBuilder : Fluent configuration\nof database context
@enduml
```

### Middleware Pattern
![Middleware Pattern](Document/Architecture/MiddlewarePattern.svg)

#### Implementation Details
- Pipeline components in order:
  1. Exception Handling
  2. Reset Password
  3. Authentication
  4. CORS
- Features:
  - Sequential request processing
  - Each middleware can modify request/response
  - Chain of responsibility pattern
  - Centralized cross-cutting concerns

```plantuml
@startuml
participant Request
participant Pipeline
participant ExceptionHandler
participant ResetPasswordOnly
participant Response

Request -> Pipeline : HTTP Request
Pipeline -> ExceptionHandler : Process
ExceptionHandler -> ResetPasswordOnly : Next
ResetPasswordOnly -> Response : Process
note right of Pipeline
  Middleware Chain:
  1. Exception Handling
  2. Reset Password
  3. Auth
  4. CORS
end note
@enduml
```

### Strategy Pattern
![Strategy Pattern](Document/Architecture/StrategyPattern.svg)

#### Implementation Details
- Query strategies implemented through delegates
- Components:
  - Predicates for filtering data
  - OrderBy for sorting results
  - Include for eager loading relations
- Benefits:
  - Flexible query composition
  - Runtime strategy selection
  - Encapsulated query logic
  - Reusable query components

```plantuml
@startuml
participant Client
participant IGenericRepository
participant GenericRepository
participant QueryableStrategy

Client -> IGenericRepository : SingleOrDefaultAsync()
IGenericRepository -> GenericRepository : Execute Query
GenericRepository -> QueryableStrategy : Apply Predicate
GenericRepository -> QueryableStrategy : Apply OrderBy
GenericRepository -> QueryableStrategy : Apply Include
QueryableStrategy --> Client : Return Result
note right of QueryableStrategy
  Different query
  strategies through
  delegate parameters
end note
@enduml
```

## Features

### Authentication
Login with Google OAuth2

![Login](Document/Feature/Login.png)

Password Management:

Change Password

![Change Password](Document/Feature/ChangePassword.png)

Forgot Password with OTP

![Forgot Password](Document/Feature/ForgotPassword.png)

OTP Email Confirmation

![OTP Email](Document/Feature/SendOtpEmail.png)

### Claim Management
Create New Claims

![Create Claim](Document/Feature/CreateClaim.png)

Return Claims

![Return Claim](Document/Feature/ReturnClaim.png)

Cancel Claims

![Cancel Claim Validation](Document/Feature/Claim/CancelClaim_ValidateClaim.png)

![Cancel Claim User Validation](Document/Feature/Claim/CancelClaim_ValidateUser.png)

Download Claims

![Download Claims](Document/Feature/Claim/DownloadClaim.png)

Paid Claims

![Paid Claims](Document/Feature/Claim/PaidClaim.png)

Staff Assignment:

Assign Staff to Claim

![Assign Staff](Document/Feature/Claim/AssignStaff.png)

Remove Staff from Claim

![Remove Staff](Document/Feature/Claim/RemoveStaff.png)

### Staff Management
View All Staff

![Get All Staff](Document/Feature/Staff/GetAllStaff.svg)

Staff Details

![Get Staff by ID](Document/Feature/Staff/GetStaffById.svg)

Staff Operations:

Create Staff

![Create Staff](Document/Feature/Staff/CreateStaff.svg)

Update Staff

![Update Staff](Document/Feature/Staff/UpdateStaff.svg)

Delete Staff

![Delete Staff](Document/Feature/Staff/DeleteStaff.svg)

Pagination Support

![Page Staff](Document/Feature/Staff/PageStaff.svg)

## Testing

### Unit Test Results
Staff Service Tests

![Staff Service Test](Document/UnitTestResults/StaffServiceTest.png)

Email Service Tests

![Email Service Tests](Document/UnitTestResults/EmailServiceTests.png)

Claim Service Tests:

Paid Claim Tests

![Paid Claim Tests](Document/UnitTestResults/PaidClaimServiceTest.png)

Reject Claim Tests

![Reject Claim Tests](Document/UnitTestResults/RejectClaimTests.png)

Assign/Remove Staff Tests

![Assign Remove Staff Tests](Document/UnitTestResults/AssignRemoveStaffTests.png)

To run tests:
```bash
dotnet test
```

Using Docker:
```bash
./scripts/run-tests.sh
```
