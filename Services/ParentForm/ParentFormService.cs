using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Apiary.Data;
using Apiary.Models.ParentForm;
using Apiary.Models.School;
using Apiary.Services.FormTokens;

namespace Apiary.Services.ParentForm
{
    public class ParentFormService : IParentFormService
    {
        private readonly ApplicationDbContext _context;
        private readonly IFormTokenService _formTokenService;

        public ParentFormService(ApplicationDbContext context, IFormTokenService formTokenService)
        {
            _context = context;
            _formTokenService = formTokenService;
        }

        public async Task<ParentFormDto?> InitializeFormAsync(string token)
        {
            var formToken = await _formTokenService.ValidateTokenAsync(token);
            if (formToken == null || !formToken.IsValid)
            {
                return null;
            }

            // Parent form tokens must have a StudentId
            if (!formToken.StudentId.HasValue)
            {
                return null;
            }

            var studentId = formToken.StudentId.Value;

            // Get placement with related data (bypass tenant filter for public form)
            var placement = await _context.Placements
                .IgnoreQueryFilters()
                .Include(p => p.Company)
                .Include(p => p.Supervisor)
                .Where(p => p.Id == formToken.PlacementId && !p.IsDeleted)
                .FirstOrDefaultAsync();

            if (placement == null)
            {
                return null;
            }

            // Get the student from the token
            var student = await _context.Students
                .IgnoreQueryFilters()
                .Where(s => s.Id == studentId && !s.IsDeleted)
                .FirstOrDefaultAsync();

            if (student == null)
            {
                return null;
            }

            // Get school info
            var school = await _context.Schools
                .IgnoreQueryFilters()
                .Where(s => s.Id == placement.TenantId && !s.IsDeleted)
                .FirstOrDefaultAsync();

            // Get existing emergency contacts for this student
            var emergencyContacts = await _context.EmergencyContacts
                .IgnoreQueryFilters()
                .Where(ec => ec.StudentId == studentId && !ec.IsDeleted)
                .OrderByDescending(ec => ec.IsPrimary)
                .ToListAsync();

            // Get existing medical conditions
            var medicalConditions = await _context.StudentMedicalConditions
                .IgnoreQueryFilters()
                .Where(mc => mc.StudentId == studentId && !mc.IsDeleted)
                .ToListAsync();

            // Get existing parent permission if any (now keyed by PlacementId + StudentId)
            var parentPermission = await _context.ParentPermissions
                .IgnoreQueryFilters()
                .Where(pp => pp.PlacementId == placement.Id && pp.StudentId == studentId && !pp.IsDeleted)
                .FirstOrDefaultAsync();

            var dto = new ParentFormDto
            {
                PlacementId = placement.Id,
                StudentName = student.FullName,
                SchoolName = school?.Name ?? "Unknown School",
                CurrentStep = 1
            };

            // Pre-fill Student Details
            dto.StudentDetails = new StudentDetailsDto
            {
                StudentType = student.StudentType ?? string.Empty,
                MobileNumber = student.Phone ?? string.Empty
            };

            // Pre-fill Emergency Contact (use primary if exists)
            var primaryContact = emergencyContacts.FirstOrDefault();
            if (primaryContact != null)
            {
                dto.EmergencyContact = new EmergencyContactDto
                {
                    FirstName = primaryContact.FirstName,
                    LastName = primaryContact.LastName,
                    MobileNumber = primaryContact.MobileNumber ?? string.Empty,
                    Relationship = primaryContact.Relationship ?? string.Empty
                };
            }

            // Pre-fill Workplace Details
            var isCompanyPreset = placement.Company != null;
            dto.WorkplaceDetails = new WorkplaceDetailsDto
            {
                IsCompanyPreset = isCompanyPreset,
                CompanyName = placement.Company?.Name ?? string.Empty,
                ContactFirstName = placement.Supervisor?.FirstName ?? string.Empty,
                ContactLastName = placement.Supervisor?.LastName ?? string.Empty,
                ContactEmail = placement.Supervisor?.Email ?? string.Empty,
                ContactPhone = placement.Supervisor?.Phone ?? string.Empty,
                Industry = placement.Company?.Industry,
                StreetAddress = placement.Company?.StreetAddress ?? string.Empty,
                StreetAddress2 = placement.Company?.StreetAddress2,
                City = placement.Company?.City ?? string.Empty,
                State = placement.Company?.State ?? string.Empty,
                PostalCode = placement.Company?.PostalCode ?? string.Empty
            };

            // Pre-fill Transport from parent permission
            if (parentPermission != null)
            {
                dto.Transport = new TransportDto
                {
                    TransportMethod = parentPermission.TransportMethod ?? string.Empty,
                    PublicTransportDetails = parentPermission.PublicTransportDetails,
                    DriverName = parentPermission.DriverName,
                    DriverContactNumber = parentPermission.DriverContactNumber
                };
            }

            // Pre-fill Medical Details from existing conditions
            dto.MedicalDetails = new MedicalDetailsDto();
            foreach (var condition in medicalConditions)
            {
                switch (condition.ConditionType)
                {
                    case MedicalConditionTypes.Asthma:
                        dto.MedicalDetails.HasAsthma = true;
                        dto.MedicalDetails.AsthmaDetails = condition.Details;
                        break;
                    case MedicalConditionTypes.Diabetes:
                        dto.MedicalDetails.HasDiabetes = true;
                        dto.MedicalDetails.DiabetesDetails = condition.Details;
                        break;
                    case MedicalConditionTypes.Epilepsy:
                        dto.MedicalDetails.HasEpilepsy = true;
                        dto.MedicalDetails.EpilepsyDetails = condition.Details;
                        break;
                    case MedicalConditionTypes.Allergies:
                        dto.MedicalDetails.HasAllergies = true;
                        dto.MedicalDetails.AllergiesDetails = condition.Details;
                        break;
                    case MedicalConditionTypes.LearningDifficulties:
                        dto.MedicalDetails.HasLearningDifficulties = true;
                        dto.MedicalDetails.LearningDifficultiesDetails = condition.Details;
                        break;
                    case MedicalConditionTypes.Medication:
                        dto.MedicalDetails.HasMedication = true;
                        dto.MedicalDetails.MedicationDetails = condition.Details;
                        break;
                    case MedicalConditionTypes.Other:
                        dto.MedicalDetails.HasOther = true;
                        dto.MedicalDetails.OtherDetails = condition.Details;
                        break;
                }
            }

            // Pre-fill Consent from parent permission
            if (parentPermission != null)
            {
                dto.Consent = new ConsentDto
                {
                    ShareMedicalWithEmployer = parentPermission.ShareMedicalWithEmployer,
                    RequestTeacherPrevisit = parentPermission.RequestTeacherPrevisit,
                    ParentFirstName = parentPermission.ParentFirstName ?? string.Empty,
                    ParentLastName = parentPermission.ParentLastName ?? string.Empty,
                    ConsentDate = parentPermission.ConsentDate ?? DateOnly.FromDateTime(DateTime.Today),
                    ConsentGiven = parentPermission.ConsentGiven
                };
            }

            return dto;
        }

