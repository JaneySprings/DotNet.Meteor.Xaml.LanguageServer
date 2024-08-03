using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotNet.Meteor.Xaml.LanguageServer.Syntax;

public static class SimpleTypeNameResolverExtensions {
    #region AstType

    public static string? Resolve(this TypeSyntax type) {
       if (type is PredefinedTypeSyntax)
            return Resolve((PredefinedTypeSyntax)type);
        else if (type is PointerTypeSyntax)
            return Resolve((PointerTypeSyntax)type);
        else if (type is NullableTypeSyntax)
            return Resolve((NullableTypeSyntax)type);
        else if (type is RefTypeSyntax)
            return Resolve((RefTypeSyntax)type);
        else if (type is QualifiedNameSyntax)
            return Resolve((QualifiedNameSyntax)type);
        else if (type is IdentifierNameSyntax)
            return Resolve((IdentifierNameSyntax)type);
        else if (type is GenericNameSyntax)
            return Resolve((GenericNameSyntax)type);
        return null;
    }
    #endregion AstType

    #region ComposedType

    static string Resolve(this PointerTypeSyntax type) {
        return type.ElementType.Resolve() + "*";
    }

    static string? Resolve(this RefTypeSyntax type) {
        return type.Type.Resolve();
    }

    static string? Resolve(this NullableTypeSyntax type) {
        var resolvedType = type.ElementType.Resolve();
        if (resolvedType == null)
            return null;
        return $"System.Nullable<{resolvedType}>";
    }

    #endregion ComposedType

    #region MemberType

    static string? Resolve(this QualifiedNameSyntax type) {
        string name = type.Right.Identifier.ValueText;
        if (type.Right is GenericNameSyntax genericName) {
            List<string> argsList = new();
            foreach (var arg in genericName.TypeArgumentList.Arguments) {
                string? resolved = arg.Resolve();
                if (resolved is null)
                    return null;
                argsList.Add(resolved);
            }
            name += $"<{string.Join(", ", argsList)}>";
        }
        return name;
    }

    #endregion MemberType

    #region PrimitiveType

    public static string? Resolve(this PredefinedTypeSyntax type) {
        switch (type.Keyword.Kind()) {
            case SyntaxKind.BoolKeyword: return "Boolean";
            case SyntaxKind.SByteKeyword: return "SByte";
            case SyntaxKind.ByteKeyword: return "Byte";
            case SyntaxKind.CharKeyword: return "Char";
            case SyntaxKind.ShortKeyword: return "Int16";
            case SyntaxKind.UShortKeyword: return "UInt16";
            case SyntaxKind.IntKeyword: return "Int32";
            case SyntaxKind.UIntKeyword: return "UInt32";
            case SyntaxKind.LongKeyword: return "Int64";
            case SyntaxKind.ULongKeyword: return "UInt64";
            case SyntaxKind.FloatKeyword: return "Single";
            case SyntaxKind.DoubleKeyword: return "Double";
            case SyntaxKind.DecimalKeyword: return "Decimal";
            case SyntaxKind.StringKeyword: return "String";
            case SyntaxKind.ObjectKeyword: return "Object";
            case SyntaxKind.VoidKeyword: return "Void";
            default: return null;
        }
    }

    public static string Resolve(string shortTypeName) {
        string longName;
        switch (shortTypeName) {
            case "bool": longName = "Boolean"; break;
            case "byte": longName = "Byte"; break;
            case "sbyte": longName = "SByte"; break;
            case "char": longName = "Char"; break;
            case "decimal": longName = "Decimal"; break;
            case "double": longName = "Double"; break;
            case "float": longName = "Single"; break;
            case "int": longName = "Int32"; break;
            case "uint": longName = "UInt32"; break;
            case "nint": longName = "IntPtr"; break;
            case "nuint": longName = "UIntPtr"; break;
            case "long": longName = "Int64"; break;
            case "ulong": longName = "UInt64"; break;
            case "short": longName = "Int16"; break;
            case "ushort": longName = "UInt16"; break;
            case "object": longName = "Object"; break;
            case "string": longName = "String"; break;
            case "dynamic": longName = "Object"; break;
            default: throw new ArgumentException($"Unknown type {shortTypeName}");
        }
        return longName;
    }

    #endregion PrimitiveType

    #region SimpleType

    static string? Resolve(this GenericNameSyntax type) {
        string name = type.Identifier.ValueText;
        if (type.TypeArgumentList.Arguments.Count > 0) {
            List<string> genericArgTypes = new();
            foreach (var arg in type.TypeArgumentList.Arguments) {
                string? resolved = arg.Resolve();
                if (resolved is null)
                    return null;
                genericArgTypes.Add(resolved);
            }
            name += $"<{string.Join(", ", genericArgTypes)}>";
        }
        return name;
    }

    static string Resolve(this IdentifierNameSyntax type) {
        return type.Identifier.ValueText;
    }

    #endregion SimpleType
}