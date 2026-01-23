# FutureReady Codebase Context

A comprehensive reference for AI assistants working with this codebase.

---

## 1. Project Overview

**FutureReady** is a Blazor Server application for managing student work placements in schools. It handles:
- Student management and medical records
- Work placement coordination with companies
- Parent/guardian permission workflows
- Employer acceptance forms
- Student logbook tracking during placements
- Work history documentation

---

## 2. Technology Stack

| Component | Technology |
|-----------|------------|
| Framework | .NET 9, ASP.NET Core |
| UI | Blazor Server (InteractiveServer), MVC Razor Views |
| Database | SQL Server via Entity Framework Core 8 |
| Auth | Cookie-based authentication |
| Architecture | Multi-tenant with soft delete |

---

## 3. Directory Structure

```
FutureReady/
├── Controllers/           # MVC controllers (13 total)
├── Components/
│   ├── Layout/           # MainLayout.razor
│   └── Pages/
│       ├── ParentForm/   # 6-step parent permission wizard
│       ├── EmployerForm/ # 7-step employer acceptance wizard
│       └── StudentWorkHistory/  # Work history form
├── Models/
│   ├── BaseEntity.cs     # Audit trail base class
│   ├── TenantEntity.cs   # Multi-tenant base class
│   ├── School/           # Core domain entities
│   ├── ParentForm/       # Parent form DTOs
│   ├── EmployerForm/     # Employer form DTOs
│   └── StudentWorkHistory/  # Work history DTOs
├── Services/             # Business logic (20+ services)
├── Views/                # MVC Razor views
├── Data/
│   ├── ApplicationDbContext.cs  # EF Core DbContext
│   └── DatabaseSeeder.cs        # Initial data seeding
├── Migrations/           # EF Core migrations
└── wwwroot/              # Static assets
```

---

## 4. Core Architectural Patterns

### 4.1 Entity Hierarchy

All entities inherit from `BaseEntity` which provides audit trail and soft delete:

```csharp
// Models/BaseEntity.cs
public abstract class BaseEntity
{
    public Guid Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string CreatedBy { get; set; }          // Max 256
    public DateTimeOffset? UpdatedAt { get; set; }
    public string UpdatedBy { get; set; }          // Max 256
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string DeletedBy { get; set; }          // Max 256
    public byte[] RowVersion { get; set; }         // Concurrency token
}
```

Tenant-scoped entities inherit from `TenantEntity`:

```csharp
// Models/TenantEntity.cs
public abstract class TenantEntity : BaseEntity
{
    public Guid TenantId { get; set; }  // Required
}
```

### 4.2 Multi-Tenant Architecture

**Providers (in Services/):**
- `ITenantProvider` / `HttpContextTenantProvider` - Extracts tenant from authenticated user
- `IUserProvider` / `HttpContextUserProvider` - Extracts current username

**Service Pattern:**
```csharp
public async Task<Student?> GetByIdAsync(Guid id, Guid? tenantId = null)
{
    tenantId ??= _tenantProvider?.GetCurrentTenantId();
    return await _context.Students
        .FirstOrDefaultAsync(s => s.Id == id &&
            (!tenantId.HasValue || s.TenantId == tenantId.Value));
}
```

### 4.3 Soft Delete

Implemented in `ApplicationDbContext.SaveChanges()`:
- Delete operations convert to `IsDeleted = true`
- Global query filter excludes deleted records: `HasQueryFilter(e => !e.IsDeleted)`
- Audit info recorded: `DeletedAt`, `DeletedBy`

### 4.4 Optimistic Concurrency

- `RowVersion` property configured as `IsRowVersion()` in EF Core
- Services accept optional `byte[]? rowVersion` parameter for updates

---

## 5. Core Entities

### 5.1 School & User Management

**School** (BaseEntity - NOT tenant-scoped, root tenant)
```csharp
public string Name { get; set; }        // Max 200
public string TenantKey { get; set; }   // Max 100, Unique
public string Timezone { get; set; }    // Max 100
public string? ContactEmail { get; set; }
public string? ContactPhone { get; set; }
```

**User** (TenantEntity)
```csharp
public string UserName { get; set; }    // Max 100, Unique
public string Email { get; set; }       // Max 200, Required, Unique
public string? PasswordHash { get; set; }
public bool IsActive { get; set; }
public string? DisplayName { get; set; }
```

