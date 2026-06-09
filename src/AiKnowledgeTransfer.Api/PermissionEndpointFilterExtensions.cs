namespace AiKnowledgeTransfer.Api;

using AiKnowledgeTransfer.Application.Security;
using AiKnowledgeTransfer.Contracts.Security;

internal static class PermissionEndpointFilterExtensions
{
    public static RouteHandlerBuilder RequirePermission(this RouteHandlerBuilder builder, Permission permission)
    {
        return builder.AddEndpointFilter(async (context, next) =>
        {
            var authorization = context.HttpContext.RequestServices.GetRequiredService<PermissionAuthorizationService>();
            if (!authorization.HasPermission(context.HttpContext.User, permission))
            {
                return ApiResponses.Forbidden($"Permission '{permission}' is required.");
            }

            return await next(context);
        });
    }
}
