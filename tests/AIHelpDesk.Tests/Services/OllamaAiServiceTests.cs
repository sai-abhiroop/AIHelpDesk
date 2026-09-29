using AIHelpDesk.Infrastructure.AI;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace AIHelpDesk.Tests.Services
{
    public class OllamaAiServiceTests
    {
        [Fact]
        public async Task GenerateSuggestedResponseAsync_ShouldReturnResponse()
        {
            using var httpClient = new HttpClient();

            var options = Options.Create(new OllamaSettings
            {
                BaseUrl = "http://localhost:11434",
                ChatModel = "llama3.2"
            });

            var service = new OllamaAiService(httpClient, options);

            var result = await service.GenerateSuggestedResponseAsync("Password Reset", "I forgot my password and cannot log in.");

            Assert.False(string.IsNullOrWhiteSpace(result));
        }
    }
}
