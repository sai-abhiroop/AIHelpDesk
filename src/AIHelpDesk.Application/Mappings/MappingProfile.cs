using AIHelpDesk.Application.Dtos.Categeory;
using AIHelpDesk.Application.Dtos.KnowledgeBase;
using AIHelpDesk.Application.Dtos.Ticket;
using AIHelpDesk.Application.Dtos.TicketComment;
using AIHelpDesk.Domain.Entities;
using AutoMapper;

namespace AIHelpDesk.Application.Mappings
{
    public class MappingProfile: Profile
    {
        public MappingProfile()
        {
            CreateMap<Ticket, TicketDetailsDto>().ReverseMap();
            CreateMap<Category, CategeoryDetailsDto>().ReverseMap();
            CreateMap<TicketComment, TicketCommentDto>().ReverseMap();
            CreateMap<KnowledgeArticle, KnowledgeArticleDto>().ReverseMap();
            CreateMap<KnowledgeArticle, SearchResultDto>().ReverseMap();
        }
    }
}
