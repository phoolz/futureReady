using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Apiary.Data;
using Apiary.Models;
using Apiary.Models.School;
using Apiary.Services;
using Apiary.Services.Companies;

namespace Apiary.Tests
{
    public class CompanyServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Microsoft.Data.Sqlite.SqliteConnection _connection;
        private readonly CompanyService _service;
        private readonly Guid _tenantId = Guid.NewGuid();
        private readonly FakeTenantProvider _tenantProvider;

        public CompanyServiceTests()
        {
            var userProvider = new FakeUserProvider();
            _tenantProvider = new FakeTenantProvider(_tenantId);
            (_context, _connection) = TestDbContextFactory.CreateSqliteInMemoryContext(userProvider, _tenantProvider);
            _service = new CompanyService(_context, _tenantProvider);
        }

        public void Dispose()
        {
            _context?.Dispose();
            _connection?.Dispose();
        }

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

        #region GetAllAsync Tests

        [Fact]
        public async Task GetAllAsync_ReturnsAllCompanies()
        {
            // Arrange
            var company1 = CreateTestCompany(name: "Company A");
            var company2 = CreateTestCompany(name: "Company B");
            _context.Companies.AddRange(company1, company2);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetAllAsync(_tenantId);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains(result, c => c.Name == "Company A");
            Assert.Contains(result, c => c.Name == "Company B");
        }

        [Fact]
        public async Task GetAllAsync_ReturnsEmptyListWhenNoCompanies()
        {
            // Act
            var result = await _service.GetAllAsync(_tenantId);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAllAsync_OnlyReturnsTenantCompanies()
        {
            // Arrange
            var otherTenantId = Guid.NewGuid();
            var myCompany = CreateTestCompany(name: "My Company", tenantId: _tenantId);
            var otherCompany = CreateTestCompany(name: "Other Company", tenantId: otherTenantId);
            _context.Companies.AddRange(myCompany, otherCompany);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetAllAsync(_tenantId);

            // Assert
            Assert.Single(result);
            Assert.Equal("My Company", result[0].Name);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsOrderedByName()
        {
            // Arrange
            var companyC = CreateTestCompany(name: "Charlie Corp");
            var companyA = CreateTestCompany(name: "Alpha Inc");
            var companyB = CreateTestCompany(name: "Bravo Ltd");
            _context.Companies.AddRange(companyC, companyA, companyB);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetAllAsync(_tenantId);

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Equal("Alpha Inc", result[0].Name);
            Assert.Equal("Bravo Ltd", result[1].Name);
            Assert.Equal("Charlie Corp", result[2].Name);
        }

        #endregion

        #region GetByIdAsync Tests

        [Fact]
        public async Task GetByIdAsync_ReturnsCompany()
        {
            // Arrange
            var companyId = Guid.NewGuid();
            var company = CreateTestCompany(id: companyId, name: "Find Me");
            _context.Companies.Add(company);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetByIdAsync(companyId, _tenantId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(companyId, result!.Id);
            Assert.Equal("Find Me", result.Name);
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
            var companyId = Guid.NewGuid();
            var company = CreateTestCompany(id: companyId, name: "Other Tenant Company", tenantId: otherTenantId);
            _context.Companies.Add(company);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetByIdAsync(companyId, _tenantId);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region CreateAsync Tests

        [Fact]
        public async Task CreateAsync_CreatesCompany()
        {
            // Arrange
            var company = new Company { Name = "New Company" };

            // Act
            await _service.CreateAsync(company, _tenantId);

            // Assert
            var saved = await _context.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.Name == "New Company");
            Assert.NotNull(saved);
            Assert.Equal(_tenantId, saved!.TenantId);
            Assert.NotEqual(default, saved.Id);
        }

        [Fact]
        public async Task CreateAsync_ThrowsWhenNoTenant()
        {
            // Arrange
            var serviceWithoutTenant = new CompanyService(_context, null);
            var company = new Company { Name = "No Tenant Company" };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => serviceWithoutTenant.CreateAsync(company, null));
            Assert.Contains("Tenant must be known", exception.Message);
        }

        [Fact]
        public async Task CreateAsync_SetsAuditFields()
        {
            // Arrange
            var company = new Company { Name = "Audited Company" };

            // Act
            await _service.CreateAsync(company, _tenantId);

            // Assert
            var saved = await _context.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.Name == "Audited Company");
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
            var companyId = Guid.NewGuid();
            var company = CreateTestCompany(id: companyId, name: "Original Name");
            _context.Companies.Add(company);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            var updatedCompany = new Company
            {
                Id = companyId,
                Name = "Updated Name",
                Industry = "Finance",
                StreetAddress = "456 New St",
                StreetAddress2 = "Suite 100",
                Suburb = "CBD",
                City = "Melbourne",
                State = "VIC",
                PostalCode = "3000",
                PostalStreetAddress = "PO Box 789",
                PostalSuburb = "GPO",
                PostalCity = "Melbourne",
                PostalState = "VIC",
                PostalPostalCode = "3001",
                PublicLiabilityInsurance5M = true,
                InsuranceValue = "$10M",
                HasPreviousWorkExperienceStudents = true
            };

            // Act
            await _service.UpdateAsync(updatedCompany, null, _tenantId);

            // Assert
            var saved = await _context.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.Id == companyId);
            Assert.NotNull(saved);
            Assert.Equal("Updated Name", saved!.Name);
            Assert.Equal("Finance", saved.Industry);
            Assert.Equal("456 New St", saved.StreetAddress);
            Assert.Equal("Suite 100", saved.StreetAddress2);
            Assert.Equal("CBD", saved.Suburb);
            Assert.Equal("Melbourne", saved.City);
            Assert.Equal("VIC", saved.State);
            Assert.Equal("3000", saved.PostalCode);
            Assert.Equal("PO Box 789", saved.PostalStreetAddress);
            Assert.Equal("GPO", saved.PostalSuburb);
            Assert.Equal("Melbourne", saved.PostalCity);
            Assert.Equal("VIC", saved.PostalState);
            Assert.Equal("3001", saved.PostalPostalCode);
            Assert.True(saved.PublicLiabilityInsurance5M);
            Assert.Equal("$10M", saved.InsuranceValue);
            Assert.True(saved.HasPreviousWorkExperienceStudents);
        }

