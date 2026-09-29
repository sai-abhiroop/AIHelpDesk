using AIHelpDesk.Application.Dtos.Ticket;
using AIHelpDesk.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AIHelpDesk.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TicketsController : ControllerBase
    {
        private readonly ITicketService _ticketService;

        public TicketsController(ITicketService ticketService)
        {
            _ticketService = ticketService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TicketDetailsDto>>> GetMyTickets()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(userIdClaim, out var customerId))
            {
                return Unauthorized();
            }

            var tickets = await _ticketService.GetMyTicketsAsync(customerId);

            return Ok(tickets);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<TicketDetailsDto>> GetTicket(Guid id)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var isAdmin = User.IsInRole("Admin");

            var ticket = await _ticketService.GetTicketByIdAsync(id, userId, isAdmin);

            if (ticket == null)
            {
                return NotFound();
            }

            return Ok(ticket);
        }

        [HttpGet("assigned")]
        [Authorize(Roles = "SupportAgent,Admin")]
        public async Task<ActionResult<IReadOnlyList<TicketDetailsDto>>> GetAssignedTickets()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(userIdClaim, out var agentId))
            {
                return Unauthorized();
            }

            var tickets = await _ticketService.GetAssignedTicketsAsync(agentId);

            return Ok(tickets);
        }

        [HttpGet("unassigned")]
        [Authorize(Roles = "SupportAgent,Admin")]
        public async Task<ActionResult<IReadOnlyList<TicketDetailsDto>>> GetUnassignedTickets()
        {
            var tickets = await _ticketService.GetUnassignedTicketsAsync();

            return Ok(tickets);
        }

        [HttpPost]
        public async Task<ActionResult<TicketDetailsDto>> CreateTicket(CreateTicketDto createTicketDto)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdClaim, out var customerId))
            {
                return Unauthorized();
            }

            var ticket = await _ticketService.CreateTicketAsync(createTicketDto, customerId);

            return CreatedAtAction(nameof(GetTicket), new { id = ticket.Id }, ticket);
        }

        [HttpPatch("{id:guid}/status")]
        [Authorize(Roles = "SupportAgent,Admin")]
        public async Task<ActionResult<TicketDetailsDto>> UpdateStatus(Guid id, UpdateTicketStatusDto dto)
        {
            var ticket = await _ticketService.UpdateTicketStatusAsync(id, dto.Status);

            if (ticket == null)
            {
                return NotFound();
            }

            return Ok(ticket);
        }

        [HttpPatch("{id:guid}/assign")]
        [Authorize(Roles = "SupportAgent,Admin")]
        public async Task<ActionResult<TicketDetailsDto>> AssignTicket(Guid id, AssignTicketDto dto)
        {
            var ticket = await _ticketService.AssignTicketAsync(id, dto.AgentId);

            if (ticket == null)
            {
                return NotFound();
            }

            return Ok(ticket);
        }
    }
}
