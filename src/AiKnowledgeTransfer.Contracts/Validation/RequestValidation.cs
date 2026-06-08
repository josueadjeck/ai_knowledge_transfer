using AiKnowledgeTransfer.Contracts.Projects;
using AiKnowledgeTransfer.Contracts.Exports;
using AiKnowledgeTransfer.Contracts.Roadmaps;

namespace AiKnowledgeTransfer.Contracts.Validation;

public static class RequestValidation
{
    public static readonly IReadOnlyCollection<string> AllowedKnowledgeQualityStatuses =
    [
        "ProviderSuggested",
        "FallbackReview",
        "Uncertain",
        "NeedsClarification",
        "Verified",
        "RejectedSource",
        "HumanSeeded",
        "LegacyImported"
    ];

    public static IReadOnlyDictionary<string, string[]> Validate(CreateProjectRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        AddRequired(errors, nameof(request.Name), request.Name);
        AddRequired(errors, nameof(request.Description), request.Description);
        AddRequired(errors, nameof(request.Owner), request.Owner);

        return errors;
    }

    public static IReadOnlyDictionary<string, string[]> Validate(RegisterDocumentRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        AddRequired(errors, nameof(request.FileName), request.FileName);
        AddRequired(errors, nameof(request.ContentType), request.ContentType);
        AddRequired(errors, nameof(request.Source), request.Source);

        if (request.SizeInBytes <= 0)
        {
            errors[nameof(request.SizeInBytes)] = ["Size must be greater than zero."];
        }
        else if (request.SizeInBytes > DocumentFileValidation.MaxUploadSizeInBytes)
        {
            errors[nameof(request.SizeInBytes)] = [$"Size must not exceed {DocumentFileValidation.FormatMaxUploadSize()}."];
        }

        if (!string.IsNullOrWhiteSpace(request.FileName)
            && !string.IsNullOrWhiteSpace(request.ContentType)
            && !DocumentFileValidation.IsSupported(request.FileName, request.ContentType))
        {
            errors[nameof(request.ContentType)] = [$"Unsupported file type. Supported types: {DocumentFileValidation.SupportedFileTypesDescription}."];
        }

        return errors;
    }

    public static IReadOnlyDictionary<string, string[]> Validate(ReviewKnowledgeItemRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        AddRequired(errors, nameof(request.Reviewer), request.Reviewer);
        AddRequired(errors, nameof(request.Comment), request.Comment);
        AddKnowledgeQualityStatus(errors, nameof(request.QualityStatus), request.QualityStatus);

        return errors;
    }

    public static IReadOnlyDictionary<string, string[]> Validate(BulkReviewKnowledgeItemsRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        if (request.KnowledgeItemIds.Count == 0)
        {
            errors[nameof(request.KnowledgeItemIds)] = ["At least one knowledge item id is required."];
        }

        AddRequired(errors, nameof(request.Reviewer), request.Reviewer);
        AddRequired(errors, nameof(request.Comment), request.Comment);
        AddKnowledgeQualityStatus(errors, nameof(request.QualityStatus), request.QualityStatus);

        return errors;
    }

    public static IReadOnlyDictionary<string, string[]> Validate(ApproveExportRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        AddRequired(errors, nameof(request.FileName), request.FileName);
        AddRequired(errors, nameof(request.Reviewer), request.Reviewer);
        AddRequired(errors, nameof(request.Comment), request.Comment);

        return errors;
    }

    public static IReadOnlyDictionary<string, string[]> Validate(GenerateRoadmapRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        AddRequired(errors, nameof(request.TargetRole), request.TargetRole);

        if (request.DurationInWeeks is < 1 or > 8)
        {
            errors[nameof(request.DurationInWeeks)] = ["Duration must be between 1 and 8 weeks."];
        }

        return errors;
    }

    private static void AddRequired(IDictionary<string, string[]> errors, string fieldName, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors[fieldName] = ["Value is required."];
        }
    }

    private static void AddKnowledgeQualityStatus(IDictionary<string, string[]> errors, string fieldName, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value)
            && !AllowedKnowledgeQualityStatuses.Contains(value, StringComparer.Ordinal))
        {
            errors[fieldName] = [$"QualityStatus must be one of: {string.Join(", ", AllowedKnowledgeQualityStatuses)}."];
        }
    }
}
