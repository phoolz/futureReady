# MVC to Blazor Migration Plan

## Overview

This document outlines the staged migration of MVC controllers and views to Blazor Static SSR pages. The migration is organized into 9 stages based on dependencies, complexity, and testability.

---

## Stage 1: Shared Infrastructure & Layouts

**Risk Level:** Low
**Required by:** All other pages

| Component | File Path | Notes |
|-----------|-----------|-------|
| Main Layout | `Views/Shared/_Layout.cshtml` | Main layout with sidebar |
| Auth Layout | `Views/Shared/_LayoutAuth.cshtml` | Minimal auth layout |
| Sidebar | `Views/Shared/_Sidebar.cshtml` | Role-aware navigation |
| Validation Scripts | `Views/Shared/_ValidationScriptsPartial.cshtml` | May not be needed in Blazor |
| Error Page | `Views/Shared/Error.cshtml` | Global error page |
| Access Denied | `Views/Error/AccessDenied.cshtml` | Error page |

**Testing Checklist:**
- [ ] Layout renders correctly
- [ ] Sidebar shows correct nav items per role (SiteAdmin, Teacher, Student)
- [ ] Error pages display properly
- [ ] Responsive design works

---

## Stage 2: Simple Reference Data (Companies & Supervisors)

**Risk Level:** Low
**Authorization:** Teacher only
**Dependencies:** Supervisors depends on Companies (migrate Companies first)

### CompaniesController

| View | File Path | Action |
|------|-----------|--------|
| Index | `Views/Companies/Index.cshtml` | List all companies |
| Details | `Views/Companies/Details.cshtml` | View company with supervisors and placements |
| Create | `Views/Companies/Create.cshtml` | Create company form |
| Edit | `Views/Companies/Edit.cshtml` | Edit company form |
| Delete | `Views/Companies/Delete.cshtml` | Delete confirmation |

### SupervisorsController

| View | File Path | Action |
|------|-----------|--------|
| Index | `Views/Supervisors/Index.cshtml` | List all supervisors |
| Details | `Views/Supervisors/Details.cshtml` | View supervisor details |
| Create | `Views/Supervisors/Create.cshtml` | Create form with company dropdown |
| Edit | `Views/Supervisors/Edit.cshtml` | Edit form with company dropdown |
| Delete | `Views/Supervisors/Delete.cshtml` | Delete confirmation |

**Testing Checklist:**
- [ ] Full CRUD cycle for Companies
- [ ] Full CRUD cycle for Supervisors
- [ ] Supervisor-Company relationship works
- [ ] Tenant isolation verified
- [ ] Soft delete works correctly

---

## Stage 3: Schools (Site Admin)

**Risk Level:** Low
**Authorization:** SiteAdmin only
**Dependencies:** None (not tenant-scoped)

### SchoolsController

| View | File Path | Action |
|------|-----------|--------|
| Index | `Views/Schools/Index.cshtml` | List all schools |
| Details | `Views/Schools/Details.cshtml` | View school details |
| Create | `Views/Schools/Create.cshtml` | Create school form |
| Edit | `Views/Schools/Edit.cshtml` | Edit school form |
| Delete | `Views/Schools/Delete.cshtml` | Delete confirmation |

**Testing Checklist:**
- [ ] Full CRUD cycle
- [ ] Only SiteAdmin can access
- [ ] Non-SiteAdmin users get Access Denied

---

## Stage 4: Users & Authentication

**Risk Level:** Medium (sensitive functionality)
**Authorization:** SiteAdmin for user management, all authenticated for Profile

### AuthenticationController

| View | File Path | Action |
|------|-----------|--------|
| Login | `Views/Authentication/Login.cshtml` | Login form |

**Note:** Per CLAUDE.md, login/logout may stay as minimal API endpoints rather than Blazor pages.

**Actions:**
- Login (GET/POST)
- Logout (POST)

### UsersController

| View | File Path | Action |
|------|-----------|--------|
| Index | `Views/Users/Index.cshtml` | List users with roles |
| Details | `Views/Users/Details.cshtml` | View user details |
| Create | `Views/Users/Create.cshtml` | Create user with role assignment |
| Edit | `Views/Users/Edit.cshtml` | Edit user, update role, reset password |
| Delete | `Views/Users/Delete.cshtml` | Delete confirmation |
| Profile | `Views/Users/Profile.cshtml` | Current user profile (all authenticated) |

