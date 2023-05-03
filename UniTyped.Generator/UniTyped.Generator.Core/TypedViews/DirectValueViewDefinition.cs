using Microsoft.CodeAnalysis;

namespace UniTyped.Generator.TypedViews;

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

    public override bool Match(UniTypedGeneratorContext context, ITypeSymbol type, ViewUsage viewUsage)
    {
        return viewUsage == ViewUsage.SerializeField && SymbolEqualityComparer.Default.Equals(type, FieldTypeSymbol);
    }

    public override string GetViewTypeSyntax(UniTypedGeneratorContext context, ITypeSymbol type) =>
        ViewTypeSyntax;
}