        public async Task<bool> SubmitFormAsync(string token, ParentFormDto formData)
        {
            var formToken = await _formTokenService.ValidateTokenAsync(token);
            if (formToken == null || !formToken.IsValid)
            {
                return false;
            }

            // Parent form tokens must have a StudentId
            if (!formToken.StudentId.HasValue)
            {
                return false;
            }

            var studentId = formToken.StudentId.Value;

            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Get placement (bypass tenant filter)
                    var placement = await _context.Placements
                        .IgnoreQueryFilters()
                        .Include(p => p.Company)
                        .Include(p => p.Supervisor)
                        .Where(p => p.Id == formToken.PlacementId && !p.IsDeleted)
                        .FirstOrDefaultAsync();

                    if (placement == null)
                    {
                        return false;
                    }

                    // Get the student
                    var student = await _context.Students
                        .IgnoreQueryFilters()
                        .Where(s => s.Id == studentId && !s.IsDeleted)
                        .FirstOrDefaultAsync();

                    if (student == null)
                    {
                        return false;
                    }

                    // Get the PlacementStudent record
                    var placementStudent = await _context.PlacementStudents
                        .IgnoreQueryFilters()
                        .Where(ps => ps.PlacementId == placement.Id && ps.StudentId == studentId && !ps.IsDeleted)
                        .FirstOrDefaultAsync();

                    if (placementStudent == null)
                    {
                        return false;
                    }

                    // 1. Update Student
                    student.StudentType = formData.StudentDetails.StudentType;
                    student.Phone = formData.StudentDetails.MobileNumber;

                    // 2. Delete existing EmergencyContacts for student, insert new
                    var existingContacts = await _context.EmergencyContacts
                        .IgnoreQueryFilters()
                        .Where(ec => ec.StudentId == studentId && !ec.IsDeleted)
                        .ToListAsync();

                    foreach (var contact in existingContacts)
                    {
                        contact.IsDeleted = true;
                    }

                    // Insert new emergency contact
                    var newContact = new EmergencyContact
                    {
                        Id = Guid.NewGuid(),
                        TenantId = placement.TenantId,
                        StudentId = studentId,
                        FirstName = formData.EmergencyContact.FirstName,
                        LastName = formData.EmergencyContact.LastName,
                        MobileNumber = formData.EmergencyContact.MobileNumber,
                        Relationship = formData.EmergencyContact.Relationship,
                        IsPrimary = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.EmergencyContacts.Add(newContact);

                    // 3. Create Company if not set
                    if (placement.CompanyId == null && !formData.WorkplaceDetails.IsCompanyPreset)
                    {
                        var newCompany = new Company
                        {
                            Id = Guid.NewGuid(),
                            TenantId = placement.TenantId,
                            Name = formData.WorkplaceDetails.CompanyName,
                            Industry = formData.WorkplaceDetails.Industry,
                            StreetAddress = formData.WorkplaceDetails.StreetAddress,
                            StreetAddress2 = formData.WorkplaceDetails.StreetAddress2,
                            City = formData.WorkplaceDetails.City,
                            State = formData.WorkplaceDetails.State,
                            PostalCode = formData.WorkplaceDetails.PostalCode,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        _context.Companies.Add(newCompany);
                        placement.CompanyId = newCompany.Id;
                        placement.Company = newCompany;
                    }

                    // 4. Create Supervisor if not set
                    if (placement.SupervisorId == null && placement.CompanyId != null)
                    {
                        var newSupervisor = new Supervisor
                        {
                            Id = Guid.NewGuid(),
                            TenantId = placement.TenantId,
                            CompanyId = placement.CompanyId.Value,
                            FirstName = formData.WorkplaceDetails.ContactFirstName,
                            LastName = formData.WorkplaceDetails.ContactLastName,
                            Email = formData.WorkplaceDetails.ContactEmail,
                            Phone = formData.WorkplaceDetails.ContactPhone,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        _context.Supervisors.Add(newSupervisor);
                        placement.SupervisorId = newSupervisor.Id;
                    }

                    // 5. Delete existing StudentMedicalConditions, insert new for checked conditions
                    var existingConditions = await _context.StudentMedicalConditions
                        .IgnoreQueryFilters()
                        .Where(mc => mc.StudentId == studentId && !mc.IsDeleted)
                        .ToListAsync();

                    foreach (var condition in existingConditions)
                    {
                        condition.IsDeleted = true;
                    }

                    // Insert new medical conditions
                    if (formData.MedicalDetails.HasAsthma)
                    {
                        _context.StudentMedicalConditions.Add(CreateMedicalCondition(
                            placement.TenantId, studentId,
                            MedicalConditionTypes.Asthma, formData.MedicalDetails.AsthmaDetails));
                    }
                    if (formData.MedicalDetails.HasDiabetes)
                    {
                        _context.StudentMedicalConditions.Add(CreateMedicalCondition(
                            placement.TenantId, studentId,
                            MedicalConditionTypes.Diabetes, formData.MedicalDetails.DiabetesDetails));
                    }
                    if (formData.MedicalDetails.HasEpilepsy)
                    {
                        _context.StudentMedicalConditions.Add(CreateMedicalCondition(
                            placement.TenantId, studentId,
                            MedicalConditionTypes.Epilepsy, formData.MedicalDetails.EpilepsyDetails));
                    }
                    if (formData.MedicalDetails.HasAllergies)
                    {
                        _context.StudentMedicalConditions.Add(CreateMedicalCondition(
                            placement.TenantId, studentId,
                            MedicalConditionTypes.Allergies, formData.MedicalDetails.AllergiesDetails));
                    }
                    if (formData.MedicalDetails.HasLearningDifficulties)
                    {
                        _context.StudentMedicalConditions.Add(CreateMedicalCondition(
                            placement.TenantId, studentId,
                            MedicalConditionTypes.LearningDifficulties, formData.MedicalDetails.LearningDifficultiesDetails));
                    }
                    if (formData.MedicalDetails.HasMedication)
                    {
                        _context.StudentMedicalConditions.Add(CreateMedicalCondition(
                            placement.TenantId, studentId,
                            MedicalConditionTypes.Medication, formData.MedicalDetails.MedicationDetails));
                    }
                    if (formData.MedicalDetails.HasOther)
                    {
                        _context.StudentMedicalConditions.Add(CreateMedicalCondition(
                            placement.TenantId, studentId,
                            MedicalConditionTypes.Other, formData.MedicalDetails.OtherDetails));
                    }

                    // 6. Create/update ParentPermission (now keyed by PlacementId + StudentId)
                    var parentPermission = await _context.ParentPermissions
                        .IgnoreQueryFilters()
                        .Where(pp => pp.PlacementId == placement.Id && pp.StudentId == studentId && !pp.IsDeleted)
                        .FirstOrDefaultAsync();

                    if (parentPermission == null)
                    {
                        parentPermission = new ParentPermission
                        {
                            Id = Guid.NewGuid(),
                            TenantId = placement.TenantId,
                            PlacementId = placement.Id,
                            StudentId = studentId,
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.ParentPermissions.Add(parentPermission);
                    }

                    parentPermission.TransportMethod = formData.Transport.TransportMethod;
                    parentPermission.PublicTransportDetails = formData.Transport.PublicTransportDetails;
                    parentPermission.DriverName = formData.Transport.DriverName;
                    parentPermission.DriverContactNumber = formData.Transport.DriverContactNumber;
                    parentPermission.ShareMedicalWithEmployer = formData.Consent.ShareMedicalWithEmployer;
                    parentPermission.RequestTeacherPrevisit = formData.Consent.RequestTeacherPrevisit;
                    parentPermission.ParentFirstName = formData.Consent.ParentFirstName;
                    parentPermission.ParentLastName = formData.Consent.ParentLastName;
                    parentPermission.ConsentDate = formData.Consent.ConsentDate;
                    parentPermission.ConsentGiven = formData.Consent.ConsentGiven;
                    parentPermission.UpdatedAt = DateTime.UtcNow;

                    // Build medical notes for employer if sharing is enabled
                    if (formData.Consent.ShareMedicalWithEmployer)
                    {
                        parentPermission.MedicalNotesForEmployer = BuildMedicalNotesForEmployer(formData.MedicalDetails);
                    }
                    else
                    {
                        parentPermission.MedicalNotesForEmployer = null;
                    }

                    // 7. Update PlacementStudent status
                    placementStudent.Status = "confirmed";
                    placementStudent.ParentSubmittedAt = DateTime.UtcNow;

                    // 8. Recalculate overall placement status
                    await RecalculatePlacementStatusAsync(placement);

                    await _context.SaveChangesAsync();

                    // 9. Mark token as used
                    await _formTokenService.MarkAsUsedAsync(token);

                    await transaction.CommitAsync();
                    return true;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }

        private async Task RecalculatePlacementStatusAsync(Placement placement)
        {
            // Get all PlacementStudent records for this placement
            var placementStudents = await _context.PlacementStudents
                .IgnoreQueryFilters()
                .Where(ps => ps.PlacementId == placement.Id && !ps.IsDeleted)
                .ToListAsync();

            if (!placementStudents.Any())
            {
                placement.Status = "draft";
                return;
            }

            // If employer form not submitted, stay at pending_employer
            if (!placement.EmployerSubmittedAt.HasValue)
            {
                placement.Status = "pending_employer";
                return;
            }

            var confirmedCount = placementStudents.Count(ps => ps.Status == "confirmed");
            var pendingCount = placementStudents.Count(ps => ps.Status == "pending_parent");

            if (confirmedCount == placementStudents.Count)
            {
                placement.Status = "confirmed";
            }
            else if (confirmedCount > 0)
            {
                placement.Status = "partial";
            }
            else
            {
                placement.Status = "pending_parents";
            }
        }

        private StudentMedicalCondition CreateMedicalCondition(Guid tenantId, Guid studentId, string type, string? details)
        {
            return new StudentMedicalCondition
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                StudentId = studentId,
                ConditionType = type,
                Details = details,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        private string? BuildMedicalNotesForEmployer(MedicalDetailsDto medical)
        {
            var notes = new System.Text.StringBuilder();

            if (medical.HasAsthma && !string.IsNullOrWhiteSpace(medical.AsthmaDetails))
                notes.AppendLine($"Asthma: {medical.AsthmaDetails}");
            if (medical.HasDiabetes && !string.IsNullOrWhiteSpace(medical.DiabetesDetails))
                notes.AppendLine($"Diabetes: {medical.DiabetesDetails}");
            if (medical.HasEpilepsy && !string.IsNullOrWhiteSpace(medical.EpilepsyDetails))
                notes.AppendLine($"Epilepsy: {medical.EpilepsyDetails}");
            if (medical.HasAllergies && !string.IsNullOrWhiteSpace(medical.AllergiesDetails))
                notes.AppendLine($"Allergies: {medical.AllergiesDetails}");
            if (medical.HasLearningDifficulties && !string.IsNullOrWhiteSpace(medical.LearningDifficultiesDetails))
                notes.AppendLine($"Learning Difficulties: {medical.LearningDifficultiesDetails}");
            if (medical.HasMedication && !string.IsNullOrWhiteSpace(medical.MedicationDetails))
                notes.AppendLine($"Medication: {medical.MedicationDetails}");
            if (medical.HasOther && !string.IsNullOrWhiteSpace(medical.OtherDetails))
                notes.AppendLine($"Other: {medical.OtherDetails}");

            var result = notes.ToString().Trim();
            return string.IsNullOrEmpty(result) ? null : result;
        }

        public async Task<bool> SaveStudentDetailsAsync(string token, StudentDetailsDto data)
        {
            var formToken = await _formTokenService.ValidateTokenAsync(token);
            if (formToken == null || !formToken.IsValid || !formToken.StudentId.HasValue)
            {
                return false;
            }

            var student = await _context.Students
                .IgnoreQueryFilters()
                .Where(s => s.Id == formToken.StudentId.Value && !s.IsDeleted)
                .FirstOrDefaultAsync();

            if (student == null)
            {
                return false;
            }

            student.StudentType = data.StudentType;
            student.Phone = data.MobileNumber;
            student.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SaveEmergencyContactAsync(string token, EmergencyContactDto data)
        {
            var formToken = await _formTokenService.ValidateTokenAsync(token);
            if (formToken == null || !formToken.IsValid || !formToken.StudentId.HasValue)
            {
                return false;
            }

            var studentId = formToken.StudentId.Value;

            // Get placement to get TenantId
            var placement = await _context.Placements
                .IgnoreQueryFilters()
                .Where(p => p.Id == formToken.PlacementId && !p.IsDeleted)
                .FirstOrDefaultAsync();

            if (placement == null)
            {
                return false;
            }

            // Soft delete existing emergency contacts
            var existingContacts = await _context.EmergencyContacts
                .IgnoreQueryFilters()
                .Where(ec => ec.StudentId == studentId && !ec.IsDeleted)
                .ToListAsync();

            foreach (var contact in existingContacts)
            {
                contact.IsDeleted = true;
            }

            // Insert new emergency contact
            var newContact = new EmergencyContact
            {
                Id = Guid.NewGuid(),
                TenantId = placement.TenantId,
                StudentId = studentId,
                FirstName = data.FirstName,
                LastName = data.LastName,
                MobileNumber = data.MobileNumber,
                Relationship = data.Relationship,
                IsPrimary = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.EmergencyContacts.Add(newContact);

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SaveWorkplaceDetailsAsync(string token, WorkplaceDetailsDto data)
        {
            var formToken = await _formTokenService.ValidateTokenAsync(token);
            if (formToken == null || !formToken.IsValid || !formToken.StudentId.HasValue)
            {
                return false;
            }

            var placement = await _context.Placements
                .IgnoreQueryFilters()
                .Include(p => p.Company)
                .Include(p => p.Supervisor)
                .Where(p => p.Id == formToken.PlacementId && !p.IsDeleted)
                .FirstOrDefaultAsync();

            if (placement == null)
            {
                return false;
            }

            // If company is not preset, create new company
            if (placement.CompanyId == null && !data.IsCompanyPreset)
            {
                var newCompany = new Company
                {
                    Id = Guid.NewGuid(),
                    TenantId = placement.TenantId,
                    Name = data.CompanyName,
                    Industry = data.Industry,
                    StreetAddress = data.StreetAddress,
                    StreetAddress2 = data.StreetAddress2,
                    City = data.City,
                    State = data.State,
                    PostalCode = data.PostalCode,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Companies.Add(newCompany);
                placement.CompanyId = newCompany.Id;
                placement.Company = newCompany;
            }

            // If supervisor is not set, create new supervisor
            if (placement.SupervisorId == null && placement.CompanyId != null)
            {
                var newSupervisor = new Supervisor
                {
                    Id = Guid.NewGuid(),
                    TenantId = placement.TenantId,
                    CompanyId = placement.CompanyId.Value,
                    FirstName = data.ContactFirstName,
                    LastName = data.ContactLastName,
                    Email = data.ContactEmail,
                    Phone = data.ContactPhone,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Supervisors.Add(newSupervisor);
                placement.SupervisorId = newSupervisor.Id;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SaveTransportAsync(string token, TransportDto data)
        {
            var formToken = await _formTokenService.ValidateTokenAsync(token);
            if (formToken == null || !formToken.IsValid || !formToken.StudentId.HasValue)
            {
                return false;
            }

            var studentId = formToken.StudentId.Value;

            var placement = await _context.Placements
                .IgnoreQueryFilters()
                .Where(p => p.Id == formToken.PlacementId && !p.IsDeleted)
                .FirstOrDefaultAsync();

            if (placement == null)
            {
                return false;
            }

            // Get or create ParentPermission
            var parentPermission = await _context.ParentPermissions
                .IgnoreQueryFilters()
                .Where(pp => pp.PlacementId == placement.Id && pp.StudentId == studentId && !pp.IsDeleted)
                .FirstOrDefaultAsync();

            if (parentPermission == null)
            {
                parentPermission = new ParentPermission
                {
                    Id = Guid.NewGuid(),
                    TenantId = placement.TenantId,
                    PlacementId = placement.Id,
                    StudentId = studentId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.ParentPermissions.Add(parentPermission);
            }

            parentPermission.TransportMethod = data.TransportMethod;
            parentPermission.PublicTransportDetails = data.PublicTransportDetails;
            parentPermission.DriverName = data.DriverName;
            parentPermission.DriverContactNumber = data.DriverContactNumber;
            parentPermission.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SaveMedicalDetailsAsync(string token, MedicalDetailsDto data)
        {
            var formToken = await _formTokenService.ValidateTokenAsync(token);
            if (formToken == null || !formToken.IsValid || !formToken.StudentId.HasValue)
            {
                return false;
            }

            var studentId = formToken.StudentId.Value;

            var placement = await _context.Placements
                .IgnoreQueryFilters()
                .Where(p => p.Id == formToken.PlacementId && !p.IsDeleted)
                .FirstOrDefaultAsync();

            if (placement == null)
            {
                return false;
            }

            // Soft delete existing medical conditions
            var existingConditions = await _context.StudentMedicalConditions
                .IgnoreQueryFilters()
                .Where(mc => mc.StudentId == studentId && !mc.IsDeleted)
                .ToListAsync();

            foreach (var condition in existingConditions)
            {
                condition.IsDeleted = true;
            }

            // Insert new medical conditions
            if (data.HasAsthma)
            {
                _context.StudentMedicalConditions.Add(CreateMedicalCondition(
                    placement.TenantId, studentId,
                    MedicalConditionTypes.Asthma, data.AsthmaDetails));
            }
            if (data.HasDiabetes)
            {
                _context.StudentMedicalConditions.Add(CreateMedicalCondition(
                    placement.TenantId, studentId,
                    MedicalConditionTypes.Diabetes, data.DiabetesDetails));
            }
            if (data.HasEpilepsy)
            {
                _context.StudentMedicalConditions.Add(CreateMedicalCondition(
                    placement.TenantId, studentId,
                    MedicalConditionTypes.Epilepsy, data.EpilepsyDetails));
            }
            if (data.HasAllergies)
            {
                _context.StudentMedicalConditions.Add(CreateMedicalCondition(
                    placement.TenantId, studentId,
                    MedicalConditionTypes.Allergies, data.AllergiesDetails));
            }
            if (data.HasLearningDifficulties)
            {
                _context.StudentMedicalConditions.Add(CreateMedicalCondition(
                    placement.TenantId, studentId,
                    MedicalConditionTypes.LearningDifficulties, data.LearningDifficultiesDetails));
            }
            if (data.HasMedication)
            {
                _context.StudentMedicalConditions.Add(CreateMedicalCondition(
                    placement.TenantId, studentId,
                    MedicalConditionTypes.Medication, data.MedicationDetails));
            }
            if (data.HasOther)
            {
                _context.StudentMedicalConditions.Add(CreateMedicalCondition(
                    placement.TenantId, studentId,
                    MedicalConditionTypes.Other, data.OtherDetails));
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SaveConsentAsync(string token, ConsentDto data)
        {
            var formToken = await _formTokenService.ValidateTokenAsync(token);
            if (formToken == null || !formToken.IsValid || !formToken.StudentId.HasValue)
            {
                return false;
            }

            var studentId = formToken.StudentId.Value;

            var placement = await _context.Placements
                .IgnoreQueryFilters()
                .Where(p => p.Id == formToken.PlacementId && !p.IsDeleted)
                .FirstOrDefaultAsync();

            if (placement == null)
            {
                return false;
            }

            // Get or create ParentPermission
            var parentPermission = await _context.ParentPermissions
                .IgnoreQueryFilters()
                .Where(pp => pp.PlacementId == placement.Id && pp.StudentId == studentId && !pp.IsDeleted)
                .FirstOrDefaultAsync();

            if (parentPermission == null)
            {
                parentPermission = new ParentPermission
                {
                    Id = Guid.NewGuid(),
                    TenantId = placement.TenantId,
                    PlacementId = placement.Id,
                    StudentId = studentId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.ParentPermissions.Add(parentPermission);
            }

            parentPermission.ShareMedicalWithEmployer = data.ShareMedicalWithEmployer;
            parentPermission.RequestTeacherPrevisit = data.RequestTeacherPrevisit;
            parentPermission.ParentFirstName = data.ParentFirstName;
            parentPermission.ParentLastName = data.ParentLastName;
            parentPermission.ConsentDate = data.ConsentDate;
            parentPermission.ConsentGiven = data.ConsentGiven;
            parentPermission.UpdatedAt = DateTime.UtcNow;

            // Build medical notes for employer if sharing is enabled
            if (data.ShareMedicalWithEmployer)
            {
                // Get current medical conditions to build notes
                var medicalConditions = await _context.StudentMedicalConditions
                    .IgnoreQueryFilters()
                    .Where(mc => mc.StudentId == studentId && !mc.IsDeleted)
                    .ToListAsync();

                var medical = new MedicalDetailsDto();
                foreach (var condition in medicalConditions)
                {
                    switch (condition.ConditionType)
                    {
                        case MedicalConditionTypes.Asthma:
                            medical.HasAsthma = true;
                            medical.AsthmaDetails = condition.Details;
                            break;
                        case MedicalConditionTypes.Diabetes:
                            medical.HasDiabetes = true;
                            medical.DiabetesDetails = condition.Details;
                            break;
                        case MedicalConditionTypes.Epilepsy:
                            medical.HasEpilepsy = true;
                            medical.EpilepsyDetails = condition.Details;
                            break;
                        case MedicalConditionTypes.Allergies:
                            medical.HasAllergies = true;
                            medical.AllergiesDetails = condition.Details;
                            break;
                        case MedicalConditionTypes.LearningDifficulties:
                            medical.HasLearningDifficulties = true;
                            medical.LearningDifficultiesDetails = condition.Details;
                            break;
                        case MedicalConditionTypes.Medication:
                            medical.HasMedication = true;
                            medical.MedicationDetails = condition.Details;
                            break;
                        case MedicalConditionTypes.Other:
                            medical.HasOther = true;
                            medical.OtherDetails = condition.Details;
                            break;
                    }
                }
                parentPermission.MedicalNotesForEmployer = BuildMedicalNotesForEmployer(medical);
            }
            else
            {
                parentPermission.MedicalNotesForEmployer = null;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> FinalizeSubmissionAsync(string token)
        {
            var formToken = await _formTokenService.ValidateTokenAsync(token);
            if (formToken == null || !formToken.IsValid || !formToken.StudentId.HasValue)
            {
                return false;
            }

            var studentId = formToken.StudentId.Value;

            var placement = await _context.Placements
                .IgnoreQueryFilters()
                .Where(p => p.Id == formToken.PlacementId && !p.IsDeleted)
                .FirstOrDefaultAsync();

            if (placement == null)
            {
                return false;
            }

            // Get PlacementStudent record
            var placementStudent = await _context.PlacementStudents
                .IgnoreQueryFilters()
                .Where(ps => ps.PlacementId == placement.Id && ps.StudentId == studentId && !ps.IsDeleted)
                .FirstOrDefaultAsync();

            if (placementStudent == null)
            {
                return false;
            }

            // Update PlacementStudent status
            placementStudent.Status = "confirmed";
            placementStudent.ParentSubmittedAt = DateTime.UtcNow;

            // Recalculate overall placement status
            await RecalculatePlacementStatusAsync(placement);

            await _context.SaveChangesAsync();

            // Mark token as used
            await _formTokenService.MarkAsUsedAsync(token);

            return true;
        }
    }
}
