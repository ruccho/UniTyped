using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UniTyped.Generator;

public interface IUniTypedCollector
{
    public HashSet<TypeDeclarationSyntax> UniTypedTypes { get; } 
    public HashSet<TypeDeclarationSyntax> MaterialViews { get; }
}