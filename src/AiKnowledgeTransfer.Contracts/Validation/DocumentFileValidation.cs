namespace AiKnowledgeTransfer.Contracts.Validation;

public static class DocumentFileValidation
{
    public const long MaxUploadSizeInBytes = 10 * 1024 * 1024;

    private static readonly string[] SupportedExtensions =
    [
        ".txt",
        ".md",
        ".markdown",
        ".pdf",
        ".docx"
    ];

    private static readonly string[] SupportedContentTypes =
    [
        "text/plain",
        "text/markdown",
        "application/markdown",
        "application/pdf",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    ];

    public static string SupportedFileTypesDescription => "TXT, Markdown, text-based PDF and Word .docx";

    public static IReadOnlyDictionary<string, string[]> Validate(string fileName, string contentType, long sizeInBytes)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(fileName))
        {
            errors[nameof(fileName)] = ["File name is required."];
        }

        if (string.IsNullOrWhiteSpace(contentType))
        {
            errors[nameof(contentType)] = ["Content type is required."];
        }

        if (sizeInBytes <= 0)
        {
            errors[nameof(sizeInBytes)] = ["File size must be greater than zero."];
        }
        else if (sizeInBytes > MaxUploadSizeInBytes)
        {
            errors[nameof(sizeInBytes)] = [$"File size must not exceed {FormatMaxUploadSize()}."];
        }

        if (!string.IsNullOrWhiteSpace(fileName)
            && !string.IsNullOrWhiteSpace(contentType)
            && !IsSupported(fileName, contentType))
        {
            errors[nameof(contentType)] = [$"Unsupported file type. Supported types: {SupportedFileTypesDescription}."];
        }

        return errors;
    }

    public static void EnsureValid(string fileName, string contentType, long sizeInBytes)
    {
        var errors = Validate(fileName, contentType, sizeInBytes);
        if (errors.Count == 0)
        {
            return;
        }

        throw new ArgumentException(string.Join(" ", errors.SelectMany(error => error.Value)));
    }

    public static bool IsSupported(string fileName, string contentType)
    {
        var extension = Path.GetExtension(fileName);

        return SupportedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase)
            || SupportedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase);
    }

    public static string FormatMaxUploadSize()
    {
        return $"{MaxUploadSizeInBytes / 1024 / 1024} MB";
    }
}
