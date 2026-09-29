using AIHelpDesk.Application.Dtos.AI;
using AIHelpDesk.Application.Interfaces;
using AIHelpDesk.Domain.Enums;

namespace AIHelpDesk.Application.Services
{
    public class AiTicketAnalysisService(IAiService aiService, IApplicationDbContext dbContext) : IAiTicketAnalysisService
    {
        public async Task<AiTicketAnalysisDto?> AnalyzeExistingTicketAsync(Guid ticketId, Guid userId, bool isAdmin)
        {
            var ticket = await dbContext.Tickets.FindAsync(ticketId);

            if(ticket == null)
            {
                return null;
            }

            if(!isAdmin && ticket.AssignedAgentId != userId)
            {
                return null;
            }

            return await AnalyzeTicketAsync(ticket.Title, ticket.Description);

        }

        public async Task<AiTicketAnalysisDto> AnalyzeTicketAsync(string title, string description)
        {
            var category = await aiService.ClassifyTicketAsync(title, description);
            var priority = await aiService.SuggestPriorityAsync(title, description);
            var summary = await aiService.SummarizeTicketAsync(title, description);
            var suggestedResponse = await aiService.GenerateSuggestedResponseAsync(title, description);

            priority = ValidatePriority(priority);

            return new AiTicketAnalysisDto
            {
                Category = category,
                SuggestedPriority = priority,
                Summary = summary,
                SuggestedResponse = suggestedResponse
            };
        }
        private static string ValidatePriority(string priority)
        {
            var normalizedPriority = priority.Trim();

            foreach (var validPriority in Enum.GetNames<TicketPriority>())
            {
                if (string.Equals(normalizedPriority, validPriority, StringComparison.OrdinalIgnoreCase))
                {
                    return validPriority;
                }
            }

            throw new InvalidOperationException( $"AI returned an invalid ticket priority: '{priority}'.");
        }

    }
}
