using AIHelpDesk.API.Controllers;
using AIHelpDesk.Application.Dtos.AI;
using AIHelpDesk.Application.Dtos.Ticket;
using AIHelpDesk.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace AIHelpDesk.Tests.Controllers
{
    public class AiControllerTests
    {
        private static AiController CreateController(Mock<IAiTicketAnalysisService> aiServiceMock, Mock<ITicketService> ticketServiceMock, Guid userId, bool isAdmin = false)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId.ToString())
            };

            if (isAdmin)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            }

            var identity = new ClaimsIdentity(claims, "TestAuthentication");

            var controller = new AiController(aiServiceMock.Object, ticketServiceMock.Object);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identity)
                }
            };

            return controller;
        }

        [Fact]
        public async Task AnalyzeTicket_ShouldReturnOk()
        {
            var aiServiceMock = new Mock<IAiTicketAnalysisService>();
            var ticketServiceMock = new Mock<ITicketService>();

            var dto = new AiTicketAnalysisRequestDto
            {
                Title = "Cannot login",
                Description = "User cannot access account."
            };

            var expected = new AiTicketAnalysisDto
            {
                Category = "Account & Login",
                SuggestedPriority = "Medium"
            };

            aiServiceMock.Setup(x => x.AnalyzeTicketAsync(dto.Title, dto.Description))
                         .ReturnsAsync(expected);

            var controller = CreateController(aiServiceMock, ticketServiceMock, Guid.NewGuid());

            var result = await controller.AnalyzeTicket(dto);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Same(expected, okResult.Value);
        }

        [Fact]
        public async Task AnalyzeExistingTicket_ShouldReturnOk()
        {
            var userId = Guid.NewGuid();
            var ticketId = Guid.NewGuid();

            var aiServiceMock = new Mock<IAiTicketAnalysisService>();
            var ticketServiceMock = new Mock<ITicketService>();

            var expected = new AiTicketAnalysisDto
            {
                Category = "Account & Login",
                SuggestedPriority = "Medium"
            };

            aiServiceMock.Setup(x => x.AnalyzeExistingTicketAsync(ticketId, userId, false))
                         .ReturnsAsync(expected);

            var controller = CreateController(aiServiceMock, ticketServiceMock, userId);

            var result = await controller.AnalyzeExistingTicket(ticketId);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Same(expected, okResult.Value);
        }

        [Fact]
        public async Task AnalyzeExistingTicket_ShouldReturnUnauthorized_WhenUserIdClaimIsInvalid()
        {
            var aiServiceMock = new Mock<IAiTicketAnalysisService>();
            var ticketServiceMock = new Mock<ITicketService>();

            var controller = new AiController(aiServiceMock.Object, ticketServiceMock.Object);

            var identity = new ClaimsIdentity(
                new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "invalid-id")
                },
                "TestAuthentication");

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identity)
                }
            };

            var result = await controller.AnalyzeExistingTicket(Guid.NewGuid());

            Assert.IsType<UnauthorizedResult>(result.Result);

            aiServiceMock.Verify(x => x.AnalyzeExistingTicketAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task AnalyzeExistingTicket_ShouldReturnNotFound_WhenTicketIsNotAccessible()
        {
            var userId = Guid.NewGuid();
            var ticketId = Guid.NewGuid();

            var aiServiceMock = new Mock<IAiTicketAnalysisService>();
            var ticketServiceMock = new Mock<ITicketService>();

            aiServiceMock.Setup(x => x.AnalyzeExistingTicketAsync(ticketId, userId, false))
                         .ReturnsAsync((AiTicketAnalysisDto?)null);

            var controller = CreateController(aiServiceMock, ticketServiceMock, userId);

            var result = await controller.AnalyzeExistingTicket(ticketId);

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task ApplyAnalysis_ShouldReturnOk()
        {
            var ticketId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var aiServiceMock = new Mock<IAiTicketAnalysisService>();
            var ticketServiceMock = new Mock<ITicketService>();

            var dto = new ApplyAiTicketAnalysisDto
            {
                Category = "Account & Login",
                Priority = "Medium"
            };

            var expected = new TicketDetailsDto
            {
                Id = ticketId,
                CategoryId = Guid.NewGuid()
            };

            ticketServiceMock.Setup(x => x.ApplyAiSuggestionAsync(ticketId, userId, false, dto.Category, dto.Priority))
                             .ReturnsAsync(expected);

            var controller = CreateController(aiServiceMock, ticketServiceMock, userId);

            var result = await controller.ApplyAnalysis(ticketId, dto);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Same(expected, okResult.Value);

            ticketServiceMock.Verify(x => x.ApplyAiSuggestionAsync(ticketId, userId, false, dto.Category, dto.Priority), Times.Once);
        }

        [Fact]
        public async Task ApplyAnalysis_ShouldReturnNotFound_WhenTicketDoesNotExist()
        {
            var ticketId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var aiServiceMock = new Mock<IAiTicketAnalysisService>();
            var ticketServiceMock = new Mock<ITicketService>();

            var dto = new ApplyAiTicketAnalysisDto
            {
                Category = "Account & Login",
                Priority = "Medium"
            };

            ticketServiceMock.Setup(x => x.ApplyAiSuggestionAsync(ticketId, userId, false, dto.Category, dto.Priority))
                             .ReturnsAsync((TicketDetailsDto?)null);

            var controller = CreateController(aiServiceMock, ticketServiceMock, Guid.NewGuid());

            var result = await controller.ApplyAnalysis(ticketId, dto);

            Assert.IsType<NotFoundResult>(result.Result);
        }
    }
}
