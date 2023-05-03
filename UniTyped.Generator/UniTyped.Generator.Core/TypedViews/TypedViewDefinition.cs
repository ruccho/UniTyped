using System.Text;
using Microsoft.CodeAnalysis;

namespace UniTyped.Generator.TypedViews;

public abstract class TypedViewDefinition
{
    public abstract bool IsDirectAccess { get; }

    public abstract bool Match(UniTypedGeneratorContext context, ITypeSymbol type, ViewUsage viewUsage);

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
}

public abstract class RuntimeViewDefinition : TypedViewDefinition
{
    public abstract void Resolve(UniTypedGeneratorContext context);
}

public abstract class GeneratedViewDefinition : RuntimeViewDefinition
{
    public abstract ITypeSymbol SourceType { get; }

    public abstract TypePath GetFullTypePath(UniTypedGeneratorContext context);
    
    public abstract void GenerateViewTypeOpen(UniTypedGeneratorContext context, StringBuilder sourceBuilder); 

    public abstract void GenerateViewTypeContent(UniTypedGeneratorContext context, StringBuilder sourceBuilder);
    public abstract void GenerateViewTypeClose(UniTypedGeneratorContext context, StringBuilder sourceBuilder); 
}

public class UnsupportedViewDefinition : BuiltinViewDefinition
{
    public override bool IsDirectAccess => false;

    public override bool Match(UniTypedGeneratorContext context, ITypeSymbol type, ViewUsage viewUsage)
    {
        return true;
    }

    public override string GetViewTypeSyntax(UniTypedGeneratorContext context, ITypeSymbol type)
    {
        return $"global::UniTyped.Editor.SerializedPropertyViewUnsupported";
    }
}