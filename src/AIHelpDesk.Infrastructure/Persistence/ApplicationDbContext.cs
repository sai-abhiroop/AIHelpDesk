using AIHelpDesk.Application.Interfaces;
using AIHelpDesk.Domain.Entities;
using AIHelpDesk.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AIHelpDesk.Infrastructure.Persistence
{
    public class ApplicationDbContext(DbContextOptions options):IdentityDbContext<ApplicationUser,IdentityRole<Guid>,Guid>(options),IApplicationDbContext
    {
        public DbSet<Ticket> Tickets {  get; set; }

        public DbSet<Category> Categories { get; set; }

        public DbSet<TicketComment> TicketComments { get; set; }

        public DbSet<RefreshToken> RefreshTokens { get; set; }

        public DbSet<KnowledgeArticle> KnowledgeArticles { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<TicketComment>()
                   .HasOne<Ticket>()
                   .WithMany()
                   .HasForeignKey(comment => comment.TicketId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.JwtTokenId)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(x => x.RefreshTokenValue)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(x => x.TokenFamilyId)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.HasIndex(x => x.RefreshTokenValue)
                    .IsUnique();

                entity.HasIndex(x => x.UserId);
            });

            builder.Entity<KnowledgeArticle>(entity =>
            {
                entity.HasKey(x=>x.Id);
                entity.Property(x => x.Title)
                      .IsRequired()
                      .HasMaxLength(200);
                entity.Property(x => x.Content)
                      .IsRequired();
                entity.Property(x => x.IsActive)
                      .IsRequired();
                entity.Property(x => x.CreatedAt)
                      .IsRequired();
                entity.Property(x => x.UpdatedAt)
                      .IsRequired();
                entity.Property(x => x.Embedding);
            });
        }
    }
}
