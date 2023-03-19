using Irony.Ast;
using Irony.Interpreter;
using Irony.Interpreter.Ast;
using Irony.Parsing;

namespace UniTyped.Generator.MaterialViews;

[Language("Shaderlab")]
public class ShaderlabGrammar : Grammar
{
    public static readonly ShaderlabGrammar Instance = new ShaderlabGrammar();

    public ShaderlabGrammar() : base(false)
    {
        var stringLiteral = new StringLiteral("String Literal", "\"", StringOptions.None, typeof(LiteralValueNode));

        var number = new NumberLiteral("Number Literal", NumberOptions.AllowSign, typeof(LiteralValueNode));

        var tex2d = new NonTerminal("Texture2D Value")
        {
            Rule = stringLiteral + (ToTerm("{") + "}").Q()
        };

        var vector = new NonTerminal("Vector")
        {
            Rule = ToTerm("(") + number + ("," + number + ("," + number + ("," + number).Q()).Q()).Q() + ")"
        };

        var propertyValue = new NonTerminal("Property Value")
        {
            Rule = number | tex2d | vector
        };

        var range = new NonTerminal("Range", typeof(PropertyTypeRangeNode))
        {
            Rule = ToTerm("Range") + "(" + number + "," + number + ")"
        };
        
        var propertyTypeSimple = new NonTerminal("Property Type (Simple)", typeof(PropertyTypeSimpleNode))
        {
            Rule = ToTerm("2D") | "Integer" | "Int" | "Float" | "2DArray" | "3D" | "Cube" | "CubeArray" |
                   "Color" | "Vector"
        };

        var propertyType = new NonTerminal("Property Type")
        {
            Rule = propertyTypeSimple | range
        };

        var propertyAttr = new CommentTerminal("Property Attribute", "[", new[] { "]" });

        var propertyAttrs = new NonTerminal("Property Attributes");
        propertyAttrs.Rule = MakeStarRule(propertyAttrs, propertyAttr);

        var propertyName = new IdentifierTerminal("Property Name");
        propertyName.AstConfig.NodeType = typeof(LiteralValueNode);

        var property = new NonTerminal("Property", typeof(PropertyNode))
        {
            Rule = propertyAttrs +
                   propertyName +
                   ToTerm("(") +
                   stringLiteral +
                   ToTerm(",") +
                   propertyType +
                   ToTerm(")") +
                   ToTerm("=") +
                   propertyValue
        };

        var properties = new NonTerminal("Properties");
        properties.Rule = MakeStarRule(properties, property);

        var propertiesSection = new NonTerminal("Properties Section", typeof(PropertiesNode))
        {
            Rule = ToTerm("Properties") + ToTerm("{") + properties + ToTerm("}")
        };

        var lod = new NonTerminal("LOD")
        {
            Rule = ToTerm("LOD") + number
        };

        var tag = new NonTerminal("Tag")
        {
            Rule = stringLiteral + ToTerm("=") + stringLiteral
        };

        var tags = new NonTerminal("Tags");
        tags.Rule = MakeStarRule(tags, tag);

        var tagsSection = new NonTerminal("Tags Section")
        {
            Rule = ToTerm("Tags") + ToTerm("{") + tags + ToTerm("}")
        };

        /*
        var command = new NonTerminal("Command");
        command.Rule = MakePlusRule(command, new IdentifierTerminal("Command Identifier"));

        var commands = new NonTerminal("Commands");
        commands.Rule = MakeListRule(commands, NewLinePlus, command);
        */

        var hlslProgram = new CommentTerminal("HLSLPROGRAM", "HLSLPROGRAM", new[] { "ENDHLSL" });
        var cgProgram = new CommentTerminal("CGPROGRAM", "CGPROGRAM", new[] { "ENDCG" });
        var hlslInclude = new CommentTerminal("HLSLINCLUDE", "HLSLINCLUDE", new[] { "ENDHLSL" });
        var cgInclude = new CommentTerminal("CGINCLUDE", "CGINCLUDE", new[] { "ENDCG" });

        this.NonGrammarTerminals.Add(hlslProgram);
        this.NonGrammarTerminals.Add(cgProgram);
        this.NonGrammarTerminals.Add(hlslInclude);
        this.NonGrammarTerminals.Add(cgInclude);
        this.NonGrammarTerminals.Add(new CommentTerminal("Slash Comment Single", "//", new[] { "\n" }));
        this.NonGrammarTerminals.Add(new CommentTerminal("Slash Comment Multi", "/*", new[] { "*/" }));


        var passName = new NonTerminal("Pass Name")
        {
            Rule = ToTerm("Name") + stringLiteral
        };

        var commandNames = new[]
        {
            "AlphaToMask",
            "Blend",
            "BlendOp",
            "ColorMask",
            "Conservative",
            "Cull",
            "Offset",
            "ZClip",
            "ZTest",
            "ZWrite",
            "Fog",
            "Color",
            "Material",
            "Lighting",
            "SeparateSpecular",
            "ColorMaterial",
            "AlphaTest",
            "SetTexture",
            "BindChannels",
        };

        var commands = commandNames.Aggregate((BnfExpression)null, (agg, term) =>
        {
            var command = new NonTerminal("Command")
            {
                Rule = new CommentTerminal(term, term, new[] { "\n" })
            };
            if (agg == null) return command;
            return agg | command;
        });

        var stencil = new NonTerminal("Stencil")
        {
            Rule = ToTerm("Stencil") + new CommentTerminal("Stencil Body", "{", new[] { "}" })
        };

        var passItems = new NonTerminal("Pass Items");
        passItems.Rule = MakeStarRule(passItems, passName | tagsSection | commands | stencil);

        var passSection = new NonTerminal("Pass Section")
        {
            Rule = ToTerm("Pass") + "{" + passItems + "}"
        };

        var usePass = new NonTerminal("UsePass Section")
        {
            Rule = ToTerm("UsePass") + stringLiteral
        };

        var grabPass = new NonTerminal("GrabPass Section")
        {
            Rule = ToTerm("GrabPass") + "{" + stringLiteral.Q() + "}"
        };

        var oneOrMorePasseSections = new NonTerminal("Pass Definitions");
        oneOrMorePasseSections.Rule = MakeStarRule(oneOrMorePasseSections, passSection | usePass | grabPass);

        var subshaderItems = new NonTerminal("Subshader Items");
        subshaderItems.Rule = MakeStarRule(subshaderItems, lod | tagsSection | commands | stencil);

        var subshader = new NonTerminal("Subshader")
        {
            Rule = ToTerm("SubShader") +
                   "{" +
                   subshaderItems +
                   (oneOrMorePasseSections) +
                   "}"
        };


        var shaderHeader = new NonTerminal("Shader Header")
        {
            Rule = ToTerm("Shader") + stringLiteral,
        };

        var subshaders = new NonTerminal("Subshaders");
        MakePlusRule(subshaders, subshader);

        var fallback = new NonTerminal("Fallback")
        {
            Rule = ToTerm("Fallback") + (stringLiteral | "Off"),
        };

        var customEditor = new NonTerminal("Custom Editor")
        {
            Rule = (ToTerm("CustomEditor") | "CustomEditorForRenderPipeline") + stringLiteral,
        };

        var shader = new NonTerminal("Shader", typeof(ShaderNode))
        {
            Rule = shaderHeader +
                   "{" +
                   propertiesSection.Q() +
                   subshaders +
                   fallback.Q() +
                   customEditor.Q() +
                   "}"
        };

        this.Root = shader;
        this.LanguageFlags |= LanguageFlags.CreateAst;
        
    }
}

