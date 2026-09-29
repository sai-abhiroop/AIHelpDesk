using System.ComponentModel.DataAnnotations;

namespace AIHelpDesk.Application.Dtos.KnowledgeBase
{
    public class RagQueryDto
    {
        [Required]
        [MaxLength(2000)]
        public string Question { get; set; } = string.Empty;
    }
}
