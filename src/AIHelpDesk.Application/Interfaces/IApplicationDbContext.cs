using AIHelpDesk.Domain.Entities;
using Microsoft.EntityFrameworkCore;


namespace AIHelpDesk.Application.Interfaces
{
    public interface IApplicationDbContext
    {
        DbSet<Ticket> Tickets { get; }

        DbSet<Category> Categories { get; }

        DbSet<TicketComment> TicketComments { get; }

        DbSet<KnowledgeArticle> KnowledgeArticles { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
