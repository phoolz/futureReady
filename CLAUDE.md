# Apiary - Project Context

## Overview

Apiary is a Work Experience Placement Management SaaS application for schools. It manages student work placements at companies, including forms for employers, parents, and student logbooks.

## Tech Stack

- .NET 10, ASP.NET Core + Blazor Static SSR (no MVC)
- EF Core 10, SQL Server
- Multi-tenant architecture (school = tenant)

## Simplicity Principles

- Prefer straightforward CRUD and explicit workflows over abstractions
- Avoid CQRS/MediatR unless a clear need emerges
- Avoid repository layers on top of EF Core — use DbContext via services directly
- Avoid premature domain event infrastructure
- Keep business rules in services, not scattered across UI code
- Favor one clear way of doing things over multiple patterns

## Key Conventions

### Entity Models
- All models inherit from `TenantEntity` (provides Id, TenantId, audit fields, soft delete)
- IDs are GUIDs (`Guid`)
- Date-only fields: `DateOnly` type
- Time-of-day fields: `TimeOnly` type where the value is domain data
- Use `string` for time only when it is purely a UI-facing formatted value and persistence as a true time type is unnecessary
- Foreign keys: cascade delete for children, restrict for references

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

### Soft Delete
- Use soft delete for tenant-owned business records
- Do not soft delete pure join or temporary records unless specifically required
- Unique indexes must account for `IsDeleted` to allow re-creation of records after soft delete
- Global query filters apply by default — bypass only intentionally in admin or reporting scenarios

### UI Models
- Use separate form/view models when UI requirements differ from persisted entities
- Do not bind EF entities directly to complex multi-step forms unless the shape is already a close match
- Validation attributes belong on the form model, not the entity

### Services
- Interface in `Services/Interfaces/`
- Implementation in `Services/Implementations/`
- Register in `Program.cs`

### Application Logic
- Blazor pages handle HTTP and UI concerns only — no business logic
- Services contain all business workflows and orchestration
- EF Core entities represent persisted data, not UI view models

### Forms
- External users (employers, parents) access forms via magic links (FormToken)
- Multi-step forms use Blazor Static SSR with the PRG pattern (see below)

## Authentication

- ASP.NET Identity with cookie auth (`ApiaryAuth` cookie, 7-day expiry)
- Login and logout are implemented as minimal API endpoints, not Blazor pages
- All authenticated Blazor pages use `@attribute [Authorize]`
- `ITenantProvider` and `IUserProvider` resolve from `IHttpContextAccessor` claims only

## Tenant Safety

- All database queries and writes must be scoped by `TenantId` — cross-tenant access must be impossible by default
- Tenant filtering and soft delete filtering are implemented as EF Core global query filters in `ApplicationDbContext` — never bypass these filters
- For authenticated users, `TenantId` is resolved from claims via `ITenantProvider` only — never from route, query string, or form values
- For external form users (employers, parents), `TenantId` is resolved through the `FormToken → Placement` relationship only — never from anything supplied in the request directly
- Never trust any user-supplied value to determine tenant identity

## External Form Security

- Form tokens must be cryptographically random and unguessable
- Tokens expire after a defined period — treat expired tokens as invalid
- Every step request validates the token, its expiry, and whether it has already been used
- External users may only access data related to their token's placement — nothing else
- Do not expose TenantId, internal entity IDs, or unrelated placement data in public-facing routes or responses

## Concurrency

- Use `RowVersion` for optimistic concurrency on mutable records
- Handle concurrency conflicts gracefully in admin/internal workflows
- External public form flows use last-write-wins unless a specific conflict path is required

## Frontend Architecture

### Blazor Render Mode Strategy
- Default all pages to **Static SSR** (no `@rendermode`) — no persistent connections, scales freely
- Add `@attribute [StreamRendering]` to any page that fetches from the database
- No InteractiveServer, no WASM — eliminates reconnecting modal and SignalR memory overhead
- Do not register `AddInteractiveServerComponents()` or call `AddInteractiveServerRenderMode()` in `Program.cs`
- Static SSR pages remain visible if a user loses connection mid-view, no extra code needed

### Blazor Page Conventions
- Use `<AntiforgeryToken />` in all forms
- Use `[SupplyParameterFromForm]` for form model binding
- Use TempData for passing validation errors across redirects

## Multi-Step Forms (PRG Pattern)

### Overview
- Each step POSTs directly to its destination table immediately (upsert)
- No intermediate state storage — no JSON, no session, no cookies
- Follows Post-Redirect-Get: POST → save → redirect to next step
- Users can navigate back to any step; existing data is loaded from the destination table on GET
- Incomplete records from abandoned forms are expected and acceptable — cleanup is out of scope

### FormToken
- Validates the user's access on every step request
- Tracks overall form completion via `UsedAt` timestamp only
- Marked as used after the final step is submitted
- Any invalid, expired, or already-used token redirects to a shared `/form/invalid` page

### Validation
- Validate on POST; on failure write errors to TempData and redirect back to the same step
- On GET, read errors from TempData and display them

### Upsert Convention
- Always scope upserts by TenantId via the placement relationship, never by FormToken alone

### Review Step
- Queries all destination tables to assemble a combined view
- No denormalized summary storage

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

### PerformanceRating Enum
```csharp
Unsatisfactory, Satisfactory, High, VeryHigh
```

## Form Workflows

### Employer Acceptance Form
1. Teacher generates magic link from Supervisor page
2. Employer clicks link → token validated, redirect to `/form/invalid` if invalid
3. Multi-step SSR form: Workplace → Supervisor → Insurance → OHS → General/Travel → Hazards → Review
4. Each step writes directly to its destination table (Company, Supervisor, Placement)

### Parent Permission Form
1. Teacher generates magic link
2. Parent clicks link → token validated, redirect to `/form/invalid` if invalid
3. Multi-step SSR form: Student → Emergency Contact → Workplace → Transport → Medical → Consent → Review
4. Each step writes directly to its destination table (Student, EmergencyContact, StudentMedicalConditions, ParentPermission, Placement)

### Student Logbook
- Daily attendance entries
- Task entries (at least one per day)
- Supervisor evaluations (every 55 hours)