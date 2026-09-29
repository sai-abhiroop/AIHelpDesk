using AIHelpDesk.Application.Interfaces;
using AIHelpDesk.Infrastructure.AI;
using AIHelpDesk.Infrastructure.Authentication;
using AIHelpDesk.Infrastructure.Identity;
using AIHelpDesk.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace AIHelpDesk.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services,IConfiguration configuration)
        {
            var connectionString =configuration.GetConnectionString("DefaultConnectionString")
                                               ?? throw new InvalidOperationException("Connection string 'DefaultConnectionString' was not found.");

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));

            services.AddScoped<IApplicationDbContext>(provider =>
                   provider.GetRequiredService<ApplicationDbContext>());

            services
                .AddIdentityCore<ApplicationUser>()
                .AddRoles<IdentityRole<Guid>>()
                .AddEntityFrameworkStores<ApplicationDbContext>();

            services.Configure<JwtSettings>(
                   configuration.GetSection("JwtSettings"));

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme =
                    JwtBearerDefaults.AuthenticationScheme;

                options.DefaultChallengeScheme =
                    JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
                {
                    var jwtSettings = configuration.GetSection("JwtSettings")
                                                   .Get<JwtSettings>() ?? throw new InvalidOperationException("JWT settings are not configured.");

                    var key = Encoding.UTF8.GetBytes(jwtSettings.SecretKey);

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,

                        ValidIssuer = jwtSettings.Issuer,
                        ValidAudience = jwtSettings.Audience,

                        IssuerSigningKey = new SymmetricSecurityKey(key),

                        ClockSkew = TimeSpan.Zero
                    };
                });
            services.AddAuthorization();

            services.AddScoped<ITokenService, JwtTokenService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUserService, UserService>();
            services.Configure<AiSettings>(configuration.GetSection("AI"));
            services.AddHttpClient<IEmbeddingService, OllamaEmbeddingService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(120);
            });
            services.Configure<OllamaSettings>(configuration.GetSection("Ollama"));
            services.AddHttpClient<IAiService, OllamaAiService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(120);
            });

            return services;
        }
    }
}
