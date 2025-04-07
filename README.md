![Claim Request System](https://res.cloudinary.com/crs2025/image/upload/v1743237101/CRSLogo_h5s8ez.png)

<div align="center">

# Claim Request System

A centralized system that supports the creation of claims and reduces paperwork for FPT Software staff.


## Quick Links
**[API Documentation](https://claim-request-system.azurewebsites.net/swagger/index.html)** • 
**[Live Demo](https://crs24.vercel.app/)** • 
**[Documentation](Document/Demo/FinalDemoNET04.pptx)**

</div>

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
  - [Email Services](#email-services)
  - [Integration Services](#integration-services)
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
![Builder Pattern](Document/Architecture/BuilderPattern.svg)

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

```csharp
// src/ClaimRequest.API/Program.cs
void ConfigureDatabase()
{
    builder.Services.AddDbContext<ClaimRequestDbContext>(options =>
    {
        options.UseNpgsql(builder.Configuration.GetConnectionString("SupaBaseConnection"),
            npgsqlOptionsAction: sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorCodesToAdd: null);
            });
    });
}
```

```csharp
// src/ClaimRequest.Data/Data/ClaimRequestDbContext.cs
public class ClaimRequestDbContext : DbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Schema configuration
        modelBuilder.HasDefaultSchema("ClaimRequest");

        // Enum configurations
        modelBuilder.Entity<Staff>()
            .Property(s => s.SystemRole)
            .HasConversion<string>();

        // Relationship configurations
        modelBuilder.Entity<Claim>()
            .HasOne(c => c.Claimer)
            .WithMany()
            .HasForeignKey(c => c.ClaimerId);

        // Precision configurations
        modelBuilder.Entity<Claim>()
            .Property(c => c.Amount)
            .HasColumnType("decimal(18,2)");

        // Date configurations
        modelBuilder.Entity<Claim>()
            .Property(c => c.StartDate)
            .HasColumnType("date");
    }
}
```


```plantuml
@startuml
participant Program
participant DbContextOptionsBuilder
participant NpgsqlOptionsAction
participant ClaimRequestDbContext
participant ModelBuilder

Program -> DbContextOptionsBuilder : AddDbContext()
activate DbContextOptionsBuilder

DbContextOptionsBuilder -> DbContextOptionsBuilder : UseNpgsql(connectionString)
activate NpgsqlOptionsAction

NpgsqlOptionsAction -> NpgsqlOptionsAction : EnableRetryOnFailure(\n maxRetryCount: 5,\n maxRetryDelay: 30s)

DbContextOptionsBuilder -> ClaimRequestDbContext : new(options)
activate ClaimRequestDbContext

ClaimRequestDbContext -> ModelBuilder : OnModelCreating()
activate ModelBuilder

ModelBuilder -> ModelBuilder : HasDefaultSchema("ClaimRequest")
ModelBuilder -> ModelBuilder : ConfigureEnumConversions()
note right: Configure Staff.SystemRole\nStaff.Department\nProject.Status\nClaim.Status etc.

ModelBuilder -> ModelBuilder : ConfigureRelationships()
note right: Configure relationships for\nClaim-Claimer\nClaim-Finance\nClaim-Project etc.

ModelBuilder -> ModelBuilder : ConfigureDecimalPrecision()
note right: Configure decimal precision for\nClaim.Amount\nProject.Budget\nStaff.Salary

ModelBuilder -> ModelBuilder : ConfigureDateConversions()
note right: Configure DateOnly conversions\nfor Claim and Project dates

ModelBuilder -> ClaimRequestDbContext
deactivate ModelBuilder

ClaimRequestDbContext --> Program : Return configured context
deactivate ClaimRequestDbContext
deactivate NpgsqlOptionsAction
deactivate DbContextOptionsBuilder
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


```csharp
// src/ClaimRequest.Data/Repositories/Implements/GenericRepository.cs
public class GenericRepository<T> : IGenericRepository<T> where T : class
{
    protected readonly DbSet<T> _dbSet;

    public virtual async Task<T> SingleOrDefaultAsync(
        Expression<Func<T, bool>> predicate = null,
        Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
        Func<IQueryable<T>, IIncludableQueryable<T, object>> include = null)
    {
        IQueryable<T> query = _dbSet;

        if (include != null)
            query = include(query);

        if (predicate != null)
            query = query.Where(predicate);

        if (orderBy != null)
            return await orderBy(query).AsNoTracking().FirstOrDefaultAsync();

        return await query.AsNoTracking().FirstOrDefaultAsync();
    }
}
```

```csharp
// src/ClaimRequest.Service/Services/Implements/ProjectService.cs
var project = await _unitOfWork.GetRepository<Project>()
    .SingleOrDefaultAsync(
        predicate: p => p.Id == id && p.IsActive,
        include: q => q
            .Include(p => p.ProjectManager)
            .Include(p => p.FinanceStaff)
            .Include(p => p.ProjectStaffs)
                .ThenInclude(ps => ps.Staff)
    );
```


```plantuml
@startuml
participant Client
participant IGenericRepository
participant GenericRepository
participant DbSet
participant QueryableStrategy
participant IncludeStrategy

Client -> IGenericRepository : SingleOrDefaultAsync(\npredicate, orderBy, include)
activate IGenericRepository

IGenericRepository -> GenericRepository : Execute Query
activate GenericRepository

GenericRepository -> DbSet : AsQueryable()
activate DbSet
DbSet --> GenericRepository : IQueryable<T>

alt Include Relations
    GenericRepository -> IncludeStrategy : include(query)
    activate IncludeStrategy
    note right: Example:\nq.Include(p => p.ProjectManager)\n.Include(p => p.FinanceStaff)\n.Include(p => p.ProjectStaffs)
    IncludeStrategy --> GenericRepository : Modified query
    deactivate IncludeStrategy
end

alt Apply Predicate
    GenericRepository -> QueryableStrategy : Where(predicate)
    activate QueryableStrategy
    note right: Example:\np => p.Id == id && p.IsActive
    QueryableStrategy --> GenericRepository : Filtered query
    deactivate QueryableStrategy
end

alt Apply Sorting
    GenericRepository -> QueryableStrategy : orderBy(query)
    activate QueryableStrategy
    note right: Example:\nq => q.OrderBy(x => x.Name)
    QueryableStrategy --> GenericRepository : Sorted query
    deactivate QueryableStrategy
end

GenericRepository -> DbSet : AsNoTracking()
GenericRepository -> DbSet : FirstOrDefaultAsync()

DbSet --> GenericRepository : Result
deactivate DbSet

GenericRepository --> IGenericRepository : Return result
deactivate GenericRepository

IGenericRepository --> Client : Return result
deactivate IGenericRepository
@enduml
```

## Features

<h3 align="center">Authentication</h3>

<h4 align="center">Login with Google OAuth2</h4>

<div align="center">

![Login](Document/Feature/Auth/Login.png)

</div>

<h4 align="center">Password Management</h4>

<div align="center">

##### Change Password
![Change Password](Document/Feature/Auth/ChangePassword.png)

##### Forgot Password with OTP
![Forgot Password](Document/Feature/Auth/ForgotPassword.png)

##### Refresh Token Service
![Refresh Token](Document/Feature/Auth/RefreshTokensService.png)

</div>

<h3 align="center">Email Services</h3>

<div align="center">

#### Email Notifications
![Email Service](Document/Feature/Email/EmailSerivce.png)

#### OTP Email Confirmation
![OTP Email](Document/Feature/Email/SendOtpEmail.png)

</div>

<h3 align="center">Integration Services</h3>

<div align="center">

#### Cloud Storage Integration
![Cloudinary Service](Document/Feature/CloudinaryService.png)

#### Payment Gateway Integration
![VNPay Payment](Document/Feature/VNPayPayment.png)
</div>

<h3 align="center">Claim Management</h3>

<div align="center">

#### Create and Process Claims

##### Create New Claims
![Create Claim](Document/Feature/Claim/CreateClaim.png)

##### Update Claims
![Update Claim](Document/Feature/Claim/UpdateClaim.png)

##### Return Claims
![Return Claim](Document/Feature/Claim/ReturnClaim.png)

##### Submit Claims
![Submit Claim](Document/Feature/Claim/SubmitClaim.png)

#### Claim Review Process

##### Approve Claims
![Approve Claim](Document/Feature/Claim/ApproveClaim.png)

##### Reject Claims
![Reject Claim](Document/Feature/Claim/RejectClaim.png)

#### Cancel Claims
![Cancel Claim Validation](Document/Feature/Claim/CancelClaim_ValidateClaim.png)
![Cancel Claim User Validation](Document/Feature/Claim/CancelClaim_ValidateUser.png)

#### Document Management
![Download Claims](Document/Feature/Claim/DownloadClaim.png)

#### Payment Processing
![Paid Claims](Document/Feature/Claim/PaidClaim.png)

#### Claim Listing and Details

##### View All Claims
![Get All Claims](Document/Feature/Claim/GetAllClaims.png)

#### Staff Assignment

##### Assign Staff to Claim
![Assign Staff](Document/Feature/Claim/AssignStaff.png)

##### Remove Staff from Claim
![Remove Staff](Document/Feature/Claim/RemoveStaff.png)

</div>

<h3 align="center">Staff Management</h3>

<div align="center">

#### Staff Listing and Details

##### View All Staff
![Get All Staff](Document/Feature/Staff/GetAllStaff.svg)

##### Staff Details
![Get Staff by ID](Document/Feature/Staff/GetStaffById.svg)

#### Staff Operations

##### Create Staff
![Create Staff](Document/Feature/Staff/CreateStaff.svg)

##### Update Staff
![Update Staff](Document/Feature/Staff/UpdateStaff.svg)

##### Delete Staff
![Delete Staff](Document/Feature/Staff/DeleteStaff.svg)

##### Pagination Support
![Page Staff](Document/Feature/Staff/PageStaff.svg)

</div>

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

Submit Claim Tests

![Submit Claim Tests](Document/UnitTestResults/SubmitClaimServiceTest.png)

Get Claim Tests

![Get Claim Tests](Document/UnitTestResults/GetClaimServiceTest.png)

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
