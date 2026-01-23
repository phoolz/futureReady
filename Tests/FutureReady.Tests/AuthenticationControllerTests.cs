using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Xunit;
using FutureReady.Controllers;
using FutureReady.Models;

namespace FutureReady.Tests
{
    public class AuthenticationControllerTests
    {
        private readonly Mock<SignInManager<ApplicationUser>> _mockSignInManager;
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly AuthenticationController _controller;
        private readonly Guid _tenantId = Guid.NewGuid();

        public AuthenticationControllerTests()
        {
            _mockUserManager = CreateMockUserManager();
            _mockSignInManager = CreateMockSignInManager(_mockUserManager);

            _controller = new AuthenticationController(_mockSignInManager.Object, _mockUserManager.Object);
            _controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

            // Setup URL helper for IsLocalUrl checks
            var mockUrlHelper = new Mock<IUrlHelper>();
            mockUrlHelper.Setup(x => x.IsLocalUrl(It.IsAny<string>()))
                .Returns<string>(url => !string.IsNullOrEmpty(url) && url.StartsWith("/"));
            _controller.Url = mockUrlHelper.Object;
        }

        private static Mock<UserManager<ApplicationUser>> CreateMockUserManager()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            return new Mock<UserManager<ApplicationUser>>(
                store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        }

        private static Mock<SignInManager<ApplicationUser>> CreateMockSignInManager(Mock<UserManager<ApplicationUser>> userManager)
        {
            var contextAccessor = new Mock<IHttpContextAccessor>();
            var claimsFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();

            return new Mock<SignInManager<ApplicationUser>>(
                userManager.Object,
                contextAccessor.Object,
                claimsFactory.Object,
                null!, null!, null!, null!);
        }

        private ApplicationUser CreateTestUser(string userName = "testuser", bool isActive = true)
        {
            return new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = userName,
                Email = $"{userName}@example.com",
                DisplayName = "Test User",
                TenantId = _tenantId,
                IsActive = isActive
            };
        }

        #region Login GET Tests

        [Fact]
        public void Login_Get_ReturnsViewWithReturnUrl()
        {
            // Arrange
            var returnUrl = "/secure-page";

            // Act
            var result = _controller.Login(returnUrl);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(returnUrl, viewResult.ViewData["ReturnUrl"]);
            var model = Assert.IsType<LoginViewModel>(viewResult.Model);
            Assert.Equal(returnUrl, model.ReturnUrl);
        }

