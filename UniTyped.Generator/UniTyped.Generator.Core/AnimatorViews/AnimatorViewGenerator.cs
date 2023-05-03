using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.CodeAnalysis;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

namespace UniTyped.Generator.AnimatorViews;

public static class AnimatorViewGenerator
{
    public static void GenerateViews(UniTypedGeneratorContext context, StringBuilder sourceBuilder)
    {
        sourceBuilder.AppendLine($"// AnimatorViewGenerator");

        var tempParams = new List<AnimatorControllerParameter>();


        foreach (var animatorViewType in context.Collector.AnimatorViews)
        {       
            var semanticModel = context.Compilation.GetSemanticModel(animatorViewType.SyntaxTree);
            var symbol = semanticModel.GetDeclaredSymbol(animatorViewType) as INamedTypeSymbol;
            if (symbol == null) continue;

            var attr = symbol.GetAttributes().FirstOrDefault(a =>
                SymbolEqualityComparer.Default.Equals(a.AttributeClass, context.UniTypedAnimatorViewAttribute));

            if (attr == null) continue;

            var animatorControllerPath = attr.ConstructorArguments[0].Value as string;

            if (string.IsNullOrEmpty(animatorControllerPath)) continue;

            var animatorControllerFullPath =
                $"{Path.GetDirectoryName(animatorViewType.SyntaxTree.FilePath)}/{animatorControllerPath}";

            using var contentReader = new StreamReader(animatorControllerFullPath, Encoding.UTF8);

            var yaml = new YamlStream();
            yaml.Load(contentReader);

            foreach (var doc in yaml.Documents)
            {
                var root = (YamlMappingNode)doc.RootNode;

                if (!root.Children.TryGetValue("AnimatorController", out var animatorControllerNode)) continue;

                if (animatorControllerNode is not YamlMappingNode animatorControllerNodeTyped) continue;

                if (!animatorControllerNodeTyped.Children.TryGetValue("m_AnimatorParameters", out var parametersNode))
                    continue;

                if (parametersNode is not YamlSequenceNode parametersNodeTyped) continue;

                foreach (var param in parametersNodeTyped.OfType<YamlMappingNode>())
                {
                    if (!param.Children.TryGetValue("m_Name", out var nameNode)) continue;
                    if (nameNode is not YamlScalarNode nameNodeTyped) continue;
                    if (nameNodeTyped.Value == null) continue;

                    if (!param.Children.TryGetValue("m_Type", out var typeNode)) continue;
                    if (typeNode is not YamlScalarNode typeNodeTyped) continue;
                    if (!int.TryParse(typeNodeTyped.Value, out int typeNum)) continue;

                    var type = (AnimatorControllerParameterType)typeNum;

                    tempParams.Add(new AnimatorControllerParameter(type, nameNodeTyped.Value));
                }
            }

            var ns = symbol.ContainingNamespace;
            if (!ns.IsGlobalNamespace)
            {
                sourceBuilder.AppendLine($$"""
namespace {{ns}}
{
""");
            }

            //namespace scope
            {
                sourceBuilder.AppendLine($$"""
    partial struct {{symbol.Name}}
    {
        public global::UnityEngine.Animator Target { get; set; }
""");
                foreach (var param in tempParams)
                {
                    if(!AnimatorControllerParameterProvider.Providers.TryGetValue(param.Type, out var provider)) continue;
                    
                    provider.Generate(context, sourceBuilder, param.Name);
                    
                }

                sourceBuilder.AppendLine($$"""
    }
""");


                if (!ns.IsGlobalNamespace)
                {
                    sourceBuilder.AppendLine($$"""
}
""");
                }
            }
        }
    }
}

enum AnimatorControllerParameterType
{
    Float = 1,
    Int = 3,
    Bool = 4,
    Trigger = 9,
}

class AnimatorControllerParameter
{
    public AnimatorControllerParameterType Type { get; }
    public string Name { get; }

    public AnimatorControllerParameter(AnimatorControllerParameterType type, string name)
    {
        Type = type;
        Name = name;
    }
}

