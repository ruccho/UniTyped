using Microsoft.CodeAnalysis;

namespace UniTyped.Generator;

public class FixedBufferViewDefinition : BuiltinViewDefinition
{
    public static readonly FixedBufferViewDefinition Instance = new FixedBufferViewDefinition();
    public override bool IsDirectAccess => false;

    public override bool Match(UniTypedGeneratorContext context, ITypeSymbol type, ViewUsage viewUsage)
    {
        return viewUsage == ViewUsage.SerializeField && 
            type is IPointerTypeSymbol pointerTypeSymbol &&
               Utils.IsPrimitiveSerializableType(pointerTypeSymbol.PointedAtType);
    }

    public override string GetViewTypeSyntax(UniTypedGeneratorContext context, ITypeSymbol type)
    {
        if (type is not IPointerTypeSymbol pointerTypeSymbol) throw new ArgumentException();

        var elementType = pointerTypeSymbol.PointedAtType;

        var elementView = context.GetTypedView(context, elementType, ViewUsage.SerializeField);

        return
            $"global::UniTyped.Editor.SerializedPropertyViewFixedBuffer<{elementView.GetViewTypeSyntax(context, elementType)}>";
    }
}