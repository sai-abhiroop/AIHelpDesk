using AIHelpDesk.Application.Interfaces;
using AIHelpDesk.Application.Services;
using AIHelpDesk.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace AIHelpDesk.Tests.Services
{
    public class AiTicketAnalysisServiceTests
    {
        [Fact]
        public async Task AnalyzeTicketAsync_ShouldReturnCombinedAnalysis()
        {
            var aiService = new Mock<IAiService>();
            var dbContext = new Mock<IApplicationDbContext>();

            aiService.Setup(x => x.ClassifyTicketAsync("Login issue", "Unable to login"))
                .ReturnsAsync("Authentication");

            aiService.Setup(x => x.SuggestPriorityAsync("Login issue", "Unable to login"))
                .ReturnsAsync("High");

            aiService.Setup(x => x.SummarizeTicketAsync("Login issue", "Unable to login"))
                .ReturnsAsync("Customer cannot login.");

            aiService.Setup(x => x.GenerateSuggestedResponseAsync("Login issue", "Unable to login"))
                .ReturnsAsync("Please reset your password and try again.");

            var service = new AiTicketAnalysisService(aiService.Object, dbContext.Object);

            var result = await service.AnalyzeTicketAsync("Login issue", "Unable to login");

            Assert.NotNull(result);
            Assert.Equal("Authentication", result.Category);
            Assert.Equal("High", result.SuggestedPriority);
            Assert.Equal("Customer cannot login.", result.Summary);
            Assert.Equal("Please reset your password and try again.", result.SuggestedResponse);
        }

        [Fact]
        public async Task AnalyzeTicketAsync_ShouldCallAllAiOperations()
        {
            var aiService = new Mock<IAiService>();
            var dbContext = new Mock<IApplicationDbContext>();

            aiService.Setup(x => x.ClassifyTicketAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("Authentication");

            aiService.Setup(x => x.SuggestPriorityAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("High");

            aiService.Setup(x => x.SummarizeTicketAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("Summary");

            aiService.Setup(x => x.GenerateSuggestedResponseAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("Response");

            var service = new AiTicketAnalysisService(aiService.Object, dbContext.Object);

            await service.AnalyzeTicketAsync("Login issue", "Unable to login");

            aiService.Verify(x => x.ClassifyTicketAsync("Login issue", "Unable to login"), Times.Once);
            aiService.Verify(x => x.SuggestPriorityAsync("Login issue", "Unable to login"), Times.Once);
            aiService.Verify(x => x.SummarizeTicketAsync("Login issue", "Unable to login"), Times.Once);
            aiService.Verify(x => x.GenerateSuggestedResponseAsync("Login issue", "Unable to login"), Times.Once);
        }

        [Fact]
        public async Task AnalyzeExistingTicketAsync_ShouldReturnNull_WhenTicketDoesNotExist()
        {
            var aiService = new Mock<IAiService>();
            var dbContext = new Mock<IApplicationDbContext>();

            dbContext.Setup(x => x.Tickets.FindAsync(It.IsAny<Guid>())).ReturnsAsync((Ticket?)null);

            var service = new AiTicketAnalysisService(aiService.Object, dbContext.Object);
            var result = await service.AnalyzeExistingTicketAsync(Guid.NewGuid(), Guid.NewGuid(), false);

            Assert.Null(result);
            aiService.Verify(x => x.ClassifyTicketAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task AnalyzeExistingTicketAsync_ShouldReturnNull_WhenUserHasNoAccess()
        {
            var aiService = new Mock<IAiService>();
            var dbContext = new Mock<IApplicationDbContext>();
            var customerId = Guid.NewGuid();
            var agentId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var ticket = new Ticket("Login issue", "Unable to login", customerId);
            ticket.AssignTo(agentId);

            dbContext.Setup(x => x.Tickets.FindAsync(ticket.Id)).ReturnsAsync(ticket);

            var service = new AiTicketAnalysisService(aiService.Object, dbContext.Object);
            var result = await service.AnalyzeExistingTicketAsync(ticket.Id, userId, false);

            Assert.Null(result);
            aiService.Verify(x => x.ClassifyTicketAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task AnalyzeExistingTicketAsync_ShouldAnalyze_WhenUserIsAssignedAgent()
        {
            var aiService = new Mock<IAiService>();
            var dbContext = new Mock<IApplicationDbContext>();
            var customerId = Guid.NewGuid();
            var agentId = Guid.NewGuid();

            var ticket = new Ticket("Login issue", "Unable to login", customerId);
            ticket.AssignTo(agentId);

            dbContext.Setup(x => x.Tickets.FindAsync(ticket.Id)).ReturnsAsync(ticket);
            aiService.Setup(x => x.ClassifyTicketAsync(ticket.Title, ticket.Description)).ReturnsAsync("Authentication");
            aiService.Setup(x => x.SuggestPriorityAsync(ticket.Title, ticket.Description)).ReturnsAsync("High");

            var service = new AiTicketAnalysisService(aiService.Object, dbContext.Object);
            var result = await service.AnalyzeExistingTicketAsync(ticket.Id, agentId, false);

            Assert.NotNull(result);
            Assert.Equal("Authentication", result.Category);
        }

        [Fact]
        public async Task AnalyzeExistingTicketAsync_ShouldAnalyze_WhenUserIsAdmin()
        {
            var aiService = new Mock<IAiService>();
            var dbContext = new Mock<IApplicationDbContext>();
            var ticket = new Ticket("Login issue", "Unable to login", Guid.NewGuid());

            dbContext.Setup(x => x.Tickets.FindAsync(ticket.Id)).ReturnsAsync(ticket);
            aiService.Setup(x => x.ClassifyTicketAsync(ticket.Title, ticket.Description)).ReturnsAsync("Authentication");
            aiService.Setup(x => x.SuggestPriorityAsync(ticket.Title, ticket.Description)).ReturnsAsync("High");

            var service = new AiTicketAnalysisService(aiService.Object, dbContext.Object);
            var result = await service.AnalyzeExistingTicketAsync(ticket.Id, Guid.NewGuid(), true);

            Assert.NotNull(result);
            Assert.Equal("Authentication", result.Category);
        }

        [Fact]
        public async Task AnalyzeTicketAsync_ShouldThrow_WhenAiReturnsInvalidPriority()
        {
            var aiService = new Mock<IAiService>();
            var dbContext = new Mock<IApplicationDbContext>();

            aiService.Setup(x => x.ClassifyTicketAsync(It.IsAny<string>(), It.IsAny<string>()))
                     .ReturnsAsync("Account");

            aiService.Setup(x => x.SuggestPriorityAsync(It.IsAny<string>(), It.IsAny<string>()))
                     .ReturnsAsync("UrgentAndVeryImportant");

            aiService.Setup(x => x.SummarizeTicketAsync(It.IsAny<string>(), It.IsAny<string>()))
                     .ReturnsAsync("Summary");

            aiService.Setup(x => x.GenerateSuggestedResponseAsync(It.IsAny<string>(), It.IsAny<string>()))
                     .ReturnsAsync("Response");

            var service = new AiTicketAnalysisService(aiService.Object, dbContext.Object);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.AnalyzeTicketAsync("Cannot access account", "I am unable to log in."));
        }
    }
}