        [Fact]
        public void Login_Get_WithNullReturnUrl_ReturnsViewWithNullReturnUrl()
        {
            // Act
            var result = _controller.Login(returnUrl: null);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.ViewData["ReturnUrl"]);
            var model = Assert.IsType<LoginViewModel>(viewResult.Model);
            Assert.Null(model.ReturnUrl);
        }

        #endregion

        #region Login POST Tests

        [Fact]
        public async Task Login_Post_ValidCredentials_RedirectsToReturnUrl()
        {
            // Arrange
            var user = CreateTestUser("testuser");
            var model = new LoginViewModel
            {
                Username = "testuser",
                Password = "Password123!",
                ReturnUrl = "/secure-page"
            };

            _mockUserManager.Setup(m => m.FindByNameAsync("testuser")).ReturnsAsync(user);
            _mockSignInManager.Setup(m => m.PasswordSignInAsync(user, "Password123!", false, false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

            // Act
            var result = await _controller.Login(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectResult>(result);
            Assert.Equal("/secure-page", redirectResult.Url);
        }

        [Fact]
        public async Task Login_Post_ValidCredentials_NoReturnUrl_RedirectsToHome()
        {
            // Arrange
            var user = CreateTestUser("testuser");
            var model = new LoginViewModel
            {
                Username = "testuser",
                Password = "Password123!"
            };

            _mockUserManager.Setup(m => m.FindByNameAsync("testuser")).ReturnsAsync(user);
            _mockSignInManager.Setup(m => m.PasswordSignInAsync(user, "Password123!", false, false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

            // Act
            var result = await _controller.Login(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Home", redirectResult.ControllerName);
        }

        [Fact]
        public async Task Login_Post_ValidCredentials_NonLocalReturnUrl_RedirectsToHome()
        {
            // Arrange
            var user = CreateTestUser("testuser");
            var model = new LoginViewModel
            {
                Username = "testuser",
                Password = "Password123!",
                ReturnUrl = "https://external-site.com/malicious"
            };

            _mockUserManager.Setup(m => m.FindByNameAsync("testuser")).ReturnsAsync(user);
            _mockSignInManager.Setup(m => m.PasswordSignInAsync(user, "Password123!", false, false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

            // Act
            var result = await _controller.Login(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Home", redirectResult.ControllerName);
        }

        [Fact]
        public async Task Login_Post_RememberMe_CallsSignInWithRememberMe()
        {
            // Arrange
            var user = CreateTestUser("testuser");
            var model = new LoginViewModel
            {
                Username = "testuser",
                Password = "Password123!",
                RememberMe = true
            };

            _mockUserManager.Setup(m => m.FindByNameAsync("testuser")).ReturnsAsync(user);
            _mockSignInManager.Setup(m => m.PasswordSignInAsync(user, "Password123!", true, false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

            // Act
            var result = await _controller.Login(model);

            // Assert
            _mockSignInManager.Verify(m => m.PasswordSignInAsync(user, "Password123!", true, false), Times.Once);
        }

        [Fact]
        public async Task Login_Post_UserNotFoundByUsername_TriesEmail()
        {
            // Arrange
            var user = CreateTestUser("testuser");
            var model = new LoginViewModel
            {
                Username = "testuser@example.com",
                Password = "Password123!"
            };

            _mockUserManager.Setup(m => m.FindByNameAsync("testuser@example.com")).ReturnsAsync((ApplicationUser?)null);
            _mockUserManager.Setup(m => m.FindByEmailAsync("testuser@example.com")).ReturnsAsync(user);
            _mockSignInManager.Setup(m => m.PasswordSignInAsync(user, "Password123!", false, false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

            // Act
            var result = await _controller.Login(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            _mockUserManager.Verify(m => m.FindByEmailAsync("testuser@example.com"), Times.Once);
        }

        [Fact]
        public async Task Login_Post_UserNotFound_ReturnsViewWithError()
        {
            // Arrange
            var model = new LoginViewModel
            {
                Username = "nonexistent",
                Password = "Password123!"
            };

            _mockUserManager.Setup(m => m.FindByNameAsync("nonexistent")).ReturnsAsync((ApplicationUser?)null);
            _mockUserManager.Setup(m => m.FindByEmailAsync("nonexistent")).ReturnsAsync((ApplicationUser?)null);

            // Act
            var result = await _controller.Login(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(_controller.ModelState.IsValid);
            var errors = _controller.ModelState[string.Empty]?.Errors;
            Assert.NotNull(errors);
            Assert.Contains(errors, e => e.ErrorMessage == "Invalid username or password");
        }

        [Fact]
        public async Task Login_Post_InactiveUser_ReturnsViewWithError()
        {
            // Arrange
            var user = CreateTestUser("testuser", isActive: false);
            var model = new LoginViewModel
            {
                Username = "testuser",
                Password = "Password123!"
            };

            _mockUserManager.Setup(m => m.FindByNameAsync("testuser")).ReturnsAsync(user);

            // Act
            var result = await _controller.Login(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(_controller.ModelState.IsValid);
            var errors = _controller.ModelState[string.Empty]?.Errors;
            Assert.NotNull(errors);
            Assert.Contains(errors, e => e.ErrorMessage == "Invalid username or password");
            _mockSignInManager.Verify(m => m.PasswordSignInAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task Login_Post_WrongPassword_ReturnsViewWithError()
        {
            // Arrange
            var user = CreateTestUser("testuser");
            var model = new LoginViewModel
            {
                Username = "testuser",
                Password = "WrongPassword!"
            };

            _mockUserManager.Setup(m => m.FindByNameAsync("testuser")).ReturnsAsync(user);
            _mockSignInManager.Setup(m => m.PasswordSignInAsync(user, "WrongPassword!", false, false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

            // Act
            var result = await _controller.Login(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(_controller.ModelState.IsValid);
            var errors = _controller.ModelState[string.Empty]?.Errors;
            Assert.NotNull(errors);
            Assert.Contains(errors, e => e.ErrorMessage == "Invalid username or password");
        }

        [Fact]
        public async Task Login_Post_InvalidModelState_ReturnsView()
        {
            // Arrange
            var model = new LoginViewModel
            {
                Username = "testuser"
                // Missing password
            };
            _controller.ModelState.AddModelError("Password", "Password is required");

            // Act
            var result = await _controller.Login(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Same(model, viewResult.Model);
            Assert.False(_controller.ModelState.IsValid);
        }

        [Fact]
        public async Task Login_Post_NullUsername_ReturnsViewWithError()
        {
            // Arrange
            var model = new LoginViewModel
            {
                Username = null,
                Password = "Password123!"
            };

            _mockUserManager.Setup(m => m.FindByNameAsync(string.Empty)).ReturnsAsync((ApplicationUser?)null);
            _mockUserManager.Setup(m => m.FindByEmailAsync(string.Empty)).ReturnsAsync((ApplicationUser?)null);

            // Act
            var result = await _controller.Login(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(_controller.ModelState.IsValid);
        }

        [Fact]
        public async Task Login_Post_NullPassword_ReturnsViewWithError()
        {
            // Arrange
            var user = CreateTestUser("testuser");
            var model = new LoginViewModel
            {
                Username = "testuser",
                Password = null
            };

            _mockUserManager.Setup(m => m.FindByNameAsync("testuser")).ReturnsAsync(user);
            _mockSignInManager.Setup(m => m.PasswordSignInAsync(user, string.Empty, false, false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

            // Act
            var result = await _controller.Login(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(_controller.ModelState.IsValid);
        }

        #endregion

        #region Logout POST Tests

        [Fact]
        public async Task Logout_Post_SignsOutUserAndRedirectsToHome()
        {
            // Arrange
            _mockSignInManager.Setup(m => m.SignOutAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Logout();

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Home", redirectResult.ControllerName);
            _mockSignInManager.Verify(m => m.SignOutAsync(), Times.Once);
        }

        #endregion
    }
}
