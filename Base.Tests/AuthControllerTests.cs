using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Base.API.Controllers;
using Base.API.DTOs;
using Base.Domain.Entities;
using Base.Domain.Interfaces;

namespace Base.Tests;

public class AuthControllerTests
{
    [Fact]
    public async Task Signup_ReturnsOk_WhenRegistrationSucceeds()
    {
        var authService = new FakeAuthService
        {
            RegisterResult = new User { Email = "user@test.local", Name = "User", CompanyId = 1 }
        };
        var controller = new AuthController(authService)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.Signup(new SignupRequest("user@test.local", "Password123!", "User"));

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenCredentialsInvalid()
    {
        var authService = new FakeAuthService
        {
            LoginException = new UnauthorizedAccessException("Invalid credentials.")
        };
        var controller = new AuthController(authService)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.Login(new LoginRequest("user@test.local", "wrong"));

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorized.StatusCode);
    }

    [Fact]
    public async Task Me_ReturnsUnauthorized_WhenNameIdentifierClaimMissing()
    {
        var authService = new FakeAuthService();
        var controller = new AuthController(authService)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.Me();

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal(StatusCodes.Status401Unauthorized, unauthorized.StatusCode);
    }

    private sealed class FakeAuthService : IAuthService
    {
        public User? RegisterResult { get; set; }
        public Exception? LoginException { get; set; }

        public Task<User> RegisterAsync(string email, string password, string? name)
            => Task.FromResult(RegisterResult ?? new User { Email = email, Name = name, CompanyId = 1 });

        public Task<(User user, string accessToken, string refreshToken)> LoginAsync(string email, string password, string ipAddress, string userAgent)
        {
            if (LoginException != null)
            {
                throw LoginException;
            }

            var user = new User { Email = email, Name = "Test", CompanyId = 1 };
            return Task.FromResult((user, "access", "refresh"));
        }

        public Task<(string accessToken, string refreshToken)> RefreshTokenAsync(string refreshTokenValue)
            => Task.FromResult(("access", "refresh"));

        public Task LogoutAsync(string refreshTokenValue) => Task.CompletedTask;

        public Task<User?> GetCurrentUserAsync(int userId)
            => Task.FromResult<User?>(new User { Id = userId, Email = "user@test.local", CompanyId = 1 });

        public Task InitiatePasswordResetAsync(string email) => Task.CompletedTask;

        public Task ResetPasswordAsync(string token, string newPassword) => Task.CompletedTask;
    }
}
