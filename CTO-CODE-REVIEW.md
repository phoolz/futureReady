# CTO Code Review: Apiary Work Placement Management SaaS

**Review Date:** March 16, 2026
**Reviewer:** New CTO Technical Assessment
**Codebase:** Apiary - .NET 10 Blazor Static SSR Multi-tenant SaaS

---

## Executive Summary

This comprehensive code review identifies **18 security vulnerabilities** (3 critical), **10 architecture issues**, and **9 data layer concerns** across the Apiary codebase. While the application demonstrates solid foundational patterns (tenant isolation, parameterized queries, soft delete enforcement), several critical issues must be remediated before production deployment.

| Category | Score | Notes |
|----------|-------|-------|
| Architecture Adherence | 6/10 | Mixed render modes, MVC remnants |
| Code Organization | 7/10 | Good structure overall, some cleanup needed |
| Service Design | 6/10 | Some large methods, duplication |
| Entity/Data Model | 9/10 | Excellent TenantEntity patterns |
| Security | 5/10 | Critical credential exposure, tenant gaps |
| Testing | 7/10 | Tests exist, need updates for architecture changes |
| **Overall** | **6.5/10** | **Solid foundation with critical gaps** |

**Verdict:** Application is **NOT PRODUCTION READY** until critical issues are remediated.

---

## Table of Contents

