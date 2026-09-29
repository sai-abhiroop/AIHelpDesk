using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIHelpDesk.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddKnowledgeArticleEmbedding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Embedding",
                table: "KnowledgeArticles",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Embedding",
                table: "KnowledgeArticles");
        }
    }
}
