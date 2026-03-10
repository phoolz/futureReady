using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using Apiary.Controllers;
using Apiary.Models.School;
using Apiary.Services;
using Apiary.Services.Companies;
using Apiary.Services.Supervisors;
using Apiary.Services.Placements;

namespace Apiary.Tests
{
    public class CompaniesControllerTests
    {
        private readonly Mock<ICompanyService> _mockCompanyService;
        private readonly Mock<ISupervisorService> _mockSupervisorService;
        private readonly Mock<IPlacementService> _mockPlacementService;
        private readonly Mock<ITenantProvider> _mockTenantProvider;
        private readonly CompaniesController _controller;
        private readonly Guid _tenantId = Guid.NewGuid();

        public CompaniesControllerTests()
        {
            _mockCompanyService = new Mock<ICompanyService>();
            _mockSupervisorService = new Mock<ISupervisorService>();
            _mockPlacementService = new Mock<IPlacementService>();
            _mockTenantProvider = new Mock<ITenantProvider>();

            _mockTenantProvider.Setup(t => t.GetCurrentTenantId()).Returns(_tenantId);

            _controller = new CompaniesController(
                _mockCompanyService.Object,
                _mockSupervisorService.Object,
                _mockPlacementService.Object,
                _mockTenantProvider.Object);

            _controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
        }

        private Company CreateTestCompany(Guid? id = null, string name = "Test Company")
        {
            return new Company
            {
                Id = id ?? Guid.NewGuid(),
                Name = name,
                TenantId = _tenantId,
                Industry = "Technology",
                StreetAddress = "123 Main St",
                City = "Sydney",
                State = "NSW",
                PostalCode = "2000"
            };
        }

        #region Index Tests

        [Fact]
        public async Task Index_ReturnsViewWithCompanies()
        {
            // Arrange
            var companies = new List<Company>
            {
                CreateTestCompany(name: "Company A"),
                CreateTestCompany(name: "Company B")
            };
            _mockCompanyService.Setup(s => s.GetAllAsync(_tenantId)).ReturnsAsync(companies);

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<Company>>(viewResult.Model);
            Assert.Equal(2, model.Count);
        }

        #endregion

        #region Details Tests