**Teacher** (TenantEntity)
- One-to-one with User via unique `UserId`
- Has `FirstName`, `LastName`, `Phone`, `Title`, `HireDate`, `IsActive`

### 5.2 Student & Placement Core

**Student** (TenantEntity)
```csharp
public string FirstName { get; set; }       // Max 100, Required
public string LastName { get; set; }        // Max 100, Required
public string? PreferredName { get; set; }
public DateOnly? DateOfBirth { get; set; }
public string? StudentNumber { get; set; }  // Max 50
public string? Phone { get; set; }
public string? StudentType { get; set; }    // "day" or "boarding"
public string? YearLevel { get; set; }
public int? GraduationYear { get; set; }
public string? MedicareNumber { get; set; }
public Guid? UserId { get; set; }           // Optional FK to User

// Computed (NotMapped)
public string FullName => $"{FirstName} {LastName}";
public string DisplayName => !string.IsNullOrEmpty(PreferredName) ? PreferredName : FirstName;
```

**Placement** (TenantEntity) - Central work placement record
```csharp
public Guid StudentId { get; set; }          // Required FK
public Guid? CompanyId { get; set; }         // FK
public Guid? SupervisorId { get; set; }      // FK
public int? Year { get; set; }
public string Status { get; set; }           // "draft", "pending_parent", "pending_employer", "confirmed"

// Work Details
public string? DressRequirement { get; set; }
public string? WorkStartTime { get; set; }   // HH:MM format
public string? WorkEndTime { get; set; }

// OHS (Occupational Health & Safety) - 15+ boolean/string fields
public bool? HasOhsPolicy { get; set; }
public bool? HasInductionProgram { get; set; }
// ... HasFirstAidKit, HasEmergencyProcedures, etc.

// Hazards Appendix
public bool HasChemicalHazards { get; set; }
public string? ChemicalDetails { get; set; }
// ... Plant/Machinery, Biological, Ergonomic hazards

// Timestamps
public DateTime? EmployerSubmittedAt { get; set; }
public DateTime? ParentSubmittedAt { get; set; }

// Navigation
public Student? Student { get; set; }
public Company? Company { get; set; }
public Supervisor? Supervisor { get; set; }
```

**Company** (TenantEntity)
```csharp
public string Name { get; set; }              // Max 200, Required
public string? Industry { get; set; }
// Physical Address: StreetAddress, StreetAddress2, Suburb, City, State, PostalCode
// Postal Address: PostalStreetAddress, PostalSuburb, PostalCity, PostalState, PostalPostalCode
public bool PublicLiabilityInsurance5M { get; set; }
public string? InsuranceValue { get; set; }
public bool HasPreviousWorkExperienceStudents { get; set; }
```

**Supervisor** (TenantEntity)
```csharp
public Guid CompanyId { get; set; }      // Required FK
public string FirstName { get; set; }    // Max 100, Required
public string LastName { get; set; }     // Max 100, Required
public string? JobTitle { get; set; }
public string? Email { get; set; }
public string? Phone { get; set; }
public string FullName => $"{FirstName} {LastName}";  // Computed
```

### 5.3 Permissions & Consent

**ParentPermission** (TenantEntity)
```csharp
public Guid PlacementId { get; set; }        // Required FK
public string? TransportMethod { get; set; } // "public", "private_car", "combination"
public string? PublicTransportDetails { get; set; }
public string? DriverName { get; set; }      // PII
public string? DriverContactNumber { get; set; }  // PII
public bool RequestTeacherPrevisit { get; set; }
public bool ShareMedicalWithEmployer { get; set; }
public string? MedicalNotesForEmployer { get; set; }
public string? ParentFirstName { get; set; }
public string? ParentLastName { get; set; }
public bool ConsentGiven { get; set; }
public DateOnly? ConsentDate { get; set; }
```

**StudentMedicalCondition** (TenantEntity)
```csharp
public Guid StudentId { get; set; }
public string ConditionType { get; set; }  // Asthma, Diabetes, Epilepsy, Allergies, LearningDifficulties, Medication, Other
public string? Details { get; set; }
```

**EmergencyContact** (TenantEntity)
```csharp
public Guid StudentId { get; set; }
public string FirstName { get; set; }
public string LastName { get; set; }
public string? MobileNumber { get; set; }
public string? Relationship { get; set; }
public bool IsPrimary { get; set; }
```

### 5.4 Work Experience Tracking

