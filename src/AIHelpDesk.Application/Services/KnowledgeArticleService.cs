using AIHelpDesk.Application.Dtos.KnowledgeBase;
using AIHelpDesk.Application.Interfaces;
using AIHelpDesk.Domain.Entities;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace AIHelpDesk.Application.Services
{
    public class KnowledgeArticleService : IKnowledgeArticleService, IKnowledgeSearchService
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly IEmbeddingService _embeddingService;
        private readonly IVectorSimilarityService _vectorSimilarityService;

        public KnowledgeArticleService(IApplicationDbContext dbContext, IMapper mapper, IEmbeddingService embeddingService, IVectorSimilarityService vectorSimilarityService)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _embeddingService = embeddingService;
            _vectorSimilarityService = vectorSimilarityService;
        }

        public async Task<bool> ActivateAsync(Guid id)
        {
            var article = await _dbContext.KnowledgeArticles.FindAsync(id);

            if(article == null)
            {
                return false;
            }

            article.Activate();

            await _dbContext.SaveChangesAsync();

            return true;
        }

        public async Task<KnowledgeArticleDto> CreateAsync(string title, string content)
        {
            var article = new KnowledgeArticle(title, content);

            var embeddingText = $"{title}\n{content}";
            var embedding = await _embeddingService.GenerateEmbeddingAsync(embeddingText);

            var serializedEmbedding = string.Join(",", embedding);
            article.SetEmbedding(serializedEmbedding);

            await _dbContext.KnowledgeArticles.AddAsync(article);
            await _dbContext.SaveChangesAsync();

            return _mapper.Map<KnowledgeArticleDto>(article);
        }

        public async Task<bool> DeactivateAsync(Guid id)
        {
            var article = await _dbContext.KnowledgeArticles.FindAsync(id);

            if (article == null)
            {
                return false;
            }

            article.Deactivate();

            await _dbContext.SaveChangesAsync();

            return true;
        }

        public async Task<IEnumerable<KnowledgeArticleDto>> GetActiveAsync()
        {
            var articles = await _dbContext.KnowledgeArticles
                                           .AsNoTracking()
                                           .Where(a => a.IsActive)
                                           .OrderByDescending(a => a.UpdatedAt)
                                           .ToListAsync();

            return _mapper.Map<IEnumerable<KnowledgeArticleDto>>(articles);
        }

        public async Task<KnowledgeArticleDto?> GetByIdAsync(Guid id)
        {
            var article = await _dbContext.KnowledgeArticles.FindAsync(id);

            if(article == null)
            {
                return null;
            }

            return _mapper.Map<KnowledgeArticleDto>(article);
        }

        public async Task<IEnumerable<SearchResultDto>> HybridSearchAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                throw new ArgumentException("Search term is required.", nameof(searchTerm));
            }

            var keywordResults = await SearchAsync(searchTerm);
            var semanticResults = await SemanticSearchAsync(searchTerm);

            var keywordLookup = keywordResults.ToDictionary(x => x.Id);
            var semanticLookup = semanticResults.ToDictionary(x => x.Id);

            var results = keywordResults.Select(result =>
            {
                var semanticScore = semanticLookup.TryGetValue(result.Id, out var semanticResult) ? semanticResult.Score : 0;

                result.Score = result.Score + semanticScore;

                return result;

            })
            .ToList();

            foreach (var result in semanticResults)
            {
                if (!keywordLookup.ContainsKey(result.Id))
                {
                    results.Add(result);
                }
            }

            return results.OrderByDescending(x => x.Score)
                          .ToList();
        }

        public async  Task<IEnumerable<SearchResultDto>> SearchAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                throw new ArgumentException("Search term is required", nameof(searchTerm));
            }

            var normalizedSearchTerm = searchTerm.ToLower();

            var articles = await _dbContext.KnowledgeArticles
                                           .AsNoTracking()
                                           .Where(a => a.IsActive && (a.Title.ToLower().Contains(normalizedSearchTerm) || a.Content.ToLower().Contains(normalizedSearchTerm)))
                                           .OrderByDescending(a => a.UpdatedAt)
                                           .ToListAsync();

            var results = _mapper.Map<IEnumerable<SearchResultDto>>(articles).ToList();

            foreach (var result in results)
            {
                result.Score = 0;

                if (result.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                {
                    result.Score += 2;
                }

                if (result.Content.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                {
                    result.Score += 1;
                }
            }
            return results.OrderByDescending(x => x.Score)
                          .ThenByDescending(x => articles.First(a => a.Id == x.Id).UpdatedAt)
                          .ToList();
        }

        public async Task<IEnumerable<SearchResultDto>> SemanticSearchAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                throw new ArgumentException("Search term is required.", nameof(searchTerm));
            }

            var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(searchTerm);

            var articles = await _dbContext.KnowledgeArticles
                                           .AsNoTracking()
                                           .Where(a => a.IsActive && a.Embedding != null)
                                           .ToListAsync();

            var results = new List<SearchResultDto>();

            foreach(var article in articles)
            {
                var embedding = article.Embedding!
                                       .Split(',', StringSplitOptions.RemoveEmptyEntries)
                                       .Select(float.Parse)
                                       .ToArray();

                var score = _vectorSimilarityService.CalculateCosineSimilarity(queryEmbedding, embedding);

                var result = _mapper.Map<SearchResultDto>(article);
                result.Score = score;

                results.Add(result);
            }

            return results.OrderByDescending(x => x.Score)
                          .ToList();

        }

        public async Task<KnowledgeArticleDto?> UpdateAsync(Guid id, string title, string content)
        {
            var article = await _dbContext.KnowledgeArticles.FindAsync(id);

            if (article == null)
            {
                return null;
            }

            article.Update(title, content);

            var embeddingText = $"{title}\n{content}";
            var embedding = await _embeddingService.GenerateEmbeddingAsync(embeddingText);

            var serializedEmbedding = string.Join(",", embedding);
            article.SetEmbedding(serializedEmbedding);

            await _dbContext.SaveChangesAsync();

            return _mapper.Map<KnowledgeArticleDto>(article);
        }
    }
}
