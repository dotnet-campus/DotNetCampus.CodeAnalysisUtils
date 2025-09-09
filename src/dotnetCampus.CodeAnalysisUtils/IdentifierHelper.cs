using System;
using System.Globalization;
using System.Linq;
using static System.Globalization.UnicodeCategory;

namespace DotNetCampus.CodeAnalysis.Utils;

/// <summary>
/// 辅助处理标识符名称。
/// </summary>
internal static class IdentifierHelper
{
    /// <summary>
    /// 根据<paramref name="originalName"/>创建安全的字符串（根据<see cref="IsValidIdentifierChar"/>机型字符的规则校验，将不符合的字符串使用<paramref name="replacement"/>进行替换）
    /// </summary>
    /// <param name="originalName">原名称（字符串）</param>
    /// <param name="ignoreReplacedIdentifierChars">忽略替换的字符数组</param>
    /// <param name="replacement">将不符合校验规则的字符串继续替换的字符</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static string BuildSafeIdentifier(string originalName, char[] ignoreReplacedIdentifierChars, char replacement = '_')
    {
        originalName = originalName.Trim();
        if (string.IsNullOrEmpty(originalName))
        {
            throw new ArgumentException($"“{nameof(originalName)}”不能为 null 或空。", nameof(originalName));
        }

        var shouldAddPrefix = false;
        Span<char> editable = stackalloc char[originalName.Length];
        for (var i = 0; i < originalName.Length; i++)
        {
            var c = originalName[i];
            //忽略校验的字符不进行校验和替换
            if (!ignoreReplacedIdentifierChars.Contains(c))
            {
                c = IsValidIdentifierChar(c) ? c : replacement;
            }
            editable[i] = c;
            if (i is 0 && IsValidIdentifierCharAnInValidFirstChar(c))
            {
                shouldAddPrefix = true;
            }
        }
        if (shouldAddPrefix)
        {
            Span<char> prefix = stackalloc char[1];
            prefix[0] = replacement;
            return string.Intern(ConvertKeywords(prefix.ToString() + editable.ToString()));
        }
        else
        {
            return string.Intern(ConvertKeywords(editable.ToString()));
        }
    }

    private static bool IsValidIdentifierCharAnInValidFirstChar(char c)
    {
        var uc = CharUnicodeInfo.GetUnicodeCategory(c);
        return uc is DecimalDigitNumber;
    }

    private static bool IsValidIdentifierChar(char c)
    {
        var uc = CharUnicodeInfo.GetUnicodeCategory(c);
        return uc is UppercaseLetter
            or LowercaseLetter
            or TitlecaseLetter
            or ModifierLetter
            or LetterNumber
            or OtherLetter
            or DecimalDigitNumber;
    }

    private static string ConvertKeywords(string identifier) => identifier switch
    {
        // Keywords
        "abstract" or "as"
        or "base" or "bool" or "break" or "byte"
        or "case" or "catch" or "char" or "checked" or "class" or "const" or "continue"
        or "decimal" or "default" or "delegate" or "do" or "double"
        or "else" or "enum" or "event" or "explicit" or "extern"
        or "false" or "finally" or "fixed" or "float" or "for" or "foreach"
        or "goto"
        or "if" or "implicit" or "in" or "int" or "interface" or "internal" or "is"
        or "lock" or "long"
        or "namespace" or "new" or "null"
        or "object" or "operator" or "out" or "override"
        or "params" or "private" or "protected" or "public"
        or "readonly" or "ref" or "return"
        or "sbyte" or "sealed" or "short" or "sizeof" or "stackalloc" or "static" or "string" or "struct" or "switch"
        or "this" or "throw" or "true" or "try" or "typeof"
        or "uint" or "ulong" or "unchecked" or "unsafe" or "ushort" or "using"
        or "virtual" or "void"
        or "volatile" or "while" => '@' + identifier,
        // Contextual keywords
        "add" or "and" or "alias" or "ascending" or "args" or "async" or "await"
        or "by"
        or "descending" or "dynamic"
        or "equals"
        or "from"
        or "get" or "global" or "group"
        or "init" or "into"
        or "join"
        or "let"
        or "managed"
        or "nameof" or "nint" or "not" or "notnull" or "nuint"
        or "on" or "or" or "orderby"
        or "partial"
        or "record" or "remove"
        or "select" or "set"
        or "unmanaged"
        or "value" or "var"
        or "when" or "where" or "with"
        or "yield" => '@' + identifier,
        // Others
        _ => identifier,
    };
}
