using Microsoft.CodeAnalysis;

namespace UniTyped.Generator;

public class SerializeFieldArrayViewDefinition : BuiltinViewDefinition
{
    public static readonly SerializeFieldArrayViewDefinition Instance = new SerializeFieldArrayViewDefinition();
    public override bool IsDirectAccess => false;

    public override bool Match(UniTypedGeneratorContext context, ITypeSymbol type)
    {
        return Utils.IsSerializableArrayOrList(context, type, out _);
    }

    public override string GetViewTypeSyntax(UniTypedGeneratorContext context, ITypeSymbol type)
    {
        Utils.IsSerializableArrayOrList(context, type, out var elementType);

        var elementView = context.GetTypedView(context, elementType ?? throw new NullReferenceException(), UniTypedGeneratorContext.ViewType.SerializeField);

        return
            $"global::UniTyped.Editor.SerializedPropertyViewArray<{elementView.GetViewTypeSyntax(context, elementType)}>";
    }
}

public class ManagedReferenceArrayViewDefinition : BuiltinViewDefinition
{
    public static readonly ManagedReferenceArrayViewDefinition Instance = new ManagedReferenceArrayViewDefinition();
    public override bool IsDirectAccess => false;

    public override bool Match(UniTypedGeneratorContext context, ITypeSymbol type)
    {
        return Utils.IsArrayOrList(context, type, out _);
    }

    public override string GetViewTypeSyntax(UniTypedGeneratorContext context, ITypeSymbol type)
    {
        Utils.IsArrayOrList(context, type, out var elementType);

        var elementView = context.GetTypedView(context, elementType ?? throw new NullReferenceException(), UniTypedGeneratorContext.ViewType.SerializeReferenceField);

        return
            $"global::UniTyped.Editor.SerializedPropertyViewArray<{elementView.GetViewTypeSyntax(context, elementType)}>";
    }
}