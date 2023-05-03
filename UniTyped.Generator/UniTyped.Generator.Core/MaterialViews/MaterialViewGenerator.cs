using System.Diagnostics;
using System.Text;
using Irony.Parsing;
using Microsoft.CodeAnalysis;

namespace UniTyped.Generator.MaterialViews;

public static class MaterialViewGenerator
{
    private static readonly ShaderParser[] parsers = new ShaderParser[]
    {
        new ShaderlabParser(new Parser(new ShaderlabGrammar())),
        new ShaderGraphParser()
    };

    public static void GenerateViews(UniTypedGeneratorContext context, StringBuilder sourceBuilder)
    {
        sourceBuilder.AppendLine($"// MaterialViewGenerator");

        var tempProps = new List<ShaderProperty>();

        foreach (var materialViewType in context.Collector.MaterialViews)
        {
            var semanticModel = context.Compilation.GetSemanticModel(materialViewType.SyntaxTree);
            var symbol = semanticModel.GetDeclaredSymbol(materialViewType) as INamedTypeSymbol;
            if (symbol == null) continue;

            var attr = symbol.GetAttributes().FirstOrDefault(a =>
                SymbolEqualityComparer.Default.Equals(a.AttributeClass, context.UniTypedMaterialViewAttribute));

            if (attr == null) continue;

            var shaderPath = attr.ConstructorArguments[0].Value as string;

            if (string.IsNullOrEmpty(shaderPath)) continue;

            var shaderFullPath = $"{Path.GetDirectoryName(materialViewType.SyntaxTree.FilePath)}/{shaderPath}";

            var parser = parsers.FirstOrDefault(p => p.Match(shaderFullPath));

            if (parser == null) continue;

            tempProps.Clear();
            if (!parser.Process(shaderFullPath, tempProps)) continue;
            if (!tempProps.Any()) continue;

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
                foreach (var prop in tempProps)
                {
                    prop.Provider.Generate(context, sourceBuilder, prop.Name);
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

    class ShaderlabParser : ShaderParser
    {
        private readonly Parser shaderlabParser;

        public ShaderlabParser(Parser shaderlabParser)
        {
            this.shaderlabParser = shaderlabParser;
        }

        public override bool Match(string shaderFullPath)
        {
            return Path.GetExtension(shaderFullPath) == ".shader";
        }

        public override bool Process(string shaderFullPath, IList<ShaderProperty> result)
        {
            var shaderContent = File.ReadAllText(shaderFullPath, Encoding.UTF8);

            var root = shaderlabParser.Parse(shaderContent).Root;
            if (root.AstNode is not ShaderNode shader) return false;

            foreach (var propBlock in shader.PropertiesBlocks)
            {
                foreach (var prop in propBlock.PropertyDefinitions)
                {
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

                    if (provider == null) continue;

                    result.Add(new ShaderProperty(provider, prop.Name));
                }
            }

            return true;
        }
    }
}

public class ShaderProperty
{
    public PropertyProvider Provider { get; }
    public string Name { get; }

    public ShaderProperty(PropertyProvider provider, string name)
    {
        Provider = provider;
        Name = name;
    }
}

public abstract class ShaderParser
{
    public abstract bool Match(string shaderFullPath);
    public abstract bool Process(string shaderFullPath, IList<ShaderProperty> result);
}

public abstract class PropertyProvider
{
    public abstract void Generate(UniTypedGeneratorContext context, StringBuilder sourceBuilder, string propName);
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

    public override void Generate(UniTypedGeneratorContext context, StringBuilder sourceBuilder, string propName)
    {
        var nameIdName = $"__unityped__name_{propName}";
        var target = $"Target";

        sourceBuilder.AppendLine($$"""
        private static readonly int {{nameIdName}} = global::UnityEngine.Shader.PropertyToID(@"{{propName}}");
        public {{CSharpTypeSyntax}} {{propName}} 
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

    public override void Generate(UniTypedGeneratorContext context, StringBuilder sourceBuilder, string propName)
    {
        var nameIdName = $"__unityped__name_{propName}";
        var target = $"Target";

        sourceBuilder.AppendLine($$"""
        private static readonly int {{nameIdName}} = global::UnityEngine.Shader.PropertyToID(@"{{propName}}");
        public global::UnityEngine.Texture {{propName}} 
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

        public global::UnityEngine.Vector2 {{propName}}_Offset
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

        public global::UnityEngine.Vector2 {{propName}}_Scale
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
        public bool {{propName}}_Exists
        {
            get
            {
                return {{target}}.HasTexture({{nameIdName}});
            }
        }
""");
    }
}