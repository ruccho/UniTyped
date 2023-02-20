using Microsoft.CodeAnalysis;

namespace UniTyped.Generator;

public class ArrayViewDefinition : BuiltinViewDefinition
{
    public override bool IsDirectAccess => false;

    public override bool Match(UniTypedGeneratorContext context, ITypeSymbol type)
    {
        return Utils.IsSerializableArrayOrList(context, type, out _);
    }

    public override string GetViewTypeSyntax(UniTypedGeneratorContext context, ITypeSymbol type)
    {
        Utils.IsSerializableArrayOrList(context, type, out var elementType);

        var elementView = context.GetTypedView(context, elementType ?? throw new NullReferenceException());

        return
            $"global::UniTyped.Editor.SerializedPropertyViewArray<{elementView.GetViewTypeSyntax(context, elementType)}>";
    }
}