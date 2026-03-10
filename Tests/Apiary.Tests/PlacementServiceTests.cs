using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Apiary.Data;
using Apiary.Models.School;
using Apiary.Services;
using Apiary.Services.Placements;

namespace Apiary.Tests
{
    public class PlacementServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Microsoft.Data.Sqlite.SqliteConnection _connection;
        private readonly PlacementService _service;
        private readonly Guid _tenantId = Guid.NewGuid();
        private readonly FakeTenantProvider _tenantProvider;

        public PlacementServiceTests()
        {
            var userProvider = new FakeUserProvider();
            _tenantProvider = new FakeTenantProvider(_tenantId);
            (_context, _connection) = TestDbContextFactory.CreateSqliteInMemoryContext(userProvider, _tenantProvider);
            _service = new PlacementService(_context, _tenantProvider);
        }

        public void Dispose()
        {
            _context?.Dispose();
            _connection?.Dispose();
        }

        #region Helper Methods

        private Company CreateTestCompany(Guid? id = null, string name = "Test Company", Guid? tenantId = null)
        {
            return new Company
            {
                Id = id ?? Guid.NewGuid(),
                Name = name,
                TenantId = tenantId ?? _tenantId,
                Industry = "Technology",
                StreetAddress = "123 Main St",
                City = "Sydney",
                State = "NSW",
                PostalCode = "2000"
            };
        }

        private Supervisor CreateTestSupervisor(Guid companyId, Guid? id = null, Guid? tenantId = null)
        {
            return new Supervisor
            {
                Id = id ?? Guid.NewGuid(),
                CompanyId = companyId,
                FirstName = "John",
                LastName = "Smith",
                Email = "john.smith@test.com",
                TenantId = tenantId ?? _tenantId
            };
        }

        private Placement CreateTestPlacement(Guid? id = null, Guid? companyId = null, Guid? supervisorId = null, int? year = null, string status = "draft", Guid? tenantId = null, DateTime? employerSubmittedAt = null)
        {
            return new Placement
            {
                Id = id ?? Guid.NewGuid(),
                CompanyId = companyId,
                SupervisorId = supervisorId,
                Year = year ?? 2025,
                Status = status,
                TenantId = tenantId ?? _tenantId,
                EmployerSubmittedAt = employerSubmittedAt,
                PlacementRole = "Test Role",
                DressRequirement = "Business Casual"
            };
        }

        private Student CreateTestStudent(Guid? id = null, string firstName = "Jane", string lastName = "Doe", Guid? tenantId = null)
        {
            return new Student
            {
                Id = id ?? Guid.NewGuid(),
                FirstName = firstName,
                LastName = lastName,
                TenantId = tenantId ?? _tenantId,
                Email = $"{firstName.ToLower()}.{lastName.ToLower()}@student.test.com"
            };
        }

        private PlacementStudent CreateTestPlacementStudent(Guid placementId, Guid studentId, string status = "pending_parent", Guid? id = null, Guid? tenantId = null, DateTime? parentSubmittedAt = null)
        {
            return new PlacementStudent
            {
                Id = id ?? Guid.NewGuid(),
                PlacementId = placementId,
                StudentId = studentId,
                Status = status,
                TenantId = tenantId ?? _tenantId,
                ParentSubmittedAt = parentSubmittedAt
            };
        }

        #endregion

        #region GetAllAsync Tests

