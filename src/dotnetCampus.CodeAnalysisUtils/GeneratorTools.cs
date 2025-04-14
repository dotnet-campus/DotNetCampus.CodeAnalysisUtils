using Microsoft.CodeAnalysis;

namespace DotNetCampus.CodeAnalysis.Utils;

/// <summary>
/// 提供源生成器相关的杂项工具方法。
/// </summary>
internal static class GeneratorTools
{
    private static readonly SymbolDisplayFormat GlobalDisplayFormat = new SymbolDisplayFormat(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        miscellaneousOptions:
        SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
        SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier |
        SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    /// <summary>
    /// 将符号转换为适合源生成器输出的完整类型名称。<br/>
    /// 例如：
    /// <code>
    /// string
    /// int?
    /// global::DotNetCampus.Namespace.TypeName
    /// global::DotNetCampus.Namespace.TypeName?
    /// global::DotNetCampus.Namespace.TypeName&lt;string, global::System.Action?&gt;
    /// </code>
    /// </summary>
    /// <param name="symbol">要转换为完整名称的符号。</param>
    /// <returns>类型的完整名称。</returns>
    public static string ToGlobalCodeString(this ISymbol symbol)
    {
        return symbol.ToDisplayString(GlobalDisplayFormat);
    }
}
