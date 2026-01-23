using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using FutureReady.Controllers;
using FutureReady.Data;
using FutureReady.Models;
using FutureReady.Services;

namespace FutureReady.Tests
{
    public class UsersControllerTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Microsoft.Data.Sqlite.SqliteConnection _connection;
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly Mock<IUserProvider> _mockUserProvider;
        private readonly UsersController _controller;
        private readonly Guid _tenantId = Guid.NewGuid();

        public UsersControllerTests()
        {
            var userProvider = new FakeUserProvider();
            var tenantProvider = new FakeTenantProvider(_tenantId);
            (_context, _connection) = TestDbContextFactory.CreateSqliteInMemoryContext(userProvider, tenantProvider);

            // Seed a school for SelectList tests
            var school = new FutureReady.Models.School.School
            {
                Id = _tenantId,
                Name = "Test School",
                TenantKey = "test-school",
                Timezone = "Australia/Sydney"
            };
            _context.Schools.Add(school);
            _context.SaveChanges();

            _mockUserManager = CreateMockUserManager();
            _mockUserProvider = new Mock<IUserProvider>();

            _controller = new UsersController(_context, _mockUserManager.Object, _mockUserProvider.Object);
            _controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
        }

        public void Dispose()
        {
            _context?.Dispose();
            _connection?.Dispose();
        }

        private static Mock<UserManager<ApplicationUser>> CreateMockUserManager()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            return new Mock<UserManager<ApplicationUser>>(
                store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        }

        private ApplicationUser CreateTestUser(Guid? id = null, string userName = "testuser", bool isDeleted = false)
        {
            return new ApplicationUser
            {
                Id = id ?? Guid.NewGuid(),
                UserName = userName,
                Email = $"{userName}@example.com",
                DisplayName = "Test User",
                TenantId = _tenantId,
                IsActive = true,
                IsDeleted = isDeleted
            };
        }

        #region Index Tests

        [Fact]
        public async Task Index_ReturnsViewWithAllNonDeletedUsers()
        {
            // Arrange
            var users = new List<ApplicationUser>
            {
                CreateTestUser(userName: "user1"),
                CreateTestUser(userName: "user2")
            };

            _mockUserManager.Setup(m => m.Users).Returns(new TestAsyncEnumerableQueryable<ApplicationUser>(users));

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<ApplicationUser>>(viewResult.Model);
            Assert.Equal(2, model.Count);
        }

        [Fact]
        public async Task Index_ExcludesDeletedUsers()
        {
            // Arrange
            var users = new List<ApplicationUser>
            {
                CreateTestUser(userName: "user1", isDeleted: false),
                CreateTestUser(userName: "user2", isDeleted: true)
            };

            _mockUserManager.Setup(m => m.Users).Returns(new TestAsyncEnumerableQueryable<ApplicationUser>(users));

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<ApplicationUser>>(viewResult.Model);
            Assert.Single(model);
            Assert.Equal("user1", model[0].UserName);
        }

        #endregion

        #region Details Tests

        [Fact]
        public async Task Details_WithValidId_ReturnsViewWithUser()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = CreateTestUser(id: userId);
            _mockUserManager.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync(user);

