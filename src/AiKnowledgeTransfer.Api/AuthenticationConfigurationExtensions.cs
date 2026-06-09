namespace AiKnowledgeTransfer.Api;

using AiKnowledgeTransfer.Application.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

internal static class AuthenticationConfigurationExtensions
{
    public static IServiceCollection AddConfiguredApiAuthentication(
        this IServiceCollection services,
        AuthenticationOptions options)
    {
        if (!options.IsOidc || !options.IsConfigured)
        {
            return services;
        }

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(jwt =>
            {
                jwt.Authority = options.Authority;
                jwt.Audience = options.ClientId;
                jwt.MapInboundClaims = false;
                jwt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidAudience = options.ClientId,
                    NameClaimType = "name",
                    RoleClaimType = options.RoleClaimType
                };
            });

        services.AddAuthorization();

        return services;
    }

    public static IApplicationBuilder UseConfiguredApiAuthentication(
        this IApplicationBuilder app,
        AuthenticationOptions options)
    {
        if (!options.IsOidc || !options.IsConfigured)
        {
            return app;
        }

        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }
}
