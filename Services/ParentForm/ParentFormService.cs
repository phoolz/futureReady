using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FutureReady.Data;
using FutureReady.Models.ParentForm;
using FutureReady.Models.School;
using FutureReady.Services.FormTokens;

namespace FutureReady.Services.ParentForm
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

            // Get placement with related data (bypass tenant filter for public form)
            var placement = await _context.Placements
                .IgnoreQueryFilters()
                .Include(p => p.Student)
                .Include(p => p.Company)
                .Include(p => p.Supervisor)
                .Where(p => p.Id == formToken.PlacementId && !p.IsDeleted)
                .FirstOrDefaultAsync();

            if (placement == null || placement.Student == null)
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
                .Where(ec => ec.StudentId == placement.StudentId && !ec.IsDeleted)
                .OrderByDescending(ec => ec.IsPrimary)
                .ToListAsync();

            // Get existing medical conditions
            var medicalConditions = await _context.StudentMedicalConditions
                .IgnoreQueryFilters()
                .Where(mc => mc.StudentId == placement.StudentId && !mc.IsDeleted)
                .ToListAsync();

            // Get existing parent permission if any
            var parentPermission = await _context.ParentPermissions
                .IgnoreQueryFilters()
                .Where(pp => pp.PlacementId == placement.Id && !pp.IsDeleted)
                .FirstOrDefaultAsync();

            var dto = new ParentFormDto
            {
                PlacementId = placement.Id,
                StudentName = placement.Student.FullName,
                SchoolName = school?.Name ?? "Unknown School",
                CurrentStep = 1
            };

            // Pre-fill Student Details
            dto.StudentDetails = new StudentDetailsDto
            {
                StudentType = placement.Student.StudentType ?? string.Empty,
                MobileNumber = placement.Student.Phone ?? string.Empty
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

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Get placement (bypass tenant filter)
                var placement = await _context.Placements
                    .IgnoreQueryFilters()
                    .Include(p => p.Student)
                    .Include(p => p.Company)
                    .Include(p => p.Supervisor)
                    .Where(p => p.Id == formToken.PlacementId && !p.IsDeleted)
                    .FirstOrDefaultAsync();

                if (placement == null || placement.Student == null)
                {
                    return false;
                }

                // 1. Update Student
                placement.Student.StudentType = formData.StudentDetails.StudentType;
                placement.Student.Phone = formData.StudentDetails.MobileNumber;

                // 2. Delete existing EmergencyContacts for student, insert new
                var existingContacts = await _context.EmergencyContacts
                    .IgnoreQueryFilters()
                    .Where(ec => ec.StudentId == placement.StudentId && !ec.IsDeleted)
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
                    StudentId = placement.StudentId,
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
                    .Where(mc => mc.StudentId == placement.StudentId && !mc.IsDeleted)
                    .ToListAsync();

                foreach (var condition in existingConditions)
                {
                    condition.IsDeleted = true;
                }

                // Insert new medical conditions
                if (formData.MedicalDetails.HasAsthma)
                {
                    _context.StudentMedicalConditions.Add(CreateMedicalCondition(
                        placement.TenantId, placement.StudentId,
                        MedicalConditionTypes.Asthma, formData.MedicalDetails.AsthmaDetails));
                }
                if (formData.MedicalDetails.HasDiabetes)
                {
                    _context.StudentMedicalConditions.Add(CreateMedicalCondition(
                        placement.TenantId, placement.StudentId,
                        MedicalConditionTypes.Diabetes, formData.MedicalDetails.DiabetesDetails));
                }
                if (formData.MedicalDetails.HasEpilepsy)
                {
                    _context.StudentMedicalConditions.Add(CreateMedicalCondition(
                        placement.TenantId, placement.StudentId,
                        MedicalConditionTypes.Epilepsy, formData.MedicalDetails.EpilepsyDetails));
                }
                if (formData.MedicalDetails.HasAllergies)
                {
                    _context.StudentMedicalConditions.Add(CreateMedicalCondition(
                        placement.TenantId, placement.StudentId,
                        MedicalConditionTypes.Allergies, formData.MedicalDetails.AllergiesDetails));
                }
                if (formData.MedicalDetails.HasLearningDifficulties)
                {
                    _context.StudentMedicalConditions.Add(CreateMedicalCondition(
                        placement.TenantId, placement.StudentId,
                        MedicalConditionTypes.LearningDifficulties, formData.MedicalDetails.LearningDifficultiesDetails));
                }
                if (formData.MedicalDetails.HasMedication)
                {
                    _context.StudentMedicalConditions.Add(CreateMedicalCondition(
                        placement.TenantId, placement.StudentId,
                        MedicalConditionTypes.Medication, formData.MedicalDetails.MedicationDetails));
                }
                if (formData.MedicalDetails.HasOther)
                {
                    _context.StudentMedicalConditions.Add(CreateMedicalCondition(
                        placement.TenantId, placement.StudentId,
                        MedicalConditionTypes.Other, formData.MedicalDetails.OtherDetails));
                }

                // 6. Create/update ParentPermission
                var parentPermission = await _context.ParentPermissions
                    .IgnoreQueryFilters()
                    .Where(pp => pp.PlacementId == placement.Id && !pp.IsDeleted)
                    .FirstOrDefaultAsync();

                if (parentPermission == null)
                {
                    parentPermission = new ParentPermission
                    {
                        Id = Guid.NewGuid(),
                        TenantId = placement.TenantId,
                        PlacementId = placement.Id,
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

                // 7. Set Placement.ParentSubmittedAt
                placement.ParentSubmittedAt = DateTime.UtcNow;

                // Update status if it was pending_parent
                if (placement.Status == "pending_parent")
                {
                    placement.Status = "confirmed";
                }

                await _context.SaveChangesAsync();

                // 8. Mark token as used
                await _formTokenService.MarkAsUsedAsync(token);

                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
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
    }
}
