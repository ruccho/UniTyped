using System.Text;
using Microsoft.CodeAnalysis;

namespace UniTyped.Generator;

public abstract class TypedViewDefinition
{
    public abstract bool IsDirectAccess { get; }

    public abstract bool Match(UniTypedGeneratorContext context, ITypeSymbol type);
    public abstract void GenerateView(UniTypedGeneratorContext context, StringBuilder sourceBuilder);

    public abstract string GetViewTypeSyntax(UniTypedGeneratorContext context, ITypeSymbol type);

    public virtual string GenerateViewInitialization(UniTypedGeneratorContext context, IFieldSymbol field,
        string backingFieldName, string finderSyntax)
    {
        return $$"""
                if (this.{{backingFieldName}}.Property == null)
                {
                    this.{{backingFieldName}} = new {{GetViewTypeSyntax(context, field.Type)}}()
                    {
                        Property = {{finderSyntax}}
                    };
                }
""";
    }
}

public abstract class BuiltinViewDefinition : TypedViewDefinition
{
    public override void GenerateView(UniTypedGeneratorContext context, StringBuilder sourceBuilder)
    {
    }
}

public abstract class GeneratedViewDefinition : TypedViewDefinition
{
}

public class UnsuuportedViewDefinition : BuiltinViewDefinition
{
    public override bool IsDirectAccess => false;

    public override bool Match(UniTypedGeneratorContext context, ITypeSymbol type)
    {
        return true;
    }

    public override string GetViewTypeSyntax(UniTypedGeneratorContext context, ITypeSymbol type)
    {
        return $"global::UniTyped.Editor.SerializedPropertyViewUnsupported";
    }
}