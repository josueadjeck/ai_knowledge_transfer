namespace AiKnowledgeTransfer.ArchitectureTests;

public sealed class LayerDependencyTests
{
    [Fact]
    public void Domain_does_not_reference_application_or_infrastructure()
    {
        var referencedAssemblies = typeof(AiKnowledgeTransfer.Domain.Projects.KnowledgeProject)
            .Assembly
            .GetReferencedAssemblies()
            .Select(assembly => assembly.Name)
            .ToArray();

        Assert.DoesNotContain("AiKnowledgeTransfer.Application", referencedAssemblies);
        Assert.DoesNotContain("AiKnowledgeTransfer.Infrastructure", referencedAssemblies);
    }

    [Fact]
    public void Application_does_not_reference_infrastructure()
    {
        var referencedAssemblies = typeof(AiKnowledgeTransfer.Application.Projects.ProjectService)
            .Assembly
            .GetReferencedAssemblies()
            .Select(assembly => assembly.Name)
            .ToArray();

        Assert.DoesNotContain("AiKnowledgeTransfer.Infrastructure", referencedAssemblies);
    }
}
