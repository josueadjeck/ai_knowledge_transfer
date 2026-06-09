namespace AiKnowledgeTransfer.Web;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using ApplicationAuthenticationOptions = AiKnowledgeTransfer.Application.Security.AuthenticationOptions;

internal static class WebAuthenticationConfigurationExtensions
{
    public static IServiceCollection AddConfiguredWebAuthentication(
        this IServiceCollection services,
        ApplicationAuthenticationOptions options)
    {
        if (!options.IsOidc || !options.IsConfigured)
        {
            return services;
        }

        services
            .AddAuthentication(authentication =>
            {
                authentication.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                authentication.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie()
            .AddOpenIdConnect(openIdConnect =>
            {
                openIdConnect.Authority = options.Authority;
                openIdConnect.ClientId = options.ClientId;
                openIdConnect.ResponseType = "code";
                openIdConnect.SaveTokens = true;
                openIdConnect.MapInboundClaims = false;
                openIdConnect.TokenValidationParameters = new TokenValidationParameters
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

    public static IApplicationBuilder UseConfiguredWebAuthentication(
        this IApplicationBuilder app,
        ApplicationAuthenticationOptions options)
    {
        if (!options.IsOidc || !options.IsConfigured)
        {
            return app;
        }

        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }

    public static IEndpointRouteBuilder MapConfiguredWebAuthenticationEndpoints(
        this IEndpointRouteBuilder endpoints,
        ApplicationAuthenticationOptions options)
    {
        if (!options.IsOidc || !options.IsConfigured)
        {
            return endpoints;
        }

        endpoints.MapGet("/auth/login", (string? returnUrl) =>
            Results.Challenge(
                new AuthenticationProperties { RedirectUri = NormalizeReturnUrl(returnUrl) },
                [OpenIdConnectDefaults.AuthenticationScheme]));

        endpoints.MapGet("/auth/logout", () =>
            Results.SignOut(
                new AuthenticationProperties { RedirectUri = "/" },
                [CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme]));

        return endpoints;
    }

    private static string NormalizeReturnUrl(string? returnUrl)
    {
        return string.IsNullOrWhiteSpace(returnUrl) || !returnUrl.StartsWith("/", StringComparison.Ordinal)
            ? "/"
            : returnUrl;
    }
}
