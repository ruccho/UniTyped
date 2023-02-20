using Microsoft.CodeAnalysis;

namespace UniTyped.Generator;

public class TypeParameterViewDefinition : BuiltinViewDefinition
{
    public override bool IsDirectAccess => false;

    public override bool Match(UniTypedGeneratorContext context, ITypeSymbol type)
    {
        return type is ITypeParameterSymbol;
    }

    public override string GetViewTypeSyntax(UniTypedGeneratorContext context, ITypeSymbol type)
    {
        return type.Name;
    }
}