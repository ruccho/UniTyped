using Microsoft.CodeAnalysis;

namespace UniTyped.Generator;

public class TypePath : IEquatable<TypePath>
{
    public TypePath? Parent { get; set; }
    public string Name { get; set; }

    public ITypeParameterSymbol[] TypeParams { get; set; } = Array.Empty<ITypeParameterSymbol>();

    public override string ToString()
    {
        if (Parent != null)
        {
            return $"{Parent}.{Name}";
        }
        else
        {
            return $"{Name}";
        }
    }

    public TypePath(ITypeSymbol type) : this(type.Name)
    {
        if (type.ContainingType != null)
        {
            Parent = new TypePath(type.ContainingType);
        }
        else 
        {
            if (!type.ContainingNamespace.IsGlobalNamespace)
            {
                Parent = new TypePath(type.ContainingNamespace.ToString());
            }
        }
        
        if (type is INamedTypeSymbol namedType) TypeParams = namedType.TypeParameters.ToArray();
    }
        

    public TypePath(string namespaceStr)
    {
        Parent = null;

        var spaces = namespaceStr.Split('.');

        if (spaces.Length > 1)
        {
            for (int i = 0; i < spaces.Length - 1; i++)
            {
                if (Parent == null) Parent = new TypePath(spaces[i]);
                else Parent.Append(spaces[i]);
            }
        }

        Name = spaces[spaces.Length - 1];
        if (string.IsNullOrEmpty(Name)) throw new ArgumentException();
    }

    public TypePath(TypePath parent, string name)
    {
        Parent = parent;
        if (name.IndexOf('.') >= 0 || string.IsNullOrEmpty(name)) throw new ArgumentException();
        Name = name;
    }

    public TypePath Append(string path)
    {
        return new TypePath(this, path);
    }

    public bool Equals(TypePath? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Equals(Parent, other.Parent) && Name == other.Name;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((TypePath)obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return ((Parent != null ? Parent.GetHashCode() : 0) * 397) ^ Name.GetHashCode();
        }
    }

    public static bool operator ==(TypePath? left, TypePath? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(TypePath? left, TypePath? right)
    {
        return !Equals(left, right);
    }

    private sealed class ParentNameEqualityComparer : IEqualityComparer<TypePath>
    {
        public bool Equals(TypePath x, TypePath y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            if (x.GetType() != y.GetType()) return false;
            return Equals(x.Parent, y.Parent) && x.Name == y.Name;
        }

        public int GetHashCode(TypePath obj)
        {
            unchecked
            {
                return ((obj.Parent != null ? obj.Parent.GetHashCode() : 0) * 397) ^ obj.Name.GetHashCode();
            }
        }
    }

    public static IEqualityComparer<TypePath> ParentNameComparer { get; } = new ParentNameEqualityComparer();
}