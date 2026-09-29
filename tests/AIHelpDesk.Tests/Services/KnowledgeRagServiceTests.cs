using AIHelpDesk.Application.Dtos.KnowledgeBase;
using AIHelpDesk.Application.Interfaces;
using AIHelpDesk.Application.Services;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace AIHelpDesk.Tests.Services
{
    public class KnowledgeRagServiceTests
    {
        private readonly Mock<IKnowledgeSearchService> _knowledgeSearchServiceMock = new();
        private readonly Mock<IAiService> _aiServiceMock = new();

        [Fact]
        public async Task AskAsync_ShouldReturnAnswerAndSources()
        {
            var sources = new List<SearchResultDto>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Title = "Password Reset",
                    Content = "Click Forgot Password to reset your password.",
                    Score = 0.88
                }
            };

            _knowledgeSearchServiceMock.Setup(x => x.HybridSearchAsync("How do I reset my password?"))
                                       .ReturnsAsync(sources);

            _aiServiceMock.Setup(x => x.GenerateKnowledgeBaseAnswerAsync("How do I reset my password?", It.IsAny<string>()))
                          .ReturnsAsync("Click Forgot Password to reset your password.");

            var service = new KnowledgeRagService(_knowledgeSearchServiceMock.Object, _aiServiceMock.Object);

            var result = await service.AskAsync("How do I reset my password?");

            Assert.Equal("Click Forgot Password to reset your password.", result.Answer);

            Assert.Single(result.Sources);
            Assert.Equal(sources[0].Id, result.Sources.ToList()[0].Id);

            _knowledgeSearchServiceMock.Verify(x => x.HybridSearchAsync("How do I reset my password?"), Times.Once);

            _aiServiceMock.Verify(x => x.GenerateKnowledgeBaseAnswerAsync("How do I reset my password?", It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task AskAsync_ShouldThrow_WhenQuestionIsEmpty()
        {
            var service = new KnowledgeRagService(
                _knowledgeSearchServiceMock.Object,
                _aiServiceMock.Object);

            await Assert.ThrowsAsync<ArgumentException>(
                () => service.AskAsync(""));
        }

        [Fact]
        public async Task AskAsync_ShouldUseTopThreeRelevantSources()
        {
            var sources = new List<SearchResultDto>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Title = "Article 1",
                    Content = "Content 1",
                    Score = 0.9
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Title = "Article 2",
                    Content = "Content 2",
                    Score = 0.8
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Title = "Article 3",
                    Content = "Content 3",
                    Score = 0.7
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Title = "Article 4",
                    Content = "Content 4",
                    Score = 0.6
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Title = "Article 5",
                    Content = "Content 5",
                    Score = 0.55
                }
            };

            _knowledgeSearchServiceMock.Setup(x => x.HybridSearchAsync("test question"))
                                       .ReturnsAsync(sources);

            _aiServiceMock.Setup(x => x.GenerateKnowledgeBaseAnswerAsync("test question", It.IsAny<string>()))
                          .ReturnsAsync("Test answer");

            var service = new KnowledgeRagService(_knowledgeSearchServiceMock.Object, _aiServiceMock.Object);

            var result = await service.AskAsync("test question");

            Assert.Equal(3, result.Sources.Count());

            Assert.Equal(sources[0].Id, result.Sources.ToList()[0].Id);
            Assert.Equal(sources[1].Id, result.Sources.ToList()[1].Id);
            Assert.Equal(sources[2].Id, result.Sources.ToList()[2].Id);

            _aiServiceMock.Verify(x => x.GenerateKnowledgeBaseAnswerAsync("test question", It.Is<string>(context => context.Contains("Article 1") && context.Contains("Article 2") && context.Contains("Article 3") && !context.Contains("Article 4") && !context.Contains("Article 5"))), Times.Once);
        }

        [Fact]
        public async Task AskAsync_ShouldIncludeRetrievedKnowledgeInContext()
        {
            var sources = new List<SearchResultDto>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Title = "Password Reset",
                    Content = "Click Forgot Password to reset your password.",
                    Score = 0.88
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Title = "Account Help",
                    Content = "General Account Assistance",
                    Score = 0.75
                }
            };

            _knowledgeSearchServiceMock.Setup(x => x.HybridSearchAsync("How do I reset my password?"))
                                       .ReturnsAsync(sources);

            _aiServiceMock.Setup(x => x.GenerateKnowledgeBaseAnswerAsync("How do I reset my password?", It.IsAny<string>()))
                                       .ReturnsAsync("Use the Forgot Password option.");

            var service = new KnowledgeRagService(_knowledgeSearchServiceMock.Object, _aiServiceMock.Object);

            var result = await service.AskAsync("How do I reset my password?");

            Assert.Equal("Use the Forgot Password option.", result.Answer);

            _aiServiceMock.Verify(x => x.GenerateKnowledgeBaseAnswerAsync("How do I reset my password?", It.Is<string>(context => context.Contains("Password Reset") && context.Contains("Click Forgot Password to reset your password.") && context.Contains("Account Help") && context.Contains("General Account Assistance") && context.Contains("General Account Assistance"))), Times.Once);
        }

        [Fact]
        public async Task AskAsync_ShouldReturnMessage_WhenNoRelevantSourcesAreFound()
        {
            _knowledgeSearchServiceMock.Setup(x => x.HybridSearchAsync("How do I change my email?"))
                                       .ReturnsAsync(new List<SearchResultDto>());

            var service = new KnowledgeRagService(_knowledgeSearchServiceMock.Object, _aiServiceMock.Object);

            var result = await service.AskAsync("How do I change my email?");

            Assert.Equal("I could not find relevant information in the knowledge base.", result.Answer);

            Assert.Empty(result.Sources);

            _aiServiceMock.Verify(x => x.GenerateKnowledgeBaseAnswerAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task AskAsync_ShouldIgnoreSourcesWithZeroScore()
        {
            var sources = new List<SearchResultDto>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Title = "Relevant Article",
                    Content = "Relevant knowledge.",
                    Score = 0.90
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Title = "Irrelevant Article",
                    Content = "Irrelevant knowledge.",
                    Score = 0
                }
            };

            _knowledgeSearchServiceMock.Setup(x => x.HybridSearchAsync("test question"))
                                       .ReturnsAsync(sources);

            _aiServiceMock.Setup(x => x.GenerateKnowledgeBaseAnswerAsync("test question", It.IsAny<string>()))
                                       .ReturnsAsync("Test answer");

            var service = new KnowledgeRagService(_knowledgeSearchServiceMock.Object, _aiServiceMock.Object);

            var result = await service.AskAsync("test question");

            Assert.Single(result.Sources);
            Assert.Equal(sources[0].Id, result.Sources.ToList()[0].Id);

            _aiServiceMock.Verify(x => x.GenerateKnowledgeBaseAnswerAsync("test question", It.Is<string>(context => context.Contains("Relevant Article") && context.Contains("Relevant knowledge.") && !context.Contains("Irrelevant Article") && !context.Contains("Irrelevant knowledge."))), Times.Once);
        }

        [Fact]
        public async Task AskAsync_ShouldUseHybridSearch()
        {
            var sources = new List<SearchResultDto>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Title = "Password Reset",
                    Content = "Click Forgot Password to reset your password.",
                    Score = 0.95
                }
            };

            _knowledgeSearchServiceMock.Setup(x => x.HybridSearchAsync("How do I reset my password?"))
                                       .ReturnsAsync(sources);

            _aiServiceMock.Setup(x => x.GenerateKnowledgeBaseAnswerAsync("How do I reset my password?", It.IsAny<string>()))
                          .ReturnsAsync("Test answer");

            var service = new KnowledgeRagService(_knowledgeSearchServiceMock.Object, _aiServiceMock.Object);

            var result = await service.AskAsync("How do I reset my password?");

            Assert.Equal("Test answer", result.Answer);
            Assert.Single(result.Sources);

            _knowledgeSearchServiceMock.Verify(x => x.HybridSearchAsync("How do I reset my password?"), Times.Once);
            _knowledgeSearchServiceMock.Verify(x => x.SemanticSearchAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task AskAsync_ShouldIgnoreSourcesBelowRelevanceThreshold()
        {
            var sources = new List<SearchResultDto>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Title = "Password Reset",
                    Content = "Click Forgot Password to reset your password.",
                    Score = 0.88
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Title = "Account Help",
                    Content = "General Account Assistance",
                    Score = 0.49
                }
            };

            _knowledgeSearchServiceMock.Setup(x => x.HybridSearchAsync("How do I reset my password?"))
                                       .ReturnsAsync(sources);

            _aiServiceMock.Setup(x => x.GenerateKnowledgeBaseAnswerAsync("How do I reset my password?", It.IsAny<string>()))
                          .ReturnsAsync("Use the Forgot Password option.");

            var service = new KnowledgeRagService(_knowledgeSearchServiceMock.Object, _aiServiceMock.Object);

            var result = await service.AskAsync("How do I reset my password?");

            Assert.Single(result.Sources);
            Assert.Equal(sources[0].Id, result.Sources.ToList()[0].Id);

            _aiServiceMock.Verify(x => x.GenerateKnowledgeBaseAnswerAsync("How do I reset my password?", It.Is<string>(context => context.Contains("Password Reset") && context.Contains("Click Forgot Password to reset your password.") && !context.Contains("Account Help") && !context.Contains("General Account Assistance"))), Times.Once);
        }
    }
}
