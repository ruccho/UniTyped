using Microsoft.CodeAnalysis;

namespace UniTyped.Generator;

public
    class DirectValueViewDefinition : BuiltinViewDefinition
{
    public override bool IsDirectAccess => true;
    public string ViewTypeSyntax { get; }

    private ITypeSymbol FieldTypeSymbol { get; }

    public DirectValueViewDefinition(string viewTypeSyntax, ITypeSymbol fieldTypeSymbol)
    {
        ViewTypeSyntax = viewTypeSyntax;
        FieldTypeSymbol = fieldTypeSymbol;
    }

    public override bool Match(UniTypedGeneratorContext context, ITypeSymbol type)
    {
        return SymbolEqualityComparer.Default.Equals(type, FieldTypeSymbol);
    }

    public override string GetViewTypeSyntax(UniTypedGeneratorContext context, ITypeSymbol type) =>
        ViewTypeSyntax;
}