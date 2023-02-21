using Microsoft.CodeAnalysis;

namespace UniTyped.Generator;

public class SerializeFieldArrayViewDefinition : RuntimeViewDefinition
{
    public override bool IsDirectAccess => false;

    private ITypeSymbol elementType = default;
    private TypedViewDefinition resolvedElementView = default;

    public SerializeFieldArrayViewDefinition(ITypeSymbol elementType)
    {
        this.elementType = elementType;
    }

    public override bool Match(UniTypedGeneratorContext context, ITypeSymbol type, ViewUsage viewUsage)
    {
        return viewUsage == ViewUsage.SerializeField &&
               Utils.IsSerializableArrayOrList(context, type, out var elementType) &&
               SymbolEqualityComparer.Default.Equals(elementType, this.elementType);
    }

    public override void Resolve(UniTypedGeneratorContext context)
    {
        resolvedElementView = context.GetTypedView(context, elementType, ViewUsage.SerializeField);
    }

    public override string GetViewTypeSyntax(UniTypedGeneratorContext context, ITypeSymbol type)
    {
        return
            $"global::UniTyped.Editor.SerializedPropertyViewArray<{resolvedElementView.GetViewTypeSyntax(context, elementType)}>";
    }

    public override string ToString() => $"{GetType().Name} ({elementType})";
}

public class ManagedReferenceArrayViewDefinition : RuntimeViewDefinition
{
    public override bool IsDirectAccess => false;

    private ITypeSymbol elementType = default;
    private TypedViewDefinition resolvedElementView = default;

    public ManagedReferenceArrayViewDefinition(ITypeSymbol elementType)
    {
        this.elementType = elementType;
    }

    public override bool Match(UniTypedGeneratorContext context, ITypeSymbol type, ViewUsage viewUsage)
    {
        return viewUsage == ViewUsage.SerializeReferenceField &&
               Utils.IsArrayOrList(context, type, out var elementType) &&
               SymbolEqualityComparer.Default.Equals(elementType, this.elementType);
    }

    public override void Resolve(UniTypedGeneratorContext context)
    {
        resolvedElementView = context.GetTypedView(context, elementType, ViewUsage.SerializeReferenceField);
    }

    public override string GetViewTypeSyntax(UniTypedGeneratorContext context, ITypeSymbol type)
    {
        return
            $"global::UniTyped.Editor.SerializedPropertyViewArray<{resolvedElementView.GetViewTypeSyntax(context, elementType)}>";
    }
}