using AIHelpDesk.Application.Dtos.TicketComment;
using AIHelpDesk.Application.Services;
using AIHelpDesk.Domain.Entities;
using AIHelpDesk.Infrastructure.Persistence;
using AutoMapper;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace AIHelpDesk.Tests.Services
{
    public class TicketCommentServiceTests
    {
        private static async Task<(SqliteConnection Connection, ApplicationDbContext Context, TicketCommentService Service, Mock<IMapper> MapperMock)> CreateTestContextAsync()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
            var context = new ApplicationDbContext(options);
            await context.Database.EnsureCreatedAsync();

            var mapperMock = new Mock<IMapper>();
            var service = new TicketCommentService(context, mapperMock.Object);

            return (connection, context, service, mapperMock);
        }

        [Fact]
        public async Task AddCommentAsync_WhenCustomerOwnsTicket_ShouldCreateComment()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;
            var customerId = Guid.NewGuid();
            var ticket = new Ticket("Login issue", "Cannot login.", customerId);

            context.Tickets.Add(ticket);
            await context.SaveChangesAsync();

            var dto = new CreateTicketCommentDto { Content = "I still cannot login." };
            var expectedDto = new TicketCommentDto { TicketId = ticket.Id, UserId = customerId, Content = dto.Content };

            test.MapperMock.Setup(mapper => mapper.Map<TicketCommentDto>(It.IsAny<TicketComment>())).Returns(expectedDto);

            // Act
            var result = await test.Service.AddCommentAsync(ticket.Id, customerId, false, dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(ticket.Id, result.TicketId);
            Assert.Equal(customerId, result.UserId);
            Assert.Equal(dto.Content, result.Content);

            var comment = await context.TicketComments.FirstOrDefaultAsync();
            Assert.NotNull(comment);
            Assert.Equal(ticket.Id, comment.TicketId);
            Assert.Equal(customerId, comment.UserId);
            Assert.Equal(dto.Content, comment.Content);
        }

        [Fact]
        public async Task AddCommentAsync_WhenAssignedAgent_ShouldCreateComment()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;
            var customerId = Guid.NewGuid();
            var agentId = Guid.NewGuid();
            var ticket = new Ticket("Login issue", "Cannot login.", customerId);

            ticket.AssignTo(agentId);
            context.Tickets.Add(ticket);
            await context.SaveChangesAsync();

            var dto = new CreateTicketCommentDto { Content = "I am checking this issue." };
            var expectedDto = new TicketCommentDto { TicketId = ticket.Id, UserId = agentId, Content = dto.Content };

            test.MapperMock.Setup(mapper => mapper.Map<TicketCommentDto>(It.IsAny<TicketComment>())).Returns(expectedDto);

            // Act
            var result = await test.Service.AddCommentAsync(ticket.Id, agentId, false, dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(agentId, result.UserId);
            Assert.Equal(dto.Content, result.Content);
        }

        [Fact]
        public async Task AddCommentAsync_WhenAdmin_ShouldCreateComment()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;
            var customerId = Guid.NewGuid();
            var adminId = Guid.NewGuid();
            var ticket = new Ticket("Login issue", "Cannot login.", customerId);

            context.Tickets.Add(ticket);
            await context.SaveChangesAsync();

            var dto = new CreateTicketCommentDto { Content = "Admin response." };
            var expectedDto = new TicketCommentDto { TicketId = ticket.Id, UserId = adminId, Content = dto.Content };

            test.MapperMock.Setup(mapper => mapper.Map<TicketCommentDto>(It.IsAny<TicketComment>())).Returns(expectedDto);

            // Act
            var result = await test.Service.AddCommentAsync(ticket.Id, adminId, true, dto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(adminId, result.UserId);
            Assert.Equal(dto.Content, result.Content);
        }

        [Fact]
        public async Task AddCommentAsync_WhenUserDoesNotHaveAccess_ShouldReturnNull()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;
            var customerId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var ticket = new Ticket("Login issue", "Cannot login.", customerId);

            context.Tickets.Add(ticket);
            await context.SaveChangesAsync();

            var dto = new CreateTicketCommentDto { Content = "Unauthorized comment." };

            // Act
            var result = await test.Service.AddCommentAsync(ticket.Id, otherUserId, false, dto);

            // Assert
            Assert.Null(result);
            Assert.Empty(await context.TicketComments.ToListAsync());
        }

        [Fact]
        public async Task AddCommentAsync_WhenTicketDoesNotExist_ShouldReturnNull()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;
            var userId = Guid.NewGuid();

            var dto = new CreateTicketCommentDto { Content = "Comment." };

            // Act
            var result = await test.Service.AddCommentAsync(Guid.NewGuid(), userId, false, dto);

            // Assert
            Assert.Null(result);
            Assert.Empty(await context.TicketComments.ToListAsync());
        }

        [Fact]
        public async Task GetCommentsAsync_WhenCustomerOwnsTicket_ShouldReturnComments()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;
            var customerId = Guid.NewGuid();
            var ticket = new Ticket("Login issue", "Cannot login.", customerId);

            context.Tickets.Add(ticket);

            var comment1 = new TicketComment(ticket.Id, customerId, "First comment.");
            var comment2 = new TicketComment(ticket.Id, customerId, "Second comment.");

            context.TicketComments.AddRange(comment1, comment2);
            await context.SaveChangesAsync();

            var expectedDtos = new List<TicketCommentDto>
            {
                new TicketCommentDto { Id = comment1.Id, TicketId = ticket.Id, UserId = customerId, Content = comment1.Content },
                new TicketCommentDto { Id = comment2.Id, TicketId = ticket.Id, UserId = customerId, Content = comment2.Content }
            };

            test.MapperMock.Setup(mapper => mapper.Map<IEnumerable<TicketCommentDto>>(It.IsAny<IEnumerable<TicketComment>>())).Returns(expectedDtos);

            // Act
            var result = await test.Service.GetCommentsAsync(ticket.Id, customerId, false);
            var resultList = result.ToList();

            // Assert
            Assert.Equal(2, resultList.Count);
            Assert.Equal("First comment.", resultList[0].Content);
            Assert.Equal("Second comment.", resultList[1].Content);
        }

        [Fact]
        public async Task GetCommentsAsync_WhenAssignedAgent_ShouldReturnComments()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;
            var customerId = Guid.NewGuid();
            var agentId = Guid.NewGuid();
            var ticket = new Ticket("Login issue", "Cannot login.", customerId);

            ticket.AssignTo(agentId);
            context.Tickets.Add(ticket);

            var comment = new TicketComment(ticket.Id, customerId, "Customer comment.");
            context.TicketComments.Add(comment);
            await context.SaveChangesAsync();

            var expectedDtos = new List<TicketCommentDto>
            {
                new TicketCommentDto { Id = comment.Id, TicketId = ticket.Id, UserId = customerId, Content = comment.Content }
            };

            test.MapperMock.Setup(mapper => mapper.Map<IEnumerable<TicketCommentDto>>(It.IsAny<IEnumerable<TicketComment>>())).Returns(expectedDtos);

            // Act
            var result = await test.Service.GetCommentsAsync(ticket.Id, agentId, false);
            var resultList = result.ToList();

            // Assert
            Assert.Single(result);
            Assert.Equal("Customer comment.", resultList[0].Content);
        }

        [Fact]
        public async Task GetCommentsAsync_WhenAdmin_ShouldReturnComments()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;
            var customerId = Guid.NewGuid();
            var adminId = Guid.NewGuid();
            var ticket = new Ticket("Login issue", "Cannot login.", customerId);

            context.Tickets.Add(ticket);

            var comment = new TicketComment(ticket.Id, customerId, "Customer comment.");
            context.TicketComments.Add(comment);
            await context.SaveChangesAsync();

            var expectedDtos = new List<TicketCommentDto>
            {
                new TicketCommentDto { Id = comment.Id, TicketId = ticket.Id, UserId = customerId, Content = comment.Content }
            };

            test.MapperMock.Setup(mapper => mapper.Map<IEnumerable<TicketCommentDto>>(It.IsAny<IEnumerable<TicketComment>>())).Returns(expectedDtos);

            // Act
            var result = await test.Service.GetCommentsAsync(ticket.Id, adminId, true);
            var resultList = result.ToList();

            // Assert
            Assert.Single(result);
            Assert.Equal("Customer comment.", resultList[0].Content);
        }

        [Fact]
        public async Task GetCommentsAsync_WhenUserDoesNotHaveAccess_ShouldReturnEmptyList()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;
            var customerId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var ticket = new Ticket("Login issue", "Cannot login.", customerId);

            context.Tickets.Add(ticket);

            var comment = new TicketComment(ticket.Id, customerId, "Customer comment.");
            context.TicketComments.Add(comment);
            await context.SaveChangesAsync();

            // Act
            var result = await test.Service.GetCommentsAsync(ticket.Id, otherUserId, false);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetCommentsAsync_WhenTicketDoesNotExist_ShouldReturnEmptyList()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;
            var userId = Guid.NewGuid();

            // Act
            var result = await test.Service.GetCommentsAsync(Guid.NewGuid(), userId, false);

            // Assert
            Assert.Empty(result);
        }
    }
}
