using AIHelpDesk.Application.Interfaces;

namespace AIHelpDesk.Infrastructure.AI
{
    public class FakeEmbeddingService : IEmbeddingService
    {
        public Task<float[]> GenerateEmbeddingAsync(string text)
        {
            var embedding = new float[8];

            for (var i = 0; i < text.Length; i++)
            {
                embedding[i % embedding.Length] += text[i];
            }

            return Task.FromResult(embedding);
        }
    }
}
