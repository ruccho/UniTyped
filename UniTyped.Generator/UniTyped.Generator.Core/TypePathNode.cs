using UniTyped.Generator.SerializationViews;

namespace UniTyped.Generator;

public class TypePathNode
{
    public TypePath? Path { get; }
    public List<TypePathNode> Children { get; } = new List<TypePathNode>();
    public GeneratedViewDefinition? View { get; set; }

    public TypePathNode(TypePath? path = null, GeneratedViewDefinition? view = null)
    {
        Path = path;
        View = view;
    }

    public bool IsGlobalNamesapce => Path == null;
    public bool IsNamespace => View == null;
}