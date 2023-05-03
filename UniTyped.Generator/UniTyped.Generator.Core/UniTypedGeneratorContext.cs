using Microsoft.CodeAnalysis;
using UniTyped.Generator.SerializationViews;

namespace UniTyped.Generator;

public class UniTypedGeneratorContext
{
    public IUniTypedCollector Collector { get; }
    public Compilation Compilation { get; }
    public INamedTypeSymbol List { get; }
    public INamedTypeSymbol Serializable { get; }
    public INamedTypeSymbol SerializeField { get; }
    public INamedTypeSymbol SerializeReference { get; }
    public INamedTypeSymbol UnityEngineObject { get; }
    public INamedTypeSymbol AnimationCurve { get; }
    public INamedTypeSymbol BoundsInt { get; }
    public INamedTypeSymbol Bounds { get; }
    public INamedTypeSymbol Color { get; }
    public INamedTypeSymbol Hash128 { get; }
    public INamedTypeSymbol Quaternion { get; }
    public INamedTypeSymbol RectInt { get; }
    public INamedTypeSymbol Rect { get; }
    public INamedTypeSymbol Vector2Int { get; }
    public INamedTypeSymbol Vector2 { get; }
    public INamedTypeSymbol Vector3Int { get; }
    public INamedTypeSymbol Vector3 { get; }
    public INamedTypeSymbol Vector4 { get; }
    public INamedTypeSymbol UniTypedFieldAttribute { get; }
    public INamedTypeSymbol UniTypedMaterialViewAttribute { get; }
    public INamedTypeSymbol UniTypedAnimatorViewAttribute { get; }

    private readonly BuiltinViewDefinition[] builtinSerializeFieldViews;

    public IReadOnlyList<RuntimeViewDefinition> RuntimeViews => runtimeViews;

    private List<RuntimeViewDefinition> runtimeViews = new List<RuntimeViewDefinition>();

    private static readonly UnsupportedViewDefinition unsupportedView = new UnsupportedViewDefinition();

    public TypedViewDefinition GetTypedView(UniTypedGeneratorContext context, ITypeSymbol type,
        ViewUsage viewUsage)
    {
        foreach (var v in builtinSerializeFieldViews)
        {
            if (v.Match(this, type, viewUsage)) return v;
        }

        var custom = GetOrAddObjectView(context, type, viewUsage);
        if (custom.Match(this, type, viewUsage)) return custom;
            
        else throw new InvalidOperationException($"Created view doesn't match target type: {type.MetadataName}, {custom}");
        
        //throw new InvalidOperationException("New view is null");

        return unsupportedView;
    }

    private TypedViewDefinition GetOrAddObjectView(UniTypedGeneratorContext context, ITypeSymbol type,
        ViewUsage viewUsage)
    {

        foreach (var v in runtimeViews)
        {
            if (v.Match(this, type, viewUsage)) return v;
        }

        var newView = CreateRuntimeView(context, type, viewUsage);

        if (newView != null)
        {
            runtimeViews.Add(newView);
            return newView;
        }
        
        //throw new InvalidOperationException("New view is null");
        return unsupportedView;
    }

