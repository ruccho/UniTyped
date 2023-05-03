using Microsoft.CodeAnalysis;

namespace UniTyped.Generator.SerializationViews;

public class ManagedReferenceViewDefinition : RuntimeViewDefinition
{
    public override bool IsDirectAccess => true;

    private ITypeSymbol type;

    public ManagedReferenceViewDefinition(ITypeSymbol type)
    {
        this.type = type;
    }

    public override void Resolve(UniTypedGeneratorContext context)
    {
    }

    public override bool Match(UniTypedGeneratorContext context, ITypeSymbol type, ViewUsage viewUsage)
    {
        return viewUsage == ViewUsage.SerializeReferenceField && SymbolEqualityComparer.Default.Equals(type, this.type);
    }

    public override string GetViewTypeSyntax(UniTypedGeneratorContext context, ITypeSymbol type)
    {
        return $"global::UniTyped.Editor.SerializedPropertyViewManagedReference<{Utils.GetFullQualifiedTypeName(context, type, false)}>";
    }
}