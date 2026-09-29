using System;
using System.Collections.Generic;
using System.Text;

namespace AIHelpDesk.Application.Dtos.KnowledgeBase
{
    public class SearchResultDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public double Score { get; set; }
    }
}