**StudentWorkHistory** (TenantEntity) - JSON storage for flexible data
```csharp
public Guid StudentId { get; set; }          // Unique index (one per student)
public string? CurrentCourses { get; set; }      // JSON: List<CourseDto>
public string? VetQualifications { get; set; }   // JSON: List<QualificationDto>
public string? Certificates { get; set; }        // JSON: CertificatesDto (25 certificates)
public string? PartTimeEmployment { get; set; }  // JSON: List<EmploymentDto>
public string? CommunityService { get; set; }    // JSON: List<CommunityServiceDto>
```

**LogbookEntry** (TenantEntity)
```csharp
public Guid PlacementId { get; set; }
public DateOnly Date { get; set; }           // Unique with PlacementId
public string? StartTime { get; set; }       // HH:MM
public string? LunchStartTime { get; set; }
public string? LunchEndTime { get; set; }
public string? FinishTime { get; set; }
public decimal TotalHoursWorked { get; set; }    // Precision(5,2)
public decimal CumulativeHours { get; set; }     // Precision(6,2)
public bool SupervisorVerified { get; set; }
public DateTimeOffset? SupervisorVerifiedAt { get; set; }
```

**LogbookTask** (TenantEntity)
```csharp
public Guid PlacementId { get; set; }
public string Description { get; set; }      // Max 2000
public DateOnly DatePerformed { get; set; }
```

**LogbookEvaluation** (TenantEntity) - Supervisor final evaluation
```csharp
public Guid PlacementId { get; set; }
// 8 PerformanceRating fields: AttendancePunctuality, Appearance, CommunicationSkills,
// Initiative, WorkQuality, Teamwork, SafetyAwareness, OverallPerformance
public string? SupervisorName { get; set; }
public string? Comments { get; set; }
public DateTimeOffset? SupervisorSignedAt { get; set; }
```

**PerformanceRating enum:** `Unsatisfactory(0)`, `Satisfactory(1)`, `High(2)`, `VeryHigh(3)`

### 5.5 Form Access Control

**FormToken** (TenantEntity)
```csharp
public Guid PlacementId { get; set; }
public string Token { get; set; }       // Max 100, Unique
public string FormType { get; set; }    // "parent" or "employer"
public string? Email { get; set; }
public DateTime ExpiresAt { get; set; }
public DateTime? UsedAt { get; set; }

public bool IsValid => UsedAt == null && ExpiresAt > DateTime.UtcNow && !IsDeleted;
```
- 14-day expiry
- One-time use (MarkAsUsedAsync sets UsedAt)

---

## 6. Services Layer

### 6.1 Service Naming Convention

- Interface: `Services/{Domain}/I{Entity}Service.cs`
- Implementation: `Services/{Domain}/{Entity}Service.cs`
- All services registered as Scoped in DI

### 6.2 Key Service Interfaces

**IStudentService**
```csharp
Task<List<Student>> GetAllAsync(Guid? tenantId = null);
Task<Student?> GetByIdAsync(Guid id, Guid? tenantId = null);
Task CreateAsync(Student student, Guid? tenantId = null);
Task UpdateAsync(Student student, byte[]? rowVersion = null, Guid? tenantId = null);
Task DeleteAsync(Guid id, Guid? tenantId = null);
Task<bool> ExistsAsync(Guid id, Guid? tenantId = null);
```

**IPlacementService**
```csharp
Task<List<Placement>> GetAllAsync(Guid? tenantId = null);
Task<List<Placement>> GetByStudentIdAsync(Guid studentId, Guid? tenantId = null);
Task<List<Placement>> GetByCompanyIdAsync(Guid companyId, Guid? tenantId = null);
Task<Placement?> GetByIdAsync(Guid id, Guid? tenantId = null);
Task<Placement?> GetByIdWithDetailsAsync(Guid id, Guid? tenantId = null);  // Includes navigations
Task CreateAsync(Placement placement, Guid? tenantId = null);
Task UpdateAsync(Placement placement, byte[]? rowVersion = null, Guid? tenantId = null);
Task DeleteAsync(Guid id, Guid? tenantId = null);
```

**IStudentWorkHistoryService**
```csharp
Task<StudentWorkHistory?> GetByIdAsync(Guid id, Guid? tenantId = null);
Task<StudentWorkHistory?> GetByStudentIdAsync(Guid studentId, Guid? tenantId = null);
Task CreateOrUpdateAsync(StudentWorkHistory history, Guid? tenantId = null);  // Upsert
Task DeleteAsync(Guid id, Guid? tenantId = null);
```

