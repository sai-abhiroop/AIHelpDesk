using AIHelpDesk.Application.Dtos.Auth;
using AIHelpDesk.Application.Dtos.Ticket;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;

namespace AIHelpDesk.Tests.Integration
{
    public class TicketIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public TicketIntegrationTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task CreateTicket_ShouldReturnCreated_WhenCustomerIsAuthenticated()
        {
            var email = $"ticket-{Guid.NewGuid()}@example.com";
            var password = "Password123!";

            var registerRequest = new RegisterationRequestDto
            {
                Email = email,
                Password = password,
                FirstName = "Ticket",
                LastName = "Customer"
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

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

            var ticketRequest = new CreateTicketDto
            {
                Title = "Unable to reset password",
                Description = "I cannot reset my password from the login page."
            };

            var response = await _client.PostAsJsonAsync("/api/Tickets", ticketRequest);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var ticket = await response.Content.ReadFromJsonAsync<TicketDetailsDto>();

            Assert.NotNull(ticket);
            Assert.Equal(ticketRequest.Title, ticket.Title);
            Assert.Equal(ticketRequest.Description, ticket.Description);
        }

        [Fact]
        public async Task GetAssignedTickets_ShouldReturnForbidden_WhenCustomerIsAuthenticated()
        {
            var email = $"customer-{Guid.NewGuid()}@example.com";
            var password = "Password123!";

            var registerRequest = new RegisterationRequestDto
            {
                Email = email,
                Password = password,
                FirstName = "Customer",
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

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

            var response = await _client.GetAsync("/api/Tickets/assigned");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
