using AIHelpDesk.Application.Interfaces;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;

namespace AIHelpDesk.Infrastructure.AI
{
    public class OllamaAiService : IAiService
    {
        private readonly HttpClient _httpClient;
        private readonly OllamaSettings _settings;

        public OllamaAiService(HttpClient httpClient, IOptions<OllamaSettings> options)
        {
            _httpClient = httpClient;
            _settings = options.Value;
        }

        public async Task<string> ClassifyTicketAsync(string title, string description)
        {
            return await GenerateAsync("Classify this support ticket into exactly one of the following categories:\n" + "- Account & Login\n" + "- Billing\n" + "- Booking\n" + "- General Inquiry\n" + "- Technical Issue\n\n" + "Return only the category name. Do not provide an explanation.\n\n" + $"Title: {title}\nDescription: {description}");
        }

        public async Task<string> GenerateKnowledgeBaseAnswerAsync(string question, string context)
        {
            return await GenerateAsync($"Answer the user's question using only the knowledge provided below.\n\n" + $"If the knowledge does not contain enough information to answer the question, " + $"say that the information is not available in the knowledge base.\n\n" + $"Knowledge:\n{context}\n\n" + $"User Question:\n{question}");
        }

        public async Task<string> GenerateSuggestedResponseAsync(string title, string description)
        {
            return await GenerateAsync("Generate a concise and professional support response to this ticket.\n" + "Acknowledge the customer's issue and provide helpful next steps when possible.\n" + "Do not invent account details, policies, error messages, or actions already taken.\n" + "Do not ask for unnecessary personal information.\n" + "Keep the response under 150 words.\n" + "Return only the response. Do not include a subject line or placeholders such as [Your Name].\n\n" + $"Title: {title}\nDescription: {description}");
        }

        public async Task<string> SuggestPriorityAsync(string title, string description)
        {
            return await GenerateAsync("Determine the priority of this support ticket using these rules:\n" + "- Low: Minor issue with little impact.\n" + "- Medium: Normal support issue that affects the user but is not urgent.\n" + "- High: Significant issue affecting an important function or preventing normal use.\n" + "- Critical: Severe outage, security incident, widespread impact, or business-critical failure.\n\n" + "Return exactly one of: Low, Medium, High, Critical.\n" + "Return only the priority name. Do not provide an explanation.\n\n" + $"Title: {title}\nDescription: {description}");
        }

        public async Task<string> SummarizeTicketAsync(string title, string description)
        {
            return await GenerateAsync("Summarize this support ticket in 1-2 concise sentences.\n" + "Do not repeat the title or description verbatim.\n" + "Return only the summary. Do not add headings or explanations.\n\n" + $"Title: {title}\nDescription: {description}");
        }

        private async Task<string> GenerateAsync(string prompt)
        {

            var response = await _httpClient.PostAsJsonAsync($"{_settings.BaseUrl}/api/generate",
                new
                {
                    model = _settings.ChatModel,
                    prompt,
                    stream = false
                });

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>();

            return result?.Response?.Trim() ?? throw new InvalidOperationException("Ollama returned no response.");
        }

        private class OllamaGenerateResponse
        {
            public string? Response { get; set; }
        }
    }
}