        [Fact]
        public async Task Details_WithValidId_ReturnsViewWithCompany()
        {
            // Arrange
            var companyId = Guid.NewGuid();
            var company = CreateTestCompany(id: companyId, name: "Details Company");
            _mockCompanyService.Setup(s => s.GetByIdAsync(companyId, _tenantId)).ReturnsAsync(company);
            _mockSupervisorService.Setup(s => s.GetByCompanyAsync(companyId, _tenantId))
                .ReturnsAsync(new List<Supervisor>());
            _mockPlacementService.Setup(s => s.GetByCompanyIdAsync(companyId, _tenantId))
                .ReturnsAsync(new List<Placement>());

            // Act
            var result = await _controller.Details(companyId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Company>(viewResult.Model);
            Assert.Equal(companyId, model.Id);
            Assert.Equal("Details Company", model.Name);
        }

        [Fact]
        public async Task Details_WithNullId_ReturnsNotFound()
        {
            // Act
            var result = await _controller.Details(null);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Details_WithNonExistentId_ReturnsNotFound()
        {
            // Arrange
            var companyId = Guid.NewGuid();
            _mockCompanyService.Setup(s => s.GetByIdAsync(companyId, _tenantId)).ReturnsAsync((Company?)null);

            // Act
            var result = await _controller.Details(companyId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Details_LoadsSupervisorsAndPlacements()
        {
            // Arrange
            var companyId = Guid.NewGuid();
            var company = CreateTestCompany(id: companyId);
            var supervisors = new List<Supervisor>
            {
                new Supervisor { Id = Guid.NewGuid(), FirstName = "John", LastName = "Doe", TenantId = _tenantId, CompanyId = companyId }
            };
            var placements = new List<Placement>
            {
                new Placement { Id = Guid.NewGuid(), TenantId = _tenantId, CompanyId = companyId }
            };

            _mockCompanyService.Setup(s => s.GetByIdAsync(companyId, _tenantId)).ReturnsAsync(company);
            _mockSupervisorService.Setup(s => s.GetByCompanyAsync(companyId, _tenantId)).ReturnsAsync(supervisors);
            _mockPlacementService.Setup(s => s.GetByCompanyIdAsync(companyId, _tenantId)).ReturnsAsync(placements);

            // Act
            var result = await _controller.Details(companyId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(viewResult.ViewData["Supervisors"]);
            Assert.NotNull(viewResult.ViewData["Placements"]);
            var viewSupervisors = Assert.IsAssignableFrom<List<Supervisor>>(viewResult.ViewData["Supervisors"]);
            var viewPlacements = Assert.IsAssignableFrom<List<Placement>>(viewResult.ViewData["Placements"]);
            Assert.Single(viewSupervisors);
            Assert.Single(viewPlacements);
        }

        #endregion

        #region Create Tests

        [Fact]
        public void Create_Get_ReturnsView()
        {
            // Act
            var result = _controller.Create();

            // Assert
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task Create_Post_ValidModel_CreatesAndRedirects()
        {
            // Arrange
            var company = CreateTestCompany(name: "New Company");
            _mockCompanyService.Setup(s => s.CreateAsync(It.IsAny<Company>(), null))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Create(company);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            _mockCompanyService.Verify(s => s.CreateAsync(
                It.Is<Company>(c => c.Name == "New Company"), null), Times.Once);
        }

        [Fact]
        public async Task Create_Post_InvalidModel_ReturnsViewWithErrors()
        {
            // Arrange
            var company = new Company();
            _controller.ModelState.AddModelError("Name", "Required");

            // Act
            var result = await _controller.Create(company);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Same(company, viewResult.Model);
            Assert.False(_controller.ModelState.IsValid);
            _mockCompanyService.Verify(s => s.CreateAsync(It.IsAny<Company>(), It.IsAny<Guid?>()), Times.Never);
        }

        [Fact]
        public async Task Create_Post_NoTenant_ShowsError()
        {
            // Arrange
            var company = CreateTestCompany(name: "No Tenant Company");
            _mockCompanyService.Setup(s => s.CreateAsync(It.IsAny<Company>(), null))
                .ThrowsAsync(new InvalidOperationException("Tenant must be known when creating a company."));

            // Act
            var result = await _controller.Create(company);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(_controller.ModelState.IsValid);
            Assert.Contains(_controller.ModelState.Values.SelectMany(v => v.Errors),
                e => e.ErrorMessage.Contains("Tenant must be known"));
        }

        #endregion

        #region Edit Tests

        [Fact]
        public async Task Edit_Get_WithValidId_ReturnsViewWithModel()
        {
            // Arrange
            var companyId = Guid.NewGuid();
            var company = CreateTestCompany(id: companyId, name: "Edit Company");
            _mockCompanyService.Setup(s => s.GetByIdAsync(companyId, _tenantId)).ReturnsAsync(company);

            // Act
            var result = await _controller.Edit(companyId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Company>(viewResult.Model);
            Assert.Equal(companyId, model.Id);
            Assert.Equal("Edit Company", model.Name);
        }

        [Fact]
        public async Task Edit_Get_WithNullId_ReturnsNotFound()
        {
            // Act
            var result = await _controller.Edit(null);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Edit_Get_WithNonExistentId_ReturnsNotFound()
        {
            // Arrange
            var companyId = Guid.NewGuid();
            _mockCompanyService.Setup(s => s.GetByIdAsync(companyId, _tenantId)).ReturnsAsync((Company?)null);

            // Act
            var result = await _controller.Edit(companyId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Edit_Post_ValidModel_UpdatesAndRedirects()
        {
            // Arrange
            var companyId = Guid.NewGuid();
            var company = CreateTestCompany(id: companyId, name: "Updated Company");
            _mockCompanyService.Setup(s => s.UpdateAsync(It.IsAny<Company>(), It.IsAny<byte[]?>(), _tenantId))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Edit(companyId, company);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            _mockCompanyService.Verify(s => s.UpdateAsync(
                It.Is<Company>(c => c.Name == "Updated Company"),
                It.IsAny<byte[]?>(),
                _tenantId), Times.Once);
        }

        [Fact]
        public async Task Edit_Post_IdMismatch_ReturnsNotFound()
        {
            // Arrange
            var routeId = Guid.NewGuid();
            var company = CreateTestCompany(id: Guid.NewGuid(), name: "Mismatched Company");

            // Act
            var result = await _controller.Edit(routeId, company);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Edit_Post_InvalidModel_ReturnsView()
        {
            // Arrange
            var companyId = Guid.NewGuid();
            var company = new Company { Id = companyId };
            _controller.ModelState.AddModelError("Name", "Required");

            // Act
            var result = await _controller.Edit(companyId, company);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Same(company, viewResult.Model);
            Assert.False(_controller.ModelState.IsValid);
        }

        [Fact]
        public async Task Edit_Post_ConcurrencyException_WhenCompanyExists_Rethrows()
        {
            // Arrange
            var companyId = Guid.NewGuid();
            var company = CreateTestCompany(id: companyId, name: "Concurrent Company");
            _mockCompanyService.Setup(s => s.UpdateAsync(It.IsAny<Company>(), It.IsAny<byte[]?>(), _tenantId))
                .ThrowsAsync(new DbUpdateConcurrencyException());
            _mockCompanyService.Setup(s => s.ExistsAsync(companyId, _tenantId)).ReturnsAsync(true);

            // Act & Assert
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
                () => _controller.Edit(companyId, company));
        }

        [Fact]
        public async Task Edit_Post_ConcurrencyException_WhenCompanyDeleted_ReturnsNotFound()
        {
            // Arrange
            var companyId = Guid.NewGuid();
            var company = CreateTestCompany(id: companyId, name: "Deleted Company");
            _mockCompanyService.Setup(s => s.UpdateAsync(It.IsAny<Company>(), It.IsAny<byte[]?>(), _tenantId))
                .ThrowsAsync(new DbUpdateConcurrencyException());
            _mockCompanyService.Setup(s => s.ExistsAsync(companyId, _tenantId)).ReturnsAsync(false);

            // Act
            var result = await _controller.Edit(companyId, company);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Edit_Post_CompanyNotFound_ShowsError()
        {
            // Arrange
            var companyId = Guid.NewGuid();
            var company = CreateTestCompany(id: companyId, name: "Not Found Company");
            _mockCompanyService.Setup(s => s.UpdateAsync(It.IsAny<Company>(), It.IsAny<byte[]?>(), _tenantId))
                .ThrowsAsync(new InvalidOperationException("Company not found"));

            // Act
            var result = await _controller.Edit(companyId, company);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(_controller.ModelState.IsValid);
            Assert.Contains(_controller.ModelState.Values.SelectMany(v => v.Errors),
                e => e.ErrorMessage.Contains("Company not found"));
        }

        #endregion

        #region Delete Tests

        [Fact]
        public async Task Delete_Get_WithValidId_ReturnsConfirmationView()
        {
            // Arrange
            var companyId = Guid.NewGuid();
            var company = CreateTestCompany(id: companyId, name: "Delete Company");
            _mockCompanyService.Setup(s => s.GetByIdAsync(companyId, _tenantId)).ReturnsAsync(company);

            // Act
            var result = await _controller.Delete(companyId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Company>(viewResult.Model);
            Assert.Equal(companyId, model.Id);
            Assert.Equal("Delete Company", model.Name);
        }

        [Fact]
        public async Task Delete_Get_WithNullId_ReturnsNotFound()
        {
            // Act
            var result = await _controller.Delete(null);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_Get_WithNonExistentId_ReturnsNotFound()
        {
            // Arrange
            var companyId = Guid.NewGuid();
            _mockCompanyService.Setup(s => s.GetByIdAsync(companyId, _tenantId)).ReturnsAsync((Company?)null);

            // Act
            var result = await _controller.Delete(companyId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_Post_DeletesAndRedirects()
        {
            // Arrange
            var companyId = Guid.NewGuid();
            _mockCompanyService.Setup(s => s.DeleteAsync(companyId, _tenantId)).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.DeleteConfirmed(companyId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            _mockCompanyService.Verify(s => s.DeleteAsync(companyId, _tenantId), Times.Once);
        }

        [Fact]
        public async Task Delete_Post_NonExistentId_StillRedirects()
        {
            // Arrange - DeleteAsync doesn't throw when not found
            var companyId = Guid.NewGuid();
            _mockCompanyService.Setup(s => s.DeleteAsync(companyId, _tenantId)).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.DeleteConfirmed(companyId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
        }

        #endregion
    }
}
