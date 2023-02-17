using System.Text;
using Microsoft.CodeAnalysis;

namespace UniTyped.Generator;

public class UnityEngineObjectReferenceValueViewDefinition : BuiltinViewDefinition
{
    public static readonly UnityEngineObjectReferenceValueViewDefinition Instance =
        new UnityEngineObjectReferenceValueViewDefinition();
    
    public override bool IsDirectAccess => true;

    public override bool Match(UniTypedGeneratorContext context, ITypeSymbol type)
    {
        return Utils.IsDerivedFrom(type, context.UnityEngineObject);
    }

    public override string GetViewTypeSyntax(UniTypedGeneratorContext context, ITypeSymbol type)
    {
        return $"global::UniTyped.Editor.SerializedPropertyViewObjectReference<{Utils.GetFullQualifiedTypeName(type)}>";
        /*
        return type.ContainingNamespace.IsGlobalNamespace
            ? $"global::UniTyped.Generated.{type.Name}View"
            : $"global::UniTyped.Generated.{type.ContainingNamespace}.{type.Name}View";
            */
    }

    /*
    public override void GenerateView(UniTypedGeneratorContext context, StringBuilder sourceBuilder)
    {
        var symbol = ObjectType;

        sourceBuilder.AppendLine($"// {symbol.MetadataName}");
        
        context.AddTargetNamespace(symbol.ContainingNamespace);

        if (symbol.ContainingNamespace.IsGlobalNamespace)
            sourceBuilder.AppendLine($"namespace UniTyped.Generated");
        else
            sourceBuilder.AppendLine($"namespace UniTyped.Generated.{symbol.ContainingNamespace}");
        sourceBuilder.AppendLine($"{{");

        sourceBuilder.Append($"    public struct {symbol.Name}View");
        {
            if (symbol is INamedTypeSymbol { IsGenericType: true } namedSymbol)
            {
                sourceBuilder.Append(Utils.ExtractTypeParameters(namedSymbol));
            }
        }

        sourceBuilder.Append(" : global::UniTyped.Editor.ISerializedPropertyView");

        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("    {");

        sourceBuilder.AppendLine($$"""
        public global::UnityEditor.SerializedProperty Property { get; set; }

        public {{Utils.GetFullQualifiedTypeName(symbol)}} Value
        {
            get => ({{Utils.GetFullQualifiedTypeName(symbol)}}) Property.objectReferenceValue;
            set => Property.objectReferenceValue = value;
        }
""");

        sourceBuilder.AppendLine($"    }} // struct ");
        sourceBuilder.AppendLine($"}} // namespace ");
    }*/
}