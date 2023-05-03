using Microsoft.CodeAnalysis;

namespace UniTyped.Generator.SerializationViews;

public class TypeParameterViewDefinition : BuiltinViewDefinition
{
    public override bool IsDirectAccess => false;

    public override bool Match(UniTypedGeneratorContext context, ITypeSymbol type, ViewUsage viewUsage)
    {
        return type is ITypeParameterSymbol;
    }

    public override string GetViewTypeSyntax(UniTypedGeneratorContext context, ITypeSymbol type)
    {
        return type.Name;
    }
}