**Testing Checklist:**
- [ ] Login flow works
- [ ] Logout flow works
- [ ] Role-based redirect after login (Student → StudentPortal, others → Home)
- [ ] User CRUD (SiteAdmin only)
- [ ] Profile self-edit (all authenticated users)
- [ ] Password reset functionality
- [ ] Role assignment works

---

## Stage 5: Students & Related Entities

**Risk Level:** Medium
**Authorization:** Teacher only
**Dependencies:** Migrate Students first, then child entities

### StudentsController

| View | File Path | Action |
|------|-----------|--------|
| Index | `Views/Students/Index.cshtml` | List all students |
| Details | `Views/Students/Details.cshtml` | View student with placements, logbook summaries, activation tokens |
| Create | `Views/Students/Create.cshtml` | Create student form |
| Edit | `Views/Students/Edit.cshtml` | Edit form with user linking dropdown |
| Delete | `Views/Students/Delete.cshtml` | Delete confirmation |

**Additional Actions:**
- GenerateActivationLink (POST)
- ResendActivationLink (POST)
- DeleteActivationToken (POST)

### EmergencyContactsController

| View | File Path | Action |
|------|-----------|--------|
| Index | `Views/EmergencyContacts/Index.cshtml` | List contacts for student |
| Details | `Views/EmergencyContacts/Details.cshtml` | View contact details |
| Create | `Views/EmergencyContacts/Create.cshtml` | Create contact for student |
| Edit | `Views/EmergencyContacts/Edit.cshtml` | Edit contact |
| Delete | `Views/EmergencyContacts/Delete.cshtml` | Delete confirmation |

### StudentMedicalConditionsController

| View | File Path | Action |
|------|-----------|--------|
| Index | `Views/StudentMedicalConditions/Index.cshtml` | List conditions for student |
| Details | `Views/StudentMedicalConditions/Details.cshtml` | View condition details |
| Create | `Views/StudentMedicalConditions/Create.cshtml` | Create condition with type dropdown |
| Edit | `Views/StudentMedicalConditions/Edit.cshtml` | Edit condition |
| Delete | `Views/StudentMedicalConditions/Delete.cshtml` | Delete confirmation |

**Testing Checklist:**
- [ ] Student CRUD
- [ ] Student-User linking works
- [ ] Activation link generation
- [ ] Activation link resend/revoke
- [ ] Emergency contact CRUD with student context
- [ ] Medical condition CRUD with student context
- [ ] Condition type dropdown populated
- [ ] Soft delete works
- [ ] Tenant isolation verified

---

## Stage 6: Placements (Core Workflow)

**Risk Level:** High (most complex controller)
**Authorization:** TeacherOrStudent (some actions Teacher only)
**Dependencies:** Companies, Supervisors, Students

### PlacementsController

| View | File Path | Action |
|------|-----------|--------|
| Index | `Views/Placements/Index.cshtml` | List placements (role-filtered) |
| Details | `Views/Placements/Details.cshtml` | View placement with form tokens, logbook summary |
| Create | `Views/Placements/Create.cshtml` | Create placement form |
| Edit | `Views/Placements/Edit.cshtml` | Edit form with student assignment UI |
| Delete | `Views/Placements/Delete.cshtml` | Delete confirmation |
| EmployerFormReview | `Views/Placements/EmployerFormReview.cshtml` | View submitted employer form data |

**Additional Actions (Teacher only):**
- AddStudent (POST) - Add student to placement
- RemoveStudent (POST) - Remove student from placement
- SendEmployerForm (POST) - Generate employer form token
- ResendEmployerForm (POST) - Revoke and regenerate token
- DeleteEmployerFormToken (POST) - Revoke token
- SendParentForm (POST) - Generate parent form tokens for all pending students
- SendParentFormForStudent (POST) - Generate parent form token for specific student
- ResendParentForm (POST) - Revoke and regenerate token
- DeleteParentFormToken (POST) - Revoke token

**Key Considerations:**
- Heavy TempData usage for success/error messages and form links
- Multi-student placement support
- Integrates with existing Blazor employer/parent wizards
- Form token security is critical

**Testing Checklist:**
- [ ] Placement CRUD
- [ ] Student can only see their own placements
- [ ] Teacher can see all placements
- [ ] Add student to placement
- [ ] Remove student from placement
- [ ] Generate employer form token
- [ ] View employer form link
- [ ] Resend employer form token
- [ ] Revoke employer form token
- [ ] Generate parent form tokens (single and bulk)
- [ ] View parent form links
- [ ] Resend parent form token
- [ ] Revoke parent form token
- [ ] Employer form review displays submitted data
- [ ] Integration with Blazor employer wizard works
- [ ] Integration with Blazor parent wizard works
- [ ] Company/Supervisor dropdowns populated
- [ ] Logbook summary displays correctly

