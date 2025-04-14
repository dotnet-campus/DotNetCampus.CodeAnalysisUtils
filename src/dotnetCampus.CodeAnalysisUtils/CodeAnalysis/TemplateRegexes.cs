using System.Text.RegularExpressions;

namespace DotNetCampus.CodeAnalysis.Utils.CodeAnalysis;

/// <summary>
/// 为通过模板生成的源代码提供正则表达式。
/// </summary>
public static class TemplateRegexes
{
    private static Regex? _flagRegex;
    private static Regex? _flag1Regex;
    private static Regex? _flag2Regex;
    private static Regex? _flag3Regex;
    private static Regex? _flag4Regex;

    private static Regex FlagRegex => _flagRegex ??= GetFlagRegex();
    private static Regex Flag1Regex => _flag1Regex ??= GetFlag1Regex();
    private static Regex Flag2Regex => _flag2Regex ??= GetFlag2Regex();
    private static Regex Flag3Regex => _flag3Regex ??= GetFlag3Regex();
    private static Regex Flag4Regex => _flag4Regex ??= GetFlag4Regex();

    private static Regex GetFlagRegex() => _flagRegex ??= new Regex(@"(?<=\n)\s+// <FLAG>.+?</FLAG>", RegexOptions.Compiled | RegexOptions.Singleline);
    private static Regex GetFlag1Regex() => _flag2Regex ??= new Regex(@"(?<=\n)\s+// <FLAG1>.+?</FLAG1>", RegexOptions.Compiled | RegexOptions.Singleline);
    private static Regex GetFlag2Regex() => _flag2Regex ??= new Regex(@"(?<=\n)\s+// <FLAG2>.+?</FLAG2>", RegexOptions.Compiled | RegexOptions.Singleline);
    private static Regex GetFlag3Regex() => _flag3Regex ??= new Regex(@"(?<=\n)\s+// <FLAG3>.+?</FLAG3>", RegexOptions.Compiled | RegexOptions.Singleline);
    private static Regex GetFlag4Regex() => _flag3Regex ??= new Regex(@"(?<=\n)\s+// <FLAG4>.+?</FLAG4>", RegexOptions.Compiled | RegexOptions.Singleline);

    /// <summary>
    /// 替换代码中的 // <FLAG>...</FLAG> 注释，将其替换为指定的内容。
    /// </summary>
    /// <param name="content">包含要替换的代码的字符串。</param>
    /// <param name="flagContent">要替换的内容。</param>
    /// <returns>替换后的字符串。</returns>
    public static string FlagReplace(this string content, string flagContent)
    {
        return FlagRegex.Replace(content, flagContent);
    }

    /// <summary>
    /// 替换代码中的 // <FLAG1>...</FLAG1> 注释，将其替换为指定的内容。
    /// </summary>
    /// <param name="content">包含要替换的代码的字符串。</param>
    /// <param name="flagContent">要替换的内容。</param>
    /// <returns>替换后的字符串。</returns>
    public static string Flag1Replace(this string content, string flagContent)
    {
        return Flag1Regex.Replace(content, flagContent);
    }

    /// <summary>
    /// 替换代码中的 // <FLAG2>...</FLAG2> 注释，将其替换为指定的内容。
    /// </summary>
    /// <param name="content">包含要替换的代码的字符串。</param>
    /// <param name="flagContent">要替换的内容。</param>
    /// <returns>替换后的字符串。</returns>
    public static string Flag2Replace(this string content, string flagContent)
    {
        return Flag2Regex.Replace(content, flagContent);
    }

    /// <summary>
    /// 替换代码中的 // <FLAG3>...</FLAG3> 注释，将其替换为指定的内容。
    /// </summary>
    /// <param name="content">包含要替换的代码的字符串。</param>
    /// <param name="flagContent">要替换的内容。</param>
    /// <returns>替换后的字符串。</returns>
    public static string Flag3Replace(this string content, string flagContent)
    {
        return Flag3Regex.Replace(content, flagContent);
    }

    /// <summary>
    /// 替换代码中的 // <FLAG4>...</FLAG4> 注释，将其替换为指定的内容。
    /// </summary>
    /// <param name="content">包含要替换的代码的字符串。</param>
    /// <param name="flagContent">要替换的内容。</param>
    /// <returns>替换后的字符串。</returns>
    public static string Flag4Replace(this string content, string flagContent)
    {
        return Flag4Regex.Replace(content, flagContent);
    }
}