abstract class AnimatorControllerParameterProvider
{
    public static IReadOnlyDictionary<AnimatorControllerParameterType, AnimatorControllerParameterProvider>
        Providers { get; } = new Dictionary<AnimatorControllerParameterType, AnimatorControllerParameterProvider>()
    {
        {
            AnimatorControllerParameterType.Float,
            new AnimatorControllerParameterFloatPropertyProvider()
        },
        {
            AnimatorControllerParameterType.Int,
            new AnimatorControllerParameterPropertyProvider("int", "{0}.GetInteger({1})",
                "{0}.SetInteger({1}, {2})")
        },
        {
            AnimatorControllerParameterType.Bool,
            new AnimatorControllerParameterPropertyProvider("bool", "{0}.GetBool({1})",
                "{0}.SetBool({1}, {2})")
        },
        {
            AnimatorControllerParameterType.Trigger,
            new AnimatorControllerParameterTriggerProvider()
        }
    };

    public abstract void Generate(UniTypedGeneratorContext context, StringBuilder sourceBuilder, string paramName);
}

class AnimatorControllerParameterTriggerProvider : AnimatorControllerParameterProvider
{
    public override void Generate(UniTypedGeneratorContext context, StringBuilder sourceBuilder, string paramName)
    {
        string target = "Target";
        string identifierName = Utils.ToIdentifierCompatible(paramName);
        string paramNameEscaped = Utils.ToCSharpEscapedVerbatimLiteral(paramName);
        
        string identifierNameForReset = Utils.ToIdentifierCompatible(paramName, false);
        if (Char.IsLower(identifierNameForReset[0])) identifierNameForReset = "_" + identifierNameForReset;

        sourceBuilder.AppendLine($$"""
        public void @{{identifierName}}()
        {
            {{target}}.SetTrigger(@"{{paramNameEscaped}}");
        }

        public void Reset{{identifierNameForReset}}()
        {
            {{target}}.ResetTrigger(@"{{paramNameEscaped}}");
        }
""");
    }
}

class AnimatorControllerParameterPropertyProvider : AnimatorControllerParameterProvider
{
    public string CSharpTypeSyntax { get; }
    public string GetterFormat { get; }
    public string SetterFormat { get; }

    public AnimatorControllerParameterPropertyProvider(string cSharpTypeSyntax, string getterFormat,
        string setterFormat)
    {
        CSharpTypeSyntax = cSharpTypeSyntax;
        GetterFormat = getterFormat;
        SetterFormat = setterFormat;
    }

    public override void Generate(UniTypedGeneratorContext context, StringBuilder sourceBuilder, string paramName)
    {
        string target = "Target";
        string identifierName = Utils.ToIdentifierCompatible(paramName);
        string paramNameLiteral = $"@\"{Utils.ToCSharpEscapedVerbatimLiteral(paramName)}\"";

        sourceBuilder.AppendLine($$"""
        public {{CSharpTypeSyntax}} @{{identifierName}} 
        {
            get
            {
                return {{string.Format(GetterFormat, target, paramNameLiteral)}};
            }

            set
            {
                {{string.Format(SetterFormat, target, paramNameLiteral, "value")}};
            }
        }
""");
    }
}

class AnimatorControllerParameterFloatPropertyProvider : AnimatorControllerParameterPropertyProvider
{
    public AnimatorControllerParameterFloatPropertyProvider() : base("float", "{0}.GetFloat({1})",
        "{0}.SetFloat({1}, {2})")
    {
    }

    public override void Generate(UniTypedGeneratorContext context, StringBuilder sourceBuilder, string paramName)
    {
        base.Generate(context, sourceBuilder, paramName);

        string target = "Target";
        string identifierName = Utils.ToIdentifierCompatible(paramName, false);
        if (Char.IsLower(identifierName[0])) identifierName = "_" + identifierName;
        string paramNameLiteral = $"@\"{Utils.ToCSharpEscapedVerbatimLiteral(paramName)}\"";

        sourceBuilder.AppendLine($$"""
        public void Set{{identifierName}}(float value, float dampTime, float deltaTime)
        {
            {{target}}.SetFloat({{paramNameLiteral}}, value, dampTime, deltaTime);
        }
""");
    }
}