using System;
using System.Collections.Generic;
using System.Text;

namespace AIHelpDesk.Application.Dtos.KnowledgeBase
{
    public class KnowledgeArticleDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
