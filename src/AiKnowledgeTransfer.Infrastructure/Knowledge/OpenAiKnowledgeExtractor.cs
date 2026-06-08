namespace AiKnowledgeTransfer.Infrastructure.Knowledge;

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AiKnowledgeTransfer.Application.Abstractions;
using AiKnowledgeTransfer.Domain.Documents;
using AiKnowledgeTransfer.Domain.Knowledge;

public sealed class OpenAiKnowledgeExtractor(
    HttpClient httpClient,
    OpenAiKnowledgeExtractorOptions options) : IKnowledgeExtractor
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<KnowledgeExtractionResult> ExtractAsync(
        IReadOnlyCollection<DocumentChunk> chunks,
        CancellationToken cancellationToken)
    {
        var text = string.Join(
            Environment.NewLine,
            chunks.OrderBy(chunk => chunk.ChunkNumber).Select(chunk => $"Chunk {chunk.ChunkNumber}: {chunk.Text}"));

        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(options.BaseUrl, "responses"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
        request.Content = JsonContent.Create(CreateRequestBody(text, options.Model), options: JsonOptions);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken);
        var outputText = ExtractOutputText(document.RootElement);
        var extractedItems = ParseItems(outputText);

        return new KnowledgeExtractionResult(
            extractedItems,
            "OpenAI",
            Detail: $"Model: {options.Model}");
    }

    private static object CreateRequestBody(string documentText, string model)
    {
        return new
        {
            model,
            input = new object[]
            {
                new
                {
                    role = "system",
                    content = """
                        You extract structured onboarding knowledge from technical documentation.
                        Return only grounded items. Do not invent facts.
                        Use type values only from: GlossaryTerm, Component, Workflow, OpenQuestion, Risk.
                        Mark missing or uncertain information as OpenQuestion.
                        """
                },
                new
                {
                    role = "user",
                    content = $"""
                        Extract up to 20 knowledge items from this document text.
                        Each item needs type, title and summary.

                        {documentText}
                        """
                }
            },
            text = new
            {
                format = new
                {
                    type = "json_schema",
                    name = "knowledge_extraction",
                    strict = true,
                    schema = new
                    {
                        type = "object",
                        additionalProperties = false,
                        properties = new
                        {
                            items = new
                            {
                                type = "array",
                                items = new
                                {
                                    type = "object",
                                    additionalProperties = false,
                                    properties = new
                                    {
                                        type = new
                                        {
                                            type = "string",
                                            @enum = new[] { "GlossaryTerm", "Component", "Workflow", "OpenQuestion", "Risk" }
                                        },
                                        title = new { type = "string" },
                                        summary = new { type = "string" }
                                    },
                                    required = new[] { "type", "title", "summary" }
                                }
                            }
                        },
                        required = new[] { "items" }
                    }
                }
            }
        };
    }

    private static string ExtractOutputText(JsonElement root)
    {
        if (root.TryGetProperty("output_text", out var outputText) && outputText.ValueKind == JsonValueKind.String)
        {
            return outputText.GetString() ?? string.Empty;
        }

        if (!root.TryGetProperty("output", out var output) || output.ValueKind != JsonValueKind.Array)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        foreach (var outputItem in output.EnumerateArray())
        {
            if (!outputItem.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var contentItem in content.EnumerateArray())
            {
                if (contentItem.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String)
                {
                    builder.Append(text.GetString());
                }
            }
        }

        return builder.ToString();
    }

    private static IReadOnlyCollection<ExtractedKnowledgeItem> ParseItems(string outputText)
    {
        if (string.IsNullOrWhiteSpace(outputText))
        {
            return [];
        }

        using var document = JsonDocument.Parse(outputText);
        if (!document.RootElement.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return items.EnumerateArray()
            .Select(ParseItem)
            .Where(item => item is not null)
            .Cast<ExtractedKnowledgeItem>()
            .ToArray();
    }

    private static ExtractedKnowledgeItem? ParseItem(JsonElement item)
    {
        if (!item.TryGetProperty("type", out var typeElement)
            || !item.TryGetProperty("title", out var titleElement)
            || !item.TryGetProperty("summary", out var summaryElement))
        {
            return null;
        }

        var typeText = typeElement.GetString();
        var title = titleElement.GetString();
        var summary = summaryElement.GetString();

        if (!Enum.TryParse<KnowledgeItemType>(typeText, ignoreCase: true, out var type)
            || string.IsNullOrWhiteSpace(title)
            || string.IsNullOrWhiteSpace(summary))
        {
            return null;
        }

        return new ExtractedKnowledgeItem(type, title, summary);
    }
}
