using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace AIHelpDesk.Application.Dtos.AI
{
    public class ApplyAiTicketAnalysisDto
    {
        [Required]
        public string Category { get; set; } = string.Empty;

        [Required]
        public string Priority { get; set; } = string.Empty;
    }
}
