using System.Text;
using Microsoft.CodeAnalysis;

namespace UniTyped.Generator;

public class CustomValueViewDefinition : TypedViewDefinition
{
    public override bool IsDirectAccess => false;

    public ITypeSymbol TemplateTypeSymbol { get; }
    public bool IsUnityEngineObject { get; }

    public CustomValueViewDefinition(UniTypedGeneratorContext context, ITypeSymbol typeSymbol)
    {
        var symbol = TemplateTypeSymbol = typeSymbol;
        IsUnityEngineObject = Utils.IsDerivedFrom(typeSymbol, context.UnityEngineObject);
    }

    public override bool Match(UniTypedGeneratorContext context, ITypeSymbol type)
    {
        if (type is INamedTypeSymbol { IsGenericType: true } namedType)
            type = namedType.OriginalDefinition;

        return !IsUnityEngineObject && SymbolEqualityComparer.Default.Equals(type, TemplateTypeSymbol);
    }

    public override void GenerateView(UniTypedGeneratorContext context, StringBuilder sourceBuilder)
    {
        var symbol = TemplateTypeSymbol;


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

        while (symbol != null)
        {
            var fields = symbol.GetMembers().OfType<IFieldSymbol>();

            foreach (var field in fields)
            {
                var uniTypedField = field.GetAttributes().FirstOrDefault(a =>
                    SymbolEqualityComparer.Default.Equals(a.AttributeClass, context.UniTypedField));

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
                var namedType = type as INamedTypeSymbol;

                
                if (field.IsStatic) continue;
                if (field.IsConst) continue;

                bool hasSerializeField = field.GetAttributes().Any(a =>
                    SymbolEqualityComparer.Default.Equals(a.AttributeClass, context.SerializeField));
                
                bool hasSerializeReference = field.GetAttributes().Any(a =>
                    SymbolEqualityComparer.Default.Equals(a.AttributeClass, context.SerializeReference));

                bool isSerializeField = hasSerializeField || (!hasSerializeReference && field.DeclaredAccessibility == Accessibility.Public);
                bool isSerializeReference = !isSerializeField && hasSerializeReference;
                
                sourceBuilder.AppendLine(
                    $"        //  {type.MetadataName} ({type.GetType()}) {field.MetadataName} (isSerializeField: {isSerializeField}) (isSerializeReference: {isSerializeReference})");
                
                if (!isSerializeField && !isSerializeReference) continue;

                var viewType = isSerializeField
                    ? UniTypedGeneratorContext.ViewType.SerializeField
                    : UniTypedGeneratorContext.ViewType.SerializeReferenceField;

                TypedViewDefinition view;
                if (field.IsFixedSizeBuffer)
                {
                    if (!FixedBufferViewDefinition.Instance.Match(context, type)) continue;
                    view = FixedBufferViewDefinition.Instance;
                }
                else
                {
                    if (isSerializeField && !Utils.IsSerializableType(context, type)) continue;
                    view = context.GetTypedView(context, field.Type, viewType);
                }

                if (view == null) continue;

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
                    var fullQualifiedTypeName = Utils.GetFullQualifiedTypeName(type);

                    sourceBuilder.AppendLine($$"""
        private {{viewTypeSyntax}} {{viewPropertyName}}
        {
            get
            {
{{viewInitialization}}
                return {{backingFieldName}};
            }
        }

        public {{fullQualifiedTypeName}} {{name}}
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

            symbol = symbol.BaseType;
        }

        sourceBuilder.AppendLine($"    }} // struct ");
        sourceBuilder.AppendLine($"}} // namespace ");
    }

    private static readonly StringBuilder tempStringBuilder = new StringBuilder();

    public override string GetViewTypeSyntax(UniTypedGeneratorContext context, ITypeSymbol type)
    {
        var templateType = TemplateTypeSymbol;
        var fieldType = type as INamedTypeSymbol;
        string genericsParams = "";
        if (fieldType != null && fieldType.IsGenericType)
        {
            tempStringBuilder.Clear();

            tempStringBuilder.Append("<");
            for (int i = 0; i < fieldType.TypeArguments.Length; i++)
            {
                var param = fieldType.TypeArguments[i];
                if (i > 0) tempStringBuilder.Append(", ");

                if (param is ITypeParameterSymbol)
                {
                    tempStringBuilder.Append(param.Name);
                }
                else
                {
                    tempStringBuilder.Append(context.GetTypedView(context, param)
                        .GetViewTypeSyntax(context, param));
                }
            }

            tempStringBuilder.Append(">");

            genericsParams = tempStringBuilder.ToString();
        }

        return templateType.ContainingNamespace.IsGlobalNamespace
            ? $"global::UniTyped.Generated.{templateType.Name}View{genericsParams}"
            : $"global::UniTyped.Generated.{templateType.ContainingNamespace}.{templateType.Name}View{genericsParams}";
    }
}