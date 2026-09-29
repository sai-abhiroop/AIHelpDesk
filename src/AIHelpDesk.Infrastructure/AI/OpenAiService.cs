#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed
using AIHelpDesk.Application.Interfaces;
using Microsoft.Extensions.Options;
using OpenAI.Responses;

namespace AIHelpDesk.Infrastructure.AI
{
    public class OpenAiService : IAiService
    {
        private readonly AiSettings _settings;
        private readonly ResponsesClient _client;

        public OpenAiService(IOptions<AiSettings> options)
        {
            _settings = options.Value;
            _client = new(apiKey: options.Value.ApiKey);
        }

        public async Task<string> ClassifyTicketAsync(string title, string description)
        {
            return await GenerateResponseAsync($"Classify this support ticket into a short categeory.\nTitle: {title}\nDescription: {description}");
        }

        public Task<string> GenerateKnowledgeBaseAnswerAsync(string question, string context)
        {
            throw new NotImplementedException();
        }

        public async Task<string> GenerateSuggestedResponseAsync(string title, string description)
        {
            return await GenerateResponseAsync($"Generate a profession support response for this ticket. Do not claim the issue has been resolved.\nTitle: {title}\nDescription: {description}");
        }

        public async Task<string> SuggestPriorityAsync(string title, string description)
        {
            return await GenerateResponseAsync($"Suggest a priority for this support ticket. Return only one of: Low, Medium, High, Critical.\nTitle: {title}\nDescription: {description}");
        }

        public async Task<string> SummarizeTicketAsync(string title, string description)
        {
            return await GenerateResponseAsync($"Summarize the ticket information in one or two sentences.\nTitle: {title}\nDescription: {description}");
        }

        private async Task<string> GenerateResponseAsync(string prompt)
        {
            var response = await _client.CreateResponseAsync(_settings.Model, prompt);

            return response.Value.GetOutputText().Trim();
        }  
    }
}
#pragma warning restore OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
