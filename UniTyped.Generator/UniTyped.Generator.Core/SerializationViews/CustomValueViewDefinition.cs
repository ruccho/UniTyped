using System.Text;
using Microsoft.CodeAnalysis;

namespace UniTyped.Generator.SerializationViews;

public class CustomValueViewDefinition : GeneratedViewDefinition
{
    class ResolvedFieldEntry
    {
        public IFieldSymbol Field { get; }
        public TypedViewDefinition View { get; }

        public bool ForceNested { get; }

        public ResolvedFieldEntry(IFieldSymbol field, TypedViewDefinition view, bool forceNested)
        {
            Field = field;
            View = view;
            ForceNested = forceNested;
        }
    }

    private List<ResolvedFieldEntry> resolvedFields = new List<ResolvedFieldEntry>();

    public override bool IsDirectAccess => false;


    public override ITypeSymbol SourceType => TemplateTypeSymbol;
    public ITypeSymbol TemplateTypeSymbol { get; }
    public bool IsUnityEngineObject { get; }

    public CustomValueViewDefinition(UniTypedGeneratorContext context, ITypeSymbol typeSymbol)
    {
        if (typeSymbol is INamedTypeSymbol namedType && namedType.IsGenericType) typeSymbol = namedType.OriginalDefinition;
        TemplateTypeSymbol = typeSymbol;
        IsUnityEngineObject = Utils.IsDerivedFrom(typeSymbol, context.UnityEngineObject);
    }

    public override bool Match(UniTypedGeneratorContext context, ITypeSymbol type, ViewUsage viewUsage)
    {
        if (type is INamedTypeSymbol { IsGenericType: true } namedType)
            type = namedType.OriginalDefinition;

        return SymbolEqualityComparer.Default.Equals(type, TemplateTypeSymbol);
    }

    public override void Resolve(UniTypedGeneratorContext context)
    {
        var symbol = TemplateTypeSymbol;

        resolvedFields.Clear();

        while (symbol != null)
        {
            var fields = symbol.GetMembers().OfType<IFieldSymbol>();

            foreach (var field in fields)
            {
                var uniTypedField = field.GetAttributes().FirstOrDefault(a =>
                    SymbolEqualityComparer.Default.Equals(a.AttributeClass, context.UniTypedFieldAttribute));

                bool forceNested = false;
                if (uniTypedField != null)
                {
                    var forceNestedValue = (bool?)uniTypedField.NamedArguments
                        .FirstOrDefault(p => p.Key == "forceNested")
                        .Value.Value;
                    var ignoreValue = (bool?)uniTypedField.NamedArguments.FirstOrDefault(p => p.Key == "ignore")
                        .Value.Value;

                    if (ignoreValue.HasValue && ignoreValue.Value) continue;

                    if (forceNestedValue.HasValue) forceNested = forceNestedValue.Value;
                }

                //check whether being serialized

                var type = field.Type;


                if (field.IsStatic) continue;
                if (field.IsConst) continue;

                bool hasSerializeField = field.GetAttributes().Any(a =>
                    SymbolEqualityComparer.Default.Equals(a.AttributeClass, context.SerializeField));

                bool hasSerializeReference = field.GetAttributes().Any(a =>
                    SymbolEqualityComparer.Default.Equals(a.AttributeClass, context.SerializeReference));

                bool isSerializeField = hasSerializeField ||
                                        (!hasSerializeReference && field.DeclaredAccessibility == Accessibility.Public);
                bool isSerializeReference = !isSerializeField && hasSerializeReference;


                if (!isSerializeField && !isSerializeReference) continue;

                var viewType = isSerializeField
                    ? ViewUsage.SerializeField
                    : ViewUsage.SerializeReferenceField;

                TypedViewDefinition view;
                if (field.IsFixedSizeBuffer)
                {
                    if (!FixedBufferViewDefinition.Instance.Match(context, type, viewType)) continue;
                    view = FixedBufferViewDefinition.Instance;
                }
                else
                {
                    if (isSerializeField && !Utils.IsSerializableAsSerializeField(context, type)) continue;
                    view = context.GetTypedView(context, field.Type, viewType);
                }

                resolvedFields.Add(new ResolvedFieldEntry(field, view, forceNested));
            }

            symbol = symbol.BaseType;
        }
    }

