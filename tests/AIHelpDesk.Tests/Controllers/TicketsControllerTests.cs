using AIHelpDesk.API.Controllers;
using AIHelpDesk.Application.Dtos.Ticket;
using AIHelpDesk.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace AIHelpDesk.Tests.Controllers
{
    public class TicketsControllerTests
    {
        private static TicketsController CreateController(Mock<ITicketService> serviceMock, Guid userId, bool isAdmin = false)
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

            var controller = new TicketsController(serviceMock.Object);

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
        public async Task GetMyTickets_ShouldReturnOk()
        {
            var userId = Guid.NewGuid();
            var expected = new List<TicketDetailsDto>();

            var serviceMock = new Mock<ITicketService>();

            serviceMock.Setup(x => x.GetMyTicketsAsync(userId))
                       .ReturnsAsync(expected);

            var controller = CreateController(serviceMock, userId);

            var result = await controller.GetMyTickets();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Same(expected, okResult.Value);

            serviceMock.Verify(x => x.GetMyTicketsAsync(userId), Times.Once);
        }

        [Fact]
        public async Task GetMyTickets_ShouldReturnUnauthorized_WhenUserIdClaimIsInvalid()
        {
            var serviceMock = new Mock<ITicketService>();
            var controller = new TicketsController(serviceMock.Object);

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

            var result = await controller.GetMyTickets();

            Assert.IsType<UnauthorizedResult>(result.Result);

            serviceMock.Verify(x => x.GetMyTicketsAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task GetTicket_ShouldReturnOk_WhenTicketExists()
        {
            var userId = Guid.NewGuid();
            var ticketId = Guid.NewGuid();

            var expected = new TicketDetailsDto
            {
                Id = ticketId,
                CustomerId = userId
            };

            var serviceMock = new Mock<ITicketService>();

            serviceMock.Setup(x => x.GetTicketByIdAsync(ticketId, userId, false))
                       .ReturnsAsync(expected);

            var controller = CreateController(serviceMock, userId);

            var result = await controller.GetTicket(ticketId);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Same(expected, okResult.Value);
        }

        [Fact]
        public async Task GetTicket_ShouldReturnNotFound_WhenTicketDoesNotExist()
        {
            var userId = Guid.NewGuid();
            var ticketId = Guid.NewGuid();

            var serviceMock = new Mock<ITicketService>();

            serviceMock.Setup(x => x.GetTicketByIdAsync(ticketId, userId, false))
                       .ReturnsAsync((TicketDetailsDto?)null);

            var controller = CreateController(serviceMock, userId);

            var result = await controller.GetTicket(ticketId);

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task GetTicket_ShouldPassAdminFlag()
        {
            var adminId = Guid.NewGuid();
            var ticketId = Guid.NewGuid();

            var serviceMock = new Mock<ITicketService>();

            serviceMock.Setup(x => x.GetTicketByIdAsync(ticketId, adminId, true))
                       .ReturnsAsync(new TicketDetailsDto { Id = ticketId });

            var controller = CreateController(serviceMock, adminId, true);

            var result = await controller.GetTicket(ticketId);

            Assert.IsType<OkObjectResult>(result.Result);

            serviceMock.Verify(x => x.GetTicketByIdAsync(ticketId, adminId, true), Times.Once);
        }

        [Fact]
        public async Task CreateTicket_ShouldReturnCreated()
        {
            var customerId = Guid.NewGuid();

            var dto = new CreateTicketDto
            {
                Title = "Login issue",
                Description = "Cannot login."
            };

            var expected = new TicketDetailsDto
            {
                Id = Guid.NewGuid(),
                Title = dto.Title,
                Description = dto.Description
            };

            var serviceMock = new Mock<ITicketService>();

            serviceMock.Setup(x => x.CreateTicketAsync(dto, customerId))
                       .ReturnsAsync(expected);

            var controller = CreateController(serviceMock, customerId);

            var result = await controller.CreateTicket(dto);

            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);

            Assert.Equal(nameof(TicketsController.GetTicket), createdResult.ActionName);
            Assert.Same(expected, createdResult.Value);
        }

        [Fact]
        public async Task UpdateStatus_ShouldReturnOk_WhenTicketExists()
        {
            var ticketId = Guid.NewGuid();

            var dto = new UpdateTicketStatusDto
            {
                Status = AIHelpDesk.Domain.Enums.TicketStatus.InProgress
            };

            var expected = new TicketDetailsDto
            {
                Id = ticketId
            };

            var serviceMock = new Mock<ITicketService>();

            serviceMock.Setup(x => x.UpdateTicketStatusAsync(ticketId, dto.Status))
                       .ReturnsAsync(expected);

            var controller = CreateController(serviceMock, Guid.NewGuid());

            var result = await controller.UpdateStatus(ticketId, dto);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Same(expected, okResult.Value);
        }

        [Fact]
        public async Task AssignTicket_ShouldReturnNotFound_WhenTicketDoesNotExist()
        {
            var ticketId = Guid.NewGuid();
            var dto = new AssignTicketDto
            {
                AgentId = Guid.NewGuid()
            };

            var serviceMock = new Mock<ITicketService>();

            serviceMock.Setup(x => x.AssignTicketAsync(ticketId, dto.AgentId))
                       .ReturnsAsync((TicketDetailsDto?)null);

            var controller = CreateController(serviceMock, Guid.NewGuid());

            var result = await controller.AssignTicket(ticketId, dto);

            Assert.IsType<NotFoundResult>(result.Result);
        }
    }
}