**IFormTokenService**
```csharp
Task<FormToken> GenerateTokenAsync(Guid placementId, string formType, string? email = null, Guid? tenantId = null);
Task<FormToken?> ValidateTokenAsync(string token);  // No tenant - token is auth
Task MarkAsUsedAsync(string token);
Task RevokeTokenAsync(string token);
Task<List<FormToken>> GetByPlacementAsync(Guid placementId, Guid? tenantId = null);
```

**IEmployerFormService / IParentFormService**
```csharp
Task<EmployerFormDto?> InitializeFormAsync(string token);
Task<bool> SubmitFormAsync(string token, EmployerFormDto formData);
```

### 6.3 All Services

| Service | Purpose |
|---------|---------|
| IAuthenticationService | User login/logout |
| IUserService | User CRUD |
| ISchoolService | School management |
| IStudentService | Student CRUD |
| ITeacherService | Teacher CRUD |
| IPlacementService | Placement CRUD |
| ICompanyService | Company CRUD |
| ISupervisorService | Supervisor CRUD |
| IEmergencyContactService | Emergency contact CRUD |
| IStudentMedicalConditionService | Medical conditions |
| IStudentWorkHistoryService | Work history (JSON) |
| IFormTokenService | Token generation/validation |
| IEmployerFormService | Employer form workflow |
| IParentFormService | Parent form workflow |
| IEmployerFormStateService | Form wizard state |
| IParentFormStateService | Form wizard state |
| ILogbookEntryService | Logbook entries |
| ILogbookTaskService | Logbook tasks |
| ILogbookEvaluationService | Supervisor evaluations |

---

## 7. Blazor Components

### 7.1 Render Mode

All interactive components use:
```csharp
@rendermode InteractiveServer
```

### 7.2 Parent Form Wizard

**Route:** `@page "/parent/form/{Token}"`
**File:** `Components/Pages/ParentForm/ParentWizard.razor`

**6 Steps:**
1. StudentDetailsStep - Student type, mobile
2. EmergencyContactStep - Contact name, phone, relationship
3. WorkplaceDetailsStep - Company details display
4. TransportStep - Transport method, driver details
5. MedicalDetailsStep - Medical conditions with checkboxes + detail fields
6. ConsentStep - Parent consent signature

**Pattern:**
- Token-based access (no login required)
- CascadingValue for form state
- Progress indicator component
- Step validation before proceeding

### 7.3 Employer Form Wizard

**Route:** `@page "/employer/form/{Token}"`
**File:** `Components/Pages/EmployerForm/EmployerWizard.razor`

**7 Steps:**
1. WorkplaceDetailsStep - Address, dress code, work hours
2. SupervisorDetailsStep - Supervisor name, job title, contact
3. InsuranceStep - Public liability, previous experience
4. OhsStep - OHS policy, induction, safety procedures
5. GeneralTravelStep - Staff info, working with children, vehicle travel
6. HazardsAppendixStep - Chemical/plant/biological/ergonomic hazards
7. Review/Submit

### 7.4 Student Work History Form

**Route:** `@page "/student/work-history/{StudentId:guid}"`
**File:** `Components/Pages/StudentWorkHistory/WorkHistoryForm.razor`

**Sections:**
- Current Courses (list with add/remove)
- VET Qualifications (list with add/remove)
- Certificates (25 checkboxes with optional dates)
- Part-Time Employment (list with employer/role)
- Community Service (list)

**Sub-component:** `CertificateCheckbox.razor` - Reusable checkbox + date input

### 7.5 Shared Components

**FormSection** (`Components/Pages/EmployerForm/Shared/`)
```csharp
[Parameter] public string Title { get; set; }
[Parameter] public RenderFragment? ChildContent { get; set; }
```

**ProgressIndicator** - Shows current/total steps with progress bar

**StudentInfoHeader** - Displays student name, school, company

---

## 8. Form DTOs

### 8.1 Parent Form DTO

**File:** `Models/ParentForm/ParentFormDto.cs`

```csharp
public class ParentFormDto
{
    public Guid PlacementId { get; set; }
    public string StudentName { get; set; }
    public string SchoolName { get; set; }
    public int CurrentStep { get; set; } = 1;
    public DateTime? LastSavedAt { get; set; }

    public StudentDetailsDto StudentDetails { get; set; }
    public EmergencyContactDto EmergencyContact { get; set; }
    public WorkplaceDetailsDto WorkplaceDetails { get; set; }
    public TransportDto Transport { get; set; }
    public MedicalDetailsDto MedicalDetails { get; set; }
    public ConsentDto Consent { get; set; }
}
```

