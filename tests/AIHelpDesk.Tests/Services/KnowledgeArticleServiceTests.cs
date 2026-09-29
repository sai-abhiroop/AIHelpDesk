using AIHelpDesk.Application.Dtos.KnowledgeBase;
using AIHelpDesk.Application.Interfaces;
using AIHelpDesk.Application.Services;
using AIHelpDesk.Domain.Entities;
using AIHelpDesk.Infrastructure.Persistence;
using AutoMapper;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace AIHelpDesk.Tests.Services
{
    public class KnowledgeArticleServiceTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly ApplicationDbContext _dbContext;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<IEmbeddingService> _embeddingServiceMock;
        private readonly Mock<IVectorSimilarityService> _vectorSimilarityService;

        public KnowledgeArticleServiceTests()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(_connection)
                .Options;

            _dbContext = new ApplicationDbContext(options);
            _dbContext.Database.EnsureCreated();

            _mapperMock = new Mock<IMapper>();
            _embeddingServiceMock = new Mock<IEmbeddingService>();
            _vectorSimilarityService =  new Mock<IVectorSimilarityService>();
        }

        [Fact]
        public async Task CreateAsync_ShouldCreateArticle()
        {
            var dto = new KnowledgeArticleDto
            {
                Title = "Password Reset",
                Content = "Steps to reset password."
            };

            _mapperMock.Setup(x => x.Map<KnowledgeArticleDto>(It.IsAny<KnowledgeArticle>()))
                       .Returns(dto);

            var service = new KnowledgeArticleService(_dbContext, _mapperMock.Object, _embeddingServiceMock.Object, _vectorSimilarityService.Object);

            var result = await service.CreateAsync("Password Reset", "Steps to reset password.");

            Assert.Equal("Password Reset", result.Title);
            Assert.Single(_dbContext.KnowledgeArticles);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnArticle_WhenArticleExists()
        {
            var article = new KnowledgeArticle("Password Reset", "Steps to reset password.");

            _dbContext.KnowledgeArticles.Add(article);
            await _dbContext.SaveChangesAsync();

            var dto = new KnowledgeArticleDto
            {
                Id = article.Id,
                Title = article.Title,
                Content = article.Content
            };

            _mapperMock.Setup(x => x.Map<KnowledgeArticleDto>(article)).Returns(dto);

            var service = new KnowledgeArticleService(_dbContext, _mapperMock.Object, _embeddingServiceMock.Object, _vectorSimilarityService.Object);

            var result = await service.GetByIdAsync(article.Id);

            Assert.NotNull(result);
            Assert.Equal(article.Id, result.Id);
            Assert.Equal("Password Reset", result.Title);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenArticleDoesNotExist()
        {
            var service = new KnowledgeArticleService(_dbContext, _mapperMock.Object, _embeddingServiceMock.Object, _vectorSimilarityService.Object);

            var result = await service.GetByIdAsync(Guid.NewGuid());

            Assert.Null(result);
            _mapperMock.Verify(x => x.Map<KnowledgeArticleDto>(It.IsAny<KnowledgeArticle>()), Times.Never);
        }

        [Fact]
        public async Task GetActiveAsync_ShouldReturnOnlyActiveArticles()
        {
            var activeArticle = new KnowledgeArticle("Active Article", "Active content.");
            var inactiveArticle = new KnowledgeArticle("Inactive Article", "Inactive content.");
            inactiveArticle.Deactivate();

            _dbContext.KnowledgeArticles.AddRange(activeArticle, inactiveArticle);
            await _dbContext.SaveChangesAsync();

            var activeDto = new KnowledgeArticleDto
            {
                Id = activeArticle.Id,
                Title = activeArticle.Title
            };

            _mapperMock.Setup(x => x.Map<IEnumerable<KnowledgeArticleDto>>(It.IsAny<IEnumerable<KnowledgeArticle>>()))
                       .Returns(new List<KnowledgeArticleDto> { activeDto });

            var service = new KnowledgeArticleService(_dbContext, _mapperMock.Object, _embeddingServiceMock.Object, _vectorSimilarityService.Object);

            var result = await service.GetActiveAsync();

            Assert.Single(result);
            Assert.Equal("Active Article", result.First().Title);
        }

        [Fact]
        public async Task GetActiveAsync_ShouldOrderByUpdatedAtDescending()
        {
            var olderArticle = new KnowledgeArticle("Older Article", "Older content.");
            var newerArticle = new KnowledgeArticle("Newer Article", "Newer content.");

            _dbContext.KnowledgeArticles.AddRange(olderArticle, newerArticle);
            await _dbContext.SaveChangesAsync();

            var articles = new List<KnowledgeArticleDto>
        {
            new() { Id = newerArticle.Id, Title = newerArticle.Title },
            new() { Id = olderArticle.Id, Title = olderArticle.Title }
        };

            _mapperMock.Setup(x => x.Map<IEnumerable<KnowledgeArticleDto>>(It.IsAny<IEnumerable<KnowledgeArticle>>()))
                       .Returns(articles);

            var service = new KnowledgeArticleService(_dbContext, _mapperMock.Object, _embeddingServiceMock.Object, _vectorSimilarityService.Object);

            var result = await service.GetActiveAsync();

            Assert.Equal("Newer Article", result.First().Title);
        }

        [Fact]
        public async Task SearchAsync_ShouldReturnMatchingActiveArticles()
        {
            var matchingArticle = new KnowledgeArticle("Password Reset", "Steps to reset password.");
            var nonMatchingArticle = new KnowledgeArticle("Payment Issue", "Payment failed.");

            _dbContext.KnowledgeArticles.AddRange(matchingArticle, nonMatchingArticle);
            await _dbContext.SaveChangesAsync();

            _mapperMock.Setup(x => x.Map<IEnumerable<SearchResultDto>>(It.IsAny<IEnumerable<KnowledgeArticle>>()))
                       .Returns(new List<SearchResultDto>
                       {
                            new() { Id = matchingArticle.Id, Title = matchingArticle.Title }
                       });

            var service = new KnowledgeArticleService(_dbContext, _mapperMock.Object, _embeddingServiceMock.Object, _vectorSimilarityService.Object);

            var result = await service.SearchAsync("password");

            Assert.Single(result);
            Assert.Equal("Password Reset", result.First().Title);
        }

        [Fact]
        public async Task SearchAsync_ShouldIgnoreInactiveArticles()
        {
            var activeArticle = new KnowledgeArticle("Password Reset", "Steps to reset password.");
            var inactiveArticle = new KnowledgeArticle("Old Password Guide", "Old password reset steps.");
            inactiveArticle.Deactivate();

            _dbContext.KnowledgeArticles.AddRange(activeArticle, inactiveArticle);
            await _dbContext.SaveChangesAsync();

            _mapperMock.Setup(x => x.Map<IEnumerable<SearchResultDto>>(It.IsAny<IEnumerable<KnowledgeArticle>>()))
                       .Returns(new List<SearchResultDto>
                       {
                            new() { Id = activeArticle.Id, Title = activeArticle.Title }
                       });

            var service = new KnowledgeArticleService(_dbContext, _mapperMock.Object, _embeddingServiceMock.Object, _vectorSimilarityService.Object);

            var result = await service.SearchAsync("password");

            Assert.Single(result);
            Assert.Equal("Password Reset", result.First().Title);
        }

        [Fact]
        public async Task SearchAsync_ShouldReturnEmpty_WhenNoArticleMatches()
        {
            var article = new KnowledgeArticle("Password Reset", "Steps to reset password.");

            _dbContext.KnowledgeArticles.Add(article);
            await _dbContext.SaveChangesAsync();

            _mapperMock.Setup(x => x.Map<IEnumerable<SearchResultDto>>(It.IsAny<IEnumerable<KnowledgeArticle>>()))
                       .Returns(new List<SearchResultDto>());

            var service = new KnowledgeArticleService(_dbContext, _mapperMock.Object, _embeddingServiceMock.Object, _vectorSimilarityService.Object);

            var result = await service.SearchAsync("booking");

            Assert.Empty(result);
        }

        [Fact]
        public async Task SearchAsync_ShouldMatchArticleTitle()
        {
            var article = new KnowledgeArticle("Password Reset", "General account help.");

            _dbContext.KnowledgeArticles.Add(article);
            await _dbContext.SaveChangesAsync();

            _mapperMock.Setup(x => x.Map<IEnumerable<SearchResultDto>>(It.IsAny<IEnumerable<KnowledgeArticle>>()))
                       .Returns(new List<SearchResultDto>
                       {
                            new() { Id = article.Id, Title = article.Title }
                       });

            var service = new KnowledgeArticleService(_dbContext, _mapperMock.Object, _embeddingServiceMock.Object, _vectorSimilarityService.Object);

            var result = await service.SearchAsync("password");

            Assert.Single(result);
            Assert.Equal(article.Id, result.First().Id);
        }

        [Fact]
        public async Task SearchAsync_ShouldMatchArticleContent()
        {
            var article = new KnowledgeArticle("Account Help", "Users can reset their password from the login page.");

            _dbContext.KnowledgeArticles.Add(article);
            await _dbContext.SaveChangesAsync();

            _mapperMock.Setup(x => x.Map<IEnumerable<SearchResultDto>>(It.IsAny<IEnumerable<KnowledgeArticle>>()))
                       .Returns(new List<SearchResultDto>
                       {
                            new() { Id = article.Id, Title = article.Title }
                       });

            var service = new KnowledgeArticleService(_dbContext, _mapperMock.Object, _embeddingServiceMock.Object, _vectorSimilarityService.Object);

            var result = await service.SearchAsync("password");

            Assert.Single(result);
            Assert.Equal(article.Id, result.First().Id);
        }

        [Fact]
        public async Task SearchAsync_ShouldThrow_WhenSearchTermIsEmpty()
        {
            var service = new KnowledgeArticleService(_dbContext, _mapperMock.Object, _embeddingServiceMock.Object, _vectorSimilarityService.Object);

            await Assert.ThrowsAsync<ArgumentException>(() => service.SearchAsync(""));
        }

        [Fact]
        public async Task SearchAsync_ShouldThrow_WhenSearchTermIsWhitespace()
        {
            var service = new KnowledgeArticleService(_dbContext, _mapperMock.Object, _embeddingServiceMock.Object, _vectorSimilarityService.Object);

            await Assert.ThrowsAsync<ArgumentException>(() => service.SearchAsync("   "));
        }

        [Fact]
        public async Task SearchAsync_ShouldGiveScore3_WhenTitleAndContentMatch()
        {
            var article = new KnowledgeArticle("Password Reset", "Use password reset to change your password.");

            _dbContext.KnowledgeArticles.Add(article);
            await _dbContext.SaveChangesAsync();

            _mapperMock.Setup(x => x.Map<IEnumerable<SearchResultDto>>(It.IsAny<IEnumerable<KnowledgeArticle>>()))
                       .Returns(new List<SearchResultDto>
                       {
                   new() { Id = article.Id, Title = article.Title, Content = article.Content }
                       });

            var service = new KnowledgeArticleService(_dbContext, _mapperMock.Object, _embeddingServiceMock.Object, _vectorSimilarityService.Object);

            var result = await service.SearchAsync("password");

            Assert.Single(result);
            Assert.Equal(3, result.First().Score);
        }

        [Fact]
        public async Task SearchAsync_ShouldGiveScore2_WhenOnlyTitleMatches()
        {
            var article = new KnowledgeArticle("Password Reset", "General account help.");

            _dbContext.KnowledgeArticles.Add(article);
            await _dbContext.SaveChangesAsync();

            _mapperMock.Setup(x => x.Map<IEnumerable<SearchResultDto>>(It.IsAny<IEnumerable<KnowledgeArticle>>()))
                       .Returns(new List<SearchResultDto>
                       {
                   new() { Id = article.Id, Title = article.Title, Content = article.Content }
                       });

            var service = new KnowledgeArticleService(_dbContext, _mapperMock.Object, _embeddingServiceMock.Object, _vectorSimilarityService.Object);

            var result = await service.SearchAsync("password");

            Assert.Single(result);
            Assert.Equal(2, result.First().Score);
        }

        [Fact]
        public async Task SearchAsync_ShouldGiveScore1_WhenOnlyContentMatches()
        {
            var article = new KnowledgeArticle("Account Help", "Use the password reset option.");

            _dbContext.KnowledgeArticles.Add(article);
            await _dbContext.SaveChangesAsync();

            _mapperMock.Setup(x => x.Map<IEnumerable<SearchResultDto>>(It.IsAny<IEnumerable<KnowledgeArticle>>()))
                       .Returns(new List<SearchResultDto>
                       {
                   new() { Id = article.Id, Title = article.Title, Content = article.Content }
                       });

            var service = new KnowledgeArticleService(_dbContext, _mapperMock.Object, _embeddingServiceMock.Object, _vectorSimilarityService.Object);

            var result = await service.SearchAsync("password");

            Assert.Single(result);
            Assert.Equal(1, result.First().Score);
        }

        [Fact]
        public async Task SearchAsync_ShouldOrderByScoreDescending()
        {
            var contentMatch = new KnowledgeArticle("Account Help", "Use the password reset option.");
            var titleMatch = new KnowledgeArticle("Password Reset", "General account help.");

            _dbContext.KnowledgeArticles.AddRange(contentMatch, titleMatch);
            await _dbContext.SaveChangesAsync();

            _mapperMock.Setup(x => x.Map<IEnumerable<SearchResultDto>>(It.IsAny<IEnumerable<KnowledgeArticle>>()))
                       .Returns((IEnumerable<KnowledgeArticle> articles) => articles
                   .Select(article => new SearchResultDto
                   {
                       Id = article.Id,
                       Title = article.Title,
                       Content = article.Content
                   }));

            var service = new KnowledgeArticleService(_dbContext, _mapperMock.Object, _embeddingServiceMock.Object, _vectorSimilarityService.Object);

            var result = await service.SearchAsync("password");

            Assert.Equal(2, result.Count());
            Assert.Equal(titleMatch.Id, result.First().Id);
            Assert.Equal(2, result.First().Score);
            Assert.Equal(1, result.Last().Score);
        }

        [Fact]
        public async Task CreateAsync_ShouldGenerateEmbedding()
        {
            var embedding = new float[] { 1.1f, 2.2f, 3.3f };

            _embeddingServiceMock.Setup(x => x.GenerateEmbeddingAsync("Password Reset\nUse the password reset option."))
                                  .ReturnsAsync(embedding);

            _mapperMock.Setup(x => x.Map<KnowledgeArticleDto>(It.IsAny<KnowledgeArticle>()))
                       .Returns((KnowledgeArticle article) => new KnowledgeArticleDto
                       {
                           Id = article.Id,
                           Title = article.Title,
                           Content = article.Content,
                           IsActive = article.IsActive,
                           CreatedAt = article.CreatedAt,
                           UpdatedAt = article.UpdatedAt
                       });

            var service = new KnowledgeArticleService(_dbContext, _mapperMock.Object, _embeddingServiceMock.Object, _vectorSimilarityService.Object);

            var result = await service.CreateAsync("Password Reset", "Use the password reset option.");

            var article = await _dbContext.KnowledgeArticles.FirstAsync();

            Assert.Equal("Password Reset", result.Title);
            Assert.Equal("Use the password reset option.", result.Content);
            Assert.Equal("1.1,2.2,3.3", article.Embedding);

            _embeddingServiceMock.Verify(x => x.GenerateEmbeddingAsync("Password Reset\nUse the password reset option."), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateArticleAndRegenerateEmbedding()
        {
            var article = new KnowledgeArticle("Password Reset", "Old password reset instructions.");
            _dbContext.KnowledgeArticles.Add(article);
            await _dbContext.SaveChangesAsync();

            var embedding = new float[] { 4.4f, 5.5f, 6.6f };

            _embeddingServiceMock.Setup(x => x.GenerateEmbeddingAsync("Updated Password Reset\nNew password reset instructions."))
                                  .ReturnsAsync(embedding);

            _mapperMock.Setup(x => x.Map<KnowledgeArticleDto>(It.IsAny<KnowledgeArticle>()))
                       .Returns((KnowledgeArticle a) => new KnowledgeArticleDto
                       {
                           Id = a.Id,
                           Title = a.Title,
                           Content = a.Content,
                           IsActive = a.IsActive,
                           CreatedAt = a.CreatedAt,
                           UpdatedAt = a.UpdatedAt
                       });

            var service = new KnowledgeArticleService(_dbContext, _mapperMock.Object, _embeddingServiceMock.Object, _vectorSimilarityService.Object);

            var result = await service.UpdateAsync(article.Id, "Updated Password Reset", "New password reset instructions.");

            var updatedArticle = await _dbContext.KnowledgeArticles.FirstAsync();

            Assert.NotNull(result);
            Assert.Equal("Updated Password Reset", result.Title);
            Assert.Equal("New password reset instructions.", result.Content);
            Assert.Equal("4.4,5.5,6.6", updatedArticle.Embedding);

            _embeddingServiceMock.Verify(x => x.GenerateEmbeddingAsync("Updated Password Reset\nNew password reset instructions."), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ShouldReturnNull_WhenArticleDoesNotExist()
        {
            var service = new KnowledgeArticleService(_dbContext, _mapperMock.Object, _embeddingServiceMock.Object, _vectorSimilarityService.Object);

            var result = await service.UpdateAsync(Guid.NewGuid(), "Updated Title", "Updated Content");

            Assert.Null(result);

            _embeddingServiceMock.Verify(x => x.GenerateEmbeddingAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task SemanticSearchAsync_ShouldOrderResultsBySimilarityScore()
        {
            var firstArticle = new KnowledgeArticle("Password Reset", "Reset your password.");
            var secondArticle = new KnowledgeArticle("Account Help", "General account assistance.");

            firstArticle.SetEmbedding("1,2,3");
            secondArticle.SetEmbedding("4,5,6");

            _dbContext.KnowledgeArticles.AddRange(firstArticle, secondArticle);
            await _dbContext.SaveChangesAsync();

            _embeddingServiceMock.Setup(x => x.GenerateEmbeddingAsync("How do I reset my password?"))
                                 .ReturnsAsync(new float[] { 1, 2, 3 });

            _vectorSimilarityService.Setup(x => x.CalculateCosineSimilarity(It.IsAny<float[]>(), It.IsAny<float[]>()))
                                    .Returns((float[] first, float[] second) => second.SequenceEqual(new float[] { 1, 2, 3 }) ? 0.95 : 0.40);

            _mapperMock.Setup(x => x.Map<SearchResultDto>(It.IsAny<KnowledgeArticle>()))
                       .Returns((KnowledgeArticle article) => new SearchResultDto
                       {
                           Id = article.Id,
                           Title = article.Title,
                           Content = article.Content
                       });

            var service = new KnowledgeArticleService(_dbContext, _mapperMock.Object, _embeddingServiceMock.Object, _vectorSimilarityService.Object);

            var result = await service.SemanticSearchAsync("How do I reset my password?");

            Assert.Equal(2, result.Count());
            Assert.Equal(firstArticle.Id, result.First().Id);
            Assert.Equal(0.95, result.First().Score);
            Assert.Equal(secondArticle.Id, result.Last().Id);
            Assert.Equal(0.40, result.Last().Score);
        }

        [Fact]
        public async Task SemanticSearchAsync_ShouldThrow_WhenSearchTermIsEmpty()
        {
            var service = new KnowledgeArticleService(_dbContext, _mapperMock.Object, _embeddingServiceMock.Object, _vectorSimilarityService.Object);

            await Assert.ThrowsAsync<ArgumentException>(() => service.SemanticSearchAsync(" "));
        }

        [Fact]
        public async Task SemanticSearchAsync_ShouldIgnoreArticlesWithoutEmbedding()
        {
            var articleWithEmbedding = new KnowledgeArticle("Password Reset", "Reset your password.");
            articleWithEmbedding.SetEmbedding("1,2,3");

            var articleWithoutEmbedding = new KnowledgeArticle("Account Help", "General account assistance.");

            _dbContext.KnowledgeArticles.AddRange(articleWithEmbedding, articleWithoutEmbedding);
            await _dbContext.SaveChangesAsync();

            _embeddingServiceMock.Setup(x => x.GenerateEmbeddingAsync("password"))
                                 .ReturnsAsync(new float[] { 1, 2, 3 });

            _vectorSimilarityService.Setup(x => x.CalculateCosineSimilarity(It.IsAny<float[]>(), It.IsAny<float[]>()))
                                        .Returns(0.90);

            _mapperMock.Setup(x => x.Map<SearchResultDto>(It.IsAny<KnowledgeArticle>()))
                       .Returns((KnowledgeArticle article) => new SearchResultDto
                       {
                           Id = article.Id,
                           Title = article.Title,
                           Content = article.Content
                       });

            var service = new KnowledgeArticleService(_dbContext, _mapperMock.Object, _embeddingServiceMock.Object, _vectorSimilarityService.Object);

            var result = await service.SemanticSearchAsync("password");

            Assert.Single(result);
            Assert.Equal(articleWithEmbedding.Id, result.First().Id);
        }

        [Fact]
        public async Task HybridSearchAsync_ShouldCombineKeywordAndSemanticScores()
        {
            var passwordArticle = new KnowledgeArticle("Password Reset", "Reset your password.");

            passwordArticle.SetEmbedding("1,2,3");

            var accountArticle = new KnowledgeArticle("Account Help", "General account assistance.");

            accountArticle.SetEmbedding("4,5,6");

            await _dbContext.KnowledgeArticles.AddRangeAsync(passwordArticle, accountArticle);

            await _dbContext.SaveChangesAsync();

            _embeddingServiceMock.Setup(x => x.GenerateEmbeddingAsync("password"))
                                 .ReturnsAsync(new float[] { 1, 2, 3 });

            _vectorSimilarityService.Setup(x => x.CalculateCosineSimilarity(It.IsAny<float[]>(), It.IsAny<float[]>()))
                                    .Returns(0.90);

            _mapperMock.Setup(x => x.Map<IEnumerable<SearchResultDto>>(It.IsAny<IEnumerable<KnowledgeArticle>>()))
                      .Returns((IEnumerable<KnowledgeArticle> articles) =>
                        {
                            return articles.Select(article => new SearchResultDto
                                    {
                                        Id = article.Id,
                                        Title = article.Title,
                                        Content = article.Content
                                    });
                        });

            _mapperMock.Setup(x => x.Map<SearchResultDto>(It.IsAny<KnowledgeArticle>()))
                       .Returns((KnowledgeArticle article) => new SearchResultDto
                       {
                           Id = article.Id,
                           Title = article.Title,
                           Content = article.Content
                       });

            var service = new KnowledgeArticleService(_dbContext, _mapperMock.Object, _embeddingServiceMock.Object, _vectorSimilarityService.Object);

            var result = await service.HybridSearchAsync("password");

            Assert.Equal(2, result.Count());
            Assert.Equal(3.90, result.ToList()[0].Score);
            Assert.Equal(0.90, result.ToList()[1].Score);
        }

        [Fact]
        public async Task HybridSearchAsync_ShouldIncludeSemanticOnlyResults()
        {
            var passwordArticle = new KnowledgeArticle("Password Reset", "Reset your password.");

            var accountArticle = new KnowledgeArticle("Account Help", "General account assistance.");

            passwordArticle.SetEmbedding("1,2,3");
            accountArticle.SetEmbedding("4,5,6");

            await _dbContext.KnowledgeArticles.AddRangeAsync(passwordArticle, accountArticle);

            await _dbContext.SaveChangesAsync();

            _embeddingServiceMock.Setup(x => x.GenerateEmbeddingAsync("password"))
                                 .ReturnsAsync(new float[] { 1, 2, 3 });

            _vectorSimilarityService.Setup(x => x.CalculateCosineSimilarity(It.IsAny<float[]>(), It.IsAny<float[]>()))
                                    .Returns(0.90);

            _mapperMock.Setup(x => x.Map<IEnumerable<SearchResultDto>>(It.IsAny<IEnumerable<KnowledgeArticle>>()))
                       .Returns((IEnumerable<KnowledgeArticle> articles) => articles.Select(article => new SearchResultDto
                       {
                           Id = article.Id,
                           Title = article.Title,
                           Content = article.Content
                       }));

            _mapperMock.Setup(x => x.Map<SearchResultDto>(It.IsAny<KnowledgeArticle>()))
                       .Returns((KnowledgeArticle article) => new SearchResultDto
                       {
                           Id = article.Id,
                           Title = article.Title,
                           Content = article.Content
                       });

            var service = new KnowledgeArticleService(_dbContext, _mapperMock.Object, _embeddingServiceMock.Object, _vectorSimilarityService.Object);

            var result = await service.HybridSearchAsync("password");

            Assert.Equal(2, result.Count());
            Assert.Contains(result, x => x.Id == passwordArticle.Id);
            Assert.Contains(result, x => x.Id == accountArticle.Id);
        }

        [Fact]
        public async Task HybridSearchAsync_ShouldThrow_WhenSearchTermIsEmpty()
        {
            var service = new KnowledgeArticleService(_dbContext, _mapperMock.Object, _embeddingServiceMock.Object, _vectorSimilarityService.Object);

            await Assert.ThrowsAsync<ArgumentException>(() => service.HybridSearchAsync(""));
        }

        [Fact]
        public async Task HybridSearchAsync_ShouldOrderResultsByCombinedScore()
        {
            var passwordArticle = new KnowledgeArticle("Password Reset", "Reset your password.");

            var accountArticle = new KnowledgeArticle("Account Help", "General account assistance.");

            passwordArticle.SetEmbedding("1,2,3");
            accountArticle.SetEmbedding("4,5,6");

            await _dbContext.KnowledgeArticles.AddRangeAsync(passwordArticle, accountArticle);

            await _dbContext.SaveChangesAsync();

            _embeddingServiceMock.Setup(x => x.GenerateEmbeddingAsync("password"))
                                 .ReturnsAsync(new float[] { 1, 2, 3 });

            _vectorSimilarityService.Setup(x => x.CalculateCosineSimilarity(It.IsAny<float[]>(), It.Is<float[]>(v => v[0] == 1 && v[1] == 2 && v[2] == 3)))
                                    .Returns(0.40);

            _vectorSimilarityService.Setup(x => x.CalculateCosineSimilarity(It.IsAny<float[]>(), It.Is<float[]>(v => v[0] == 4 && v[1] == 5 && v[2] == 6)))
                                    .Returns(0.90);

            _mapperMock.Setup(x => x.Map<IEnumerable<SearchResultDto>>(It.IsAny<IEnumerable<KnowledgeArticle>>()))
                       .Returns((IEnumerable<KnowledgeArticle> articles) => articles.Select(article => new SearchResultDto
                       {
                           Id = article.Id,
                           Title = article.Title,
                           Content = article.Content
                       }));

            _mapperMock.Setup(x => x.Map<SearchResultDto>(It.IsAny<KnowledgeArticle>()))
                       .Returns((KnowledgeArticle article) => new SearchResultDto
                       {
                           Id = article.Id,
                           Title = article.Title,
                           Content = article.Content
                       });

            var service = new KnowledgeArticleService(_dbContext, _mapperMock.Object, _embeddingServiceMock.Object, _vectorSimilarityService.Object);

            var result = await service.HybridSearchAsync("password");

            Assert.Equal(2, result.Count());
            Assert.Equal(passwordArticle.Id, result.ToList()[0].Id);
            Assert.Equal(accountArticle.Id, result.ToList()[1].Id);
        }

        [Fact]
        public async Task DeactivateAsync_ShouldDeactivateArticle()
        {
            var article = new KnowledgeArticle("Password Reset", "Reset your password.");

            await _dbContext.KnowledgeArticles.AddAsync(article);
            await _dbContext.SaveChangesAsync();

            var service = new KnowledgeArticleService(_dbContext, _mapperMock.Object, _embeddingServiceMock.Object, _vectorSimilarityService.Object);

            var result = await service.DeactivateAsync(article.Id);

            Assert.True(result);

            var updatedArticle = await _dbContext.KnowledgeArticles.FindAsync(article.Id);

            Assert.False(updatedArticle!.IsActive);
        }

        [Fact]
        public async Task DeactivateAsync_ShouldReturnFalse_WhenArticleDoesNotExist()
        {
            var service = new KnowledgeArticleService(_dbContext, _mapperMock.Object, _embeddingServiceMock.Object, _vectorSimilarityService.Object);

            var result = await service.DeactivateAsync(Guid.NewGuid());

            Assert.False(result);
        }

        [Fact]
        public async Task ActivateAsync_ShouldActivateArticle()
        {
            var article = new KnowledgeArticle("Password Reset", "Reset your password.");

            article.Deactivate();

            await _dbContext.KnowledgeArticles.AddAsync(article);
            await _dbContext.SaveChangesAsync();

            var service = new KnowledgeArticleService(_dbContext, _mapperMock.Object, _embeddingServiceMock.Object, _vectorSimilarityService.Object);

            var result = await service.ActivateAsync(article.Id);

            Assert.True(result);

            var updatedArticle = await _dbContext.KnowledgeArticles.FindAsync(article.Id);

            Assert.True(updatedArticle!.IsActive);
        }

        [Fact]
        public async Task ActivateAsync_ShouldReturnFalse_WhenArticleDoesNotExist()
        {
            var service = new KnowledgeArticleService(_dbContext, _mapperMock.Object, _embeddingServiceMock.Object, _vectorSimilarityService.Object);

            var result = await service.ActivateAsync(Guid.NewGuid());

            Assert.False(result);
        }

        public void Dispose()
        {
            _dbContext.Dispose();
            _connection.Dispose();
        }
    }
}
