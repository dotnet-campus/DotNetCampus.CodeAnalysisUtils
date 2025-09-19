#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace DotNetCampus.CodeAnalysis.Utils.Generators.Builders;

/// <summary>
/// <see cref="SourceTextBuilder"/> 的扩展方法。
/// </summary>
public static class SourceTextBuilderExtensions
{
    private static readonly WeakAsyncLocalAccessor<SourceTextBuilder> SourceTextBuilderLocal = new();

    private static readonly HashSet<string> KeywordTypeNames =
    [
        "bool", "byte", "sbyte", "char", "decimal", "double", "float", "int", "uint", "long", "ulong", "nint", "nuint", "short", "ushort",
        "object", "string", "void",
    ];

    internal static IDisposable BeginBuild(SourceTextBuilder sourceTextBuilder)
    {
        SourceTextBuilderLocal.Value = sourceTextBuilder;
        return new ActionDisposable(sourceTextBuilder, () => SourceTextBuilderLocal.Value = null);
    }

    /// <summary>
    /// 如果当前正在生成的源代码允许通过 using 引用命名空间，则：<br/>
    /// 提取 <paramref name="typeSymbol"/> 的命名空间，将其引用添加到当前正在生成的源代码的 using 列表中，
    /// 并返回简化后的类型名称字符串。<br/>
    /// 如果当前正在生成的源代码不允许通过 using 引用命名空间，则直接返回 <paramref name="typeSymbol"/> 的完整类型名称字符串。
    /// </summary>
    /// <param name="typeSymbol">要简化名称的类型符号。</param>
    /// <returns>简化后的类型名称字符串，或完整类型名称字符串。</returns>
    public static string ToUsingString(this ITypeSymbol typeSymbol)
    {
        var root = SourceTextBuilderLocal.Value;
        if (root is null)
        {
            // 当前没有正在生成的源代码，无法通过 using 引用命名空间。
            return typeSymbol.ToGlobalDisplayString();
        }
        if (!root.SimplifyTypeNamesByUsingNamespace)
        {
            // 当前正在生成的源代码不允许通过 using 引用命名空间。
            return root.ShouldPrependGlobal
                ? typeSymbol.ToGlobalDisplayString()
                : typeSymbol.ToDisplayString();
        }

        var namespaces = new List<string>();
        var simplifiedName = SimplifyNameByAddUsing(typeSymbol, namespaces);
        foreach (var @namespace in namespaces)
        {
            root.Using(@namespace);
        }
        return simplifiedName;
    }

    private static string SimplifyNameByAddUsing(ITypeSymbol typeSymbol, List<string> namespaces)
    {
        if (typeSymbol.Kind is SymbolKind.ArrayType)
        {
            // 数组类型（如 int[]、string[,] 等）
            var arrayType = (IArrayTypeSymbol)typeSymbol;
            return $"{SimplifyNameByAddUsing(arrayType.ElementType, namespaces)}[{new string(',', arrayType.Rank - 1)}]";
        }

        var originalDefinitionString = typeSymbol.OriginalDefinition.ToString();
        if (KeywordTypeNames.Contains(originalDefinitionString))
        {
            // 关键字类型（如 int、string 等）
            return originalDefinitionString;
        }
        if (originalDefinitionString.Equals("System.Nullable<T>", StringComparison.Ordinal))
        {
            // Nullable<T> 类型
            var genericType = ((INamedTypeSymbol)typeSymbol).TypeArguments[0];
            return $"{SimplifyNameByAddUsing(genericType, namespaces)}?";
        }
        if (originalDefinitionString.Equals("System.IntPtr", StringComparison.Ordinal))
        {
            // nint 类型
            return "nint";
        }
        if (originalDefinitionString.Equals("System.UIntPtr", StringComparison.Ordinal))
        {
            // nuint 类型
            return "nuint";
        }
        if (typeSymbol is INamedTypeSymbol { IsTupleType: true } valueTupleTypeSymbol)
        {
            // ValueTuple<T1, T2, ...> 类型
            var tupleMembers = string.Join(", ", valueTupleTypeSymbol.TupleElements
                .Select(x => $"{SimplifyNameByAddUsing(x.Type, namespaces)} {x.Name}"));
            return $"({tupleMembers})";
        }

        // 常规类型（或常规泛型类型）
        if (typeSymbol.ContainingNamespace is { IsGlobalNamespace: false } @namespace)
        {
            namespaces.Add(@namespace.ToDisplayString());
        }
        var recursiveTypeName = GetNestedTypeNameRecursively(typeSymbol);
        var nullablePostfix = typeSymbol.NullableAnnotation is NullableAnnotation.Annotated ? "?" : "";
        if (typeSymbol is INamedTypeSymbol { TypeArguments.Length: > 0 } namedTypeSymbol)
        {
            // Class<T> 类型
            var genericTypes = string.Join(", ", namedTypeSymbol.TypeArguments.Select(x => SimplifyNameByAddUsing(x, namespaces)));
            return $"{recursiveTypeName}<{genericTypes}>{nullablePostfix}";
        }
        else
        {
            if (typeSymbol is not INamedTypeSymbol)
            {
                throw new NotSupportedException($"目前尚未支持 {typeSymbol.GetType().FullName} 类型的名称简化。");
            }

            // T 类型
            return $"{recursiveTypeName}{nullablePostfix}";
        }

        // 返回一个类型的嵌套内部类名称。
        // 无视特殊类型（如 Nullable、ValueTuple 等），因此请勿对特殊类型调用此方法。
        static string GetNestedTypeNameRecursively(ITypeSymbol typeSymbol)
        {
            if (typeSymbol.ContainingType is { } containingType)
            {
                return $"{GetNestedTypeNameRecursively(containingType)}.{typeSymbol.Name}";
            }
            return typeSymbol.Name;
        }
    }
}

/// <summary>
/// 一个在处置时执行指定操作的可处置对象。
/// </summary>
/// <param name="holdInstance">需要避免被 GC 的实例。</param>
/// <param name="disposeAction">在处置时执行的操作。</param>
file sealed class ActionDisposable(object holdInstance, Action disposeAction) : IDisposable
{
    /// <summary>
    /// 我们需要保留此字段以便在本实例不被 GC 时，holdInstance 实例一定不会被 GC。
    /// </summary>
    private readonly object _holdInstance = holdInstance;

    ~ActionDisposable()
    {
        Dispose();
    }

    public void Dispose()
    {
        // 如果出现了并发多次执行，那就多次执行吧，不影响的。
        _ = _holdInstance;
        disposeAction();
        GC.SuppressFinalize(this);
    }
}