        [Fact]
        public async Task GetAllAsync_ReturnsAllPlacements()
        {
            // Arrange
            var placement1 = CreateTestPlacement();
            var placement2 = CreateTestPlacement();
            _context.Placements.AddRange(placement1, placement2);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetAllAsync(_tenantId);

            // Assert
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsEmptyListWhenNoPlacements()
        {
            // Act
            var result = await _service.GetAllAsync(_tenantId);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAllAsync_OnlyReturnsTenantPlacements()
        {
            // Arrange
            var otherTenantId = Guid.NewGuid();
            var myPlacement = CreateTestPlacement(tenantId: _tenantId);
            var otherPlacement = CreateTestPlacement(tenantId: otherTenantId);
            _context.Placements.AddRange(myPlacement, otherPlacement);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetAllAsync(_tenantId);

            // Assert
            Assert.Single(result);
            Assert.Equal(myPlacement.Id, result[0].Id);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsOrderedByCreatedAtDescending()
        {
            // Arrange - save entities first (they'll get auto-assigned CreatedAt)
            var placement1 = CreateTestPlacement();
            var placement2 = CreateTestPlacement();
            var placement3 = CreateTestPlacement();
            _context.Placements.AddRange(placement1, placement2, placement3);
            await _context.SaveChangesAsync();

            // Now update CreatedAt values directly (Modified state doesn't overwrite CreatedAt)
            placement1.CreatedAt = DateTimeOffset.UtcNow.AddDays(-2);
            placement2.CreatedAt = DateTimeOffset.UtcNow.AddDays(-1);
            placement3.CreatedAt = DateTimeOffset.UtcNow;
            _context.Entry(placement1).Property(p => p.CreatedAt).IsModified = true;
            _context.Entry(placement2).Property(p => p.CreatedAt).IsModified = true;
            _context.Entry(placement3).Property(p => p.CreatedAt).IsModified = true;
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            // Act
            var result = await _service.GetAllAsync(_tenantId);

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Equal(placement3.Id, result[0].Id);
            Assert.Equal(placement2.Id, result[1].Id);
            Assert.Equal(placement1.Id, result[2].Id);
        }

        [Fact]
        public async Task GetAllAsync_IncludesPlacementStudentsAndCompanyAndSupervisor()
        {
            // Arrange
            var company = CreateTestCompany();
            var supervisor = CreateTestSupervisor(company.Id);
            var student = CreateTestStudent();
            var placement = CreateTestPlacement(companyId: company.Id, supervisorId: supervisor.Id);
            var placementStudent = CreateTestPlacementStudent(placement.Id, student.Id);

            _context.Companies.Add(company);
            _context.Supervisors.Add(supervisor);
            _context.Students.Add(student);
            _context.Placements.Add(placement);
            _context.PlacementStudents.Add(placementStudent);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            // Act
            var result = await _service.GetAllAsync(_tenantId);

            // Assert
            Assert.Single(result);
            Assert.NotNull(result[0].Company);
            Assert.Equal(company.Name, result[0].Company!.Name);
            Assert.NotNull(result[0].Supervisor);
            Assert.Equal(supervisor.FirstName, result[0].Supervisor!.FirstName);
            Assert.Single(result[0].PlacementStudents);
            Assert.NotNull(result[0].PlacementStudents.First().Student);
        }

        #endregion

        #region GetByStudentIdAsync Tests

        [Fact]
        public async Task GetByStudentIdAsync_ReturnsPlacementsForStudent()
        {
            // Arrange
            var student = CreateTestStudent();
            var placement1 = CreateTestPlacement(year: 2025);
            var placement2 = CreateTestPlacement(year: 2024);
            var ps1 = CreateTestPlacementStudent(placement1.Id, student.Id);
            var ps2 = CreateTestPlacementStudent(placement2.Id, student.Id);

            _context.Students.Add(student);
            _context.Placements.AddRange(placement1, placement2);
            _context.PlacementStudents.AddRange(ps1, ps2);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetByStudentIdAsync(student.Id, _tenantId);

            // Assert
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetByStudentIdAsync_ReturnsEmptyWhenNoPlacementsForStudent()
        {
            // Arrange
            var student = CreateTestStudent();
            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetByStudentIdAsync(student.Id, _tenantId);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetByStudentIdAsync_OnlyReturnsTenantPlacements()
        {
            // Arrange
            var otherTenantId = Guid.NewGuid();
            var student = CreateTestStudent();
            var myPlacement = CreateTestPlacement(tenantId: _tenantId);
            var otherPlacement = CreateTestPlacement(tenantId: otherTenantId);
            var ps1 = CreateTestPlacementStudent(myPlacement.Id, student.Id, tenantId: _tenantId);
            var ps2 = CreateTestPlacementStudent(otherPlacement.Id, student.Id, tenantId: otherTenantId);

            _context.Students.Add(student);
            _context.Placements.AddRange(myPlacement, otherPlacement);
            _context.PlacementStudents.AddRange(ps1, ps2);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetByStudentIdAsync(student.Id, _tenantId);

            // Assert
            Assert.Single(result);
            Assert.Equal(myPlacement.Id, result[0].Id);
        }

        #endregion

        #region GetByCompanyIdAsync Tests

        [Fact]
        public async Task GetByCompanyIdAsync_ReturnsPlacementsForCompany()
        {
            // Arrange
            var company = CreateTestCompany();
            var placement1 = CreateTestPlacement(companyId: company.Id, year: 2025);
            var placement2 = CreateTestPlacement(companyId: company.Id, year: 2024);

            _context.Companies.Add(company);
            _context.Placements.AddRange(placement1, placement2);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetByCompanyIdAsync(company.Id, _tenantId);

            // Assert
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetByCompanyIdAsync_ReturnsEmptyWhenNoPlacementsForCompany()
        {
            // Arrange
            var company = CreateTestCompany();
            _context.Companies.Add(company);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetByCompanyIdAsync(company.Id, _tenantId);

            // Assert
            Assert.Empty(result);
        }

        #endregion

        #region GetByIdAsync Tests

        [Fact]
        public async Task GetByIdAsync_ReturnsPlacement()
        {
            // Arrange
            var placementId = Guid.NewGuid();
            var placement = CreateTestPlacement(id: placementId);
            _context.Placements.Add(placement);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetByIdAsync(placementId, _tenantId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(placementId, result!.Id);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsNullWhenNotFound()
        {
            // Act
            var result = await _service.GetByIdAsync(Guid.NewGuid(), _tenantId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsNullForOtherTenant()
        {
            // Arrange
            var otherTenantId = Guid.NewGuid();
            var placementId = Guid.NewGuid();
            var placement = CreateTestPlacement(id: placementId, tenantId: otherTenantId);
            _context.Placements.Add(placement);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetByIdAsync(placementId, _tenantId);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetByIdWithDetailsAsync Tests

        [Fact]
        public async Task GetByIdWithDetailsAsync_ReturnsPlacementWithAllIncludes()
        {
            // Arrange
            var company = CreateTestCompany();
            var supervisor = CreateTestSupervisor(company.Id);
            var student = CreateTestStudent();
            var placementId = Guid.NewGuid();
            var placement = CreateTestPlacement(id: placementId, companyId: company.Id, supervisorId: supervisor.Id);
            var placementStudent = CreateTestPlacementStudent(placement.Id, student.Id);

            _context.Companies.Add(company);
            _context.Supervisors.Add(supervisor);
            _context.Students.Add(student);
            _context.Placements.Add(placement);
            _context.PlacementStudents.Add(placementStudent);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            // Act
            var result = await _service.GetByIdWithDetailsAsync(placementId, _tenantId);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result!.Company);
            Assert.NotNull(result.Supervisor);
            Assert.Single(result.PlacementStudents);
            Assert.NotNull(result.PlacementStudents.First().Student);
        }

        [Fact]
        public async Task GetByIdWithDetailsAsync_ReturnsNullWhenNotFound()
        {
            // Act
            var result = await _service.GetByIdWithDetailsAsync(Guid.NewGuid(), _tenantId);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region CreateAsync Tests

        [Fact]
        public async Task CreateAsync_CreatesPlacement()
        {
            // Arrange
            var placement = new Placement { Year = 2025, Status = "draft" };

            // Act
            await _service.CreateAsync(placement, _tenantId);

            // Assert
            var saved = await _context.Placements.AsNoTracking().FirstOrDefaultAsync(p => p.Year == 2025);
            Assert.NotNull(saved);
            Assert.NotEqual(default, saved!.Id);
        }

        [Fact]
        public async Task CreateAsync_SetsTenantId()
        {
            // Arrange
            var placement = new Placement { Year = 2025, Status = "draft" };

            // Act
            await _service.CreateAsync(placement, _tenantId);

            // Assert
            var saved = await _context.Placements.AsNoTracking().FirstOrDefaultAsync(p => p.Year == 2025);
            Assert.NotNull(saved);
            Assert.Equal(_tenantId, saved!.TenantId);
        }

        [Fact]
        public async Task CreateAsync_ThrowsWhenNoTenant()
        {
            // Arrange
            var serviceWithoutTenant = new PlacementService(_context, null);
            var placement = new Placement { Year = 2025, Status = "draft" };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => serviceWithoutTenant.CreateAsync(placement, null));
            Assert.Contains("Tenant must be known", exception.Message);
        }

        [Fact]
        public async Task CreateAsync_SetsAuditFields()
        {
            // Arrange
            var placement = new Placement { Year = 2025, Status = "draft" };

            // Act
            await _service.CreateAsync(placement, _tenantId);

            // Assert
            var saved = await _context.Placements.AsNoTracking().FirstOrDefaultAsync(p => p.Year == 2025);
            Assert.NotNull(saved);
            Assert.NotEqual(default, saved!.CreatedAt);
            Assert.Equal("test-user", saved.CreatedBy);
        }

        #endregion

        #region UpdateAsync Tests

        [Fact]
        public async Task UpdateAsync_UpdatesAllFields()
        {
            // Arrange
            var company1 = CreateTestCompany(name: "Company 1");
            var company2 = CreateTestCompany(name: "Company 2");
            var supervisor1 = CreateTestSupervisor(company1.Id);
            var supervisor2 = CreateTestSupervisor(company2.Id);
            var placementId = Guid.NewGuid();
            var placement = CreateTestPlacement(id: placementId, companyId: company1.Id, supervisorId: supervisor1.Id, year: 2024);

            _context.Companies.AddRange(company1, company2);
            _context.Supervisors.AddRange(supervisor1, supervisor2);
            _context.Placements.Add(placement);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            var updatedPlacement = new Placement
            {
                Id = placementId,
                CompanyId = company2.Id,
                SupervisorId = supervisor2.Id,
                Year = 2025,
                Status = "confirmed",
                PlacementRole = "Updated Role",
                DressRequirement = "Formal",
                WorkStartTime = "09:00",
                WorkEndTime = "17:00",
                HasOhsPolicy = true,
                HasInductionProgram = true,
                EmployerSubmittedAt = DateTime.UtcNow
            };

            // Act
            await _service.UpdateAsync(updatedPlacement, null, _tenantId);

            // Assert
            var saved = await _context.Placements.AsNoTracking().FirstOrDefaultAsync(p => p.Id == placementId);
            Assert.NotNull(saved);
            Assert.Equal(company2.Id, saved!.CompanyId);
            Assert.Equal(supervisor2.Id, saved.SupervisorId);
            Assert.Equal(2025, saved.Year);
            Assert.Equal("confirmed", saved.Status);
            Assert.Equal("Updated Role", saved.PlacementRole);
            Assert.Equal("Formal", saved.DressRequirement);
            Assert.Equal("09:00", saved.WorkStartTime);
            Assert.Equal("17:00", saved.WorkEndTime);
            Assert.True(saved.HasOhsPolicy);
            Assert.True(saved.HasInductionProgram);
            Assert.NotNull(saved.EmployerSubmittedAt);
        }

        [Fact]
        public async Task UpdateAsync_ThrowsWhenPlacementNotFound()
        {
            // Arrange
            var updatedPlacement = new Placement
            {
                Id = Guid.NewGuid(),
                Year = 2025
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.UpdateAsync(updatedPlacement, null, _tenantId));
            Assert.Contains("Placement not found", exception.Message);
        }

        [Fact]
        public async Task UpdateAsync_ThrowsForOtherTenantPlacement()
        {
            // Arrange
            var otherTenantId = Guid.NewGuid();
            var placementId = Guid.NewGuid();
            var placement = CreateTestPlacement(id: placementId, tenantId: otherTenantId);
            _context.Placements.Add(placement);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            var updatedPlacement = new Placement
            {
                Id = placementId,
                Year = 2025
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.UpdateAsync(updatedPlacement, null, _tenantId));
            Assert.Contains("Placement not found", exception.Message);
        }

        #endregion

        #region DeleteAsync Tests

        [Fact]
        public async Task DeleteAsync_SoftDeletesPlacement()
        {
            // Arrange
            var placementId = Guid.NewGuid();
            var placement = CreateTestPlacement(id: placementId);
            _context.Placements.Add(placement);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            // Act
            await _service.DeleteAsync(placementId, _tenantId);

            // Assert - soft delete handled by DbContext interceptor
            var deleted = await _context.Placements.IgnoreQueryFilters().AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == placementId);
            Assert.NotNull(deleted);
            Assert.True(deleted!.IsDeleted);
        }

        [Fact]
        public async Task DeleteAsync_SetsDeleteAuditFields()
        {
            // Arrange
            var placementId = Guid.NewGuid();
            var placement = CreateTestPlacement(id: placementId);
            _context.Placements.Add(placement);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            // Act
            await _service.DeleteAsync(placementId, _tenantId);

            // Assert
            var deleted = await _context.Placements.IgnoreQueryFilters().AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == placementId);
            Assert.NotNull(deleted);
            Assert.NotNull(deleted!.DeletedAt);
            Assert.Equal("test-user", deleted.DeletedBy);
        }

        [Fact]
        public async Task DeleteAsync_DoesNothingWhenNotFound()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act - should not throw
            await _service.DeleteAsync(nonExistentId, _tenantId);

            // Assert - no exception thrown
            Assert.True(true);
        }

        [Fact]
        public async Task DeleteAsync_DoesNotDeleteOtherTenantPlacement()
        {
            // Arrange
            var otherTenantId = Guid.NewGuid();
            var placementId = Guid.NewGuid();
            var placement = CreateTestPlacement(id: placementId, tenantId: otherTenantId);
            _context.Placements.Add(placement);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            // Act
            await _service.DeleteAsync(placementId, _tenantId);

            // Assert - placement should still exist (not deleted)
            var stillExists = await _context.Placements.IgnoreQueryFilters().AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == placementId);
            Assert.NotNull(stillExists);
            Assert.False(stillExists!.IsDeleted);
        }

        #endregion

        #region ExistsAsync Tests

        [Fact]
        public async Task ExistsAsync_ReturnsTrueWhenExists()
        {
            // Arrange
            var placementId = Guid.NewGuid();
            var placement = CreateTestPlacement(id: placementId);
            _context.Placements.Add(placement);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.ExistsAsync(placementId, _tenantId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ExistsAsync_ReturnsFalseWhenNotFound()
        {
            // Act
            var result = await _service.ExistsAsync(Guid.NewGuid(), _tenantId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ExistsAsync_ReturnsFalseForOtherTenant()
        {
            // Arrange
            var otherTenantId = Guid.NewGuid();
            var placementId = Guid.NewGuid();
            var placement = CreateTestPlacement(id: placementId, tenantId: otherTenantId);
            _context.Placements.Add(placement);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.ExistsAsync(placementId, _tenantId);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region RecalculateStatusAsync Tests

        [Fact]
        public async Task RecalculateStatusAsync_NoStudents_SetsDraft()
        {
            // Arrange
            var placementId = Guid.NewGuid();
            var placement = CreateTestPlacement(id: placementId, status: "confirmed");
            _context.Placements.Add(placement);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            // Act
            await _service.RecalculateStatusAsync(placementId, _tenantId);

            // Assert
            var result = await _context.Placements.AsNoTracking().FirstOrDefaultAsync(p => p.Id == placementId);
            Assert.NotNull(result);
            Assert.Equal("draft", result!.Status);
        }

        [Fact]
        public async Task RecalculateStatusAsync_EmployerNotSubmitted_SetsPendingEmployer()
        {
            // Arrange
            var student = CreateTestStudent();
            var placementId = Guid.NewGuid();
            var placement = CreateTestPlacement(id: placementId, status: "draft", employerSubmittedAt: null);
            var ps = CreateTestPlacementStudent(placementId, student.Id);

            _context.Students.Add(student);
            _context.Placements.Add(placement);
            _context.PlacementStudents.Add(ps);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            // Act
            await _service.RecalculateStatusAsync(placementId, _tenantId);

            // Assert
            var result = await _context.Placements.AsNoTracking().FirstOrDefaultAsync(p => p.Id == placementId);
            Assert.NotNull(result);
            Assert.Equal("pending_employer", result!.Status);
        }

        [Fact]
        public async Task RecalculateStatusAsync_AllStudentsConfirmed_SetsConfirmed()
        {
            // Arrange
            var student1 = CreateTestStudent(firstName: "Alice");
            var student2 = CreateTestStudent(firstName: "Bob");
            var placementId = Guid.NewGuid();
            var placement = CreateTestPlacement(id: placementId, status: "pending_parents", employerSubmittedAt: DateTime.UtcNow);
            var ps1 = CreateTestPlacementStudent(placementId, student1.Id, status: "confirmed");
            var ps2 = CreateTestPlacementStudent(placementId, student2.Id, status: "confirmed");

            _context.Students.AddRange(student1, student2);
            _context.Placements.Add(placement);
            _context.PlacementStudents.AddRange(ps1, ps2);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            // Act
            await _service.RecalculateStatusAsync(placementId, _tenantId);

            // Assert
            var result = await _context.Placements.AsNoTracking().FirstOrDefaultAsync(p => p.Id == placementId);
            Assert.NotNull(result);
            Assert.Equal("confirmed", result!.Status);
        }

        [Fact]
        public async Task RecalculateStatusAsync_SomeStudentsConfirmed_SetsPartial()
        {
            // Arrange
            var student1 = CreateTestStudent(firstName: "Alice");
            var student2 = CreateTestStudent(firstName: "Bob");
            var placementId = Guid.NewGuid();
            var placement = CreateTestPlacement(id: placementId, status: "draft", employerSubmittedAt: DateTime.UtcNow);
            var ps1 = CreateTestPlacementStudent(placementId, student1.Id, status: "confirmed");
            var ps2 = CreateTestPlacementStudent(placementId, student2.Id, status: "pending_parent");

            _context.Students.AddRange(student1, student2);
            _context.Placements.Add(placement);
            _context.PlacementStudents.AddRange(ps1, ps2);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            // Act
            await _service.RecalculateStatusAsync(placementId, _tenantId);

            // Assert
            var result = await _context.Placements.AsNoTracking().FirstOrDefaultAsync(p => p.Id == placementId);
            Assert.NotNull(result);
            Assert.Equal("partial", result!.Status);
        }

        [Fact]
        public async Task RecalculateStatusAsync_NoStudentsConfirmed_SetsPendingParents()
        {
            // Arrange
            var student1 = CreateTestStudent(firstName: "Alice");
            var student2 = CreateTestStudent(firstName: "Bob");
            var placementId = Guid.NewGuid();
            var placement = CreateTestPlacement(id: placementId, status: "confirmed", employerSubmittedAt: DateTime.UtcNow);
            var ps1 = CreateTestPlacementStudent(placementId, student1.Id, status: "pending_parent");
            var ps2 = CreateTestPlacementStudent(placementId, student2.Id, status: "pending_parent");

            _context.Students.AddRange(student1, student2);
            _context.Placements.Add(placement);
            _context.PlacementStudents.AddRange(ps1, ps2);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            // Act
            await _service.RecalculateStatusAsync(placementId, _tenantId);

            // Assert
            var result = await _context.Placements.AsNoTracking().FirstOrDefaultAsync(p => p.Id == placementId);
            Assert.NotNull(result);
            Assert.Equal("pending_parents", result!.Status);
        }

        [Fact]
        public async Task RecalculateStatusAsync_ExcludesSoftDeletedStudents()
        {
            // Arrange
            var student1 = CreateTestStudent(firstName: "Alice");
            var student2 = CreateTestStudent(firstName: "Bob");
            var placementId = Guid.NewGuid();
            var placement = CreateTestPlacement(id: placementId, status: "partial", employerSubmittedAt: DateTime.UtcNow);
            var ps1 = CreateTestPlacementStudent(placementId, student1.Id, status: "confirmed");
            var ps2 = CreateTestPlacementStudent(placementId, student2.Id, status: "pending_parent");
            ps2.IsDeleted = true;
            ps2.DeletedAt = DateTime.UtcNow;

            _context.Students.AddRange(student1, student2);
            _context.Placements.Add(placement);
            _context.PlacementStudents.AddRange(ps1, ps2);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            // Act
            await _service.RecalculateStatusAsync(placementId, _tenantId);

            // Assert - Only ps1 (confirmed) counts, so should be "confirmed"
            var result = await _context.Placements.AsNoTracking().FirstOrDefaultAsync(p => p.Id == placementId);
            Assert.NotNull(result);
            Assert.Equal("confirmed", result!.Status);
        }

        [Fact]
        public async Task RecalculateStatusAsync_DoesNothingWhenPlacementNotFound()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act - should not throw
            await _service.RecalculateStatusAsync(nonExistentId, _tenantId);

            // Assert - no exception thrown
            Assert.True(true);
        }

        #endregion

        #region Test Helpers

        private class FakeUserProvider : IUserProvider
        {
            public string? GetCurrentUsername() => "test-user";
        }

        private class FakeTenantProvider : ITenantProvider
        {
            private readonly Guid _id;
            public FakeTenantProvider(Guid? tenantId = null) => _id = tenantId ?? Guid.NewGuid();
            public Guid? GetCurrentTenantId() => _id;
        }

        #endregion
    }
}
