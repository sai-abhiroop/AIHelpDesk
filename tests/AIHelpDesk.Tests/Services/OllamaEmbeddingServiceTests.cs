using AIHelpDesk.Infrastructure.AI;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace AIHelpDesk.Tests.Services
{
    public class OllamaEmbeddingServiceTests
    {
        [Fact]
        public async Task GenerateEmbeddingAsync_ShouldReturnEmbedding()
        {
            using var httpClient = new HttpClient();

            var options = Options.Create(new OllamaSettings
            {
                BaseUrl = "http://localhost:11434",
                EmbeddingModel = "nomic-embed-text"
            });

            var service = new OllamaEmbeddingService(httpClient, options);

            var result = await service.GenerateEmbeddingAsync("How do I reset my password?");

            Assert.NotEmpty(result);
            Assert.True(result.Length > 0);
        }
    }
}
