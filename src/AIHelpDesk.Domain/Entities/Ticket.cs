using AIHelpDesk.Domain.Enums;

namespace AIHelpDesk.Domain.Entities
{
    public class Ticket
    {
        public Guid Id { get; private set; }

        public string Title { get; private set; } = string.Empty;

        public string Description { get; private set; } = string.Empty;

        public TicketStatus Status { get; private set; }

        public TicketPriority Priority { get; private set; }

        public Guid CustomerId { get; private set; }

        public Guid? AssignedAgentId { get; private set; }

        public Guid? CategoryId { get; private set; }

        public DateTime CreatedAt { get; private set; }

        public DateTime UpdatedAt { get; private set; }

        public DateTime? ResolvedAt { get; private set; }

        private Ticket()
        {

        }

        public Ticket(string title,string description,Guid customerId)
        {
            Id= Guid.NewGuid();
            
            Title= title;
            Description= description;
            
            CustomerId= customerId;

            Status = TicketStatus.Open;
            Priority=TicketPriority.Medium;

            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public void AssignTo(Guid agentId)
        {
            AssignedAgentId = agentId;
            UpdatedAt = DateTime.UtcNow;
        }

        public void StartProgress()
        {
            if (Status != TicketStatus.Open)
            {
                throw new InvalidOperationException( "Only an open ticket can be moved to in-progress.");
            }

            Status = TicketStatus.InProgress;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Resolve()
        {
            if (Status != TicketStatus.InProgress)
            {
                throw new InvalidOperationException("Only an in-progress ticket can be resolved.");
            }

            Status = TicketStatus.Resolved;
            ResolvedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Close()
        {
            if (Status != TicketStatus.Resolved)
            {
                throw new InvalidOperationException("Only a resolved ticket can be closed.");
            }

            Status = TicketStatus.Closed;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Reopen()
        {
            if (Status != TicketStatus.Resolved &&
                Status != TicketStatus.Closed)
            {
                throw new InvalidOperationException("Only a resolved or closed ticket can be reopened.");
            }

            Status = TicketStatus.Open;
            ResolvedAt = null;
            UpdatedAt = DateTime.UtcNow;
        }

        public void AssignCategory(Guid categoryId)
        {
            CategoryId = categoryId;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetPriority(TicketPriority priority)
        {
            Priority = priority;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
