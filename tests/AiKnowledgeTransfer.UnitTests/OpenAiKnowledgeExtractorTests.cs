namespace AiKnowledgeTransfer.UnitTests;

using System.Net;
using System.Text;
using AiKnowledgeTransfer.Domain.Documents;
using AiKnowledgeTransfer.Infrastructure.Knowledge;

public sealed class OpenAiKnowledgeExtractorTests
{
    [Fact]
    public async Task ExtractAsync_maps_structured_response_items()
    {
        var httpClient = new HttpClient(new StubHttpMessageHandler("""
            {
              "output": [
                {
                  "content": [
                    {
                      "type": "output_text",
                      "text": "{\"items\":[{\"type\":\"GlossaryTerm\",\"title\":\"CTU\",\"summary\":\"Concentrator unit candidate.\"}]}"
                    }
                  ]
                }
              ]
            }
            """));

        var extractor = new OpenAiKnowledgeExtractor(
            httpClient,
            new OpenAiKnowledgeExtractorOptions(
                "test-key",
                "test-model",
                new Uri("https://api.openai.test/v1/")));

        var result = await extractor.ExtractAsync(
            [new DocumentChunk(1, "CTU communication must be reviewed.", 0, 35)],
            CancellationToken.None);

        var item = Assert.Single(result.Items);
        Assert.Equal("CTU", item.Title);
        Assert.Equal(Domain.Knowledge.KnowledgeItemType.GlossaryTerm, item.Type);
    }

    private sealed class StubHttpMessageHandler(string responseJson) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
            Assert.Equal("test-key", request.Headers.Authorization?.Parameter);

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            };

            return Task.FromResult(response);
        }
    }
}
