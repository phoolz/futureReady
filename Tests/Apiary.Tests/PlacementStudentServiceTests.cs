using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Apiary.Data;
using Apiary.Models.School;
using Apiary.Services;
using Apiary.Services.PlacementStudents;

namespace Apiary.Tests
{
    public class PlacementStudentServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Microsoft.Data.Sqlite.SqliteConnection _connection;
        private readonly PlacementStudentService _service;
        private readonly Guid _tenantId = Guid.NewGuid();
        private readonly FakeTenantProvider _tenantProvider;

        public PlacementStudentServiceTests()
        {
            var userProvider = new FakeUserProvider();
            _tenantProvider = new FakeTenantProvider(_tenantId);
            (_context, _connection) = TestDbContextFactory.CreateSqliteInMemoryContext(userProvider, _tenantProvider);
            _service = new PlacementStudentService(_context, _tenantProvider);
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

        #region GetByPlacementIdAsync Tests

        [Fact]
        public async Task GetByPlacementIdAsync_ReturnsStudentsForPlacement()
        {
            // Arrange
            var student1 = CreateTestStudent(firstName: "Alice", lastName: "Smith");
            var student2 = CreateTestStudent(firstName: "Bob", lastName: "Jones");
            var placement = CreateTestPlacement();
            var ps1 = CreateTestPlacementStudent(placement.Id, student1.Id);
            var ps2 = CreateTestPlacementStudent(placement.Id, student2.Id);

            _context.Students.AddRange(student1, student2);
            _context.Placements.Add(placement);
            _context.PlacementStudents.AddRange(ps1, ps2);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetByPlacementIdAsync(placement.Id, _tenantId);

            // Assert
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetByPlacementIdAsync_ReturnsEmptyWhenNoStudents()
        {
            // Arrange
            var placement = CreateTestPlacement();
            _context.Placements.Add(placement);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetByPlacementIdAsync(placement.Id, _tenantId);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetByPlacementIdAsync_IncludesStudentNavigation()
        {
            // Arrange
            var student = CreateTestStudent(firstName: "Alice", lastName: "Smith");
            var placement = CreateTestPlacement();
            var ps = CreateTestPlacementStudent(placement.Id, student.Id);

            _context.Students.Add(student);
            _context.Placements.Add(placement);
            _context.PlacementStudents.Add(ps);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            // Act
            var result = await _service.GetByPlacementIdAsync(placement.Id, _tenantId);

            // Assert
            Assert.Single(result);
            Assert.NotNull(result[0].Student);
            Assert.Equal("Alice", result[0].Student!.FirstName);
            Assert.Equal("Smith", result[0].Student.LastName);
        }

        [Fact]
        public async Task GetByPlacementIdAsync_OrdersByLastNameThenFirstName()
        {
            // Arrange
            var student1 = CreateTestStudent(firstName: "Zoe", lastName: "Adams");
            var student2 = CreateTestStudent(firstName: "Alice", lastName: "Williams");
            var student3 = CreateTestStudent(firstName: "Bob", lastName: "Adams");
            var placement = CreateTestPlacement();
            var ps1 = CreateTestPlacementStudent(placement.Id, student1.Id);
            var ps2 = CreateTestPlacementStudent(placement.Id, student2.Id);
            var ps3 = CreateTestPlacementStudent(placement.Id, student3.Id);

            _context.Students.AddRange(student1, student2, student3);
            _context.Placements.Add(placement);
            _context.PlacementStudents.AddRange(ps1, ps2, ps3);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            // Act
            var result = await _service.GetByPlacementIdAsync(placement.Id, _tenantId);

            // Assert
            Assert.Equal(3, result.Count);
            // Adams comes before Williams
            Assert.Equal("Adams", result[0].Student!.LastName);
            Assert.Equal("Adams", result[1].Student!.LastName);
            Assert.Equal("Williams", result[2].Student!.LastName);
            // Bob comes before Zoe within Adams
            Assert.Equal("Bob", result[0].Student!.FirstName);
            Assert.Equal("Zoe", result[1].Student!.FirstName);
        }

        [Fact]
        public async Task GetByPlacementIdAsync_OnlyReturnsTenantStudents()
        {
            // Arrange
            var otherTenantId = Guid.NewGuid();
            var student1 = CreateTestStudent(firstName: "My", lastName: "Student");
            var student2 = CreateTestStudent(firstName: "Other", lastName: "Student", tenantId: otherTenantId);
            var placement = CreateTestPlacement();
            var ps1 = CreateTestPlacementStudent(placement.Id, student1.Id, tenantId: _tenantId);
            var ps2 = CreateTestPlacementStudent(placement.Id, student2.Id, tenantId: otherTenantId);

            _context.Students.AddRange(student1, student2);
            _context.Placements.Add(placement);
            _context.PlacementStudents.AddRange(ps1, ps2);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetByPlacementIdAsync(placement.Id, _tenantId);

            // Assert
            Assert.Single(result);
            Assert.Equal("My", result[0].Student!.FirstName);
        }

        #endregion

        #region GetByIdAsync Tests

        [Fact]
        public async Task GetByIdAsync_ReturnsPlacementStudent()
        {
            // Arrange
            var student = CreateTestStudent();
            var placement = CreateTestPlacement();
            var psId = Guid.NewGuid();
            var ps = CreateTestPlacementStudent(placement.Id, student.Id, id: psId);

            _context.Students.Add(student);
            _context.Placements.Add(placement);
            _context.PlacementStudents.Add(ps);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetByIdAsync(psId, _tenantId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(psId, result!.Id);
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
            var student = CreateTestStudent(tenantId: otherTenantId);
            var placement = CreateTestPlacement(tenantId: otherTenantId);
            var psId = Guid.NewGuid();
            var ps = CreateTestPlacementStudent(placement.Id, student.Id, id: psId, tenantId: otherTenantId);

            _context.Students.Add(student);
            _context.Placements.Add(placement);
            _context.PlacementStudents.Add(ps);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetByIdAsync(psId, _tenantId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetByIdAsync_IncludesStudentAndPlacement()
        {
            // Arrange
            var student = CreateTestStudent(firstName: "Test", lastName: "Student");
            var placement = CreateTestPlacement(year: 2025);
            var psId = Guid.NewGuid();
            var ps = CreateTestPlacementStudent(placement.Id, student.Id, id: psId);

            _context.Students.Add(student);
            _context.Placements.Add(placement);
            _context.PlacementStudents.Add(ps);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            // Act
            var result = await _service.GetByIdAsync(psId, _tenantId);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result!.Student);
            Assert.NotNull(result.Placement);
            Assert.Equal("Test", result.Student!.FirstName);
            Assert.Equal(2025, result.Placement!.Year);
        }

        #endregion

        #region GetByPlacementAndStudentAsync Tests

        [Fact]
        public async Task GetByPlacementAndStudentAsync_ReturnsCorrectRecord()
        {
            // Arrange
            var student = CreateTestStudent();
            var placement = CreateTestPlacement();
            var ps = CreateTestPlacementStudent(placement.Id, student.Id, status: "confirmed");

            _context.Students.Add(student);
            _context.Placements.Add(placement);
            _context.PlacementStudents.Add(ps);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetByPlacementAndStudentAsync(placement.Id, student.Id, _tenantId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(placement.Id, result!.PlacementId);
            Assert.Equal(student.Id, result.StudentId);
            Assert.Equal("confirmed", result.Status);
        }

        [Fact]
        public async Task GetByPlacementAndStudentAsync_ReturnsNullWhenNotFound()
        {
            // Act
            var result = await _service.GetByPlacementAndStudentAsync(Guid.NewGuid(), Guid.NewGuid(), _tenantId);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetByStudentIdAsync Tests

        [Fact]
        public async Task GetByStudentIdAsync_ReturnsAllPlacementsForStudent()
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
        public async Task GetByStudentIdAsync_IncludesPlacementWithCompanyAndSupervisor()
        {
            // Arrange
            var company = CreateTestCompany(name: "Test Corp");
            var supervisor = CreateTestSupervisor(company.Id);
            var student = CreateTestStudent();
            var placement = CreateTestPlacement(companyId: company.Id, supervisorId: supervisor.Id);
            var ps = CreateTestPlacementStudent(placement.Id, student.Id);

            _context.Companies.Add(company);
            _context.Supervisors.Add(supervisor);
            _context.Students.Add(student);
            _context.Placements.Add(placement);
            _context.PlacementStudents.Add(ps);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            // Act
            var result = await _service.GetByStudentIdAsync(student.Id, _tenantId);

            // Assert
            Assert.Single(result);
            Assert.NotNull(result[0].Placement);
            Assert.NotNull(result[0].Placement!.Company);
            Assert.NotNull(result[0].Placement.Supervisor);
            Assert.Equal("Test Corp", result[0].Placement.Company!.Name);
        }

        #endregion

        #region AddStudentToPlacementAsync Tests

        [Fact]
        public async Task AddStudentToPlacementAsync_AddsStudent()
        {
            // Arrange
            var student = CreateTestStudent();
            var placement = CreateTestPlacement();

            _context.Students.Add(student);
            _context.Placements.Add(placement);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            // Act
            var result = await _service.AddStudentToPlacementAsync(placement.Id, student.Id, _tenantId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(placement.Id, result.PlacementId);
            Assert.Equal(student.Id, result.StudentId);

            var saved = await _context.PlacementStudents.AsNoTracking()
                .FirstOrDefaultAsync(ps => ps.PlacementId == placement.Id && ps.StudentId == student.Id);
            Assert.NotNull(saved);
        }

        [Fact]
        public async Task AddStudentToPlacementAsync_SetsStatusToPendingParent()
        {
            // Arrange
            var student = CreateTestStudent();
            var placement = CreateTestPlacement();

            _context.Students.Add(student);
            _context.Placements.Add(placement);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            // Act
            var result = await _service.AddStudentToPlacementAsync(placement.Id, student.Id, _tenantId);

            // Assert
            Assert.Equal("pending_parent", result.Status);
        }

        [Fact]
        public async Task AddStudentToPlacementAsync_SetsTenantId()
        {
            // Arrange
            var student = CreateTestStudent();
            var placement = CreateTestPlacement();

            _context.Students.Add(student);
            _context.Placements.Add(placement);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            // Act
            var result = await _service.AddStudentToPlacementAsync(placement.Id, student.Id, _tenantId);

            // Assert
            Assert.Equal(_tenantId, result.TenantId);
        }

        [Fact]
        public async Task AddStudentToPlacementAsync_ThrowsWhenNoTenant()
        {
            // Arrange
            var serviceWithoutTenant = new PlacementStudentService(_context, null);
            var student = CreateTestStudent();
            var placement = CreateTestPlacement();

            _context.Students.Add(student);
            _context.Placements.Add(placement);
            await _context.SaveChangesAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => serviceWithoutTenant.AddStudentToPlacementAsync(placement.Id, student.Id, null));
            Assert.Contains("Tenant must be known", exception.Message);
        }

        [Fact]
        public async Task AddStudentToPlacementAsync_ThrowsWhenStudentAlreadyInPlacement()
        {
            // Arrange
            var student = CreateTestStudent();
            var placement = CreateTestPlacement();
            var existingPs = CreateTestPlacementStudent(placement.Id, student.Id);

            _context.Students.Add(student);
            _context.Placements.Add(placement);
            _context.PlacementStudents.Add(existingPs);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.AddStudentToPlacementAsync(placement.Id, student.Id, _tenantId));
            Assert.Contains("already assigned", exception.Message);
        }

        [Fact]
        public async Task AddStudentToPlacementAsync_ReturnsCreatedRecordWithStudentLoaded()
        {
            // Arrange
            var student = CreateTestStudent(firstName: "Loaded", lastName: "Student");
            var placement = CreateTestPlacement();

            _context.Students.Add(student);
            _context.Placements.Add(placement);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            // Act
            var result = await _service.AddStudentToPlacementAsync(placement.Id, student.Id, _tenantId);

            // Assert
            Assert.NotNull(result.Student);
            Assert.Equal("Loaded", result.Student!.FirstName);
            Assert.Equal("Student", result.Student.LastName);
        }

        #endregion

        #region RemoveStudentFromPlacementAsync Tests

        [Fact]
        public async Task RemoveStudentFromPlacementAsync_RemovesStudent()
        {
            // Arrange
            var student = CreateTestStudent();
            var placement = CreateTestPlacement();
            var ps = CreateTestPlacementStudent(placement.Id, student.Id);

            _context.Students.Add(student);
            _context.Placements.Add(placement);
            _context.PlacementStudents.Add(ps);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            // Act
            await _service.RemoveStudentFromPlacementAsync(placement.Id, student.Id, _tenantId);

            // Assert - soft delete handled by DbContext interceptor
            var deleted = await _context.PlacementStudents.IgnoreQueryFilters().AsNoTracking()
                .FirstOrDefaultAsync(p => p.PlacementId == placement.Id && p.StudentId == student.Id);
            Assert.NotNull(deleted);
            Assert.True(deleted!.IsDeleted);
        }

        [Fact]
        public async Task RemoveStudentFromPlacementAsync_DoesNothingWhenNotFound()
        {
            // Act - should not throw
            await _service.RemoveStudentFromPlacementAsync(Guid.NewGuid(), Guid.NewGuid(), _tenantId);

            // Assert - no exception thrown
            Assert.True(true);
        }

        [Fact]
        public async Task RemoveStudentFromPlacementAsync_DoesNotRemoveOtherTenantStudent()
        {
            // Arrange
            var otherTenantId = Guid.NewGuid();
            var student = CreateTestStudent(tenantId: otherTenantId);
            var placement = CreateTestPlacement(tenantId: otherTenantId);
            var ps = CreateTestPlacementStudent(placement.Id, student.Id, tenantId: otherTenantId);

            _context.Students.Add(student);
            _context.Placements.Add(placement);
            _context.PlacementStudents.Add(ps);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            // Act
            await _service.RemoveStudentFromPlacementAsync(placement.Id, student.Id, _tenantId);

            // Assert - record should still exist (not deleted)
            var stillExists = await _context.PlacementStudents.IgnoreQueryFilters().AsNoTracking()
                .FirstOrDefaultAsync(p => p.PlacementId == placement.Id && p.StudentId == student.Id);
            Assert.NotNull(stillExists);
            Assert.False(stillExists!.IsDeleted);
        }

        [Fact]
        public async Task RemoveStudentFromPlacementAsync_SoftDeletesRecord()
        {
            // Arrange
            var student = CreateTestStudent();
            var placement = CreateTestPlacement();
            var ps = CreateTestPlacementStudent(placement.Id, student.Id);

            _context.Students.Add(student);
            _context.Placements.Add(placement);
            _context.PlacementStudents.Add(ps);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            // Act
            await _service.RemoveStudentFromPlacementAsync(placement.Id, student.Id, _tenantId);

            // Assert
            var deleted = await _context.PlacementStudents.IgnoreQueryFilters().AsNoTracking()
                .FirstOrDefaultAsync(p => p.PlacementId == placement.Id && p.StudentId == student.Id);
            Assert.NotNull(deleted);
            Assert.True(deleted!.IsDeleted);
            Assert.NotNull(deleted.DeletedAt);
            Assert.Equal("test-user", deleted.DeletedBy);
        }

        #endregion

        #region MarkParentSubmittedAsync Tests

        [Fact]
        public async Task MarkParentSubmittedAsync_SetsStatusToConfirmed()
        {
            // Arrange
            var student = CreateTestStudent();
            var placement = CreateTestPlacement();
            var ps = CreateTestPlacementStudent(placement.Id, student.Id, status: "pending_parent");

            _context.Students.Add(student);
            _context.Placements.Add(placement);
            _context.PlacementStudents.Add(ps);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            // Act
            await _service.MarkParentSubmittedAsync(placement.Id, student.Id, _tenantId);

            // Assert
            var updated = await _context.PlacementStudents.AsNoTracking()
                .FirstOrDefaultAsync(p => p.PlacementId == placement.Id && p.StudentId == student.Id);
            Assert.NotNull(updated);
            Assert.Equal("confirmed", updated!.Status);
        }

        [Fact]
        public async Task MarkParentSubmittedAsync_SetsParentSubmittedAtTimestamp()
        {
            // Arrange
            var student = CreateTestStudent();
            var placement = CreateTestPlacement();
            var ps = CreateTestPlacementStudent(placement.Id, student.Id, status: "pending_parent");

            _context.Students.Add(student);
            _context.Placements.Add(placement);
            _context.PlacementStudents.Add(ps);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            var beforeTime = DateTime.UtcNow;

            // Act
            await _service.MarkParentSubmittedAsync(placement.Id, student.Id, _tenantId);

            var afterTime = DateTime.UtcNow;

            // Assert
            var updated = await _context.PlacementStudents.AsNoTracking()
                .FirstOrDefaultAsync(p => p.PlacementId == placement.Id && p.StudentId == student.Id);
            Assert.NotNull(updated);
            Assert.NotNull(updated!.ParentSubmittedAt);
            Assert.True(updated.ParentSubmittedAt >= beforeTime && updated.ParentSubmittedAt <= afterTime);
        }

        [Fact]
        public async Task MarkParentSubmittedAsync_ThrowsWhenNotFound()
        {
            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.MarkParentSubmittedAsync(Guid.NewGuid(), Guid.NewGuid(), _tenantId));
            Assert.Contains("PlacementStudent not found", exception.Message);
        }

        [Fact]
        public async Task MarkParentSubmittedAsync_ThrowsForOtherTenant()
        {
            // Arrange
            var otherTenantId = Guid.NewGuid();
            var student = CreateTestStudent(tenantId: otherTenantId);
            var placement = CreateTestPlacement(tenantId: otherTenantId);
            var ps = CreateTestPlacementStudent(placement.Id, student.Id, tenantId: otherTenantId);

            _context.Students.Add(student);
            _context.Placements.Add(placement);
            _context.PlacementStudents.Add(ps);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.MarkParentSubmittedAsync(placement.Id, student.Id, _tenantId));
            Assert.Contains("PlacementStudent not found", exception.Message);
        }

        #endregion

        #region UpdateStatusAsync Tests

        [Fact]
        public async Task UpdateStatusAsync_UpdatesStatus()
        {
            // Arrange
            var student = CreateTestStudent();
            var placement = CreateTestPlacement();
            var psId = Guid.NewGuid();
            var ps = CreateTestPlacementStudent(placement.Id, student.Id, id: psId, status: "pending_parent");

            _context.Students.Add(student);
            _context.Placements.Add(placement);
            _context.PlacementStudents.Add(ps);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            // Act
            await _service.UpdateStatusAsync(psId, "withdrawn", _tenantId);

            // Assert
            var updated = await _context.PlacementStudents.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == psId);
            Assert.NotNull(updated);
            Assert.Equal("withdrawn", updated!.Status);
        }

        [Fact]
        public async Task UpdateStatusAsync_ThrowsWhenNotFound()
        {
            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.UpdateStatusAsync(Guid.NewGuid(), "withdrawn", _tenantId));
            Assert.Contains("PlacementStudent not found", exception.Message);
        }

        [Fact]
        public async Task UpdateStatusAsync_ThrowsForOtherTenant()
        {
            // Arrange
            var otherTenantId = Guid.NewGuid();
            var student = CreateTestStudent(tenantId: otherTenantId);
            var placement = CreateTestPlacement(tenantId: otherTenantId);
            var psId = Guid.NewGuid();
            var ps = CreateTestPlacementStudent(placement.Id, student.Id, id: psId, tenantId: otherTenantId);

            _context.Students.Add(student);
            _context.Placements.Add(placement);
            _context.PlacementStudents.Add(ps);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.UpdateStatusAsync(psId, "withdrawn", _tenantId));
            Assert.Contains("PlacementStudent not found", exception.Message);
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
