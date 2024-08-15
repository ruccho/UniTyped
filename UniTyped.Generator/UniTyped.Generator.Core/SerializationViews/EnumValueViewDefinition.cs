using System.Text;
using Microsoft.CodeAnalysis;

namespace UniTyped.Generator.SerializationViews;

public class EnumValueViewDefinition : GeneratedViewDefinition
{
    public override bool IsDirectAccess => true;

    private ITypeSymbol EnumType { get; }

    public override ITypeSymbol SourceType => EnumType;


    public EnumValueViewDefinition(ITypeSymbol enumType)
    {
        if (enumType.TypeKind != TypeKind.Enum) throw new ArgumentException();
        EnumType = enumType;
    }

    public override bool Match(UniTypedGeneratorContext context, ITypeSymbol type, ViewUsage viewUsage)
    {
        return viewUsage == ViewUsage.SerializeField && SymbolEqualityComparer.Default.Equals(type, EnumType);
    }

    public override string GetViewTypeSyntax(UniTypedGeneratorContext context, ITypeSymbol type)
    {
        return $"global::UniTyped.Generated.{Utils.GetFullQualifiedTypeName(context, type, true, "View")}";
    }

    public override void Resolve(UniTypedGeneratorContext context)
    {
        
    }

    public override TypePath GetFullTypePath(UniTypedGeneratorContext context)
    {
        var templateType = EnumType;

        var templateTypePath = Utils.GetTypePath(templateType);
        
        var root = templateTypePath;
        while (root.Parent != null)
        {
            root = root.Parent;
        }
        
        root.Parent = new TypePath("UniTyped.Generated");
        templateTypePath.Name += "View";

        return templateTypePath;
    }

    public override void GenerateViewTypeOpen(UniTypedGeneratorContext context, StringBuilder sourceBuilder)
    {
        var symbol = EnumType;
        sourceBuilder.Append($"    public struct {symbol.Name}View");

        sourceBuilder.Append(" : global::UniTyped.Editor.ISerializedPropertyView");

        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("    {");
    }

    public override void GenerateViewTypeContent(UniTypedGeneratorContext context, StringBuilder sourceBuilder)
    {
        var symbol = EnumType;

        sourceBuilder.AppendLine($"// {symbol.MetadataName}");



        sourceBuilder.AppendLine($$"""
        public global::UnityEditor.SerializedProperty Property { get; set; }

        public global::{{Utils.GetFullQualifiedTypeName(context, symbol, false)}} Value
        {
            get => (global::{{Utils.GetFullQualifiedTypeName(context, symbol, false)}}) Property.enumValueFlag;
            set => Property.enumValueFlag = (int) value;
        }
""");

    }

    public override void GenerateViewTypeClose(UniTypedGeneratorContext context, StringBuilder sourceBuilder)
    {
        var symbol = EnumType;
        sourceBuilder.AppendLine($"    }} // struct {symbol.Name}View");
    }
}