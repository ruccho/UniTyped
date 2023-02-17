using System.Text;
using Microsoft.CodeAnalysis;

namespace UniTyped.Generator;

public class EnumValueViewDefinition : TypedViewDefinition
{
    public override bool IsDirectAccess => true;

    private ITypeSymbol EnumType { get; }


    public EnumValueViewDefinition(ITypeSymbol enumType)
    {
        if (enumType.TypeKind != TypeKind.Enum) throw new ArgumentException();
        EnumType = enumType;
    }

    public override bool Match(UniTypedGeneratorContext context, ITypeSymbol type)
    {
        return SymbolEqualityComparer.Default.Equals(type, EnumType);
    }

    public override string GetViewTypeSyntax(UniTypedGeneratorContext context, ITypeSymbol type)
    {
        return type.ContainingNamespace.IsGlobalNamespace
            ? $"global::UniTyped.Generated.{type.Name}View"
            : $"global::UniTyped.Generated.{type.ContainingNamespace}.{type.Name}View";
    }

    public override void GenerateView(UniTypedGeneratorContext context, StringBuilder sourceBuilder)
    {
        var symbol = EnumType;

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
            get => ({{Utils.GetFullQualifiedTypeName(symbol)}}) Property.enumValueIndex;
            set => Property.enumValueIndex = (int) value;
        }
""");

        sourceBuilder.AppendLine($"    }} // struct ");
        sourceBuilder.AppendLine($"}} // namespace ");
    }
}