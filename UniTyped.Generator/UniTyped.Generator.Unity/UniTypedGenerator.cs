using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UniTyped.Generator.Unity
{
    [Generator]
    public class UniTypedGenerator : ISourceGenerator
    {
        public class SyntaxContextReceiver : ISyntaxContextReceiver, IUniTypedCollector
        {
            internal static ISyntaxContextReceiver Create()
            {
                return new SyntaxContextReceiver();
            }

            public HashSet<TypeDeclarationSyntax> UniTypedTypes { get; } = new HashSet<TypeDeclarationSyntax>();
            public HashSet<TypeDeclarationSyntax> MaterialViews { get; } = new HashSet<TypeDeclarationSyntax>();

            public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
            {
                var node = context.Node;

                if (node is ClassDeclarationSyntax
                    or StructDeclarationSyntax)
                {
                    var typeSyntax = (TypeDeclarationSyntax)node;
                    if (typeSyntax.AttributeLists.Count > 0)
                    {
                        var attributes = typeSyntax.AttributeLists.SelectMany(x => x.Attributes);
                        if (attributes.Any(x => x.Name.ToString() is "UniTyped" or "UniTyped.UniTyped" or "UniTypedAttribute"
                                or "UniTyped.UniTypedAttribute"))
                        {
                            UniTypedTypes.Add(typeSyntax);
                        }

                        if (attributes.Any(x => x.Name.ToString() is "UniTypedMaterialView"
                                or "UniTypedMaterialViewAttribute" or "UniTyped.UniTypedMaterialView"
                                or "UniTyped.UniTypedMaterialViewAttribute"))
                        {
                            MaterialViews.Add(typeSyntax);
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
            if (roslynContext.SyntaxContextReceiver is not SyntaxContextReceiver receiver)
                throw new InvalidOperationException();

            string? result = Generator.UniTypedGenerator.Execute(roslynContext.Compilation, receiver);

            if (result != null)
            {
                roslynContext.AddSource($"{roslynContext.Compilation.AssemblyName}.g.cs", SourceText.From(result.ToString(), Encoding.UTF8));
            }
        }

    }
}