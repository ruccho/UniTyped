using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UniTyped.Generator;

public interface IUniTypedCollector
{
    public HashSet<TypeDeclarationSyntax> UniTypedTypes { get; } 
}