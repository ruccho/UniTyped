using System.Text;
using Irony.Parsing;
using Microsoft.CodeAnalysis;

namespace UniTyped.Generator.MaterialViews;

public static class UniTypedMaterialViewGenerator
{
    public static void GenerateViews(UniTypedGeneratorContext context, StringBuilder sourceBuilder)
    {
        sourceBuilder.AppendLine($"// UniTypedMaterialViewGenerator");

        //return;

        var parser = new Parser(new ShaderlabGrammar());

        foreach (var materialViewType in context.Collector.MaterialViews)
        {
            var semanticModel = context.Compilation.GetSemanticModel(materialViewType.SyntaxTree);
            var symbol = semanticModel.GetDeclaredSymbol(materialViewType) as INamedTypeSymbol;
            if (symbol == null) continue;

            var attr = symbol.GetAttributes().FirstOrDefault(a =>
                SymbolEqualityComparer.Default.Equals(a.AttributeClass, context.UniTypedMaterialViewAttribute));

            if (attr == null) continue;

            var shaderPath = attr.ConstructorArguments[0].Value as string;

            if (shaderPath == null) continue;

            var shaderFullPath = $"{Path.GetDirectoryName(materialViewType.SyntaxTree.FilePath)}/{shaderPath}";

            var shaderContent = File.ReadAllText(shaderFullPath, Encoding.UTF8);

            var root = parser.Parse(shaderContent).Root;
            if (root.AstNode is not ShaderNode shader) continue;

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
        public global::UnityEngine.Material Target { get; set; }
""");
                foreach (var propBlock in shader.PropertiesBlocks)
                {
                    foreach (var prop in propBlock.PropertyDefinitions)
                    {
                        sourceBuilder.AppendLine($$"""
        // - {{prop.Name}} {{prop.DisplayName}}
""");

                        PropertyProvider? provider = prop.Type switch
                        {
                            PropertyTypeSimpleNode { Type: "2d" } => TexturePropertyProvider.Instance,
                            PropertyTypeSimpleNode { Type: "integer" or "int" } => SimplePropertyProvider.Integer,
                            PropertyTypeSimpleNode { Type: "float" } or PropertyTypeRangeNode => SimplePropertyProvider
                                .Float,
                            PropertyTypeSimpleNode { Type: "color" } => SimplePropertyProvider.Color,
                            PropertyTypeSimpleNode { Type: "vector" } => SimplePropertyProvider.Vector,
                            _ => null
                        };

                        if (provider == null)
                        {
                            sourceBuilder.AppendLine($$"""
        //   - skipped
""");
                            continue;
                        }

                        provider.Generate(context, sourceBuilder, prop);
                    }
                }

                sourceBuilder.AppendLine($$"""
    }
""");
            }


            if (!ns.IsGlobalNamespace)
            {
                sourceBuilder.AppendLine($$"""
}
""");
            }
        }
    }
}

public abstract class PropertyProvider
{
    public abstract void Generate(UniTypedGeneratorContext context, StringBuilder sourceBuilder, PropertyNode prop);
}

public class SimplePropertyProvider : PropertyProvider
{

    public static readonly SimplePropertyProvider Integer =
        new SimplePropertyProvider("int", "{0}.GetInt({1})", "{0}.SetInt({1}, {2})");

    public static readonly SimplePropertyProvider Float =
        new SimplePropertyProvider("float", "{0}.GetFloat({1})", "{0}.SetFloat({1}, {2})");

    public static readonly SimplePropertyProvider Color =
        new SimplePropertyProvider("global::UnityEngine.Color", "{0}.GetColor({1})", "{0}.SetColor({1}, {2})");

    public static readonly SimplePropertyProvider Vector =
        new SimplePropertyProvider("global::UnityEngine.Vector4", "{0}.GetVector({1})", "{0}.SetVector({1}, {2})");

    public string CSharpTypeSyntax { get; }
    public string GetterFormat { get; }
    public string SetterFormat { get; }

    SimplePropertyProvider(string cSharpTypeSyntax, string getterFormat, string setterFormat)
    {
        CSharpTypeSyntax = cSharpTypeSyntax;
        GetterFormat = getterFormat;
        SetterFormat = setterFormat;
    }

    public override void Generate(UniTypedGeneratorContext context, StringBuilder sourceBuilder, PropertyNode prop)
    {
        var nameIdName = $"__unityped__name_{prop.Name}";
        var target = $"Target";

        sourceBuilder.AppendLine($$"""
        private static readonly int {{nameIdName}} = global::UnityEngine.Shader.PropertyToID(@"{{prop.Name}}");
        public {{CSharpTypeSyntax}} {{prop.Name}} 
        {
            get
            {
                return {{string.Format(GetterFormat, target, nameIdName)}};
            }

            set
            {
                {{string.Format(SetterFormat, target, nameIdName, "value")}};
            }
        }
""");
    }
}

public class TexturePropertyProvider : PropertyProvider
{
    public static readonly TexturePropertyProvider Instance = new TexturePropertyProvider();

    TexturePropertyProvider()
    {
    }

    public override void Generate(UniTypedGeneratorContext context, StringBuilder sourceBuilder, PropertyNode prop)
    {
        var nameIdName = $"__unityped__name_{prop.Name}";
        var target = $"Target";

        sourceBuilder.AppendLine($$"""
        private static readonly int {{nameIdName}} = global::UnityEngine.Shader.PropertyToID(@"{{prop.Name}}");
        public global::UnityEngine.Texture {{prop.Name}} 
        {
            get
            {
                return {{target}}.GetTexture({{nameIdName}});
            }

            set
            {
                {{target}}.SetTexture({{nameIdName}}, value);
            }
        }

        public global::UnityEngine.Vector2 {{prop.Name}}_Offset
        {
            get
            {
                return {{target}}.GetTextureOffset({{nameIdName}});
            } 

            set
            {
                {{target}}.SetTextureOffset({{nameIdName}}, value);
            }
        }

        public global::UnityEngine.Vector2 {{prop.Name}}_Scale
        {
            get
            {
                return {{target}}.GetTextureScale({{nameIdName}});
            } 

            set
            {
                {{target}}.SetTextureScale({{nameIdName}}, value);
            }
        }
        public bool {{prop.Name}}_Exists
        {
            get
            {
                return {{target}}.HasTexture({{nameIdName}});
            }
        }
""");
    }
}