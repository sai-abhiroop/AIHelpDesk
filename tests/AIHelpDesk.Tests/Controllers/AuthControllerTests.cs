using AIHelpDesk.API.Controllers;
using AIHelpDesk.Application.Dtos.Auth;
using AIHelpDesk.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AIHelpDesk.Tests.Controllers
{
    public class AuthControllerTests
    {
        private readonly Mock<IAuthService> _authServiceMock = new();

        private AuthController CreateController()
        {
            return new AuthController(_authServiceMock.Object);
        }

        [Fact]
        public async Task Register_ShouldReturnOk()
        {
            var request = new RegisterationRequestDto
            {
                Email = "test@example.com",
                Password = "Password123!",
                FirstName = "Test",
                LastName = "User"
            };

            var user = new UserDto
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Role = "Customer"
            };

            _authServiceMock.Setup(x => x.RegisterAsync(request))
                            .ReturnsAsync(user);

            var controller = CreateController();

            var result = await controller.Register(request);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(user, okResult.Value);
        }

        [Fact]
        public async Task Login_ShouldReturnOk_WhenCredentialsAreValid()
        {
            var request = new LoginRequestDto
            {
                Email = "test@example.com",
                Password = "Password123!"
            };

            var token = new TokenDto
            {
                AccessToken = "access-token",
                RefreshToken = "refresh-token"
            };

            _authServiceMock.Setup(x => x.LoginAsync(request))
                            .ReturnsAsync(token);

            var controller = CreateController();

            var result = await controller.Login(request);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(token, okResult.Value);
        }

        [Fact]
        public async Task Login_ShouldReturnUnauthorized_WhenCredentialsAreInvalid()
        {
            var request = new LoginRequestDto
            {
                Email = "test@example.com",
                Password = "WrongPassword"
            };

            _authServiceMock.Setup(x => x.LoginAsync(request))
                            .ReturnsAsync((TokenDto?)null);

            var controller = CreateController();

            var result = await controller.Login(request);

            var unauthorizedResult =
                Assert.IsType<UnauthorizedObjectResult>(result.Result);

            Assert.Equal("Invalid email or password.", unauthorizedResult.Value);
        }

        [Fact]
        public async Task Refresh_ShouldReturnOk_WhenTokenIsValid()
        {
            var request = new RefreshTokenRequestDto
            {
                RefreshToken = "valid-refresh-token"
            };

            var token = new TokenDto
            {
                AccessToken = "new-access-token",
                RefreshToken = "new-refresh-token"
            };

            _authServiceMock.Setup(x => x.RefreshAccessTokenAsync(request)).ReturnsAsync(token);

            var controller = CreateController();

            var result = await controller.Refresh(request);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(token, okResult.Value);
        }

        [Fact]
        public async Task Refresh_ShouldReturnUnauthorized_WhenTokenIsInvalid()
        {
            var request = new RefreshTokenRequestDto
            {
                RefreshToken = "invalid-refresh-token"
            };

            _authServiceMock.Setup(x => x.RefreshAccessTokenAsync(request)).ReturnsAsync((TokenDto?)null);

            var controller = CreateController();

            var result = await controller.Refresh(request);

            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);

            Assert.Equal("Invalid or expired refresh token.", unauthorizedResult.Value);
        }

        [Fact]
        public async Task Register_ShouldCallAuthService()
        {
            var request = new RegisterationRequestDto
            {
                Email = "test@example.com",
                Password = "Password123!",
                FirstName = "Test",
                LastName = "User"
            };

            var user = new UserDto
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Role = "Customer"
            };

            _authServiceMock.Setup(x => x.RegisterAsync(request)).ReturnsAsync(user);

            var controller = CreateController();

            await controller.Register(request);

            _authServiceMock.Verify(x => x.RegisterAsync(request), Times.Once);
        }
    }
}