        [Fact]
        public async Task UpdateAsync_ThrowsWhenCompanyNotFound()
        {
            // Arrange
            var updatedCompany = new Company
            {
                Id = Guid.NewGuid(),
                Name = "Non-existent"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.UpdateAsync(updatedCompany, null, _tenantId));
            Assert.Contains("Company not found", exception.Message);
        }

        [Fact]
        public async Task UpdateAsync_ThrowsForOtherTenantCompany()
        {
            // Arrange
            var otherTenantId = Guid.NewGuid();
            var companyId = Guid.NewGuid();
            var company = CreateTestCompany(id: companyId, name: "Other Company", tenantId: otherTenantId);
            _context.Companies.Add(company);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            var updatedCompany = new Company
            {
                Id = companyId,
                Name = "Attempted Update"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.UpdateAsync(updatedCompany, null, _tenantId));
            Assert.Contains("Company not found", exception.Message);
        }

        #endregion

        #region DeleteAsync Tests

        [Fact]
        public async Task DeleteAsync_SoftDeletesCompany()
        {
            // Arrange
            var companyId = Guid.NewGuid();
            var company = CreateTestCompany(id: companyId, name: "Delete Me");
            _context.Companies.Add(company);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            // Act
            await _service.DeleteAsync(companyId, _tenantId);

            // Assert - soft delete handled by DbContext interceptor
            var deleted = await _context.Companies.IgnoreQueryFilters().AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == companyId);
            Assert.NotNull(deleted);
            Assert.True(deleted!.IsDeleted);
        }

        [Fact]
        public async Task DeleteAsync_SetsDeleteAuditFields()
        {
            // Arrange
            var companyId = Guid.NewGuid();
            var company = CreateTestCompany(id: companyId, name: "Audit Delete");
            _context.Companies.Add(company);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            // Act
            await _service.DeleteAsync(companyId, _tenantId);

            // Assert
            var deleted = await _context.Companies.IgnoreQueryFilters().AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == companyId);
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
        public async Task DeleteAsync_DoesNotDeleteOtherTenantCompany()
        {
            // Arrange
            var otherTenantId = Guid.NewGuid();
            var companyId = Guid.NewGuid();
            var company = CreateTestCompany(id: companyId, name: "Other Tenant", tenantId: otherTenantId);
            _context.Companies.Add(company);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            // Act
            await _service.DeleteAsync(companyId, _tenantId);

            // Assert - company should still exist (not deleted)
            var stillExists = await _context.Companies.IgnoreQueryFilters().AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == companyId);
            Assert.NotNull(stillExists);
            Assert.False(stillExists!.IsDeleted);
        }

        #endregion

        #region ExistsAsync Tests

        [Fact]
        public async Task ExistsAsync_ReturnsTrueWhenExists()
        {
            // Arrange
            var companyId = Guid.NewGuid();
            var company = CreateTestCompany(id: companyId, name: "Exists");
            _context.Companies.Add(company);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.ExistsAsync(companyId, _tenantId);

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
            var companyId = Guid.NewGuid();
            var company = CreateTestCompany(id: companyId, name: "Other Tenant", tenantId: otherTenantId);
            _context.Companies.Add(company);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.ExistsAsync(companyId, _tenantId);

            // Assert
            Assert.False(result);
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