---

## Stage 7: Logbook Entries

**Risk Level:** Low
**Authorization:** TeacherOrStudent
**Dependencies:** Placements

### LogbookEntriesController

| View | File Path | Action |
|------|-----------|--------|
| Index | `Views/LogbookEntries/Index.cshtml` | List entries for placement with cumulative hours |

**Note:** Create/Edit already exists as Blazor LogbookEntryForm. This is just the list view.

**Testing Checklist:**
- [ ] Teacher can view any placement's logbook entries
- [ ] Student can only view their own placement's entries
- [ ] Cumulative hours calculation displays correctly
- [ ] Links to Blazor LogbookEntryForm work

---

## Stage 8: Dashboard & Home

**Risk Level:** Medium
**Authorization:** All authenticated users
**Dependencies:** Students, Placements, Logbook data

### HomeController

| View | File Path | Action |
|------|-----------|--------|
| Index | `Views/Home/Index.cshtml` | Dashboard (includes role-specific partials) |
| _StudentDashboard | `Views/Home/_StudentDashboard.cshtml` | Student-specific content |
| _TeacherDashboard | `Views/Home/_TeacherDashboard.cshtml` | Teacher-specific content |
| _AdminDashboard | `Views/Home/_AdminDashboard.cshtml` | Admin-specific content |
| Privacy | `Views/Home/Privacy.cshtml` | Privacy page |

**Additional Actions:**
- CheckRoles (GET) - Diagnostic action for debugging
- Error (GET) - Error display

**Testing Checklist:**
- [ ] Student sees student dashboard
- [ ] Teacher sees teacher dashboard
- [ ] SiteAdmin sees admin dashboard
- [ ] Dashboard data displays correctly
- [ ] Links to other pages work
- [ ] Privacy page displays

---

## Stage 9: Cleanup

**Risk Level:** Low
**Action:** Remove deprecated MVC code

### Files to Remove

| File/Folder | Reason |
|-------------|--------|
| `Views/Employer/EmployerAcceptanceForm.cshtml` | Superseded by Blazor EmployerWizard |
| `Views/Employer/TokenInvalid.cshtml` | Superseded by Blazor |
| `Views/Employer/TokenExpired.cshtml` | Superseded by Blazor |
| `Views/Employer/FormSubmitted.cshtml` | Superseded by Blazor |
| `Views/Parent/TokenInvalid.cshtml` | Superseded by Blazor |
| `Views/Parent/TokenExpired.cshtml` | Superseded by Blazor |
| `Views/Parent/FormSubmitted.cshtml` | Superseded by Blazor |
| `Controllers/TenantController.cs` | Empty file |

### Controllers to Evaluate

| Controller | Decision |
|------------|----------|
| EmployerController | Remove or keep minimal redirects for compatibility |
| ParentController | Remove or keep minimal redirects for compatibility |

**Testing Checklist:**
- [ ] Employer form flow works end-to-end via Blazor
- [ ] Parent form flow works end-to-end via Blazor
- [ ] No broken links or 404s
- [ ] Old bookmarked URLs redirect appropriately (if keeping redirects)

---

## Migration Order Summary

```
Stage 1: Shared Infrastructure & Layouts
    ↓
Stage 2: Companies & Supervisors
    ↓
Stage 3: Schools
    ↓
Stage 4: Users & Authentication
    ↓
Stage 5: Students & Related Entities
    ↓   
Stage 6: Placements (Core Workflow)
    ↓
Stage 7: Logbook Entries
    ↓
Stage 8: Dashboard & Home
    ↓
Stage 9: Cleanup
```

---

## Statistics

| Metric | Count |
|--------|-------|
| Total Controllers | 14 (excluding empty TenantController) |
| Total Actions | ~85 |
| Total Views | 65 |
| Shared Views/Layouts | 8 |
| Controller-Specific Views | 57 |

---

## Key Patterns to Establish

During migration, establish these patterns in early stages:

1. **Form Handling** - PRG pattern with TempData for validation errors
2. **Dropdowns** - Blazor equivalent of PopulateDropdowns helpers
3. **Authorization** - `@attribute [Authorize(Roles = "...")]` patterns
4. **Soft Delete** - Consistent handling across all CRUD operations
5. **Tenant Isolation** - Service injection with ITenantProvider
6. **Success/Error Messages** - TempData or equivalent notification system
7. **Role-Based Content** - AuthorizeView components for conditional rendering
