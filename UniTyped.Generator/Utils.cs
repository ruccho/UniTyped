using Microsoft.CodeAnalysis;

namespace UniTyped.Generator;

internal static class Utils
{

    public static bool IsPrimitiveSerializableType(ITypeSymbol symbol) => symbol.SpecialType is
            SpecialType.System_Boolean or
            SpecialType.System_Char or
            SpecialType.System_SByte or
            SpecialType.System_Byte or
            SpecialType.System_Int16 or
            SpecialType.System_UInt16 or
            SpecialType.System_Int32 or
            SpecialType.System_UInt32 or
            SpecialType.System_Int64 or
            SpecialType.System_UInt64 or
            SpecialType.System_Single or
            SpecialType.System_Double or
            SpecialType.System_String;

        public static bool IsSerializableEnumType(ITypeSymbol symbol)
        {
            return symbol is INamedTypeSymbol { TypeKind: TypeKind.Enum } namedTypeSymbol &&
                   namedTypeSymbol.EnumUnderlyingType is
                   {
                       SpecialType:
                       SpecialType.System_SByte or
                       SpecialType.System_Byte or
                       SpecialType.System_Int16 or
                       SpecialType.System_UInt16 or
                       SpecialType.System_Int32 or
                       SpecialType.System_UInt32
                   };
        }

        public static bool IsUnityBuiltinType(UniTypedGeneratorContext context, ITypeSymbol symbol)
        {
            return SymbolEqualityComparer.Default.Equals(symbol, context.AnimationCurve) ||
                   SymbolEqualityComparer.Default.Equals(symbol, context.BoundsInt) ||
                   SymbolEqualityComparer.Default.Equals(symbol, context.Bounds) ||
                   SymbolEqualityComparer.Default.Equals(symbol, context.Color) ||
                   SymbolEqualityComparer.Default.Equals(symbol, context.Hash128) ||
                   SymbolEqualityComparer.Default.Equals(symbol, context.Quaternion) ||
                   SymbolEqualityComparer.Default.Equals(symbol, context.RectInt) ||
                   SymbolEqualityComparer.Default.Equals(symbol, context.Rect) ||
                   SymbolEqualityComparer.Default.Equals(symbol, context.Vector2Int) ||
                   SymbolEqualityComparer.Default.Equals(symbol, context.Vector2) ||
                   SymbolEqualityComparer.Default.Equals(symbol, context.Vector3Int) ||
                   SymbolEqualityComparer.Default.Equals(symbol, context.Vector3) ||
                   SymbolEqualityComparer.Default.Equals(symbol, context.Vector4);
        }

        public static bool IsDerivedFrom(ITypeSymbol? type, ITypeSymbol baseType)
        {
            while (type != null)
            {
                if (SymbolEqualityComparer.Default.Equals(type, baseType)) return true;
                type = type.BaseType;
            }

            return false;
        }

        public static bool IsCustomSerializable(UniTypedGeneratorContext context, ITypeSymbol symbol)
        {
            if (symbol.TypeKind is not TypeKind.Class and not TypeKind.Struct) return false;
            return symbol.GetAttributes().Any(a =>
                SymbolEqualityComparer.Default.Equals(a.AttributeClass, context.Serializable));
        }

        public static string ExtractTypeParameters(INamedTypeSymbol type)
        {
            string result = "";
            if (type.IsGenericType)
            {
                result = "<";
                for (int i = 0; i < type.TypeArguments.Length; i++)
                {
                    var param = type.TypeArguments[i];
                    if (i > 0) result += ", ";
                    result += GetFullQualifiedTypeName(param);
                }

                result += ">";
            }

            return result;
        }

        public static string GetFullQualifiedTypeName(ITypeSymbol type)
        {
            if (type is ITypeParameterSymbol) return type.Name;

            return type.ContainingNamespace.IsGlobalNamespace
                ? $"global::{type.Name}"
                : $"global::{type.ContainingNamespace}.{type.Name}";
        }

        public static bool IsSerializableArrayOrList(UniTypedGeneratorContext context, ITypeSymbol symbol,
            out ITypeSymbol? elementType)
        {
            elementType = null;

            //serializable array
            if (symbol is IArrayTypeSymbol arraySymbol)
            {
                elementType = arraySymbol.ElementType;
                return IsSerializableType(context, elementType);
            }

            //serializable List<T>
            {
                if (symbol is INamedTypeSymbol { IsGenericType: true } namedSymbol &&
                    SymbolEqualityComparer.Default.Equals(namedSymbol.OriginalDefinition, context.List))
                {
                    elementType = namedSymbol.TypeArguments[0];
                    return IsSerializableType(context, elementType);
                }
            }

            return false;
        }

        public static bool IsSerializableType(UniTypedGeneratorContext context, ITypeSymbol symbol)
        {
            if (symbol is ITypeParameterSymbol) return true; //resolve runtime

            if (IsPrimitiveSerializableType(symbol)) return true;
            if (IsSerializableEnumType(symbol)) return true;
            if (IsUnityBuiltinType(context, symbol)) return true;
            if (IsDerivedFrom(symbol, context.UnityEngineObject)) return true;
            if (IsCustomSerializable(context, symbol)) return true;
            if (IsSerializableArrayOrList(context, symbol, out _)) return true;

            return false;
        }
}