public class ShaderNode : AstNode
{
    private readonly List<PropertiesNode> propertiesBlocks = new List<PropertiesNode>();

    public IReadOnlyList<PropertiesNode> PropertiesBlocks => propertiesBlocks;

    public override void Init(AstContext context, ParseTreeNode treeNode)
    {
        base.Init(context, treeNode);

        var nodes = treeNode.GetMappedChildNodes();
        foreach (var n in
                 nodes.Select(n =>
                 {
                     if ((n.Term.Flags & TermFlags.IsNullable) != TermFlags.None)
                     {
                         return n.ChildNodes.Count == 1 ? n.ChildNodes[0] : null;
                     }

                     return n;
                 }).Where(n => n?.AstNode is PropertiesNode)
                )
        {
            var block = AddChild("Properties", n);
            if (block is PropertiesNode blockTyped) propertiesBlocks.Add(blockTyped);
        }
    }
}

public class PropertiesNode : AstNode
{
    private readonly List<PropertyNode> propertyDefinitions = new List<PropertyNode>();

    public IReadOnlyList<PropertyNode> PropertyDefinitions => propertyDefinitions;

    public override void Init(AstContext context, ParseTreeNode treeNode)
    {
        base.Init(context, treeNode);

        var nodes = treeNode.GetMappedChildNodes()[2].ChildNodes;

        foreach (var node in nodes)
        {
            var property = AddChild("Property", node);
            if (property is PropertyNode propertyTyped) propertyDefinitions.Add(propertyTyped);
        }
    }
}

public class PropertyNode : AstNode
{
    public string Name { get; private set; }
    public string DisplayName { get; private set; }
    public AstNode Type { get; private set; }
    public AstNode DefaultValue { get; private set; }

    public override void Init(AstContext context, ParseTreeNode treeNode)
    {
        base.Init(context, treeNode);

        var nodes = treeNode.GetMappedChildNodes();
        Name = (AddChild("Name", nodes[1]) as LiteralValueNode).Value as string;
        DisplayName = (AddChild("DisplayName", nodes[3]) as LiteralValueNode).Value as string;
        Type = AddChild("Type", nodes[5].ChildNodes[0]);
        DefaultValue = AddChild("DefaultValue", nodes[8]);
    }
}

public class PropertyTypeSimpleNode : AstNode
{
    public string Type { get; private set; }

    public override void Init(AstContext context, ParseTreeNode treeNode)
    {
        base.Init(context, treeNode);

        var nodes = treeNode.GetMappedChildNodes();
        this.Type = nodes[0].Token.ValueString;
    }

    protected override object DoEvaluate(ScriptThread thread)
    {
        return Type;
    }
}

public class PropertyTypeRangeNode : AstNode
{
    public AstNode StartNode { get; private set; }
    public AstNode EndNode { get; private set; }

    public override void Init(AstContext context, ParseTreeNode treeNode)
    {
        base.Init(context, treeNode);

        var nodes = treeNode.GetMappedChildNodes();
        StartNode = AddChild("StartNode", nodes[2]);
        EndNode = AddChild("EndNode", nodes[4]);
    }
}