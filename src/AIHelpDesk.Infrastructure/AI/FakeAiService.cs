using AIHelpDesk.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace AIHelpDesk.Infrastructure.AI
{
    public class FakeAiService : IAiService
    {
        public Task<string> ClassifyTicketAsync(string title, string description)
        {
            return Task.FromResult("General");
        }

        public Task<string> GenerateKnowledgeBaseAnswerAsync(string question, string context)
        {
            return Task.FromResult("Based on the knowledge base, here is the answer.");
        }

        public Task<string> GenerateSuggestedResponseAsync(string title, string description)
        {
            return Task.FromResult("Thank you for contacting support. Our support team will review your request and get back to you.");
        }

        public Task<string> SuggestPriorityAsync(string title, string description)
        {
            return Task.FromResult("Medium");
        }

        public Task<string> SummarizeTicketAsync(string title, string description)
        {
            return Task.FromResult($"Summary: {description}");
        }
    }
}
