using System.Text;
using Microsoft.CodeAnalysis;

namespace UniTyped.Generator;

public class ManagedReferenceViewDefinition : BuiltinViewDefinition
{
    public static readonly ManagedReferenceViewDefinition Instance = new ManagedReferenceViewDefinition();
    public override bool IsDirectAccess => true;
    public override bool Match(UniTypedGeneratorContext context, ITypeSymbol type)
    {
        return true;
    }

    public override string GetViewTypeSyntax(UniTypedGeneratorContext context, ITypeSymbol type)
    {
        return $"global::UniTyped.Editor.SerializedPropertyViewManagedReference<{Utils.GetFullQualifiedTypeName(type)}>";
    }
}