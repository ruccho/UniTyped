using Microsoft.CodeAnalysis;

namespace UniTyped.Generator.SerializationViews;

public class UnityEngineObjectReferenceValueViewDefinition : RuntimeViewDefinition
{   
    public override bool IsDirectAccess => true;

    private readonly ITypeSymbol type;

    public UnityEngineObjectReferenceValueViewDefinition(ITypeSymbol type)
    {
        this.type = type;
    }

    public override bool Match(UniTypedGeneratorContext context, ITypeSymbol type, ViewUsage viewUsage)
    {
        return viewUsage == ViewUsage.SerializeField && SymbolEqualityComparer.Default.Equals(type, this.type);
    }

    public override void Resolve(UniTypedGeneratorContext context)
    {
    }

    public override string GetViewTypeSyntax(UniTypedGeneratorContext context, ITypeSymbol type)
    {
        return $"global::UniTyped.Editor.SerializedPropertyViewObjectReference<{Utils.GetFullQualifiedTypeName(context, type, false)}>";
    }
}