### 8.2 Employer Form DTO

**File:** `Models/EmployerForm/EmployerFormDto.cs`

```csharp
public class EmployerFormDto
{
    public Guid PlacementId { get; set; }
    public string StudentName { get; set; }
    public string SchoolName { get; set; }
    public string CompanyName { get; set; }

    public WorkplaceDetailsDto WorkplaceDetails { get; set; }
    public SupervisorDetailsDto SupervisorDetails { get; set; }
    public InsuranceDto Insurance { get; set; }
    public OhsDto Ohs { get; set; }
    public GeneralTravelDto GeneralTravel { get; set; }
    public HazardsAppendixDto HazardsAppendix { get; set; }

    public int CurrentStep { get; set; } = 1;
    public DateTime? LastSavedAt { get; set; }
}
```

### 8.3 Work History DTO

**File:** `Models/StudentWorkHistory/WorkHistoryFormDto.cs`

```csharp
public class WorkHistoryFormDto
{
    public List<CourseDto> CurrentCourses { get; set; } = new();
    public List<QualificationDto> VetQualifications { get; set; } = new();
    public CertificatesDto Certificates { get; set; } = new();
    public List<EmploymentDto> PartTimeEmployment { get; set; } = new();
    public List<CommunityServiceDto> CommunityService { get; set; } = new();
}
```

**CertificatesDto** contains 25 certificates:
- 16 WorkSafe SmartMove modules (Automotive, Building, Business/IT, etc.)
- 9 Other certificates (White Card, First Aid, Bronze Medallion, etc.)

Each certificate has: `bool Is{Name}` + `DateOnly? {Name}Date`

---

## 9. Controllers

### 9.1 Controller Pattern

```csharp
[Authorize]
public class StudentsController : Controller
{
    private readonly IStudentService _studentService;
    private readonly ITenantProvider? _tenantProvider;

    public StudentsController(
        IStudentService studentService,
        ITenantProvider? tenantProvider = null)
    {
        _studentService = studentService;
        _tenantProvider = tenantProvider;
    }

    public async Task<IActionResult> Index()
    {
        var tenantId = _tenantProvider?.GetCurrentTenantId();
        var students = await _studentService.GetAllAsync(tenantId);
        return View(students);
    }
}
```

### 9.2 Controllers List

| Controller | Purpose |
|------------|---------|
| AuthenticationController | Login/logout |
| HomeController | Landing pages |
| StudentsController | Student CRUD |
| PlacementsController | Placement CRUD + form review |
| CompaniesController | Company CRUD |
| SupervisorsController | Supervisor CRUD |
| EmergencyContactsController | Emergency contacts |
| StudentMedicalConditionsController | Medical conditions |
| SchoolsController | School management |
| UsersController | User management |
| TenantController | Tenant management |
| ParentController | Parent form handling |
| EmployerController | Employer form handling |

---

## 10. JSON Serialization

Used for storing flexible data in `StudentWorkHistory` entity.

**Pattern:**
```csharp
private static readonly JsonSerializerOptions JsonOptions = new()
{
    PropertyNameCaseInsensitive = true
};

// Deserialize
var courses = JsonSerializer.Deserialize<List<CourseDto>>(entity.CurrentCourses, JsonOptions);

// Serialize
entity.CurrentCourses = JsonSerializer.Serialize(dto.CurrentCourses, JsonOptions);
```

---

## 11. Database Configuration

**DbContext:** `Data/ApplicationDbContext.cs`

**DbSets (19 total):**
- Schools, Users, Students, Teachers, EmergencyContacts
- StudentMedicalConditions, Companies, Supervisors, Placements
- ParentPermissions, FormTokens, LogbookEntries, LogbookTasks
- LogbookEvaluations, StudentWorkHistories

**Key Features:**
- Global query filter for soft delete
- SaveChanges override for audit trail
- Auto-sets TenantId on new TenantEntity instances
- Optimistic concurrency via RowVersion

**Unique Indexes:**
- User.UserName, User.Email
- School.TenantKey
- Teacher.UserId (one-to-one)
- FormToken.Token
- StudentWorkHistory.StudentId (one per student)

