using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UniTyped.Generator
{
    public static class UniTypedGenerator
    {
        public static string? Execute(Compilation compilation, IUniTypedCollector collector)
        {
            var assembly = compilation.AssemblyName;
            if (assembly == null) return null;

            if (compilation.ReferencedAssemblyNames.All(a => a.Name != "UniTyped")) return null;

            var sourceBuilder = new StringBuilder();
            sourceBuilder.AppendLine($"// {DateTime.Now}");
            sourceBuilder.AppendLine("#if UNITY_EDITOR");
            sourceBuilder.AppendLine("class UniTypedGeneratedTracker {}");

            UniTypedGeneratorContext? context = null;
            try
            {
                context = new UniTypedGeneratorContext(compilation, collector);

                GenerateViews(context, sourceBuilder);
            }
            catch (Exception e)
            {
                sourceBuilder.AppendLine();
                sourceBuilder.AppendLine("/*");
                sourceBuilder.AppendLine($"{e.GetType().Name}: {e.Message}");
                sourceBuilder.AppendLine(e.StackTrace);
                sourceBuilder.AppendLine("*/");
                sourceBuilder.AppendLine();
            }

            sourceBuilder.AppendLine("#endif");

            return sourceBuilder.ToString();
        }

        private static void GenerateViews(UniTypedGeneratorContext context, StringBuilder sourceBuilder)
        {
            foreach (var uniTypedType in context.Collector.UniTypedTypes)
            {
                var semanticModel = context.Compilation.GetSemanticModel(uniTypedType.SyntaxTree);
                var symbol = semanticModel.GetDeclaredSymbol(uniTypedType) as INamedTypeSymbol;
                if (symbol == null) continue;

                context.GetTypedView(context, symbol, ViewUsage.Root);
            }

            for (int i = 0; i < context.RuntimeViews.Count; i++)
            {
                var v = context.RuntimeViews[i];
                v.Resolve(context);
            }

            //Build namespace tree

            var globalNamespace = new TypePathNode();

            TypePathNode GetNode(TypePath path)
            {
                TypePathNode parent;
                if (path.Parent != null)
                {
                    parent = GetNode(path.Parent);
                }
                else
                {
                    parent = globalNamespace;
                }

                var existing = parent.Children.FirstOrDefault(c => c.Path == path);
                if (existing != null) return existing;

                var node = new TypePathNode(path);
                parent.Children.Add(node);
                return node;
            }

            foreach (var v in context.RuntimeViews.OfType<GeneratedViewDefinition>())
            {
                var path = v.GetFullTypePath(context);
                sourceBuilder.AppendLine($"// {path}");
                var node = GetNode(path);
                node.View = v;
            }

            void GenerateFromTree(TypePathNode node)
            {
                if (node.IsNamespace)
                {
                    if (!node.IsGlobalNamesapce)
                    {
                        if (node.Path.TypeParams.Length > 0)
                        {
                            sourceBuilder.Append($$"""
public static class {{node.Path.Name}}<{{string.Join(", ", node.Path.TypeParams.Select(p => p.Name))}}>
""");

                            foreach (var p in node.Path.TypeParams)
                            {
                                sourceBuilder.Append($" where {p.Name} : struct, global::UniTyped.Editor.ISerializedPropertyView");
                            }

                            sourceBuilder.AppendLine();
                            
                            sourceBuilder.AppendLine($$"""
{
""");
                        }
                        else
                        {
                            sourceBuilder.AppendLine($$"""
namespace {{node.Path.Name}}
{
""");
                        }
                    }

                    foreach (var child in node.Children)
                    {
                        GenerateFromTree(child);
                        sourceBuilder.AppendLine();
                    }

                    if (!node.IsGlobalNamesapce)
                    {
                        if (node.Path.TypeParams.Length > 0)
                        {
                            sourceBuilder.AppendLine($$"""
} // class {{node.Path.Name}}
""");
                        }
                        else
                        {
                            sourceBuilder.AppendLine($$"""
} // namespace {{node.Path.Name}}
""");
                        }

                    }
                }
                else
                {
                    node.View.GenerateViewTypeOpen(context, sourceBuilder);

                    node.View.GenerateViewTypeContent(context, sourceBuilder);

                    foreach (var child in node.Children)
                    {
                        GenerateFromTree(child);
                        sourceBuilder.AppendLine();
                    }

                    node.View.GenerateViewTypeClose(context, sourceBuilder);
                }
            }

            GenerateFromTree(globalNamespace);
        }

        class TypePathNode
        {
            public TypePath? Path { get; }
            public List<TypePathNode> Children { get; } = new List<TypePathNode>();
            public GeneratedViewDefinition? View { get; set; }

            public TypePathNode(TypePath? path = null, GeneratedViewDefinition? view = null)
            {
                Path = path;
                View = view;
            }

            public bool IsGlobalNamesapce => Path == null;
            public bool IsNamespace => View == null;
        }
    }
}