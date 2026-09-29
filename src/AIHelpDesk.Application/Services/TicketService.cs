using AIHelpDesk.Application.Dtos.Ticket;
using AIHelpDesk.Application.Interfaces;
using AIHelpDesk.Domain.Entities;
using AIHelpDesk.Domain.Enums;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace AIHelpDesk.Application.Services
{
    public class TicketService : ITicketService
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly IUserService _userService;
        public TicketService(IApplicationDbContext dbContext, IMapper mapper, IUserService userService)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _userService = userService;
        }

        public async Task<TicketDetailsDto?> ApplyAiSuggestionAsync(Guid ticketId, Guid userId, bool isAdmin, string category, string priority)
        {
            var ticket = await _dbContext.Tickets.FirstOrDefaultAsync(t => t.Id == ticketId);

            if (ticket == null)
            {
                return null;
            }

            if (!isAdmin && ticket.AssignedAgentId != userId)
            {
                return null;
            }

            var categoryEntity = await _dbContext.Categories.FirstOrDefaultAsync(c => c.IsActive && c.Name == category);

            if (categoryEntity == null)
            {
                throw new KeyNotFoundException("The selected category does not exist or is inactive.");
            }

            if (!Enum.TryParse<TicketPriority>(priority, true, out var ticketPriority))
            {
                throw new ArgumentException("The selected priority is invalid.", nameof(priority));
            }

            ticket.AssignCategory(categoryEntity.Id);
            ticket.SetPriority(ticketPriority);

            await _dbContext.SaveChangesAsync();

            return _mapper.Map<TicketDetailsDto>(ticket);
        }

        public async Task<TicketDetailsDto?> AssignTicketAsync(Guid ticketId, Guid agentId)
        {
            var ticket = await _dbContext.Tickets
                                         .FirstOrDefaultAsync(t => t.Id == ticketId);

            if (ticket == null)
            {
                return null;
            }

            var isValidAgent = await _userService.IsActiveSupportAgentAsync(agentId);

            if (!isValidAgent)
            {
                throw new InvalidOperationException("The selected user is not an active support agent.");
            }

            ticket.AssignTo(agentId);

            await _dbContext.SaveChangesAsync();

            return _mapper.Map<TicketDetailsDto>(ticket);
        }

        public async Task<TicketDetailsDto> CreateTicketAsync(CreateTicketDto createTicketDto, Guid customerId)
        {
            var ticket = new Ticket(createTicketDto.Title, createTicketDto.Description, customerId);

            if (createTicketDto.CategoryId.HasValue)
            {
                var categeoryExists = await _dbContext.Categories.AnyAsync(c => c.Id == createTicketDto.CategoryId.Value && c.IsActive);

                if (!categeoryExists)
                {
                    throw new KeyNotFoundException("The selected category does not exist or is inactive.");
                }

                ticket.AssignCategory(createTicketDto.CategoryId.Value);
            }

            _dbContext.Tickets.Add(ticket);
            await _dbContext.SaveChangesAsync();

            return _mapper.Map<TicketDetailsDto>(ticket);
        }

        public async Task<IEnumerable<TicketDetailsDto>> GetAssignedTicketsAsync(Guid agentId)
        {
            var tickets = await _dbContext.Tickets
                                          .AsNoTracking()
                                          .Where(t => t.AssignedAgentId == agentId)
                                          .OrderByDescending(t => t.CreatedAt)
                                          .ToListAsync();

            return _mapper.Map<IEnumerable<TicketDetailsDto>>(tickets);
        }

        public async Task<IEnumerable<TicketDetailsDto>> GetMyTicketsAsync(Guid customerId)
        {
            var tickets = await _dbContext.Tickets
                                         .AsNoTracking()
                                         .Where(t => t.CustomerId == customerId)
                                         .OrderByDescending(t => t.CreatedAt)
                                         .ToListAsync();

            return _mapper.Map<IEnumerable<TicketDetailsDto>>(tickets);
        }

        public async Task<TicketDetailsDto?> GetTicketByIdAsync(Guid ticketId, Guid userId, bool isAdmin)
        {
            var query = _dbContext.Tickets
                                  .AsNoTracking()
                                  .Where(t => t.Id == ticketId);

            if (!isAdmin)
            {
                query = query.Where(t => t.CustomerId == userId || t.AssignedAgentId == userId);
            }

            var ticket = await query.FirstOrDefaultAsync();

            if (ticket == null)
            {
                return null;
            }

            return _mapper.Map<TicketDetailsDto>(ticket);                              
        }

        public async Task<IEnumerable<TicketDetailsDto>> GetUnassignedTicketsAsync()
        {
            var tickets = await _dbContext.Tickets
                                          .AsNoTracking()
                                          .Where(t => t.AssignedAgentId == null)
                                          .OrderBy(t => t.CreatedAt)
                                          .ToListAsync();
            
            return _mapper.Map<IEnumerable<TicketDetailsDto>>(tickets);
        }

        public async Task<TicketDetailsDto?> UpdateTicketStatusAsync(Guid ticketId, TicketStatus status)
        {
            var ticket = await _dbContext.Tickets
                                         .FirstOrDefaultAsync(t => t.Id == ticketId);
            if(ticket == null)
            {
                return null;
            }

            switch (status)
            {
                case TicketStatus.InProgress:
                    ticket.StartProgress();
                    break;

                case TicketStatus.Resolved:
                    ticket.Resolve();
                    break;

                case TicketStatus.Closed:
                    ticket.Close();
                    break;

                case TicketStatus.Open:
                     ticket.Reopen();
                     break;

                default:
                    throw new InvalidOperationException("The requested ticket status is not supported");
            }

            await _dbContext.SaveChangesAsync();

            return _mapper.Map<TicketDetailsDto>(ticket);
        }
    }
}
