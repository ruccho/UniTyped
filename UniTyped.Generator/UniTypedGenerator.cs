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
            
            if(context != null) GenerateNamespaceHolders(context, sourceBuilder);

            roslynContext.AddSource($"{assembly}.g.cs", SourceText.From(sourceBuilder.ToString(), Encoding.UTF8));
        }

        private void GenerateViews(UniTypedGeneratorContext context, StringBuilder sourceBuilder)
        {
            foreach (var uniTypedType in context.Receiver.UniTypedTypes)
            {
                var semanticModel = context.RoslynContext.Compilation.GetSemanticModel(uniTypedType.SyntaxTree);
                var symbol = semanticModel.GetDeclaredSymbol(uniTypedType) as INamedTypeSymbol;
                if (symbol == null) continue;

                context.GetOrAddObjectView(context, symbol, true);
            }

            for (int i = 0; i < context.CustomValueViews.Count; i++)
            {
                var v = context.CustomValueViews[i];
                v.GenerateView(context, sourceBuilder);
            }
        }

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
    }
}