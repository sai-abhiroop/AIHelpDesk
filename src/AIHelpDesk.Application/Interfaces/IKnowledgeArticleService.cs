using AIHelpDesk.Application.Dtos.KnowledgeBase;
using System;
using System.Collections.Generic;
using System.Text;

namespace AIHelpDesk.Application.Interfaces
{
    public interface IKnowledgeArticleService
    {
        Task<KnowledgeArticleDto> CreateAsync(string title, string content);

        Task<KnowledgeArticleDto?> GetByIdAsync(Guid id);

        Task<IEnumerable<KnowledgeArticleDto>> GetActiveAsync();

        Task<KnowledgeArticleDto?> UpdateAsync(Guid id, string title, string content);

        Task<bool> DeactivateAsync(Guid id);

        Task<bool> ActivateAsync(Guid id);
    }
}
