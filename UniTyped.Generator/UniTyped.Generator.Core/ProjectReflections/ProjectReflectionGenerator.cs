using System.Text;
using YamlDotNet.RepresentationModel;

namespace UniTyped.Generator.ProjectReflections;

public static class ProjectReflectionGenerator
{
    public static void GenerateViews(UniTypedGeneratorContext context, StringBuilder sourceBuilder)
    {
        sourceBuilder.AppendLine($$"""
namespace UniTyped.Reflection
{
""");

        List<string> tags = new();
        List<(int index, string name)> layers = new();
        List<(int id, string name)> sortingLayers = new();

        try
        {
            var projectPath = GetProjectPathFromAnchor(context);
            var projectSettingsPath = Path.Combine(projectPath, "ProjectSettings");

            // tags and layers
            var tagManagerPath = Path.Combine(projectSettingsPath, "TagManager.asset");

            using var tagManagerContent = new StreamReader(tagManagerPath, Encoding.UTF8);
            var tagManagerYaml = new YamlStream();
            tagManagerYaml.Load(tagManagerContent);

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
                
                if (tagManagerNodeTyped.Children.TryGetValue("m_SortingLayers", out var sortingLayersNode) &&
                    sortingLayersNode is YamlSequenceNode sortingLayersNodeTyped)
                {
                    foreach (var sortingLayerNode in sortingLayersNodeTyped)
                    {
                        if (sortingLayerNode is not YamlMappingNode sortingLayerNodeTyped) continue;

                        if (!sortingLayerNodeTyped.Children.TryGetValue("name", out var nameNode) ||
                            nameNode is not YamlScalarNode nameNodeTyped) continue;
                        if (!sortingLayerNodeTyped.Children.TryGetValue("uniqueID", out var idNode) ||
                            idNode is not YamlScalarNode idNodeTyped) continue;

                        if (string.IsNullOrEmpty(nameNodeTyped.Value)) continue;
                        if (string.IsNullOrEmpty(idNodeTyped.Value)) continue;
                        if (!int.TryParse(idNodeTyped.Value, out int id)) continue;

                        sortingLayers.Add((id, nameNodeTyped.Value));
                    }
                }
            }
        }
        finally
        {
            // tags
            {
                sourceBuilder.AppendLine($$"""
    public enum Tags
    {
""");

                for (var i = 0; i < tags.Count; i++)
                {
                    var tag = tags[i];
                    string identifierName = Utils.ToIdentifierCompatible(tag);
                    sourceBuilder.AppendLine($$"""
        /// <summary>
        /// {{tag}}
        /// </summary>
        @{{identifierName}} = {{i.ToString()}},

""");
                }

                sourceBuilder.AppendLine($$"""
    } // enum Tags

""");
            }

            // tag data
            {
                sourceBuilder.AppendLine($$"""
    internal static class TagData
    {
        public static readonly string[] tagNames =
        {
""");

                for (var i = 0; i < tags.Count; i++)
                {
                    var tag = tags[i];
                    string literalName = Utils.ToCSharpEscapedVerbatimLiteral(tag);
                    sourceBuilder.AppendLine($$"""
            @"{{literalName}}",
""");
                }

                sourceBuilder.AppendLine($$"""
        };
    } // class TagData

""");
            }

            // layers
            {
                sourceBuilder.AppendLine($$"""
    public enum Layers
    {
""");

                foreach (var layer in layers)
                {
                    string identifierName = Utils.ToIdentifierCompatible(layer.name);
                    sourceBuilder.AppendLine($$"""
        /// <summary>
        /// {{layer.name}}
        /// </summary>
        @{{identifierName}} = {{layer.index.ToString()}},

""");
                }

                sourceBuilder.AppendLine($$"""
    } // enum Layers

""");
            }

            // sorting layers
            {
                sourceBuilder.AppendLine($$"""
    public enum SortingLayers
    {
""");

                foreach (var sortingLayer in sortingLayers)
                {
                    string identifierName = Utils.ToIdentifierCompatible(sortingLayer.name);
                    sourceBuilder.AppendLine($$"""
        /// <summary>
        /// {{sortingLayer.name}}
        /// </summary>
        @{{identifierName}} = {{sortingLayer.id.ToString()}},

""");
                }

                sourceBuilder.AppendLine($$"""
    } // enum SortingLayers

""");
            }

            sourceBuilder.AppendLine($$"""
} //namespace UniTyped.Reflection

""");
        }
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