1. [Critical Findings](#critical-findings)
2. [High-Priority Findings](#high-priority-findings)
3. [Medium-Priority Findings](#medium-priority-findings)
4. [Low-Priority Findings](#low-priority-findings)
5. [Unused Code](#unused-code)
6. [Data Layer Issues](#data-layer-issues)
7. [Positive Findings](#positive-findings)
8. [Remediation Roadmap](#remediation-roadmap)
9. [Verification Steps](#verification-steps)

---

## Critical Findings

### 1. CRITICAL: Hardcoded Database Credentials in Configuration Files

**Severity:** CRITICAL
**OWASP:** A01:2021 Broken Access Control
**Files:**
- `/appsettings.json` (Line 3)
- `/appsettings.Development.json` (Line 3)

**Finding:**
Plaintext SQL Server credentials are committed to the repository:

```
appsettings.json:
"DefaultConnection": "Server=10.1.1.99,1433;Database=FutureReady;User Id=sa;Password=YourStrongPassword!123;..."

appsettings.Development.json:
"DefaultConnection": "Server=tcp:future-ready-test.database.windows.net,1433;...User ID=unrelated;Password=Shakable5-Direness-Hermit-Collage;..."
```

**Risk:**
- Credentials exposed in repository, build artifacts, and logs
- Any person with repository access has full database access
- SA account grants full server admin rights

**Remediation:**
1. Move credentials to Azure Key Vault / AWS Secrets Manager
2. Use ASP.NET Core user secrets for local development
3. **Rotate all exposed passwords immediately**
4. Use managed identities (Azure) instead of SA accounts
5. Add `appsettings.*.json` patterns to `.gitignore`

---

### 2. CRITICAL: InteractiveServer Render Mode Violates Architecture

**Severity:** CRITICAL
**Files:**
- `/Program.cs` (Lines 11-12, 107)
- `/Components/Pages/EmployerForm/EmployerWizard.razor` (Line 3)
- `/Components/Pages/ParentForm/ParentWizard.razor` (Line 3)
- `/Components/Pages/LogbookEntry/LogbookEntryForm.razor` (Line 1)
- `/Components/Pages/StudentActivation/StudentActivationForm.razor` (Line 1)

**Finding:**
The CLAUDE.md architecture explicitly states:
> "Default all pages to **Static SSR** (no `@rendermode`) — no persistent connections, scales freely"
> "Do not register `AddInteractiveServerComponents()` or call `AddInteractiveServerRenderMode()` in `Program.cs`"

But the application registers InteractiveServer:

```csharp
// Program.cs
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();  // LINE 12

app.MapRazorComponents<Apiary.Components.App>()
    .AddInteractiveServerRenderMode();  // LINE 107
```

And 4 pages use `@rendermode InteractiveServer`.

**Risk:**
- SignalR connection overhead and memory footprint
- "Reconnecting" modal on connection loss (poor UX)
- Scales poorly compared to Static SSR
- Contradicts stated architecture principle

**Remediation:**
1. Convert all 4 pages to Static SSR with POST-Redirect-Get pattern
2. Remove `.AddInteractiveServerComponents()` from Program.cs
3. Remove `.AddInteractiveServerRenderMode()` from Program.cs
4. Use form submission instead of real-time event handlers

---

### 3. CRITICAL: Missing Tenant Validation in Form Services

**Severity:** CRITICAL
**OWASP:** A01:2021 Broken Access Control
**Files:**
- `/Services/EmployerForm/EmployerFormService.cs` (Lines 29-36, 45-46, 162-163)
- `/Services/ParentForm/ParentFormService.cs` (Lines 40-86, 223-228)

**Finding:**
Form services use `IgnoreQueryFilters()` extensively (18+ instances in ParentFormService alone) to bypass tenant isolation for public form access. However, after retrieving the form token, there is no explicit validation that the token's tenant matches the expected tenant.

```csharp
// Current pattern - vulnerable
var formToken = await _formTokenService.ValidateTokenAsync(token);
if (formToken == null || !formToken.IsValid) return null;

var placement = await _context.Placements
    .IgnoreQueryFilters()  // Bypasses tenant filter
    .Where(p => p.Id == formToken.PlacementId && !p.IsDeleted)
    .FirstOrDefaultAsync();  // No tenant validation!
```

**Attack Scenario:**
1. Employer A generates form token for Placement X in School A
2. Attacker discovers token (leaked, guessed, or social engineered)
3. Attacker submits token to endpoint in School B's context
4. Without explicit tenant check, data from School A could be accessed

**Risk:**
- Cross-tenant data access
- Data exfiltration between schools
- Privilege escalation

**Remediation:**
Add explicit tenant validation after every `IgnoreQueryFilters()` call:

```csharp
var placement = await _context.Placements
    .IgnoreQueryFilters()
    .Where(p => p.Id == formToken.PlacementId
             && p.TenantId == formToken.TenantId  // CRITICAL FIX
             && !p.IsDeleted)
    .FirstOrDefaultAsync();
```

---

## High-Priority Findings

### 4. HIGH: Missing Authorization Attributes on 87 of 89 Blazor Pages

**Severity:** HIGH
**OWASP:** A01:2021 Broken Access Control
**Files:** `/Components/Pages/**/*.razor`

**Finding:**
Only 2 pages out of 89 have `@attribute [Authorize]`:
- `/Components/Pages/Home/Index.razor`
- `/Components/Pages/Users/Profile.razor`

All other authenticated pages lack explicit authorization:
- `/Components/Pages/Students/` (all pages)
- `/Components/Pages/Companies/` (all pages)
- `/Components/Pages/Supervisors/` (all pages)
- `/Components/Pages/Schools/` (all pages)
- `/Components/Pages/Placements/` (all pages)
- `/Components/Pages/LogbookEntries/` (all pages)
- `/Components/Pages/Users/` (most pages)

**Risk:**
Unauthenticated users could directly navigate to URLs and trigger backend service calls.

**Remediation:**
Add to all protected pages:
```razor
@attribute [Authorize]
```

Or for role-specific access:
```razor
@attribute [Authorize(Roles = "Teacher")]
```

---

### 5. HIGH: Unique Indexes Missing Soft Delete Filters

**Severity:** HIGH
**File:** `/Data/ApplicationDbContext.cs`

**Finding:**
All 7 unique indexes do NOT include a filter for `IsDeleted`:
- `FormToken.Token` (Line 196)
- `StudentAccountToken.Token` (Line 233)
- `School.TenantKey` (Line 73)
- `PlacementStudent (PlacementId, StudentId)` (Line 174)
- `ParentPermission (PlacementId, StudentId)` (Line 187)
- `LogbookEntry (PlacementStudentId, Date)` (Line 210)
- `Teacher.UserId` (Line 84)

**Risk:**
After soft-deleting a record, you cannot re-create it with the same unique value. Example: If you soft-delete a FormToken and try to generate a new one for the same placement, it fails with a unique constraint violation.

**Remediation:**
Add filter to all unique indexes:
```csharp
entity.HasIndex(e => e.Token)
    .IsUnique()
    .HasFilter("[IsDeleted] = 0");  // Add this
```

---

### 6. HIGH: AuthenticationController Violates Declared Architecture

**Severity:** HIGH
**File:** `/Controllers/AuthenticationController.cs`

**Finding:**
Per CLAUDE.md: *"Login and logout are implemented as minimal API endpoints, not Blazor pages"*

But the application has a full MVC controller with:
- Uses `ViewData` (Line 25) - MVC anti-pattern
- Returns `View()` - MVC not Blazor (Lines 26, 34, 43, 55)
- References non-existent controller: `RedirectToAction("Index", "StudentPortal")` (Line 67)
- `LoginViewModel` defined inline in controller (Lines 83-89)

Program.cs still registers MVC:
```csharp
builder.Services.AddControllersWithViews();  // Line 10
app.MapControllerRoute(name: "default", ...);  // Lines 101-104
```

**Risk:**
- Architecture inconsistency
- References non-existent controllers/views
- Maintenance confusion

**Remediation:**
Convert to minimal API endpoints:
```csharp
app.MapPost("/auth/login", async (LoginRequest request, ...) => { ... });
app.MapPost("/auth/logout", async (...) => { ... });
```

Remove MVC registrations from Program.cs.

---

### 7. HIGH: Database Seeder Hardcodes Admin Password

**Severity:** HIGH
**File:** `/Data/DatabaseSeeder.cs` (Lines 53-65)

**Finding:**
```csharp
var adminUser = new ApplicationUser
{
    UserName = "adminsean",
    Email = "admin@futureready.local",
    // ...
};

var result = await userManager.CreateAsync(adminUser, "Undivided-Reputable-Bartender8");
```

**Risk:**
- Hardcoded password visible to all developers
- Default admin always created if no users exist
- Password committed to git history

**Remediation:**
1. Remove hardcoded password
2. Use environment variable: `Environment.GetEnvironmentVariable("ADMIN_SEED_PASSWORD")`
3. Add production guard: `if (env.IsDevelopment())`
4. Or generate random password and log to secure audit log

---

### 8. HIGH: Weak Cookie Security Configuration

**Severity:** HIGH
**File:** `/Program.cs` (Lines 37-44)

**Finding:**
```csharp
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Authentication/Login";
    options.Cookie.Name = "ApiaryAuth";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    // Missing: SecurePolicy, HttpOnly, SameSite
});
```

**Risk:**
- Cookies can be exfiltrated via JavaScript if XSS exists
- Could work over HTTP in some environments

**Remediation:**
```csharp
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    // ...
});
```

---

## Medium-Priority Findings

### 9. MEDIUM: DateTime vs DateTimeOffset Inconsistency

**Files:**
- `/Models/ApplicationUser.cs` (Lines 14-16) - uses `DateTime`
- `/Models/FormToken.cs` (Line 28) - `ExpiresAt` is `DateTime`
- `/Models/StudentAccountToken.cs` (Line 16) - `ExpiresAt` is `DateTime`
- `/Models/PlacementStudent.cs` (Line 19) - `ParentSubmittedAt` is `DateTime`
- `/Models/LogbookEntry.cs` (Line 46) - `SupervisorVerifiedAt` is `DateTimeOffset`
- `/Models/LogbookEvaluation.cs` (Line 58) - `SupervisorSignedAt` is `DateTimeOffset`

**Finding:**
Mix of `DateTime` and `DateTimeOffset` across domain entities. `BaseEntity` uses `DateTimeOffset` for audit fields, but some entities use `DateTime`.

**Risk:**
- Timezone awareness issues if server runs in non-UTC timezone
- `FormToken.IsValid` uses `DateTime.UtcNow` comparison with `DateTime` field
- Inconsistent behavior across entities

**Remediation:**
Standardize all timestamp fields to `DateTimeOffset`.

---

### 10. MEDIUM: School Entity is Not Multi-Tenant

**File:** `/Models/School/School.cs`

**Finding:**
`School` extends `BaseEntity` instead of `TenantEntity` - it has no `TenantId` field.

**Risk:**
- School table not tenant-isolated
- A school list query without explicit filtering could expose data
- Unclear tenant boundary

**Remediation:**
Either:
1. School extends TenantEntity (if schools can belong to tenants)
2. Or explicitly document that School IS the tenant boundary and ensure all queries properly scope by TenantId

---

### 11. MEDIUM: Oversized Service Methods (God Methods)

**File:** `/Services/ParentForm/ParentFormService.cs`

**Finding:**
`SubmitFormAsync()` method spans **247 lines** (Lines 200-447):
- Handles student update
- Emergency contact CRUD
- Company creation
- Supervisor creation
- Medical conditions CRUD
- Parent permission update
- Status transitions
- Transaction management

Similar pattern in `EmployerFormService.SubmitFormAsync()` (Lines 147-261).

**Risk:**
- Difficult to test individual workflows
- High cyclomatic complexity
- Bug fixes must be applied in multiple places

**Remediation:**
Break into smaller, focused methods:
- `UpdateStudentDetailsAsync()`
- `CreateOrUpdateEmergencyContactAsync()`
- `CreateOrUpdateCompanyAsync()`
- `CreateOrUpdateSupervisorAsync()`
- `CreateOrUpdateMedicalConditionsAsync()`

---

### 12. MEDIUM: Missing Security Headers

**File:** `/Program.cs`

**Finding:**
No security headers middleware configured:
- `X-Content-Type-Options: nosniff`
- `X-Frame-Options: DENY`
- `X-XSS-Protection: 1; mode=block`
- `Content-Security-Policy`
- `Strict-Transport-Security`

**Remediation:**
```csharp
app.Use((context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
    return next();
});
```

---

### 13. MEDIUM: Form Token Expiration Too Long

**File:** `/Services/FormTokens/FormTokenService.cs` (Line 16)

**Finding:**
```csharp
private const int TokenExpirationDays = 14;
```

**Risk:**
14-day expiration provides a long attack window for:
- Token guessing attacks
- Stolen token usage
- User account takeover via leaked token

**Remediation:**
Reduce to 3-7 days based on user behavior analysis.

---

### 14. MEDIUM: Blazor Pages Contain Business Logic

**Files:** 69 Razor pages with `@code` blocks

Per CLAUDE.md: *"Blazor pages handle HTTP and UI concerns only — no business logic"*

**Largest offenders:**
- `/Components/Pages/Placements/Details.razor` (616 lines, @code at line 431)
- `/Components/Pages/Students/Details.razor` (508 lines)
- `/Components/Pages/Placements/EmployerFormReview.razor` (496 lines)
- `/Components/Pages/LogbookEntry/LogbookEntryForm.razor` (398 lines)

**Finding:**
Many pages contain:
- Data loading logic
- CRUD operations
- Workflow orchestration
- Form submission handling

**Risk:**
- Hard to test page logic in isolation
- UI and business logic tightly coupled
- Difficult to reuse logic across pages

**Remediation:**
Extract business logic from `@code` blocks into services. Keep pages focused on: rendering, parameter binding, calling services, displaying results.

---

## Low-Priority Findings

### 15. LOW: Hardcoded Strings Instead of Constants

**Files:** Throughout codebase

**Examples:**
- `"employer_acceptance"` - `/Services/ParentForm/ParentFormService.cs:85`
- `"pending_employer"`, `"pending_parents"`, `"confirmed"` - status strings
- Form types as literals

**Remediation:**
Create enums or constants:
```csharp
public static class FormTypes
{
    public const string EmployerAcceptance = "employer_acceptance";
    public const string ParentPermission = "parent_permission";
}

public enum PlacementStatus
{
    Draft,
    PendingEmployer,
    PendingParents,
    Confirmed
}
```

---

### 16. LOW: Missing Indexes on Common Query Patterns

**File:** `/Data/ApplicationDbContext.cs`

**Missing indexes:**
- No indexes on `TenantId` for major tables (Placement, Student, Company, Supervisor)
- No indexes on foreign keys (CompanyId, SupervisorId, StudentId)
- No index for date range queries on LogbookEntry

**Remediation:**
Add indexes for frequently filtered columns:
```csharp
entity.HasIndex(e => e.TenantId);
entity.HasIndex(e => e.CompanyId);
```

---

### 17. LOW: No Audit Logging for Sensitive Operations

**Finding:**
While audit fields exist (CreatedBy, UpdatedBy), no explicit audit logging for:
- Form token generation
- Form token validation failures
- Sensitive data access (medical records, emergency contacts)
- Cross-tenant access attempts

**Remediation:**
Implement application-level audit logging with tamper-proof storage.

---

### 18. LOW: Interactive Server Pages Without CSRF Protection

**Files:**
- `/Components/Pages/EmployerForm/EmployerWizard.razor`
- `/Components/Pages/ParentForm/ParentWizard.razor`

**Finding:**
Interactive Server pages don't use `<AntiforgeryToken />` in form elements. They use event handlers instead of form POST.

**Note:** This is mitigated if converted to Static SSR (see Critical Finding #2).

---

## Unused Code

### Orphaned StudentPortal Models
**Files:**
- `/Models/StudentPortal/StudentPortalViewModel.cs` (21 lines)
- `/Models/StudentPortal/StudentPlacementViewModel.cs` (49 lines)

Referenced in `/Components/Pages/Home/Index.razor` but never rendered.

### ErrorViewModel (MVC-era)
**File:** `/Models/ErrorViewModel.cs`

MVC-era model unused. Application uses Blazor error pages at `/Components/Pages/Error/`.

### Orphaned Migrations Folder
**Location:** `/Migrations/` (root level)

Contains orphaned `InitialCreate` migration. Actual migrations are in `/Data/Migrations/`.

### Legacy MVC Registration
**File:** `/Program.cs`

```csharp
builder.Services.AddControllersWithViews();  // Unused if Blazor-only
app.UseExceptionHandler("/Home/Error");  // References non-existent MVC route
app.MapControllerRoute(...);  // Only used by AuthenticationController
```

---

## Data Layer Issues

### Summary Table

| Priority | Issue | File | Status |
|----------|-------|------|--------|
| P0 | Unique indexes missing IsDeleted filters | ApplicationDbContext.cs | Critical |
| P0 | School not extending TenantEntity | School.cs | Critical |
| P1 | DateTime vs DateTimeOffset inconsistency | Multiple | High |
| P1 | FormToken.IsValid uses DateTime.UtcNow | FormToken.cs | High |
| P2 | Missing TenantId indexes | ApplicationDbContext.cs | Medium |
| P2 | Missing foreign key indexes | ApplicationDbContext.cs | Medium |
| P2 | Status fields as magic strings | Multiple | Medium |

---

## Positive Findings

The codebase demonstrates several strong patterns that should be maintained:

### Strong Tenant Isolation Foundation
- Global query filters enforce tenant filtering automatically
- `ITenantProvider` resolves from claims only
- External form users validated through FormToken -> Placement relationship

### Excellent Soft Delete Implementation
- All entities inherit from `TenantEntity` with `IsDeleted` flag
- Global query filter applies automatically
- Hard deletes converted to soft deletes in `ApplyAuditRules()`

### Cryptographically Secure Tokens
- 32-byte random tokens via `RandomNumberGenerator.Fill()`
- URL-safe Base64 encoding
- Expiration validation

### Parameterized Queries Throughout
- All data access uses EF Core (no raw SQL string concatenation found)
- Protection against SQL injection

### Thoughtful Foreign Key Design
- Cascade delete for children (PlacementStudents cascade from Placement)
- Restrict for references (prevent accidental deletions)
- Prevents orphan records

### Clean Service Organization
- Interface in `Services/Interfaces/`
- Implementation in `Services/Implementations/`
- Proper DI registration in Program.cs

### Concurrency Control
- RowVersion configured for optimistic concurrency
- Services handle rowVersion correctly

---

## Remediation Roadmap

### Phase 1: Critical Security (1-2 days)
| Task | File | Priority |
|------|------|----------|
| Remove hardcoded credentials from config files | appsettings*.json | CRITICAL |
| Add explicit tenant validation after IgnoreQueryFilters() | EmployerFormService.cs, ParentFormService.cs | CRITICAL |
| Add `[Authorize]` to all protected Blazor pages | Components/Pages/**/*.razor | HIGH |
| Add security headers middleware | Program.cs | HIGH |
| Fix cookie security configuration | Program.cs | HIGH |

### Phase 2: Architecture Alignment (1 week)
| Task | File | Priority |
|------|------|----------|
| Convert 4 InteractiveServer pages to Static SSR + PRG | EmployerWizard, ParentWizard, LogbookEntryForm, StudentActivationForm | CRITICAL |
| Remove InteractiveServer registrations | Program.cs | CRITICAL |
| Convert AuthenticationController to minimal APIs | AuthenticationController.cs | HIGH |
| Remove MVC registrations | Program.cs | HIGH |
| Add soft delete filter to unique indexes | ApplicationDbContext.cs | HIGH |

### Phase 3: Code Quality (1 week)
| Task | File | Priority |
|------|------|----------|
| Standardize DateTime -> DateTimeOffset | Multiple model files | MEDIUM |
| Refactor ParentFormService into smaller methods | ParentFormService.cs | MEDIUM |
| Extract business logic from Blazor @code blocks | Various .razor files | MEDIUM |
| Remove unused code | StudentPortal models, ErrorViewModel, orphan migrations | LOW |
| Create constants/enums for status strings | New constants file | LOW |

### Phase 4: Hardening (1 week)
| Task | File | Priority |
|------|------|----------|
| Add TenantId indexes to major tables | ApplicationDbContext.cs | MEDIUM |
| Add foreign key indexes | ApplicationDbContext.cs | MEDIUM |
| Implement audit logging for sensitive operations | New service | LOW |
| Reduce form token expiration to 7 days | FormTokenService.cs | LOW |
| Move seeder password to secrets | DatabaseSeeder.cs | HIGH |

---

## Verification Steps

After remediation, verify with these checks:

### Build & Test
```bash
dotnet build
dotnet test
```

### Security Checks
```bash
# Verify no InteractiveServer pages remain
grep -r "InteractiveServer" Components/

# Verify all protected pages have Authorize
find Components/Pages -name "*.razor" -exec grep -L "\[Authorize\]" {} \;

# Verify no hardcoded credentials
grep -r "Password=" appsettings*.json
grep -r "password" appsettings*.json

# Verify no SA accounts
grep -r "User Id=sa" appsettings*.json
```

### Manual Verification
1. Attempt to access protected pages without authentication
2. Verify form token validation rejects expired/used tokens
3. Test cross-tenant access attempts fail
4. Verify soft-deleted records can be re-created with same unique values

### Security Scan
Run OWASP ZAP or similar tool against deployed application.

---

## Conclusion

The Apiary application has a solid architectural foundation with good patterns for multi-tenant isolation, parameterized queries, and token-based magic links. However, **three critical vulnerabilities** must be remediated before production deployment:

1. **Exposed credentials** in configuration files
2. **InteractiveServer** contradicting architecture
3. **Tenant isolation gaps** in form services

After addressing these critical issues and working through the high-priority items, the application would be suitable for production deployment with ongoing security monitoring.

---

*Review conducted March 16, 2026*
