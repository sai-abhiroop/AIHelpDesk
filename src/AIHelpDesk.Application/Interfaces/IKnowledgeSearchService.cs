using AIHelpDesk.Application.Dtos.KnowledgeBase;
using System;
using System.Collections.Generic;
using System.Text;

namespace AIHelpDesk.Application.Interfaces
{
    public interface IKnowledgeSearchService
    {
        Task<IEnumerable<SearchResultDto>> SearchAsync(string searchTerm);
        Task<IEnumerable<SearchResultDto>> SemanticSearchAsync(string searchTerm);
        Task<IEnumerable<SearchResultDto>> HybridSearchAsync(string searchTerm);
        
    }
}
