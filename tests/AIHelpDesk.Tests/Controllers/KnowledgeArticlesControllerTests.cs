using AIHelpDesk.API.Controllers;
using AIHelpDesk.Application.Dtos.KnowledgeBase;
using AIHelpDesk.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace AIHelpDesk.Tests.Controllers
{
    public class KnowledgeArticlesControllerTests
    {
        private readonly Mock<IKnowledgeArticleService> _serviceMock = new();
        private readonly Mock<IKnowledgeSearchService> _searchServiceMock = new();
        private readonly Mock<IKnowledgeRagService> _ragServiceMock = new();

        private KnowledgeArticlesController CreateController()
        {
            return new KnowledgeArticlesController(_serviceMock.Object, _searchServiceMock.Object, _ragServiceMock.Object);
        }

        [Fact]
        public async Task GetActiveKnowledgeArticles_ShouldReturnOk()
        {
            var articles = new List<KnowledgeArticleDto>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Title = "Password Reset",
                    Content = "Reset your password from the login page.",
                    IsActive = true
                }
            };

            _serviceMock.Setup(x => x.GetActiveAsync())
                        .ReturnsAsync(articles);

            var controller = CreateController();

            var result = await controller.GetActiveKnowledgeArticles();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(articles, okResult.Value);
        }

        [Fact]
        public async Task GetById_ShouldReturnOk_WhenArticleExists()
        {
            var id = Guid.NewGuid();

            var article = new KnowledgeArticleDto
            {
                Id = id,
                Title = "Password Reset",
                Content = "Reset your password.",
                IsActive = true
            };

            _serviceMock.Setup(x => x.GetByIdAsync(id))
                        .ReturnsAsync(article);

            var controller = CreateController();

            var result = await controller.GetById(id);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(article, okResult.Value);
        }

        [Fact]
        public async Task GetById_ShouldReturnNotFound_WhenArticleDoesNotExist()
        {
            var id = Guid.NewGuid();

            _serviceMock.Setup(x => x.GetByIdAsync(id))
                        .ReturnsAsync((KnowledgeArticleDto?)null);

            var controller = CreateController();

            var result = await controller.GetById(id);

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task Search_ShouldReturnOk()
        {
            var searchResults = new List<SearchResultDto>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Title = "Password Reset",
                    Content = "Reset your password.",
                    Score = 2
                }
            };

            _searchServiceMock.Setup(x => x.SearchAsync("password"))
                              .ReturnsAsync(searchResults);

            var controller = CreateController();

            var result = await controller.Search("password");

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(searchResults, okResult.Value);
        }

        [Fact]
        public async Task SemanticSearch_ShouldReturnOk()
        {
            var searchResults = new List<SearchResultDto>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Title = "Password Reset",
                    Content = "Reset your password.",
                    Score = 0.87
                }
            };

            _searchServiceMock.Setup(x => x.SemanticSearchAsync("forgot password"))
                              .ReturnsAsync(searchResults);

            var controller = CreateController();

            var result = await controller.SemanticSearch("forgot password");

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(searchResults, okResult.Value);
        }

        [Fact]
        public async Task HybridSearch_ShouldReturnOk()
        {
            var searchResults = new List<SearchResultDto>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Title = "Password Reset",
                    Content = "Reset your password.",
                    Score = 2.87
                }
            };

            _searchServiceMock.Setup(x => x.HybridSearchAsync("password"))
                              .ReturnsAsync(searchResults);

            var controller = CreateController();

            var result = await controller.HybridSearch("password");

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(searchResults, okResult.Value);
        }

        [Fact]
        public async Task Ask_ShouldReturnOk()
        {
            var response = new RagResponseDto
            {
                Answer = "Select Forgot Password on the login page.",
                Sources = []
            };

            var request = new RagQueryDto
            {
                Question = "How do I reset my password?"
            };

            _ragServiceMock.Setup(x => x.AskAsync(request.Question))
                           .ReturnsAsync(response);

            var controller = CreateController();

            var result = await controller.Ask(request);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(response, okResult.Value);
        }

        [Fact]
        public async Task CreateKnowledgeArticle_ShouldReturnCreated()
        {
            var article = new KnowledgeArticleDto
            {
                Id = Guid.NewGuid(),
                Title = "Password Reset",
                Content = "Reset your password.",
                IsActive = true
            };

            var request = new CreateKnowledgeArticleDto
            {
                Title = article.Title,
                Content = article.Content
            };

            _serviceMock.Setup(x => x.CreateAsync(request.Title, request.Content))
                        .ReturnsAsync(article);

            var controller = CreateController();

            var result = await controller.CreateKnowledgeArticle(request);

            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);

            Assert.Equal(nameof(controller.GetById), createdResult.ActionName);
            Assert.Equal(article, createdResult.Value);
        }

        [Fact]
        public async Task UpdateKnowledgeArticle_ShouldReturnOk_WhenArticleExists()
        {
            var id = Guid.NewGuid();

            var article = new KnowledgeArticleDto
            {
                Id = id,
                Title = "Updated Password Reset",
                Content = "Updated content.",
                IsActive = true
            };

            var request = new UpdateKnowledgeArticleDto
            {
                Title = article.Title,
                Content = article.Content
            };

            _serviceMock
                .Setup(x => x.UpdateAsync(id, request.Title, request.Content))
                .ReturnsAsync(article);

            var controller = CreateController();

            var result = await controller.UpdateKnowledgeArticle(id, request);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(article, okResult.Value);
        }

        [Fact]
        public async Task UpdateKnowledgeArticle_ShouldReturnNotFound_WhenArticleDoesNotExist()
        {
            var id = Guid.NewGuid();

            var request = new UpdateKnowledgeArticleDto
            {
                Title = "Updated Article",
                Content = "Updated content."
            };

            _serviceMock
                .Setup(x => x.UpdateAsync(id, request.Title, request.Content))
                .ReturnsAsync((KnowledgeArticleDto?)null);

            var controller = CreateController();

            var result = await controller.UpdateKnowledgeArticle(id, request);

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task Deactivate_ShouldReturnNoContent_WhenSuccessful()
        {
            var id = Guid.NewGuid();

            _serviceMock.Setup(x => x.DeactivateAsync(id))
                        .ReturnsAsync(true);

            var controller = CreateController();

            var result = await controller.Deactivate(id);

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task Deactivate_ShouldReturnNotFound_WhenArticleDoesNotExist()
        {
            var id = Guid.NewGuid();

            _serviceMock.Setup(x => x.DeactivateAsync(id))
                        .ReturnsAsync(false);

            var controller = CreateController();

            var result = await controller.Deactivate(id);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Activate_ShouldReturnNoContent_WhenSuccessful()
        {
            var id = Guid.NewGuid();

            _serviceMock.Setup(x => x.ActivateAsync(id))
                        .ReturnsAsync(true);

            var controller = CreateController();

            var result = await controller.Activate(id);

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task Activate_ShouldReturnNotFound_WhenArticleDoesNotExist()
        {
            var id = Guid.NewGuid();

            _serviceMock.Setup(x => x.ActivateAsync(id))
                        .ReturnsAsync(false);

            var controller = CreateController();

            var result = await controller.Activate(id);

            Assert.IsType<NotFoundResult>(result);
        }
    }
}
