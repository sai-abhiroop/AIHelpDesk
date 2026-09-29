using System;
using System.Collections.Generic;
using System.Text;

namespace AIHelpDesk.Application.Dtos.AI
{
    public class AiTicketAnalysisDto
    {
        public string Category { get; set; } = string.Empty;
        public string SuggestedPriority { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string SuggestedResponse { get; set; } = string.Empty;
    }
}