            // Act
            var result = await _controller.Details(userId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ApplicationUser>(viewResult.Model);
            Assert.Equal(userId, model.Id);
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
            var userId = Guid.NewGuid();
            _mockUserManager.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync((ApplicationUser?)null);

            // Act
            var result = await _controller.Details(userId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Details_WithDeletedUser_ReturnsNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = CreateTestUser(id: userId, isDeleted: true);
            _mockUserManager.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync(user);

            // Act
            var result = await _controller.Details(userId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        #endregion

        #region Create Tests

        [Fact]
        public void Create_Get_ReturnsViewWithSchoolSelectList()
        {
            // Act
            var result = _controller.Create();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(viewResult.ViewData["SchoolId"]);
        }

        [Fact]
        public async Task Create_Post_ValidModel_CreatesUserAndRedirects()
        {
            // Arrange
            var model = new CreateUserViewModel
            {
                UserName = "newuser",
                Email = "newuser@example.com",
                DisplayName = "New User",
                Password = "Password123!",
                IsActive = true,
                TenantId = _tenantId
            };

            _mockUserManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _controller.Create(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            _mockUserManager.Verify(m => m.CreateAsync(
                It.Is<ApplicationUser>(u => u.UserName == "newuser" && u.Email == "newuser@example.com"),
                "Password123!"), Times.Once);
        }

        [Fact]
        public async Task Create_Post_InvalidModel_ReturnsViewWithErrors()
        {
            // Arrange
            var model = new CreateUserViewModel();
            _controller.ModelState.AddModelError("UserName", "Required");

            // Act
            var result = await _controller.Create(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(viewResult.ViewData["SchoolId"]);
            Assert.False(_controller.ModelState.IsValid);
        }

        [Fact]
        public async Task Create_Post_IdentityFailure_AddsErrorsToModelState()
        {
            // Arrange
            var model = new CreateUserViewModel
            {
                UserName = "newuser",
                Email = "newuser@example.com",
                Password = "weak"
            };

            var identityErrors = new List<IdentityError>
            {
                new IdentityError { Code = "PasswordTooShort", Description = "Password must be at least 6 characters." }
            };
            _mockUserManager.Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Failed(identityErrors.ToArray()));

            // Act
            var result = await _controller.Create(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(_controller.ModelState.IsValid);
            Assert.Contains(_controller.ModelState.Values.SelectMany(v => v.Errors),
                e => e.ErrorMessage.Contains("Password must be at least 6 characters"));
        }

        #endregion

        #region Edit Tests

        [Fact]
        public async Task Edit_Get_WithValidId_ReturnsViewWithModel()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = CreateTestUser(id: userId, userName: "existinguser");
            _mockUserManager.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync(user);

            // Act
            var result = await _controller.Edit(userId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<EditUserViewModel>(viewResult.Model);
            Assert.Equal(userId, model.Id);
            Assert.Equal("existinguser", model.UserName);
        }

        [Fact]
        public async Task Edit_Get_WithNullId_ReturnsNotFound()
        {
            // Act
            var result = await _controller.Edit((Guid?)null);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Edit_Get_WithDeletedUser_ReturnsNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = CreateTestUser(id: userId, isDeleted: true);
            _mockUserManager.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync(user);

            // Act
            var result = await _controller.Edit(userId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Edit_Post_ValidModel_UpdatesUserAndRedirects()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var existingUser = CreateTestUser(id: userId);
            var model = new EditUserViewModel
            {
                Id = userId,
                UserName = "updateduser",
                Email = "updated@example.com",
                DisplayName = "Updated User",
                IsActive = true
            };

            _mockUserManager.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync(existingUser);
            _mockUserManager.Setup(m => m.UpdateAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _controller.Edit(userId, model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            _mockUserManager.Verify(m => m.UpdateAsync(
                It.Is<ApplicationUser>(u => u.UserName == "updateduser")), Times.Once);
        }

        [Fact]
        public async Task Edit_Post_IdMismatch_ReturnsNotFound()
        {
            // Arrange
            var model = new EditUserViewModel { Id = Guid.NewGuid() };

            // Act
            var result = await _controller.Edit(Guid.NewGuid(), model);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Edit_Post_WithNewPassword_ResetsPassword()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var existingUser = CreateTestUser(id: userId);
            var model = new EditUserViewModel
            {
                Id = userId,
                UserName = "testuser",
                Email = "test@example.com",
                NewPassword = "NewPassword123!"
            };

            _mockUserManager.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync(existingUser);
            _mockUserManager.Setup(m => m.UpdateAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(m => m.GeneratePasswordResetTokenAsync(existingUser))
                .ReturnsAsync("reset-token");
            _mockUserManager.Setup(m => m.ResetPasswordAsync(existingUser, "reset-token", "NewPassword123!"))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _controller.Edit(userId, model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            _mockUserManager.Verify(m => m.ResetPasswordAsync(existingUser, "reset-token", "NewPassword123!"), Times.Once);
        }

        [Fact]
        public async Task Edit_Post_PasswordResetFailure_AddsErrorsToModelState()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var existingUser = CreateTestUser(id: userId);
            var model = new EditUserViewModel
            {
                Id = userId,
                UserName = "testuser",
                Email = "test@example.com",
                NewPassword = "weak"
            };

            var identityErrors = new List<IdentityError>
            {
                new IdentityError { Code = "PasswordTooShort", Description = "Password must be at least 6 characters." }
            };

            _mockUserManager.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync(existingUser);
            _mockUserManager.Setup(m => m.UpdateAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(m => m.GeneratePasswordResetTokenAsync(existingUser))
                .ReturnsAsync("reset-token");
            _mockUserManager.Setup(m => m.ResetPasswordAsync(existingUser, "reset-token", "weak"))
                .ReturnsAsync(IdentityResult.Failed(identityErrors.ToArray()));

            // Act
            var result = await _controller.Edit(userId, model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(_controller.ModelState.IsValid);
            Assert.Contains(_controller.ModelState.Values.SelectMany(v => v.Errors),
                e => e.ErrorMessage.Contains("Password must be at least 6 characters"));
        }

        [Fact]
        public async Task Edit_Post_InvalidModelState_ReturnsView()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var model = new EditUserViewModel { Id = userId };
            _controller.ModelState.AddModelError("UserName", "Required");

            // Act
            var result = await _controller.Edit(userId, model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Same(model, viewResult.Model);
        }

        [Fact]
        public async Task Edit_Post_UserNotFound_ReturnsNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var model = new EditUserViewModel
            {
                Id = userId,
                UserName = "testuser",
                Email = "test@example.com"
            };

            _mockUserManager.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync((ApplicationUser?)null);

            // Act
            var result = await _controller.Edit(userId, model);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Edit_Post_UpdateFailure_AddsErrorsToModelState()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var existingUser = CreateTestUser(id: userId);
            var model = new EditUserViewModel
            {
                Id = userId,
                UserName = "testuser",
                Email = "test@example.com"
            };

            var identityErrors = new List<IdentityError>
            {
                new IdentityError { Code = "ConcurrencyFailure", Description = "User was modified by another request." }
            };

            _mockUserManager.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync(existingUser);
            _mockUserManager.Setup(m => m.UpdateAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(IdentityResult.Failed(identityErrors.ToArray()));

            // Act
            var result = await _controller.Edit(userId, model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(_controller.ModelState.IsValid);
        }

        #endregion

        #region Delete Tests

        [Fact]
        public async Task Delete_Get_WithValidId_ReturnsConfirmationView()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = CreateTestUser(id: userId);
            _mockUserManager.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync(user);

            // Act
            var result = await _controller.Delete(userId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ApplicationUser>(viewResult.Model);
            Assert.Equal(userId, model.Id);
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
        public async Task Delete_Get_WithDeletedUser_ReturnsNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = CreateTestUser(id: userId, isDeleted: true);
            _mockUserManager.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync(user);

            // Act
            var result = await _controller.Delete(userId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_Post_SoftDeletesUserAndRedirects()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = CreateTestUser(id: userId);
            _mockUserManager.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
            _mockUserManager.Setup(m => m.UpdateAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _controller.DeleteConfirmed(userId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            _mockUserManager.Verify(m => m.UpdateAsync(
                It.Is<ApplicationUser>(u => u.IsDeleted == true)), Times.Once);
        }

        [Fact]
        public async Task Delete_Post_NonExistentUser_RedirectsWithoutError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _mockUserManager.Setup(m => m.FindByIdAsync(userId.ToString())).ReturnsAsync((ApplicationUser?)null);

            // Act
            var result = await _controller.DeleteConfirmed(userId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            _mockUserManager.Verify(m => m.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
        }

        #endregion

        #region Profile Tests

        [Fact]
        public async Task Profile_Get_AuthenticatedUser_ReturnsViewWithUser()
        {
            // Arrange
            var user = CreateTestUser(userName: "currentuser");
            _mockUserProvider.Setup(m => m.GetCurrentUsername()).Returns("currentuser");
            _mockUserManager.Setup(m => m.FindByNameAsync("currentuser")).ReturnsAsync(user);

            // Act
            var result = await _controller.Profile();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ApplicationUser>(viewResult.Model);
            Assert.Equal("currentuser", model.UserName);
        }

        [Fact]
        public async Task Profile_Get_NoUsername_RedirectsToLogin()
        {
            // Arrange
            _mockUserProvider.Setup(m => m.GetCurrentUsername()).Returns((string?)null);

            // Act
            var result = await _controller.Profile();

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirectResult.ActionName);
            Assert.Equal("Authentication", redirectResult.ControllerName);
        }

        [Fact]
        public async Task Profile_Get_UserNotFound_ReturnsNotFound()
        {
            // Arrange
            _mockUserProvider.Setup(m => m.GetCurrentUsername()).Returns("unknownuser");
            _mockUserManager.Setup(m => m.FindByNameAsync("unknownuser")).ReturnsAsync((ApplicationUser?)null);
            _mockUserManager.Setup(m => m.FindByEmailAsync("unknownuser")).ReturnsAsync((ApplicationUser?)null);

            // Act
            var result = await _controller.Profile();

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Profile_Get_DeletedUser_ReturnsNotFound()
        {
            // Arrange
            var user = CreateTestUser(userName: "deleteduser", isDeleted: true);
            _mockUserProvider.Setup(m => m.GetCurrentUsername()).Returns("deleteduser");
            _mockUserManager.Setup(m => m.FindByNameAsync("deleteduser")).ReturnsAsync(user);

            // Act
            var result = await _controller.Profile();

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Profile_Post_ValidModel_UpdatesAndRedirects()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = CreateTestUser(id: userId, userName: "currentuser");
            var model = new ProfileViewModel
            {
                Id = userId,
                DisplayName = "Updated Name",
                Email = "updated@example.com"
            };

            _mockUserProvider.Setup(m => m.GetCurrentUsername()).Returns("currentuser");
            _mockUserManager.Setup(m => m.FindByNameAsync("currentuser")).ReturnsAsync(user);
            _mockUserManager.Setup(m => m.UpdateAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _controller.Profile(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Profile", redirectResult.ActionName);
            _mockUserManager.Verify(m => m.UpdateAsync(
                It.Is<ApplicationUser>(u => u.DisplayName == "Updated Name" && u.Email == "updated@example.com")), Times.Once);
        }

        [Fact]
        public async Task Profile_Post_IdMismatch_ReturnsNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = CreateTestUser(id: userId, userName: "currentuser");
            var model = new ProfileViewModel
            {
                Id = Guid.NewGuid(), // Different ID
                DisplayName = "Updated Name"
            };

            _mockUserProvider.Setup(m => m.GetCurrentUsername()).Returns("currentuser");
            _mockUserManager.Setup(m => m.FindByNameAsync("currentuser")).ReturnsAsync(user);

            // Act
            var result = await _controller.Profile(model);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Profile_Post_NoUsername_RedirectsToLogin()
        {
            // Arrange
            _mockUserProvider.Setup(m => m.GetCurrentUsername()).Returns((string?)null);
            var model = new ProfileViewModel { Id = Guid.NewGuid() };

            // Act
            var result = await _controller.Profile(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirectResult.ActionName);
            Assert.Equal("Authentication", redirectResult.ControllerName);
        }

        [Fact]
        public async Task Profile_Post_InvalidModelState_ReturnsViewWithUser()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = CreateTestUser(id: userId, userName: "currentuser");
            var model = new ProfileViewModel { Id = userId };
            _controller.ModelState.AddModelError("Email", "Invalid email");

            _mockUserProvider.Setup(m => m.GetCurrentUsername()).Returns("currentuser");
            _mockUserManager.Setup(m => m.FindByNameAsync("currentuser")).ReturnsAsync(user);

            // Act
            var result = await _controller.Profile(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.IsType<ApplicationUser>(viewResult.Model);
        }

        [Fact]
        public async Task Profile_Post_UpdateFailure_AddsErrorsToModelState()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = CreateTestUser(id: userId, userName: "currentuser");
            var model = new ProfileViewModel
            {
                Id = userId,
                DisplayName = "Updated Name",
                Email = "invalid"
            };

            var identityErrors = new List<IdentityError>
            {
                new IdentityError { Code = "InvalidEmail", Description = "Invalid email format." }
            };

            _mockUserProvider.Setup(m => m.GetCurrentUsername()).Returns("currentuser");
            _mockUserManager.Setup(m => m.FindByNameAsync("currentuser")).ReturnsAsync(user);
            _mockUserManager.Setup(m => m.UpdateAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(IdentityResult.Failed(identityErrors.ToArray()));

            // Act
            var result = await _controller.Profile(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(_controller.ModelState.IsValid);
            Assert.Contains(_controller.ModelState.Values.SelectMany(v => v.Errors),
                e => e.ErrorMessage.Contains("Invalid email format"));
        }

        [Fact]
        public async Task Profile_Get_FindsByEmail_WhenUsernameNotFound()
        {
            // Arrange
            var user = CreateTestUser(userName: "testuser");
            _mockUserProvider.Setup(m => m.GetCurrentUsername()).Returns("test@example.com");
            _mockUserManager.Setup(m => m.FindByNameAsync("test@example.com")).ReturnsAsync((ApplicationUser?)null);
            _mockUserManager.Setup(m => m.FindByEmailAsync("test@example.com")).ReturnsAsync(user);

            // Act
            var result = await _controller.Profile();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ApplicationUser>(viewResult.Model);
            Assert.Equal("testuser", model.UserName);
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

    /// <summary>
    /// IQueryable wrapper that implements IAsyncEnumerable for EF Core ToListAsync() compatibility in tests.
    /// </summary>
    internal class TestAsyncEnumerableQueryable<T> : IQueryable<T>, IAsyncEnumerable<T>, IOrderedQueryable<T>
    {
        private readonly IQueryable<T> _inner;

        public TestAsyncEnumerableQueryable(IEnumerable<T> data)
        {
            _inner = data.AsQueryable();
        }

        public Type ElementType => _inner.ElementType;
        public Expression Expression => _inner.Expression;
        public IQueryProvider Provider => new TestAsyncQueryProvider<T>(_inner.Provider);

        public IEnumerator<T> GetEnumerator() => _inner.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _inner.GetEnumerator();

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new TestAsyncEnumerator<T>(_inner.GetEnumerator());
        }
    }

    internal class TestAsyncQueryProvider<T> : IQueryProvider
    {
        private readonly IQueryProvider _inner;

        public TestAsyncQueryProvider(IQueryProvider inner)
        {
            _inner = inner;
        }

        public IQueryable CreateQuery(Expression expression)
        {
            return new TestAsyncEnumerableQueryable<T>(_inner.CreateQuery<T>(expression));
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new TestAsyncEnumerableQueryable<TElement>(
                _inner.CreateQuery<TElement>(expression));
        }

        public object? Execute(Expression expression) => _inner.Execute(expression);
        public TResult Execute<TResult>(Expression expression) => _inner.Execute<TResult>(expression);
    }

    internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;

        public TestAsyncEnumerator(IEnumerator<T> inner)
        {
            _inner = inner;
        }

        public T Current => _inner.Current;

        public ValueTask DisposeAsync()
        {
            _inner.Dispose();
            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> MoveNextAsync()
        {
            return ValueTask.FromResult(_inner.MoveNext());
        }
    }
}
