namespace AIHelpDesk.Domain.Entities
{
    public class KnowledgeArticle
    {
        public Guid Id { get; private set; }
        public string Title { get; private set; } = string.Empty;
        public string Content { get; private set; } = string.Empty;
        public bool IsActive { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }
        public string? Embedding { get; private set; }

        private KnowledgeArticle()
        {

        }

        public KnowledgeArticle(string title, string content)
        {
            Id = Guid.NewGuid();
            Title = title;
            Content = content;
            IsActive = true;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Update(string title, string content)
        {
            Title = title;
            Content = content;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Activate()
        {
            IsActive = true;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Deactivate()
        {
            IsActive = false;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetEmbedding(string embedding)
        {
            Embedding = embedding;
        }
    }
}
