using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UniTyped.Generator
{
    [Generator]
    public class UniTypedGenerator : ISourceGenerator
    {
        public class SyntaxContextReceiver : ISyntaxContextReceiver
        {
            internal static ISyntaxContextReceiver Create()
            {
                return new SyntaxContextReceiver();
            }

            public HashSet<TypeDeclarationSyntax> UniTypedTypes { get; } = new();

            public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
            {
                var node = context.Node;

                if (node is ClassDeclarationSyntax
                    or StructDeclarationSyntax)
                {
                    var typeSyntax = (TypeDeclarationSyntax)node;
                    if (typeSyntax.AttributeLists.Count > 0)
                    {
                        if (typeSyntax.AttributeLists.SelectMany(x => x.Attributes)
                            .Any(x => x.Name.ToString() is "UniTyped" or "UniTyped.UniTyped" or "UniTypedAttribute"
                                or "UniTyped.UniTypedAttribute"))
                        {
                            UniTypedTypes.Add(typeSyntax);
                        }
                    }
                }
            }
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(SyntaxContextReceiver.Create);
        }

        public void Execute(GeneratorExecutionContext roslynContext)
        {
            var assembly = roslynContext.Compilation.AssemblyName;
            if (assembly == null) return;

            if (roslynContext.Compilation.ReferencedAssemblyNames.All(a => a.Name != "UniTyped")) return;

            var sourceBuilder = new StringBuilder();
            sourceBuilder.AppendLine("#if UNITY_EDITOR");
            sourceBuilder.AppendLine("class UniTypedGeneratedTracker {}");

            UniTypedGeneratorContext? context = null;
            try
            {
                context = new UniTypedGeneratorContext(roslynContext);

                GenerateViews(context, sourceBuilder);
            }
            catch (Exception e)
            {
                sourceBuilder.AppendLine();
                sourceBuilder.AppendLine("/*");
                sourceBuilder.AppendLine($"{e.GetType().Name}: {e.Message} {e.StackTrace}");
                sourceBuilder.AppendLine("*/");
                sourceBuilder.AppendLine();
            }

            sourceBuilder.AppendLine("#endif");

            //if(context != null) GenerateNamespaceHolders(context, sourceBuilder);

            roslynContext.AddSource($"{assembly}.g.cs", SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
        }

        private void GenerateViews(UniTypedGeneratorContext context, StringBuilder sourceBuilder)
        {
            foreach (var uniTypedType in context.Receiver.UniTypedTypes)
            {
                var semanticModel = context.RoslynContext.Compilation.GetSemanticModel(uniTypedType.SyntaxTree);
                var symbol = semanticModel.GetDeclaredSymbol(uniTypedType) as INamedTypeSymbol;
                if (symbol == null) continue;

                context.GetTypedView(context, symbol, UniTypedGeneratorContext.ViewType.Root);
            }

            for (int i = 0; i < context.GeneratedViews.Count; i++)
            {
                var v = context.GeneratedViews[i];
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

            foreach (var v in context.GeneratedViews)
            {
                var path = v.GetFullTypePath(context);
                sourceBuilder.AppendLine($"// {path}");
                var node = GetNode(path);
                node.View = v;
            }

            void PrintTree(TypePathNode node)
            {
                if (!node.IsGlobalNamesapce)
                {
                    sourceBuilder.AppendLine($"// < {node.Path.Name} ({node.Path})");
                    foreach (var c in node.Children) PrintTree(c);
                    sourceBuilder.AppendLine($"// > {node.Path.Name}");
                }
                else
                {
                    foreach (var c in node.Children) PrintTree(c);
                }
            }

            //PrintTree(globalNamespace);

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

        /*
        private void GenerateNamespaceHolders(UniTypedGeneratorContext context, StringBuilder sourceBuilder)
        {
            foreach (var ns in context.TargetNamespaces)
            {
                sourceBuilder.AppendLine(ns.IsGlobalNamespace
                    ? $"namespace UniTyped.Generated"
                    : $"namespace UniTyped.Generated.{ns}");
                sourceBuilder.AppendLine($$"""
{
    static class NamespaceHolder {}
}
""");
            }
        }
        */
    }
}