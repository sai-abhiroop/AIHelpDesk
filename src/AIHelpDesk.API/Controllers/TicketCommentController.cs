using AIHelpDesk.Application.Dtos.TicketComment;
using AIHelpDesk.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AIHelpDesk.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Customer,SupportAgent,Admin")]
    public class TicketCommentController : ControllerBase
    {
        private readonly ITicketCommentService _commentService;

        public TicketCommentController(ITicketCommentService commentService)
        {
            _commentService = commentService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TicketCommentDto>>> GetComments(Guid ticketId)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            bool isAdmin = User.IsInRole("Admin");
                
            var comments = await _commentService.GetCommentsAsync(ticketId, userId, isAdmin);

            return Ok(comments);
        }

        [HttpPost]
        public async Task<ActionResult<TicketCommentDto>> AddComment(Guid ticketId, CreateTicketCommentDto dto)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            bool isAdmin = User.IsInRole("Admin");

            var comment = await _commentService.AddCommentAsync(ticketId, userId, isAdmin, dto);

            if (comment == null)
            {
                return NotFound();
            }
                
            return Ok(comment);
        }
    }
}
