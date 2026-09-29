using AIHelpDesk.Application.Dtos.Ticket;
using AIHelpDesk.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace AIHelpDesk.Application.Interfaces
{
    public interface ITicketService
    {
        Task<TicketDetailsDto> CreateTicketAsync(CreateTicketDto createTicketDto, Guid customerId);

        Task<TicketDetailsDto?> GetTicketByIdAsync(Guid ticketId, Guid userId, bool isAdmin);

        Task<IEnumerable<TicketDetailsDto>> GetMyTicketsAsync(Guid customerId);

        Task<TicketDetailsDto?> UpdateTicketStatusAsync(Guid ticketId, TicketStatus status);

        Task<TicketDetailsDto?> AssignTicketAsync(Guid ticketId, Guid agentId);

        Task<IEnumerable<TicketDetailsDto>> GetUnassignedTicketsAsync();

        Task<IEnumerable<TicketDetailsDto>> GetAssignedTicketsAsync(Guid agentId);

        Task<TicketDetailsDto?> ApplyAiSuggestionAsync(Guid ticketId, Guid userId, bool isAdmin, string category, string priority);
    }
}
