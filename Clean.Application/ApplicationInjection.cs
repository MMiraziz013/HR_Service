using Clean.Application.Services.JWT;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Clean.Application;

public static class ApplicationInjection
{

    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddTransient<IJwtTokenService, JwtTokenService>();
        
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<JwtTokenService>(configuration.GetSection(JwtOptions.SectionName));

        return services;
    }
}