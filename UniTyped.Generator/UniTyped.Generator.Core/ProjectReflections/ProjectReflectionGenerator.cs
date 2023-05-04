using System.Text;
using YamlDotNet.RepresentationModel;

namespace UniTyped.Generator.ProjectReflections;

public static class ProjectReflectionGenerator
{
    public static void GenerateViews(UniTypedGeneratorContext context, StringBuilder sourceBuilder)
    {
        var projectPath = GetProjectPathFromAnchor(context);
        var projectSettingsPath = Path.Combine(projectPath, "ProjectSettings");

        sourceBuilder.AppendLine($$"""
namespace UniTyped.Reflection
{
""");

        // tags and layers
        var tagManagerPath = Path.Combine(projectSettingsPath, "TagManager.asset");

        using var tagManagerContent = new StreamReader(tagManagerPath, Encoding.UTF8);
        var tagManagerYaml = new YamlStream();
        tagManagerYaml.Load(tagManagerContent);

        List<string> tags = new ();
        List<(int index, string name)> layers = new ();

        foreach (var doc in tagManagerYaml.Documents)
        {
            var root = (YamlMappingNode)doc.RootNode;

            if (!root.Children.TryGetValue("TagManager", out var tagManagerNode)) continue;
            if (tagManagerNode is not YamlMappingNode tagManagerNodeTyped) continue;

            if (tagManagerNodeTyped.Children.TryGetValue("tags", out var tagsNode) &&
                tagsNode is YamlSequenceNode tagsNodeTyped)
            {
                tags.AddRange(tagsNodeTyped.OfType<YamlScalarNode>().Select(t => t.Value).Where(t => t != null));
            }
            
            if (tagManagerNodeTyped.Children.TryGetValue("layers", out var layersNode) &&
                layersNode is YamlSequenceNode layersNodeTyped)
            {
                int i = 0;
                foreach (var layerNode in layersNodeTyped)
                {
                    try
                    {
                        if (layerNode is not YamlScalarNode layerNodeTyped) continue;
                        if (string.IsNullOrEmpty(layerNodeTyped.Value)) continue;
                        
                        layers.Add((i, layerNodeTyped.Value));
                    }
                    finally
                    {
                        i++;
                    }
                }
            }

        }

        // tags
        {

            sourceBuilder.AppendLine($$"""
    public enum Tags
    {
""");

            foreach (var tag in tags)
            {
                string identifierName = Utils.ToIdentifierCompatible(tag);
                sourceBuilder.AppendLine($$"""
        @{{identifierName}}, // {{tag}}
""");
            }

            sourceBuilder.AppendLine($$"""
    } // enum Tags
""");
        }

        // layers
        
        sourceBuilder.AppendLine($$"""
    public enum Layers
    {
""");

        foreach (var layer in layers)
        {
            string identifierName = Utils.ToIdentifierCompatible(layer.name);
            sourceBuilder.AppendLine($$"""
        @{{identifierName}} = {{layer.index.ToString()}}, // {{layer.name}}
""");
        }
        sourceBuilder.AppendLine($$"""
    } // enum Layers
""");

        sourceBuilder.AppendLine($$"""
} //namespace UniTyped.Reflection
""");
    }

    private static string GetProjectPathFromAnchor(UniTypedGeneratorContext context)
    {
        var compilation = context.Compilation;

        if (compilation.AssemblyName != "UniTyped")
            throw new InvalidOperationException(
                "Project path is only available in UniTyped runtime assembly compilation.");

        var projectAnchorSyntax = context.UniTypedProjectAnchor.DeclaringSyntaxReferences[0];
        var projectAnchorPath = projectAnchorSyntax.SyntaxTree.FilePath;

        // ProjectAnchor is located in
        //  - Packages/com.ruccho.unityped/Runtime/Scripts/ProjectAnchor.cs (in dev)
        //  - Library/PackageCache/com.ruccho.unityped/Runtime/Scripts/ProjectAnchor.cs (imported from git or package repository)
        // when imported as local dependency, correct path is not obtained with this approach.
        var packageDirectory = new DirectoryInfo(Path.GetDirectoryName(projectAnchorPath)).Parent?.Parent?.Parent;

        if (packageDirectory == null)
            throw new NullReferenceException("Project path cannot be determined from source.");

        DirectoryInfo? projectDirectory = null;
        switch (packageDirectory.Name)
        {
            case "Packages":
                projectDirectory = packageDirectory.Parent;
                break;
            case "PackageCache":
                projectDirectory = packageDirectory.Parent?.Parent;
                break;
        }

        if (projectDirectory == null)
            throw new NullReferenceException("Project path cannot be determined from source.");

        return projectDirectory.FullName;
    }
}