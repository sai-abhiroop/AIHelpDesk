using AIHelpDesk.API.Controllers;
using AIHelpDesk.Application.Dtos.TicketComment;
using AIHelpDesk.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace AIHelpDesk.Tests.Controllers
{
    public class TicketCommentControllerTests
    {
        private static TicketCommentController CreateController(Mock<ITicketCommentService> serviceMock, Guid userId, bool isAdmin = false)
        {
            var controller = new TicketCommentController(serviceMock.Object);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId.ToString())
            };

            if (isAdmin)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            }

            var identity = new ClaimsIdentity(claims, "TestAuthentication");

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
        public async Task GetComments_ShouldReturnOk_WhenUserHasAccess()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var ticketId = Guid.NewGuid();

            var expectedComments = new List<TicketCommentDto>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    TicketId = ticketId,
                    UserId = userId,
                    Content = "Customer comment."
                }
            };

            var serviceMock = new Mock<ITicketCommentService>();

            serviceMock.Setup(service => service.GetCommentsAsync(ticketId, userId, false))
                       .ReturnsAsync(expectedComments);

            var controller = CreateController(serviceMock, userId);

            // Act
            var result = await controller.GetComments(ticketId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var comments = Assert.IsAssignableFrom<IEnumerable<TicketCommentDto>>(okResult.Value);

            Assert.Single(comments);
            Assert.Equal("Customer comment.", comments.First().Content);

            serviceMock.Verify(service => service.GetCommentsAsync(ticketId, userId, false), Times.Once);
        }

        [Fact]
        public async Task GetComments_ShouldReturnUnauthorized_WhenUserIdClaimIsInvalid()
        {
            var serviceMock = new Mock<ITicketCommentService>();
            var controller = new TicketCommentController(serviceMock.Object);

            var identity = new ClaimsIdentity(
                new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "invalid-user-id")
                }, "TestAuthentication");

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identity)
                }
            };

            var result = await controller.GetComments(Guid.NewGuid());

            Assert.IsType<UnauthorizedResult>(result.Result);

            serviceMock.Verify(x => x.GetCommentsAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task AddComment_ShouldReturnOk_WhenCommentIsCreated()
        {
            var userId = Guid.NewGuid();
            var ticketId = Guid.NewGuid();

            var dto = new CreateTicketCommentDto
            {
                Content = "I am checking this issue."
            };

            var expectedComment = new TicketCommentDto
            {
                Id = Guid.NewGuid(),
                TicketId = ticketId,
                UserId = userId,
                Content = dto.Content
            };

            var serviceMock = new Mock<ITicketCommentService>();

            serviceMock.Setup(x => x.AddCommentAsync(ticketId, userId, false, dto))
                       .ReturnsAsync(expectedComment);

            var controller = CreateController(serviceMock, userId);

            var result = await controller.AddComment(ticketId, dto);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var comment = Assert.IsType<TicketCommentDto>(okResult.Value);

            Assert.Equal(expectedComment.Id, comment.Id);
            Assert.Equal(dto.Content, comment.Content);

            serviceMock.Verify(x => x.AddCommentAsync(ticketId, userId, false, dto), Times.Once);
        }

        [Fact]
        public async Task AddComment_ShouldReturnNotFound_WhenTicketIsInaccessible()
        {
            var userId = Guid.NewGuid();
            var ticketId = Guid.NewGuid();

            var dto = new CreateTicketCommentDto
            {
                Content = "Unauthorized comment."
            };

            var serviceMock = new Mock<ITicketCommentService>();

            serviceMock.Setup(x => x.AddCommentAsync(ticketId, userId, false, dto))
                       .ReturnsAsync((TicketCommentDto?)null);

            var controller = CreateController(serviceMock, userId);

            var result = await controller.AddComment(ticketId, dto);

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task GetComments_ShouldPassAdminFlag_WhenUserIsAdmin()
        {
            var adminId = Guid.NewGuid();
            var ticketId = Guid.NewGuid();

            var serviceMock = new Mock<ITicketCommentService>();

            serviceMock.Setup(x => x.GetCommentsAsync(ticketId, adminId, true))
                       .ReturnsAsync([]);

            var controller = CreateController(serviceMock, adminId, true);

            var result = await controller.GetComments(ticketId);

            Assert.IsType<OkObjectResult>(result.Result);

            serviceMock.Verify(x => x.GetCommentsAsync(ticketId, adminId, true), Times.Once);
        }
    }
}
