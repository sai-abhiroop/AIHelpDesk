using AIHelpDesk.Application.Dtos.Auth;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;

namespace AIHelpDesk.Tests.Integration
{
    public class AuthIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public AuthIntegrationTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Register_ShouldReturnOk_WhenRequestIsValid()
        {
            var request = new RegisterationRequestDto
            {
                Email = $"test@example.com",
                Password = "Password123!",
                FirstName = "Test",
                LastName = "User"
            };

            var response = await _client.PostAsJsonAsync("/api/Auth/register", request);

            var responseBody = await response.Content.ReadAsStringAsync();

            Assert.True(response.IsSuccessStatusCode, $"Registration failed with {response.StatusCode}: {responseBody}");

            var result = await response.Content.ReadFromJsonAsync<UserDto>();

            Assert.NotNull(result);
            Assert.Equal(request.Email, result.Email);
            Assert.Equal(request.FirstName, result.FirstName);
            Assert.Equal(request.LastName, result.LastName);
        }

        [Fact]
        public async Task Login_ShouldReturnOk_WithAccessAndRefreshTokens()
        {
            var email = $"login-{Guid.NewGuid()}@example.com";
            var password = "Password123!";

            var registerRequest = new RegisterationRequestDto
            {
                Email = email,
                Password = password,
                FirstName = "Login",
                LastName = "Test"
            };

            var registerResponse = await _client.PostAsJsonAsync("/api/Auth/register", registerRequest);

            Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

            var loginRequest = new LoginRequestDto
            {
                Email = email,
                Password = password
            };

            var loginResponse = await _client.PostAsJsonAsync("/api/Auth/login", loginRequest);

            Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

            var result = await loginResponse.Content.ReadFromJsonAsync<TokenDto>();

            Assert.NotNull(result);
            Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
            Assert.False(string.IsNullOrWhiteSpace(result.RefreshToken));
        }

        [Fact]
        public async Task Login_ShouldReturnUnauthorized_WhenPasswordIsInvalid()
        {
            var email = $"invalid-login-{Guid.NewGuid()}@example.com";
            var password = "Password123!";

            var registerRequest = new RegisterationRequestDto
            {
                Email = email,
                Password = password,
                FirstName = "Invalid",
                LastName = "Login"
            };

            var registerResponse = await _client.PostAsJsonAsync("/api/Auth/register", registerRequest);

            Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

            var loginRequest = new LoginRequestDto
            {
                Email = email,
                Password = "WrongPassword123!"
            };

            var loginResponse = await _client.PostAsJsonAsync("/api/Auth/login", loginRequest);

            Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
        }

        [Fact]
        public async Task ProtectedEndpoint_ShouldReturnOk_WhenValidJwtIsProvided()
        {
            var email = $"protected-{Guid.NewGuid()}@example.com";
            var password = "Password123!";

            var registerRequest = new RegisterationRequestDto
            {
                Email = email,
                Password = password,
                FirstName = "Protected",
                LastName = "Test"
            };

            var registerResponse = await _client.PostAsJsonAsync("/api/Auth/register", registerRequest);

            Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

            var loginRequest = new LoginRequestDto
            {
                Email = email,
                Password = password
            };

            var loginResponse = await _client.PostAsJsonAsync("/api/Auth/login", loginRequest);

            Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

            var token = await loginResponse.Content.ReadFromJsonAsync<TokenDto>();

            Assert.NotNull(token);
            Assert.False(string.IsNullOrWhiteSpace(token.AccessToken));

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

            var response = await _client.GetAsync("/api/Tickets");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task ProtectedEndpoint_ShouldReturnUnauthorized_WhenJwtIsMissing()
        {
            _client.DefaultRequestHeaders.Authorization = null;

            var response = await _client.GetAsync("/api/Tickets");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
