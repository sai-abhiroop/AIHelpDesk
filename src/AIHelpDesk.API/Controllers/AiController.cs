using AIHelpDesk.Application.Dtos.AI;
using AIHelpDesk.Application.Dtos.Ticket;
using AIHelpDesk.Application.Interfaces;
using AIHelpDesk.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIHelpDesk.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "SupportAgent,Admin")]
    public class AiController(IAiTicketAnalysisService aiTicketAnalysisService, ITicketService ticketService) : ControllerBase
    {
        [HttpPost("analyze-ticket")]
        public async Task<ActionResult<AiTicketAnalysisDto>> AnalyzeTicket(AiTicketAnalysisRequestDto dto)
        {
            var result = await aiTicketAnalysisService.AnalyzeTicketAsync(dto.Title, dto.Description);

            return Ok(result);
        }

        [HttpPost("tickets/{ticketId:guid}/analyze")]
        public async Task<ActionResult<AiTicketAnalysisDto>> AnalyzeExistingTicket(Guid ticketId)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var isAdmin = User.IsInRole("Admin");

            var result = await aiTicketAnalysisService.AnalyzeExistingTicketAsync(ticketId, userId, isAdmin);

            if (result == null)
            {
                return NotFound();
            }

            return Ok(result);
        }

        [HttpPost("tickets/{ticketId:guid}/apply-analysis")]
        public async Task<ActionResult<TicketDetailsDto>> ApplyAnalysis(Guid ticketId, ApplyAiTicketAnalysisDto dto)
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var isAdmin = User.IsInRole("Admin");

            var result = await ticketService.ApplyAiSuggestionAsync(ticketId, userId, isAdmin, dto.Category, dto.Priority);

            if (result == null)
                return NotFound();

            return Ok(result);
        }
    }
}