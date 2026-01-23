# FutureReady Project Context

## Overview
Blazor Server application for managing student work placements, including parent/employer forms, student logbooks, and work history tracking.

## Architecture

- **Framework**: .NET 9, Blazor Server with InteractiveServer render mode
- **Database**: Entity Framework Core with multi-tenant support via `TenantEntity` base class
- **Pattern**: Services inject `ITenantProvider` for tenant isolation

## Key Directories

```
Controllers/           - MVC controllers for admin views
Components/Pages/      - Blazor page components
  ParentForm/         - Parent permission wizard
  EmployerForm/       - Employer form wizard
  StudentWorkHistory/ - Student work history form (NEW)
Models/
  School/             - Core entities (Student, Placement, etc.)
  ParentForm/         - Parent form DTOs
  EmployerForm/       - Employer form DTOs
  StudentWorkHistory/ - Work history DTOs (NEW)
Services/             - Business logic services
Views/                - MVC Razor views
```

## Student Work History Feature

### Files Created
- `Models/StudentWorkHistory/WorkHistoryFormDto.cs` - DTOs for JSON serialization
- `Components/Pages/StudentWorkHistory/WorkHistoryForm.razor` - Main form at `/student/work-history/{StudentId:guid}`
- `Components/Pages/StudentWorkHistory/CertificateCheckbox.razor` - Reusable certificate checkbox component
- `Components/Pages/StudentWorkHistory/_Imports.razor` - Namespace imports

### Data Model
`Models/School/StudentWorkHistory.cs` stores JSON strings for:
- CurrentCourses
- VetQualifications
- Certificates (16 SmartMove modules + 8 other certificates)
- PartTimeEmployment
- CommunityService

### Service
`Services/StudentWorkHistories/IStudentWorkHistoryService.cs`:
- `GetByStudentIdAsync(Guid studentId)` - Fetch by student
- `CreateOrUpdateAsync(StudentWorkHistory)` - Upsert pattern
- `DeleteAsync(Guid id)` - Delete

### Pending
- Add navigation link from `Views/Students/Details.cshtml` to the Blazor form

## Patterns to Follow

### Blazor Forms
- Use `@rendermode InteractiveServer`
- Follow checkbox + conditional fields pattern from `MedicalDetailsStep.razor`
- Use `FormSection` component from `Components/Pages/EmployerForm/Shared/`

### Controllers
- Inherit from `Controller`, add `[Authorize]`
- Inject service + `ITenantProvider`
- Follow patterns in `StudentMedicalConditionsController.cs`

### JSON Serialization
- Use `System.Text.Json.JsonSerializer`
- Use `PropertyNameCaseInsensitive = true` for deserialization