---

## 12. Authentication

**Type:** Cookie-based authentication

**Configuration:**
```csharp
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Authentication/Login";
        options.LogoutPath = "/Authentication/Logout";
        options.Cookie.Name = "FutureReadyAuth";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
    });
```

**Token-based form access:**
- Parent/employer forms accessed via FormToken
- No login required - token provides authorization
- Tokens are single-use with 14-day expiry

---

## 13. Naming Conventions

### Entities
- DbSet names: Plural (Students, Placements)
- Class names: Singular (Student, Placement)
- Foreign keys: `{EntityName}Id` (StudentId, PlacementId)
- Navigation properties: Entity name (Student, Placement)

### Services
- Interface: `I{Entity}Service`
- Implementation: `{Entity}Service`
- Location: `Services/{Domain}/`

### Controllers
- Class: `{Entity}Controller` or `{Entities}Controller`
- Location: `Controllers/`
- Methods: Index, Details, Create, Edit, Delete, DeleteConfirmed

### Blazor Components
- Location: `Components/Pages/{Domain}/`
- Naming: PascalCase (ParentWizard.razor)
- Step components in `Steps/` subdirectory
- Shared components in `Shared/` subdirectory

### Views
- Location: `Views/{ControllerName}/`
- Naming: {ActionName}.cshtml

---

## 14. Common Patterns

### Checkbox + Conditional Fields (Medical Details)

```razor
<div class="form-check">
    <InputCheckbox @bind-Value="FormData.MedicalDetails.HasAsthma" class="form-check-input" id="hasAsthma" />
    <label class="form-check-label" for="hasAsthma">Asthma</label>
</div>
@if (FormData.MedicalDetails.HasAsthma)
{
    <div class="mt-2">
        <label>Asthma Details</label>
        <InputTextArea @bind-Value="FormData.MedicalDetails.AsthmaDetails" class="form-control" />
    </div>
}
```

### Service Tenant Isolation

```csharp
public async Task<List<Student>> GetAllAsync(Guid? tenantId = null)
{
    tenantId ??= _tenantProvider?.GetCurrentTenantId();

    var query = _context.Students.AsQueryable();

    if (tenantId.HasValue)
        query = query.Where(s => s.TenantId == tenantId.Value);

    return await query.ToListAsync();
}
```

### Form Token Validation

```csharp
public async Task<FormToken?> ValidateTokenAsync(string token)
{
    var formToken = await _context.FormTokens
        .Include(t => t.Placement)
        .FirstOrDefaultAsync(t => t.Token == token);

    if (formToken == null || !formToken.IsValid)
        return null;

    return formToken;
}
```

---

## 15. Key Files Quick Reference

| Purpose | File Path |
|---------|-----------|
| DbContext | `Data/ApplicationDbContext.cs` |
| Base entity | `Models/BaseEntity.cs` |
| Tenant entity | `Models/TenantEntity.cs` |
| Student model | `Models/School/Student.cs` |
| Placement model | `Models/School/Placement.cs` |
| Company model | `Models/School/Company.cs` |
| Parent form DTO | `Models/ParentForm/ParentFormDto.cs` |
| Employer form DTO | `Models/EmployerForm/EmployerFormDto.cs` |
| Work history DTO | `Models/StudentWorkHistory/WorkHistoryFormDto.cs` |
| Parent wizard | `Components/Pages/ParentForm/ParentWizard.razor` |
| Employer wizard | `Components/Pages/EmployerForm/EmployerWizard.razor` |
| Work history form | `Components/Pages/StudentWorkHistory/WorkHistoryForm.razor` |
| Tenant provider | `Services/HttpContextTenantProvider.cs` |
| Form token service | `Services/FormTokens/FormTokenService.cs` |
| Startup config | `Program.cs` |

---

## 16. Status Values

**Placement.Status:**
- `draft` - Initial state
- `pending_parent` - Awaiting parent permission
- `pending_employer` - Awaiting employer form
- `confirmed` - Both forms completed

**StudentMedicalCondition.ConditionType:**
- `Asthma`, `Diabetes`, `Epilepsy`, `Allergies`
- `LearningDifficulties`, `Medication`, `Other`

**ParentPermission.TransportMethod:**
- `public` - Public transport
- `private_car` - Private vehicle
- `combination` - Both methods

**FormToken.FormType:**
- `parent` - Parent permission form
- `employer` - Employer acceptance form
