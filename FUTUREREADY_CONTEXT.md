# FutureReady - Project Context

## Overview

FutureReady is a Work Experience Placement Management SaaS application for schools. It manages student work placements at companies, including forms for employers, parents, and student logbooks.

## Tech Stack

- .NET 9, ASP.NET Core MVC
- EF Core 8, SQL Server
- Blazor components for multi-step forms
- Multi-tenant architecture (school = tenant)

## Project Structure

```
FutureReady/
├── Models/
│   ├── BaseEntity.cs              # Audit fields (CreatedAt/By, UpdatedAt/By, IsDeleted, etc.)
│   ├── TenantEntity.cs            # Extends BaseEntity, adds TenantId
│   └── School/
│       ├── Student.cs
│       ├── Teacher.cs
│       ├── Company.cs
│       ├── Supervisor.cs
│       ├── Placement.cs           # Links student → company/supervisor
│       ├── ParentPermission.cs
│       ├── EmergencyContact.cs
│       ├── StudentMedicalCondition.cs
│       ├── FormToken.cs           # Magic links for external forms
│       ├── LogbookEntry.cs        # Daily attendance
│       ├── LogbookTask.cs         # Tasks performed
│       ├── LogbookEvaluation.cs   # Supervisor evaluation
│       ├── StudentWorkHistory.cs  # Student's prior experience
│       └── Enums/
│           └── PerformanceRating.cs
├── Data/
│   ├── ApplicationDbContext.cs
│   └── Migrations/
├── Services/
│   ├── Interfaces/
│   └── Implementations/
├── Controllers/
├── Views/
└── Components/
    └── Pages/
        ├── EmployerForm/
        └── ParentForm/
```

## Key Conventions

### Models
- All models inherit from `TenantEntity` (provides Id, TenantId, audit fields, soft delete)
- IDs are GUIDs (`Guid`)
- Time display fields: `string` with `MaxLength(10)` e.g. "09:00"
- Date-only fields: `DateOnly` type
- Foreign keys: cascade delete for children, restrict for references
- Soft delete via `IsDeleted` flag with global query filter

### TenantEntity Fields (inherited by all models)
```csharp
Guid Id
Guid TenantId
string? CreatedBy
DateTimeOffset CreatedAt
string? UpdatedBy
DateTimeOffset? UpdatedAt
bool IsDeleted
string? DeletedBy
DateTimeOffset? DeletedAt
byte[]? RowVersion
```

### Services
- Interface in `Services/Interfaces/`
- Implementation in `Services/Implementations/`
- Register in `Program.cs`

### Forms
- External users (employers, parents) access forms via magic links (FormToken)
- Multi-step forms use Blazor components
- Client-side state, single submission at end

## Core Entities

### Placement
Central entity linking everything together:
- StudentId (required)
- CompanyId (optional)
- SupervisorId (optional)
- Status: draft → pending_parent → pending_employer → confirmed
- OHS fields, hazards, travel info
- EmployerSubmittedAt, ParentSubmittedAt timestamps

### FormToken (Magic Links)
- Token (unique string for URL)
- PlacementId
- FormType: "employer_acceptance" or "parent_permission"
- Email, ExpiresAt, UsedAt

### Logbook Models
- **LogbookEntry**: Daily attendance (date, times, hours worked, supervisor verified)
- **LogbookTask**: Tasks/activities performed (description, date)
- **LogbookEvaluation**: Supervisor evaluation after each 55 hours (8 rating fields using PerformanceRating enum)
- **StudentWorkHistory**: Student's background (courses, VET, certificates, employment, community service - stored as JSON)

### PerformanceRating Enum
```csharp
Unsatisfactory, Satisfactory, High, VeryHigh
```

## Form Workflows

### Employer Acceptance Form
1. Teacher generates magic link from Supervisor page
2. Employer clicks link → validates token
3. Multi-step Blazor form: Workplace → Supervisor → Insurance → OHS → General/Travel → Hazards → Review
4. Single submission updates Company, Supervisor, Placement

### Parent Permission Form
1. Teacher generates magic link
2. Parent clicks link → validates token
3. Multi-step form: Student → Emergency Contact → Workplace → Transport → Medical → Consent → Review
4. Submission updates Student, EmergencyContact, StudentMedicalConditions, ParentPermission, Placement

### Student Logbook
- Work History (one-time background info)
- Daily attendance entries
- Task entries (at least one per day)
- Supervisor evaluations (every 55 hours)
