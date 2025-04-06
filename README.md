# Claim Request System

A centralized system that supports the creation of claims and reduces paperwork for FPT Software staff.

## Quick Links
- **API Documentation**: [Swagger UI](https://claim-request-system.azurewebsites.net/swagger/index.html)
- **Live Demo**: [Frontend Application](https://crs24.vercel.app/)
- **Documentation**: [Presentation Slides](Document/FinalDemoNET04.pptx)

## Table of Contents
- [Quick Links](#quick-links)
- [Getting Started](#getting-started)
- [Architecture](#architecture)
- [Features](#features)
- [Testing](#testing)

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