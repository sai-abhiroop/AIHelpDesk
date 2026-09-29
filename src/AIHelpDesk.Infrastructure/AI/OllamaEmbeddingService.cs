using AIHelpDesk.Application.Interfaces;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;

namespace AIHelpDesk.Infrastructure.AI
{
    public class OllamaEmbeddingService : IEmbeddingService
    {
        private readonly HttpClient _httpClient;
        private readonly OllamaSettings _settings;

        public OllamaEmbeddingService(HttpClient httpClient, IOptions<OllamaSettings> options)
        {
            _httpClient = httpClient;
            _settings = options.Value;
        }

        public async Task<float[]> GenerateEmbeddingAsync(string text)
        {
            var response = await _httpClient.PostAsJsonAsync($"{_settings.BaseUrl}/api/embed",
                new
                {
                    model = _settings.EmbeddingModel,
                    input = text
                });

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<OllamaEmbeddingResponse>();

            return result?.Embeddings?.FirstOrDefault() ?? throw new InvalidOperationException("Ollama returned no embedding.");
        }

        private class OllamaEmbeddingResponse
        {
            public List<float[]> Embeddings { get; set; } = [];
        }
    }
}
