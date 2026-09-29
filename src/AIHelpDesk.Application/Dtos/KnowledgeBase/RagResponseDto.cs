using System;
using System.Collections.Generic;
using System.Text;

namespace AIHelpDesk.Application.Dtos.KnowledgeBase
{
    public class RagResponseDto
    {
        public string Answer { get; set; } = string.Empty;
        public IEnumerable<SearchResultDto> Sources { get; set; } = [];
    }
}
