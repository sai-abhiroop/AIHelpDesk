using AIHelpDesk.Application.Dtos.Ticket;
using AIHelpDesk.Application.Interfaces;
using AIHelpDesk.Application.Services;
using AIHelpDesk.Domain.Entities;
using AIHelpDesk.Domain.Enums;
using AIHelpDesk.Infrastructure.Persistence;
using AutoMapper;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace AIHelpDesk.Tests.Services
{
    public class TicketServiceTests
    {
        private static async Task<(SqliteConnection Connection, ApplicationDbContext Context, TicketService Service, Mock<IMapper> MapperMock, Mock<IUserService> UserServiceMock)> CreateTestContextAsync()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
            var context = new ApplicationDbContext(options);
            await context.Database.EnsureCreatedAsync();

            var mapperMock = new Mock<IMapper>();
            var userServiceMock = new Mock<IUserService>();
            var service = new TicketService(context, mapperMock.Object, userServiceMock.Object);

            return (connection, context, service, mapperMock, userServiceMock);
        }

        [Fact]
        public async Task CreateTicketAsync_ShouldCreateTicketSuccessfully()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;
            var customerId = Guid.NewGuid();
            var dto = new CreateTicketDto { Title = "Unable to login", Description = "I cannot log into my account." };
            var expectedDto = new TicketDetailsDto { Title = dto.Title, Description = dto.Description, CustomerId = customerId, Status = TicketStatus.Open, Priority = TicketPriority.Medium };

            test.MapperMock.Setup(mapper => mapper.Map<TicketDetailsDto>(It.IsAny<Ticket>())).Returns(expectedDto);

            // Act
            var result = await test.Service.CreateTicketAsync(dto, customerId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(dto.Title, result.Title);
            Assert.Equal(dto.Description, result.Description);
            Assert.Equal(customerId, result.CustomerId);

            var ticket = await context.Tickets.SingleAsync();
            Assert.Equal(dto.Title, ticket.Title);
            Assert.Equal(dto.Description, ticket.Description);
            Assert.Equal(customerId, ticket.CustomerId);
            Assert.Equal(TicketStatus.Open, ticket.Status);
            Assert.Equal(TicketPriority.Medium, ticket.Priority);
            Assert.Null(ticket.AssignedAgentId);
            Assert.Null(ticket.CategoryId);
        }

        [Fact]
        public async Task CreateTicketAsync_WithoutCategory_ShouldCreateTicket()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;
            var customerId = Guid.NewGuid();
            var dto = new CreateTicketDto { Title = "Payment issue", Description = "My payment failed." };

            test.MapperMock.Setup(mapper => mapper.Map<TicketDetailsDto>(It.IsAny<Ticket>())).Returns(new TicketDetailsDto());

            // Act
            await test.Service.CreateTicketAsync(dto, customerId);

            // Assert
            var ticket = await context.Tickets.SingleAsync();
            Assert.Equal(customerId, ticket.CustomerId);
            Assert.Null(ticket.CategoryId);
        }

        [Fact]
        public async Task CreateTicketAsync_WithValidCategory_ShouldAssignCategory()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;
            var customerId = Guid.NewGuid();
            var category = new Category("Billing", "Billing related issues.");

            context.Categories.Add(category);
            await context.SaveChangesAsync();

            var dto = new CreateTicketDto { Title = "Billing issue", Description = "I was charged twice.", CategoryId = category.Id };

            test.MapperMock.Setup(mapper => mapper.Map<TicketDetailsDto>(It.IsAny<Ticket>())).Returns(new TicketDetailsDto());

            // Act
            await test.Service.CreateTicketAsync(dto, customerId);

            // Assert
            var ticket = await context.Tickets.SingleAsync();
            Assert.Equal(category.Id, ticket.CategoryId);
        }

        [Fact]
        public async Task CreateTicketAsync_WithInvalidCategory_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;
            var dto = new CreateTicketDto { Title = "Billing issue", Description = "I was charged twice.", CategoryId = Guid.NewGuid() };

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() => test.Service.CreateTicketAsync(dto, Guid.NewGuid()));
            Assert.Empty(context.Tickets);
        }

        [Fact]
        public async Task CreateTicketAsync_WithInactiveCategory_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;
            var category = new Category("Billing", "Billing related issues.");
            category.Deactivate();

            context.Categories.Add(category);
            await context.SaveChangesAsync();

            var dto = new CreateTicketDto { Title = "Billing issue", Description = "I was charged twice.", CategoryId = category.Id };

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() => test.Service.CreateTicketAsync(dto, Guid.NewGuid()));
            Assert.Empty(context.Tickets);
        }

        [Fact]
        public async Task GetTicketByIdAsync_WhenCustomerOwnsTicket_ShouldReturnTicket()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;
            var customerId = Guid.NewGuid();
            var ticket = new Ticket("Login issue", "Cannot login.", customerId);

            context.Tickets.Add(ticket);
            await context.SaveChangesAsync();

            var expectedDto = new TicketDetailsDto { Id = ticket.Id, Title = ticket.Title, Description = ticket.Description, CustomerId = customerId };

            test.MapperMock.Setup(mapper => mapper.Map<TicketDetailsDto>(It.IsAny<Ticket>())).Returns(expectedDto);

            // Act
            var result = await test.Service.GetTicketByIdAsync(ticket.Id, customerId, false);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(ticket.Id, result.Id);
            Assert.Equal(ticket.Title, result.Title);
            Assert.Equal(customerId, result.CustomerId);
        }

        [Fact]
        public async Task GetTicketByIdAsync_WhenCustomerDoesNotOwnTicket_ShouldReturnNull()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;
            var ticketOwnerId = Guid.NewGuid();
            var otherCustomerId = Guid.NewGuid();
            var ticket = new Ticket("Login issue", "Cannot login.", ticketOwnerId);

            context.Tickets.Add(ticket);
            await context.SaveChangesAsync();

            // Act
            var result = await test.Service.GetTicketByIdAsync(ticket.Id, otherCustomerId, false);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetTicketByIdAsync_WhenAgentIsAssignedToTicket_ShouldReturnTicket()
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

            var expectedDto = new TicketDetailsDto { Id = ticket.Id, Title = ticket.Title, Description = ticket.Description, CustomerId = customerId, AssignedAgentId = agentId };

            test.MapperMock.Setup(mapper => mapper.Map<TicketDetailsDto>(It.IsAny<Ticket>())).Returns(expectedDto);

            // Act
            var result = await test.Service.GetTicketByIdAsync(ticket.Id, agentId, false);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(ticket.Id, result.Id);
            Assert.Equal(agentId, result.AssignedAgentId);
        }

        [Fact]
        public async Task GetTicketByIdAsync_WhenAdminRequestsTicket_ShouldReturnTicket()
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

            var expectedDto = new TicketDetailsDto { Id = ticket.Id, Title = ticket.Title, Description = ticket.Description, CustomerId = customerId };

            test.MapperMock.Setup(mapper => mapper.Map<TicketDetailsDto>(It.IsAny<Ticket>())).Returns(expectedDto);

            // Act
            var result = await test.Service.GetTicketByIdAsync(ticket.Id, adminId, true);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(ticket.Id, result.Id);
            Assert.Equal(customerId, result.CustomerId);
        }

        [Fact]
        public async Task GetTicketByIdAsync_WhenTicketDoesNotExist_ShouldReturnNull()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;

            // Act
            var result = await test.Service.GetTicketByIdAsync(Guid.NewGuid(), Guid.NewGuid(), false);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetMyTicketsAsync_ShouldReturnOnlyCustomerTickets()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;
            var customerId = Guid.NewGuid();
            var otherCustomerId = Guid.NewGuid();

            var customerTicket1 = new Ticket("Login issue", "Cannot login.", customerId);
            var customerTicket2 = new Ticket("Payment issue", "Payment failed.", customerId);
            var otherCustomerTicket = new Ticket("Booking issue", "Cannot book.", otherCustomerId);

            context.Tickets.AddRange(customerTicket1, customerTicket2, otherCustomerTicket);
            await context.SaveChangesAsync();

            var expectedDtos = new List<TicketDetailsDto>
            {
                new TicketDetailsDto { Id = customerTicket1.Id, Title = customerTicket1.Title, CustomerId = customerId },
                new TicketDetailsDto { Id = customerTicket2.Id, Title = customerTicket2.Title, CustomerId = customerId }
            };

            test.MapperMock.Setup(mapper => mapper.Map<IEnumerable<TicketDetailsDto>>(It.IsAny<IEnumerable<Ticket>>())).Returns(expectedDtos);

            // Act
            var result = await test.Service.GetMyTicketsAsync(customerId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.All(result, ticket => Assert.Equal(customerId, ticket.CustomerId));
            Assert.DoesNotContain(result, ticket => ticket.CustomerId == otherCustomerId);
        }

        [Fact]
        public async Task GetMyTicketsAsync_WhenCustomerHasNoTickets_ShouldReturnEmptyList()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;
            var customerId = Guid.NewGuid();

            // Act
            var result = await test.Service.GetMyTicketsAsync(customerId);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetMyTicketsAsync_ShouldReturnTicketsOrderedByCreatedAtDescending()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;
            var customerId = Guid.NewGuid();

            var olderTicket = new Ticket("Older ticket", "Older issue.", customerId);
            await Task.Delay(10);
            var newerTicket = new Ticket("Newer ticket", "Newer issue.", customerId);

            context.Tickets.AddRange(olderTicket, newerTicket);
            await context.SaveChangesAsync();

            var expectedDtos = new List<TicketDetailsDto>
            {
                new TicketDetailsDto { Id = newerTicket.Id, Title = newerTicket.Title, CustomerId = customerId },
                new TicketDetailsDto { Id = olderTicket.Id, Title = olderTicket.Title, CustomerId = customerId }
            };

            test.MapperMock.Setup(mapper => mapper.Map<IEnumerable<TicketDetailsDto>>(It.IsAny<IEnumerable<Ticket>>())).Returns(expectedDtos);

            // Act
            var result = await test.Service.GetMyTicketsAsync(customerId);
            var resultList = result.ToList();

            // Assert
            Assert.Equal(2, resultList.Count);
            Assert.Equal("Newer ticket", resultList[0].Title);
            Assert.Equal("Older ticket", resultList[1].Title);
        }

        [Fact]
        public async Task UpdateTicketStatusAsync_WhenTicketIsOpen_ShouldMoveToInProgress()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;
            var customerId = Guid.NewGuid();
            var ticket = new Ticket("Login issue", "Cannot login.", customerId);

            context.Tickets.Add(ticket);
            await context.SaveChangesAsync();

            var expectedDto = new TicketDetailsDto { Id = ticket.Id, Title = ticket.Title, Status = TicketStatus.InProgress };

            test.MapperMock.Setup(mapper => mapper.Map<TicketDetailsDto>(It.IsAny<Ticket>())).Returns(expectedDto);

            // Act
            var result = await test.Service.UpdateTicketStatusAsync(ticket.Id, TicketStatus.InProgress);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(TicketStatus.InProgress, ticket.Status);
            Assert.Equal(TicketStatus.InProgress, result.Status);
        }

        [Fact]
        public async Task UpdateTicketStatusAsync_WhenTicketIsInProgress_ShouldResolveTicket()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;
            var customerId = Guid.NewGuid();
            var ticket = new Ticket("Login issue", "Cannot login.", customerId);

            ticket.StartProgress();
            context.Tickets.Add(ticket);
            await context.SaveChangesAsync();

            var expectedDto = new TicketDetailsDto { Id = ticket.Id, Title = ticket.Title, Status = TicketStatus.Resolved };

            test.MapperMock.Setup(mapper => mapper.Map<TicketDetailsDto>(It.IsAny<Ticket>())).Returns(expectedDto);

            // Act
            var result = await test.Service.UpdateTicketStatusAsync(ticket.Id, TicketStatus.Resolved);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(TicketStatus.Resolved, ticket.Status);
            Assert.NotNull(ticket.ResolvedAt);
            Assert.Equal(TicketStatus.Resolved, result.Status);
        }

        [Fact]
        public async Task UpdateTicketStatusAsync_WhenTicketIsResolved_ShouldCloseTicket()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;
            var customerId = Guid.NewGuid();
            var ticket = new Ticket("Login issue", "Cannot login.", customerId);

            ticket.StartProgress();
            ticket.Resolve();
            context.Tickets.Add(ticket);
            await context.SaveChangesAsync();

            var expectedDto = new TicketDetailsDto { Id = ticket.Id, Title = ticket.Title, Status = TicketStatus.Closed };

            test.MapperMock.Setup(mapper => mapper.Map<TicketDetailsDto>(It.IsAny<Ticket>())).Returns(expectedDto);

            // Act
            var result = await test.Service.UpdateTicketStatusAsync(ticket.Id, TicketStatus.Closed);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(TicketStatus.Closed, ticket.Status);
            Assert.Equal(TicketStatus.Closed, result.Status);
        }

        [Fact]
        public async Task UpdateTicketStatusAsync_WhenTicketDoesNotExist_ShouldReturnNull()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;

            // Act
            var result = await test.Service.UpdateTicketStatusAsync(Guid.NewGuid(), TicketStatus.InProgress);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateTicketStatusAsync_WhenStatusTransitionIsInvalid_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;
            var customerId = Guid.NewGuid();
            var ticket = new Ticket("Login issue", "Cannot login.", customerId);

            context.Tickets.Add(ticket);
            await context.SaveChangesAsync();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => test.Service.UpdateTicketStatusAsync(ticket.Id, TicketStatus.Closed));
        }

        [Fact]
        public async Task UpdateTicketStatusAsync_WhenTicketIsResolved_ShouldReopenTicket()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;
            var customerId = Guid.NewGuid();
            var ticket = new Ticket("Login issue", "Cannot login.", customerId);

            ticket.StartProgress();
            ticket.Resolve();
            context.Tickets.Add(ticket);
            await context.SaveChangesAsync();

            var expectedDto = new TicketDetailsDto { Id = ticket.Id, Title = ticket.Title, Status = TicketStatus.Open };

            test.MapperMock.Setup(mapper => mapper.Map<TicketDetailsDto>(It.IsAny<Ticket>())).Returns(expectedDto);

            // Act
            var result = await test.Service.UpdateTicketStatusAsync(ticket.Id, TicketStatus.Open);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(TicketStatus.Open, ticket.Status);
            Assert.Null(ticket.ResolvedAt);
            Assert.Equal(TicketStatus.Open, result.Status);
        }

        [Fact]
        public async Task UpdateTicketStatusAsync_WhenTicketIsClosed_ShouldReopenTicket()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;
            var customerId = Guid.NewGuid();
            var ticket = new Ticket("Login issue", "Cannot login.", customerId);

            ticket.StartProgress();
            ticket.Resolve();
            ticket.Close();
            context.Tickets.Add(ticket);
            await context.SaveChangesAsync();

            var expectedDto = new TicketDetailsDto { Id = ticket.Id, Title = ticket.Title, Status = TicketStatus.Open };

            test.MapperMock.Setup(mapper => mapper.Map<TicketDetailsDto>(It.IsAny<Ticket>())).Returns(expectedDto);

            // Act
            var result = await test.Service.UpdateTicketStatusAsync(ticket.Id, TicketStatus.Open);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(TicketStatus.Open, ticket.Status);
            Assert.Null(ticket.ResolvedAt);
            Assert.Equal(TicketStatus.Open, result.Status);
        }

        [Fact]
        public async Task UpdateTicketStatusAsync_WhenTicketIsOpen_ShouldThrowForClosedStatus()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;
            var customerId = Guid.NewGuid();
            var ticket = new Ticket("Login issue", "Cannot login.", customerId);

            context.Tickets.Add(ticket);
            await context.SaveChangesAsync();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => test.Service.UpdateTicketStatusAsync(ticket.Id, TicketStatus.Closed));
        }

        [Fact]
        public async Task AssignTicketAsync_WhenAgentIsActiveSupportAgent_ShouldAssignTicket()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;
            var customerId = Guid.NewGuid();
            var agentId = Guid.NewGuid();
            var ticket = new Ticket("Login issue", "Cannot login.", customerId);

            context.Tickets.Add(ticket);
            await context.SaveChangesAsync();

            test.UserServiceMock.Setup(service => service.IsActiveSupportAgentAsync(agentId)).ReturnsAsync(true);

            var expectedDto = new TicketDetailsDto { Id = ticket.Id, Title = ticket.Title, AssignedAgentId = agentId };

            test.MapperMock.Setup(mapper => mapper.Map<TicketDetailsDto>(It.IsAny<Ticket>())).Returns(expectedDto);

            // Act
            var result = await test.Service.AssignTicketAsync(ticket.Id, agentId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(agentId, ticket.AssignedAgentId);
            Assert.Equal(agentId, result.AssignedAgentId);
        }

        [Fact]
        public async Task AssignTicketAsync_WhenTicketDoesNotExist_ShouldReturnNull()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;
            var agentId = Guid.NewGuid();

            // Act
            var result = await test.Service.AssignTicketAsync(Guid.NewGuid(), agentId);

            // Assert
            Assert.Null(result);
            test.UserServiceMock.Verify(service => service.IsActiveSupportAgentAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task AssignTicketAsync_WhenAgentIsNotActiveSupportAgent_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;
            var customerId = Guid.NewGuid();
            var agentId = Guid.NewGuid();
            var ticket = new Ticket("Login issue", "Cannot login.", customerId);

            context.Tickets.Add(ticket);
            await context.SaveChangesAsync();

            test.UserServiceMock.Setup(service => service.IsActiveSupportAgentAsync(agentId)).ReturnsAsync(false);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => test.Service.AssignTicketAsync(ticket.Id, agentId));

            Assert.Null(ticket.AssignedAgentId);
        }

        [Fact]
        public async Task AssignTicketAsync_ShouldPersistAssignedAgent()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;
            var customerId = Guid.NewGuid();
            var agentId = Guid.NewGuid();
            var ticket = new Ticket("Login issue", "Cannot login.", customerId);

            context.Tickets.Add(ticket);
            await context.SaveChangesAsync();

            test.UserServiceMock.Setup(service => service.IsActiveSupportAgentAsync(agentId)).ReturnsAsync(true);

            var expectedDto = new TicketDetailsDto { Id = ticket.Id, Title = ticket.Title, AssignedAgentId = agentId };

            test.MapperMock.Setup(mapper => mapper.Map<TicketDetailsDto>(It.IsAny<Ticket>())).Returns(expectedDto);

            // Act
            await test.Service.AssignTicketAsync(ticket.Id, agentId);

            // Assert
            var savedTicket = await context.Tickets.FindAsync(ticket.Id);

            Assert.NotNull(savedTicket);
            Assert.Equal(agentId, savedTicket.AssignedAgentId);
        }

        [Fact]
        public async Task GetUnassignedTicketsAsync_ShouldReturnOnlyUnassignedTickets()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;
            var customerId = Guid.NewGuid();
            var agentId = Guid.NewGuid();

            var unassignedTicket1 = new Ticket("Login issue", "Cannot login.", customerId);
            var unassignedTicket2 = new Ticket("Payment issue", "Payment failed.", customerId);
            var assignedTicket = new Ticket("Booking issue", "Cannot book.", customerId);

            assignedTicket.AssignTo(agentId);

            context.Tickets.AddRange(unassignedTicket1, unassignedTicket2, assignedTicket);
            await context.SaveChangesAsync();

            var expectedDtos = new List<TicketDetailsDto>
            {
                new TicketDetailsDto { Id = unassignedTicket1.Id, Title = unassignedTicket1.Title },
                new TicketDetailsDto { Id = unassignedTicket2.Id, Title = unassignedTicket2.Title }
            };

            test.MapperMock.Setup(mapper => mapper.Map<IEnumerable<TicketDetailsDto>>(It.IsAny<IEnumerable<Ticket>>())).Returns(expectedDtos);

            // Act
            var result = await test.Service.GetUnassignedTicketsAsync();

            // Assert
            Assert.Equal(2, result.Count());
            Assert.DoesNotContain(result, ticket => ticket.Id == assignedTicket.Id);
        }

        [Fact]
        public async Task GetUnassignedTicketsAsync_WhenNoUnassignedTickets_ShouldReturnEmptyList()
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

            test.MapperMock.Setup(mapper => mapper.Map<IEnumerable<TicketDetailsDto>>(It.IsAny<IEnumerable<Ticket>>())).Returns(new List<TicketDetailsDto>());

            // Act
            var result = await test.Service.GetUnassignedTicketsAsync();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAssignedTicketsAsync_ShouldReturnOnlyTicketsAssignedToAgent()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;
            var customerId = Guid.NewGuid();
            var agentId = Guid.NewGuid();
            var otherAgentId = Guid.NewGuid();

            var agentTicket1 = new Ticket("Login issue", "Cannot login.", customerId);
            var agentTicket2 = new Ticket("Payment issue", "Payment failed.", customerId);
            var otherAgentTicket = new Ticket("Booking issue", "Cannot book.", customerId);

            agentTicket1.AssignTo(agentId);
            agentTicket2.AssignTo(agentId);
            otherAgentTicket.AssignTo(otherAgentId);

            context.Tickets.AddRange(agentTicket1, agentTicket2, otherAgentTicket);
            await context.SaveChangesAsync();

            var expectedDtos = new List<TicketDetailsDto>
            {
                new TicketDetailsDto { Id = agentTicket1.Id, Title = agentTicket1.Title, AssignedAgentId = agentId },
                new TicketDetailsDto { Id = agentTicket2.Id, Title = agentTicket2.Title, AssignedAgentId = agentId }
            };

            test.MapperMock.Setup(mapper => mapper.Map<IEnumerable<TicketDetailsDto>>(It.IsAny<IEnumerable<Ticket>>())).Returns(expectedDtos);

            // Act
            var result = await test.Service.GetAssignedTicketsAsync(agentId);

            // Assert
            Assert.Equal(2, result.Count());
            Assert.All(result, ticket => Assert.Equal(agentId, ticket.AssignedAgentId));
            Assert.DoesNotContain(result, ticket => ticket.AssignedAgentId == otherAgentId);
        }

        [Fact]
        public async Task GetAssignedTicketsAsync_WhenAgentHasNoTickets_ShouldReturnEmptyList()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;
            var agentId = Guid.NewGuid();

            // Act
            var result = await test.Service.GetAssignedTicketsAsync(agentId);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetUnassignedTicketsAsync_ShouldReturnTicketsOrderedByCreatedAtAscending()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;
            var customerId = Guid.NewGuid();

            var olderTicket = new Ticket("Older ticket", "Older issue.", customerId);
            await Task.Delay(10);
            var newerTicket = new Ticket("Newer ticket", "Newer issue.", customerId);

            context.Tickets.AddRange(newerTicket, olderTicket);
            await context.SaveChangesAsync();

            var expectedDtos = new List<TicketDetailsDto>
            {
                new TicketDetailsDto { Id = olderTicket.Id, Title = olderTicket.Title },
                new TicketDetailsDto { Id = newerTicket.Id, Title = newerTicket.Title }
            };

            test.MapperMock.Setup(mapper => mapper.Map<IEnumerable<TicketDetailsDto>>(It.IsAny<IEnumerable<Ticket>>())).Returns(expectedDtos);

            // Act
            var result = await test.Service.GetUnassignedTicketsAsync();

            // Assert
            var resultList = result.ToList();
            Assert.Equal(2, resultList.Count);
            Assert.Equal("Older ticket", resultList[0].Title);
            Assert.Equal("Newer ticket", resultList[1].Title);
        }

        [Fact]
        public async Task GetAssignedTicketsAsync_ShouldReturnTicketsOrderedByCreatedAtDescending()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;
            var customerId = Guid.NewGuid();
            var agentId = Guid.NewGuid();

            var olderTicket = new Ticket("Older ticket", "Older issue.", customerId);
            olderTicket.AssignTo(agentId);

            await Task.Delay(10);

            var newerTicket = new Ticket("Newer ticket", "Newer issue.", customerId);
            newerTicket.AssignTo(agentId);

            context.Tickets.AddRange(olderTicket, newerTicket);
            await context.SaveChangesAsync();

            var expectedDtos = new List<TicketDetailsDto>
            {
                new TicketDetailsDto { Id = newerTicket.Id, Title = newerTicket.Title, AssignedAgentId = agentId },
                new TicketDetailsDto { Id = olderTicket.Id, Title = olderTicket.Title, AssignedAgentId = agentId }
            };

            test.MapperMock.Setup(mapper => mapper.Map<IEnumerable<TicketDetailsDto>>(It.IsAny<IEnumerable<Ticket>>())).Returns(expectedDtos);

            // Act
            var result = await test.Service.GetAssignedTicketsAsync(agentId);

            // Assert
            var resultList = result.ToList();
            Assert.Equal(2, resultList.Count);
            Assert.Equal("Newer ticket", resultList[0].Title);
            Assert.Equal("Older ticket", resultList[1].Title);
        }

        [Fact]
        public async Task ApplyAiSuggestionAsync_ShouldReturnNull_WhenTicketDoesNotExist()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;

            var userId = Guid.NewGuid();

            // Act
            var result = await test.Service.ApplyAiSuggestionAsync(Guid.NewGuid(), userId, false, "Account & Login", "Medium");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task ApplyAiSuggestionAsync_ShouldUpdateCategoryAndPriority()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;

            var customerId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var ticket = new Ticket("Cannot login", "User cannot access the account.", customerId);

            ticket.AssignTo(userId);

            var category = new Category("Account & Login", "Problems related to accounts, passwords, and login.");

            context.Tickets.Add(ticket);
            context.Categories.Add(category);
            await context.SaveChangesAsync();

            var expectedDto = new TicketDetailsDto
            {
                Id = ticket.Id,
                CategoryId = category.Id,
                Priority = TicketPriority.Medium
            };

            test.MapperMock
                .Setup(mapper => mapper.Map<TicketDetailsDto>(It.IsAny<Ticket>()))
                .Returns(expectedDto);

            // Act
            var result = await test.Service.ApplyAiSuggestionAsync(ticket.Id, userId, false, "Account & Login", "Medium");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(category.Id, result.CategoryId);
            Assert.Equal(TicketPriority.Medium, result.Priority);

            var savedTicket = await context.Tickets.SingleAsync();

            Assert.Equal(category.Id, savedTicket.CategoryId);
            Assert.Equal(TicketPriority.Medium, savedTicket.Priority);
        }

        [Fact]
        public async Task ApplyAiSuggestionAsync_ShouldThrow_WhenCategoryDoesNotExistOrIsInactive()
        {
            // Arrange
            var test = await CreateTestContextAsync();
            await using var connection = test.Connection;
            await using var context = test.Context;

            var userId = Guid.NewGuid();

            var ticket = new Ticket("Cannot login", "User cannot access the account.", Guid.NewGuid());
            ticket.AssignTo(userId);

            context.Tickets.Add(ticket);
            await context.SaveChangesAsync();

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() => test.Service.ApplyAiSuggestionAsync(ticket.Id, userId, false, "Account & Login", "Medium"));
        }
    }
}