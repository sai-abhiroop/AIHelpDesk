using AIHelpDesk.Application.Dtos.Auth;
using AIHelpDesk.Application.Dtos.KnowledgeBase;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;

namespace AIHelpDesk.Tests.Integration
{
    public class KnowledgeArticleIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public KnowledgeArticleIntegrationTests(
            CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task CreateKnowledgeArticle_ShouldReturnCreated_WhenAdminIsAuthenticated()
        {
            var loginRequest = new LoginRequestDto
            {
                Email = "admin@test.com",
                Password = "Password123!"
            };

            var loginResponse = await _client.PostAsJsonAsync("/api/Auth/login", loginRequest);

            Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

            var token = await loginResponse.Content.ReadFromJsonAsync<TokenDto>();

            Assert.NotNull(token);
            Assert.False(string.IsNullOrWhiteSpace(token.AccessToken));

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

            var articleRequest = new CreateKnowledgeArticleDto
            {
                Title = "Password Reset",
                Content = "Customers can reset their password from the login page."
            };

            var response = await _client.PostAsJsonAsync("/api/KnowledgeArticles", articleRequest);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var article = await response.Content.ReadFromJsonAsync<KnowledgeArticleDto>();

            Assert.NotNull(article);
            Assert.Equal(articleRequest.Title, article.Title);
            Assert.Equal(articleRequest.Content, article.Content);
            Assert.True(article.IsActive);
        }


    }
}
