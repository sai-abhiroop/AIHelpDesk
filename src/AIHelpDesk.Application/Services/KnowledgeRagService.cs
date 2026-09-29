using AIHelpDesk.Application.Dtos.KnowledgeBase;
using AIHelpDesk.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace AIHelpDesk.Application.Services
{
    public class KnowledgeRagService : IKnowledgeRagService
    {
        private readonly IKnowledgeSearchService _searchService;
        private readonly IAiService _aiService;
        private const double MinimumRelevanceScore = 0.5;

        public KnowledgeRagService(IKnowledgeSearchService searchService, IAiService aiService)
        {
            _searchService = searchService;
            _aiService = aiService;
        }

        public async Task<RagResponseDto> AskAsync(string question)
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                throw new ArgumentException("Question is required.", nameof(question));
            }

            var sources = await _searchService.HybridSearchAsync(question);

            var relevantSources = sources.Where(x => x.Score >= MinimumRelevanceScore)
                                         .Take(3)
                                         .ToList();

            if (relevantSources.Count == 0)
            {
                return new RagResponseDto
                {
                    Answer = "I could not find relevant information in the knowledge base.",
                    Sources = []
                };
            }

            var context = string.Join("\n\n", relevantSources.Select(x => $"Title: {x.Title}\nContent: {x.Content}"));

            var answer = await _aiService.GenerateKnowledgeBaseAnswerAsync(question, context);

            return new RagResponseDto
            {
                Answer = answer,
                Sources = relevantSources
            };

        }
    }
}
