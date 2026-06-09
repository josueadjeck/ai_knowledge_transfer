namespace AiKnowledgeTransfer.UnitTests;

using AiKnowledgeTransfer.Application.Security;
using AiKnowledgeTransfer.Infrastructure;

public sealed class RuntimeSecretResolverTests
{
    [Fact]
    public void GetSecret_reads_secret_from_mounted_file_provider()
    {
        var root = Path.Combine(Path.GetTempPath(), "ai-knowledge-transfer-secret-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, "OPENAI_API_KEY"), "mounted-secret-value");
        var resolver = new RuntimeSecretResolver(new SecretManagementOptions(
            "SecretStore",
            "MountedFiles",
            "Operations",
            root));

        var secret = resolver.GetSecret("OPENAI_API_KEY");

        Assert.Equal("mounted-secret-value", secret);
    }

    [Fact]
    public void GetSecret_falls_back_to_environment_when_mounted_file_is_missing()
    {
        const string variableName = "AKT_TEST_SECRET_FALLBACK";
        Environment.SetEnvironmentVariable(variableName, "environment-secret-value");
        try
        {
            var root = Path.Combine(Path.GetTempPath(), "ai-knowledge-transfer-secret-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            var resolver = new RuntimeSecretResolver(new SecretManagementOptions(
                "SecretStore",
                "MountedFiles",
                "Operations",
                root));

            var secret = resolver.GetSecret(variableName);

            Assert.Equal("environment-secret-value", secret);
        }
        finally
        {
            Environment.SetEnvironmentVariable(variableName, null);
        }
    }
}