    private RuntimeViewDefinition? CreateRuntimeView(UniTypedGeneratorContext context, ITypeSymbol type,
        ViewUsage viewUsage)
    {
        
        //Array
        if (Utils.IsArrayOrList(context, type, out var elementType))
        {
            if (viewUsage is ViewUsage.SerializeField && Utils.IsSerializableAsSerializeField(context, elementType)) return new SerializeFieldArrayViewDefinition(elementType);
            if(viewUsage is ViewUsage.SerializeReferenceField) return new ManagedReferenceArrayViewDefinition(elementType);
            throw new InvalidOperationException("Unserializable");
            //return null;
        }
        
        if (viewUsage == ViewUsage.SerializeReferenceField)
        {
            return new ManagedReferenceViewDefinition(type);
        }
        
        if (viewUsage == ViewUsage.SerializeField && Utils.IsDerivedFrom(type, context.UnityEngineObject))
        {
            return new UnityEngineObjectReferenceValueViewDefinition(type);
        }

        switch (type.TypeKind)
        {
            case TypeKind.Class:
            case TypeKind.Struct:
                return new CustomValueViewDefinition(this, type);
            case TypeKind.Enum:
                return new EnumValueViewDefinition(type);
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public UniTypedGeneratorContext(Compilation compilation, IUniTypedCollector collector)
    {

        Collector = collector;

        Compilation = compilation;

        INamedTypeSymbol GetSymbol(string metadataName) =>
            compilation.GetTypeByMetadataName(metadataName) ??
            throw new NullReferenceException($"Symbol not found: {metadataName}");

        List = GetSymbol("System.Collections.Generic.List`1");
        Serializable = GetSymbol("System.SerializableAttribute");
        SerializeField = GetSymbol("UnityEngine.SerializeField");
        SerializeReference = GetSymbol("UnityEngine.SerializeReference");
        UnityEngineObject = GetSymbol("UnityEngine.Object");
        AnimationCurve = GetSymbol("UnityEngine.AnimationCurve");
        BoundsInt = GetSymbol("UnityEngine.BoundsInt");
        Bounds = GetSymbol("UnityEngine.Bounds");
        Color = GetSymbol("UnityEngine.Color");
        Hash128 = GetSymbol("UnityEngine.Hash128");
        Quaternion = GetSymbol("UnityEngine.Quaternion");
        RectInt = GetSymbol("UnityEngine.RectInt");
        Rect = GetSymbol("UnityEngine.Rect");
        Vector2Int = GetSymbol("UnityEngine.Vector2Int");
        Vector2 = GetSymbol("UnityEngine.Vector2");
        Vector3Int = GetSymbol("UnityEngine.Vector3Int");
        Vector3 = GetSymbol("UnityEngine.Vector3");
        Vector4 = GetSymbol("UnityEngine.Vector4");

        UniTypedFieldAttribute = GetSymbol("UniTyped.UniTypedFieldAttribute");
        UniTypedMaterialViewAttribute = GetSymbol("UniTyped.UniTypedMaterialViewAttribute");
        UniTypedAnimatorViewAttribute = GetSymbol("UniTyped.UniTypedAnimatorViewAttribute");

        builtinSerializeFieldViews = new BuiltinViewDefinition[]
        {
            new TypeParameterViewDefinition(),
            new DirectValueViewDefinition("global::UniTyped.Editor.SerializedPropertyViewByte",
                GetSymbol("System.Byte")),
            new DirectValueViewDefinition("global::UniTyped.Editor.SerializedPropertyViewSByte",
                GetSymbol("System.SByte")),
            new DirectValueViewDefinition("global::UniTyped.Editor.SerializedPropertyViewShort",
                GetSymbol("System.Int16")),
            new DirectValueViewDefinition("global::UniTyped.Editor.SerializedPropertyViewUShort",
                GetSymbol("System.UInt16")),
            new DirectValueViewDefinition("global::UniTyped.Editor.SerializedPropertyViewInt",
                GetSymbol("System.Int32")),
            new DirectValueViewDefinition("global::UniTyped.Editor.SerializedPropertyViewUInt",
                GetSymbol("System.UInt32")),
            new DirectValueViewDefinition("global::UniTyped.Editor.SerializedPropertyViewLong",
                GetSymbol("System.Int64")),
            new DirectValueViewDefinition("global::UniTyped.Editor.SerializedPropertyViewULong",
                GetSymbol("System.UInt64")),
            new DirectValueViewDefinition("global::UniTyped.Editor.SerializedPropertyViewFloat",
                GetSymbol("System.Single")),
            new DirectValueViewDefinition("global::UniTyped.Editor.SerializedPropertyViewDouble",
                GetSymbol("System.Double")),
            new DirectValueViewDefinition("global::UniTyped.Editor.SerializedPropertyViewBool",
                GetSymbol("System.Boolean")),
            new DirectValueViewDefinition("global::UniTyped.Editor.SerializedPropertyViewString",
                GetSymbol("System.String")),
            new DirectValueViewDefinition("global::UniTyped.Editor.SerializedPropertyViewChar",
                GetSymbol("System.Char")),

            new DirectValueViewDefinition("global::UniTyped.Editor.SerializedPropertyViewAnimationCurve",
                AnimationCurve),
            new DirectValueViewDefinition("global::UniTyped.Editor.SerializedPropertyViewBoundsInt", BoundsInt),
            new DirectValueViewDefinition("global::UniTyped.Editor.SerializedPropertyViewBounds", Bounds),
            new DirectValueViewDefinition("global::UniTyped.Editor.SerializedPropertyViewColor", Color),
            new DirectValueViewDefinition("global::UniTyped.Editor.SerializedPropertyViewHash128", Hash128),
            new DirectValueViewDefinition("global::UniTyped.Editor.SerializedPropertyViewQuaternion",
                Quaternion),
            new DirectValueViewDefinition("global::UniTyped.Editor.SerializedPropertyViewRectInt", RectInt),
            new DirectValueViewDefinition("global::UniTyped.Editor.SerializedPropertyViewRect", Rect),
            new DirectValueViewDefinition("global::UniTyped.Editor.SerializedPropertyViewVector2Int",
                Vector2Int),
            new DirectValueViewDefinition("global::UniTyped.Editor.SerializedPropertyViewVector2", Vector2),
            new DirectValueViewDefinition("global::UniTyped.Editor.SerializedPropertyViewVector3Int",
                Vector3Int),
            new DirectValueViewDefinition("global::UniTyped.Editor.SerializedPropertyViewVector3", Vector3),
            new DirectValueViewDefinition("global::UniTyped.Editor.SerializedPropertyViewVector4", Vector4),
        };
    }
}

public enum ViewUsage
{
    Root,
    SerializeField,
    SerializeReferenceField
}