    public override void GenerateViewTypeOpen(UniTypedGeneratorContext context, StringBuilder sourceBuilder)
    {
        var symbol = TemplateTypeSymbol;


        sourceBuilder.AppendLine($"    // {symbol.MetadataName}");
        sourceBuilder.Append($"    public struct {symbol.Name}View");
        {
            if (symbol is INamedTypeSymbol { IsGenericType: true } namedSymbol)
            {
                sourceBuilder.Append(Utils.ExtractTypeParameters(context, namedSymbol, false));
            }
        }

        if (!IsUnityEngineObject)
        {
            sourceBuilder.Append(" : global::UniTyped.Editor.ISerializedPropertyView");
        }

        {
            if (symbol is INamedTypeSymbol { IsGenericType: true } namedSymbol)
            {
                for (int i = 0; i < namedSymbol.TypeArguments.Length; i++)
                {
                    var param = namedSymbol.TypeArguments[i];
                    sourceBuilder.Append(" where ");
                    sourceBuilder.Append(param.Name);
                    sourceBuilder.Append(" : struct, global::UniTyped.Editor.ISerializedPropertyView");
                }
            }
        }

        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("    {");
    }

    public override void GenerateViewTypeClose(UniTypedGeneratorContext context, StringBuilder sourceBuilder)
    {
        var symbol = TemplateTypeSymbol;
        sourceBuilder.AppendLine($"    }} // struct {symbol.Name}View");
    }

    public override void GenerateViewTypeContent(UniTypedGeneratorContext context, StringBuilder sourceBuilder)
    {
        var symbol = TemplateTypeSymbol;


        if (IsUnityEngineObject)
        {
            sourceBuilder.AppendLine("""
        public global::UnityEditor.SerializedObject Target { get; set; }
""");
        }
        else
        {
            sourceBuilder.AppendLine("""
        public global::UnityEditor.SerializedProperty Property { get; set; }
""");
        }

        foreach (var fieldEntry in resolvedFields)
        {
            var field = fieldEntry.Field;

            var type = field.Type;

            var view = fieldEntry.View;
            var forceNested = fieldEntry.ForceNested;

            var name = field.Name;

            var finderSyntax = IsUnityEngineObject
                ? $"Target.FindProperty(\"{name}\")"
                : $"Property.FindPropertyRelative(\"{name}\")";

            var viewTypeSyntax = view.GetViewTypeSyntax(context, type);
            var viewPropertyName = $"__unityped__{name}";
            var backingFieldName = $"__unityped__{name}_backing";
            var viewInitialization =
                view.GenerateViewInitialization(context, field, backingFieldName, finderSyntax);


            sourceBuilder.AppendLine($$"""
        private {{viewTypeSyntax}} {{backingFieldName}};
""");
            if (!forceNested && view.IsDirectAccess)
            {
                var fullQualifiedTypeName = "global::" + Utils.GetFullQualifiedTypeName(context, type, false);

                sourceBuilder.AppendLine($$"""
        private {{viewTypeSyntax}} {{viewPropertyName}}
        {
            get
            {
{{viewInitialization}}
                return {{backingFieldName}};
            }
        }

        public readonly {{fullQualifiedTypeName}} {{name}}
        {
            get => {{viewPropertyName}}.Value;
            set
            {
                var view = {{viewPropertyName}};
                view.Value = value;
            }
        }

""");
            }
            else
            {
                sourceBuilder.AppendLine($$"""
        public {{viewTypeSyntax}} {{name}}
        {
            get
            {
{{viewInitialization}}
                return {{backingFieldName}};
            }
        }
""");
            }
        }
    }

    private static readonly StringBuilder tempStringBuilder = new StringBuilder();

    public override string GetViewTypeSyntax(UniTypedGeneratorContext context, ITypeSymbol type)
    {
        return $"global::UniTyped.Generated.{Utils.GetFullQualifiedTypeName(context, type, true, "View")}";
    }

    public override TypePath GetFullTypePath(UniTypedGeneratorContext context)
    {
        var templateType = TemplateTypeSymbol;

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
}