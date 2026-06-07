using AiKnowledgeTransfer.Contracts.Errors;

namespace AiKnowledgeTransfer.Api;

internal static class ApiResponses
{
    public static IResult NotFound(string message)
    {
        return Results.NotFound(new ErrorResponse("not_found", message));
    }

    public static IResult BadRequest(string message)
    {
        return Results.BadRequest(new ErrorResponse("bad_request", message));
    }

    public static IResult ValidationProblem(IReadOnlyDictionary<string, string[]> errors)
    {
        return Results.ValidationProblem(errors);
    }
}
