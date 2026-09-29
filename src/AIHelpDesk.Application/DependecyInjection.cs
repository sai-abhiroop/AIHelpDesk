using AIHelpDesk.Application.Interfaces;
using AIHelpDesk.Application.Mappings;
using AIHelpDesk.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace AIHelpDesk.Application
{
    public static class DependecyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddAutoMapper(o =>
            {
                o.AddProfile<MappingProfile>();
            });
            
            services.AddScoped<ITicketService, TicketService>();

            services.AddScoped<ICategeoryService,CategeoryService>();

            services.AddScoped<IAiTicketAnalysisService, AiTicketAnalysisService>();

            services.AddScoped<IKnowledgeArticleService, KnowledgeArticleService>();

            services.AddScoped<IKnowledgeSearchService, KnowledgeArticleService>();

            services.AddScoped<IVectorSimilarityService, VectorSimilarityService>();

            services.AddScoped<IKnowledgeRagService, KnowledgeRagService>();

            return services;
        }
    }
}
