using AIHelpDesk.Domain.Enums;
using AIHelpDesk.Domain.Entities;

namespace AIHelpDesk.Tests.Domain
{
    public class TicketTests
    {
        private static Ticket CreateTicket()
        {
            return new Ticket("Unable to login", "I cannot log into my account.", Guid.NewGuid());
        }

        [Fact]
        public void Constructor_ShouldCreateOpenTicketWithMediumPriority()
        {
            // Arrange
            var customerId = Guid.NewGuid();

            // Act
            var ticket = new Ticket("Unable to login", "I cannot log into my account.", customerId);

            // Assert
            Assert.NotEqual(Guid.Empty, ticket.Id);
            Assert.Equal("Unable to login", ticket.Title);
            Assert.Equal("I cannot log into my account.", ticket.Description);
            Assert.Equal(customerId, ticket.CustomerId);
            Assert.Equal(TicketStatus.Open, ticket.Status);
            Assert.Equal(TicketPriority.Medium, ticket.Priority);
            Assert.Null(ticket.AssignedAgentId);
            Assert.Null(ticket.CategoryId);
            Assert.Null(ticket.ResolvedAt);
            Assert.True(ticket.CreatedAt <= DateTime.UtcNow);
            Assert.True(ticket.UpdatedAt <= DateTime.UtcNow);
        }

        [Fact]
        public void AssignCategory_ShouldSetCategory()
        {
            // Arrange
            var ticket = CreateTicket();
            var categoryId = Guid.NewGuid();

            // Act
            ticket.AssignCategory(categoryId);

            // Assert
            Assert.Equal(categoryId, ticket.CategoryId);
        }

        [Fact]
        public void AssignTo_ShouldSetAssignedAgent()
        {
            // Arrange
            var ticket = CreateTicket();
            var agentId = Guid.NewGuid();

            // Act
            ticket.AssignTo(agentId);

            // Assert
            Assert.Equal(agentId, ticket.AssignedAgentId);
        }

        [Fact]
        public void StartProgress_WhenTicketIsOpen_ShouldMoveToInProgress()
        {
            // Arrange
            var ticket = CreateTicket();

            // Act
            ticket.StartProgress();

            // Assert
            Assert.Equal(TicketStatus.InProgress, ticket.Status);
        }

        [Fact]
        public void StartProgress_WhenTicketIsNotOpen_ShouldThrow()
        {
            // Arrange
            var ticket = CreateTicket();
            ticket.StartProgress();

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => ticket.StartProgress());

            Assert.Equal("Only an open ticket can be moved to in-progress.", exception.Message);
        }

        [Fact]
        public void Resolve_WhenTicketIsInProgress_ShouldResolveTicket()
        {
            // Arrange
            var ticket = CreateTicket();
            ticket.StartProgress();

            // Act
            ticket.Resolve();

            // Assert
            Assert.Equal(TicketStatus.Resolved, ticket.Status);

            Assert.NotNull(ticket.ResolvedAt);
        }

        [Fact]
        public void Resolve_WhenTicketIsNotInProgress_ShouldThrow()
        {
            // Arrange
            var ticket = CreateTicket();

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => ticket.Resolve());

            Assert.Equal("Only an in-progress ticket can be resolved.", exception.Message);
        }

        [Fact]
        public void Close_WhenTicketIsResolved_ShouldCloseTicket()
        {
            // Arrange
            var ticket = CreateTicket();
            ticket.StartProgress();
            ticket.Resolve();

            // Act
            ticket.Close();

            // Assert
            Assert.Equal(TicketStatus.Closed, ticket.Status);
        }

        [Fact]
        public void Close_WhenTicketIsNotResolved_ShouldThrow()
        {
            // Arrange
            var ticket = CreateTicket();

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => ticket.Close());

            Assert.Equal("Only a resolved ticket can be closed.", exception.Message);
        }

        [Fact]
        public void Reopen_WhenTicketIsResolved_ShouldReopenTicket()
        {
            // Arrange
            var ticket = CreateTicket();
            ticket.StartProgress();
            ticket.Resolve();

            // Act
            ticket.Reopen();

            // Assert
            Assert.Equal(TicketStatus.Open, ticket.Status);

            Assert.Null(ticket.ResolvedAt);
        }

        [Fact]
        public void Reopen_WhenTicketIsClosed_ShouldReopenTicket()
        {
            // Arrange
            var ticket = CreateTicket();
            ticket.StartProgress();
            ticket.Resolve();
            ticket.Close();

            // Act
            ticket.Reopen();

            // Assert
            Assert.Equal(TicketStatus.Open, ticket.Status);

            Assert.Null(ticket.ResolvedAt);
        }

        [Fact]
        public void Reopen_WhenTicketIsInProgress_ShouldThrow()
        {
            // Arrange
            var ticket = CreateTicket();
            ticket.StartProgress();

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => ticket.Reopen());

            Assert.Equal("Only a resolved or closed ticket can be reopened.", exception.Message);
        }
    }
}
