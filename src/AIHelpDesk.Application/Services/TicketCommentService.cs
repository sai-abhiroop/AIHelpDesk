using AIHelpDesk.Application.Dtos.TicketComment;
using AIHelpDesk.Application.Interfaces;
using AIHelpDesk.Domain.Entities;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace AIHelpDesk.Application.Services
{
    public class TicketCommentService : ITicketCommentService
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly IMapper _mapper;

        public TicketCommentService(IApplicationDbContext dbContext, IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
        }

        public async Task<TicketCommentDto?> AddCommentAsync(Guid ticketId, Guid userId, bool isAdmin, CreateTicketCommentDto dto)
        {
            var ticket = await _dbContext.Tickets.FirstOrDefaultAsync(t => t.Id == ticketId && (isAdmin || t.CustomerId == userId || t.AssignedAgentId == userId));

            if (ticket == null)
            {
                return null;
            }

            var comment = new TicketComment(ticketId, userId, dto.Content);

            _dbContext.TicketComments.Add(comment);

            await _dbContext.SaveChangesAsync();

            return _mapper.Map<TicketCommentDto>(comment);
        }

        public async Task<IEnumerable<TicketCommentDto>> GetCommentsAsync(Guid ticketId, Guid userId, bool isAdmin)
        {
            var hasAccess = await _dbContext.Tickets.AnyAsync(t => t.Id == ticketId && (isAdmin || t.CustomerId == userId || t.AssignedAgentId == userId));

            if (!hasAccess)
            {
                return [];
            }
                
            var comments = await _dbContext.TicketComments
                                           .AsNoTracking()
                                           .Where(c => c.TicketId == ticketId)
                                           .OrderBy(c => c.CreatedAt)
                                           .ToListAsync();

            return _mapper.Map<IEnumerable<TicketCommentDto>>(comments);
        }
    }
}
