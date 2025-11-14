#nullable enable
using System;
using System.Text;

namespace DotNetCampus.CodeAnalysis.Utils.Generators.Builders;

/// <summary>
/// 带有缩进功能的字符串构建器。
/// </summary>
public class IndentedStringBuilder
{
    private readonly StringBuilder _builder = new StringBuilder();
    private readonly StringBuilder _lineBuffer = new StringBuilder();
    private IndentLineProcessorHandler? _tempProcessor;

    /// <summary>
    /// 获取缩进字符串。
    /// </summary>
    public string Indentation { get; init; } = "    ";

    /// <summary>
    /// 获取或设置换行符。
    /// </summary>
    public string NewLine { get; init; } = "\n";

    /// <summary>
    /// 获取当前的缩进级别。
    /// </summary>
    public int IndentLevel { get; private set; }

    /// <summary>
    /// 获取或设置行处理器。行处理器可以自定义对每一行文本的处理方式，例如决定某些行不进行缩进等。<br/>
    /// 可从 <see cref="IndentLineProcessors"/> 类中获取一些常用的行处理器。
    /// </summary>
    public IndentLineProcessorHandler? LineProcessor { get; init; }

    /// <summary>
    /// 增加一个缩进级别。通过 using 本方法的返回值，可以在 using 作用域结束时自动恢复到之前的缩进级别。
    /// </summary>
    /// <param name="levels">
    /// 增加的缩进级别数。<br/>
    /// 默认增加 1 级，通过修改为 0 可以不增加缩进级别（适合在 switch 表达式或 ?: 表达式中与需要缩进的代码块配合使用）。
    /// </param>
    /// <returns>用于恢复缩进级别的 <see cref="IDisposable"/> 对象。</returns>
    public IndentScope IndentIn(int levels = 1)
    {
        return new IndentScope(this, levels);
    }

    /// <summary>
    /// 添加文本，不添加换行符。
    /// </summary>
    /// <param name="text">要写入的文本。</param>
    /// <returns>辅助链式调用。</returns>
    public IndentedStringBuilder Append(string text) => AppendCore(text.AsSpan(), false);

    /// <summary>
    /// 添加文本，不添加换行符。使用指定的行处理器处理本次添加的文本。
    /// </summary>
    /// <param name="text">要写入的文本。</param>
    /// <param name="lineProcessor">
    /// 用于处理行的行处理器。<br/>
    /// 如果想临时禁用全局设置的行处理器，可传入 <see cref="IndentLineProcessors.Default"/>。
    /// </param>
    /// <returns>辅助链式调用。</returns>
    public IndentedStringBuilder Append(string text, IndentLineProcessorHandler lineProcessor)
    {
        _tempProcessor = lineProcessor;
        try
        {
            return Append(text);
        }
        finally
        {
            _tempProcessor = null;
        }
    }

    /// <summary>
    /// 添加文本，不添加换行符。
    /// </summary>
    /// <param name="text">要写入的文本。</param>
    /// <returns>辅助链式调用。</returns>
    public IndentedStringBuilder Append(ReadOnlySpan<char> text) => AppendCore(text, false);

    /// <summary>
    /// 添加换行符。
    /// </summary>
    /// <returns>辅助链式调用。</returns>
    public IndentedStringBuilder AppendLine() => AppendCore([], true);

    /// <summary>
    /// 添加文本，并继续添加换行符。
    /// </summary>
    /// <param name="text">要写入的文本。</param>
    /// <returns>辅助链式调用。</returns>
    public IndentedStringBuilder AppendLine(string text) => AppendCore(text.AsSpan(), true);

    /// <summary>
    /// 添加文本，并继续添加换行符。
    /// </summary>
    /// <param name="text">要写入的文本。</param>
    /// <param name="lineProcessor">
    /// 用于处理行的行处理器。<br/>
    /// 如果想临时禁用全局设置的行处理器，可传入 <see cref="IndentLineProcessors.Default"/>。
    /// </param>
    /// <returns>辅助链式调用。</returns>
    public IndentedStringBuilder AppendLine(string text, IndentLineProcessorHandler lineProcessor)
    {
        _tempProcessor = lineProcessor;
        try
        {
            return AppendLine(text);
        }
        finally
        {
            _tempProcessor = null;
        }
    }

    /// <summary>
    /// 添加文本，并继续添加换行符。
    /// </summary>
    /// <param name="text">要写入的文本。</param>
    /// <returns>辅助链式调用。</returns>
    public IndentedStringBuilder AppendLine(ReadOnlySpan<char> text) => AppendCore(text, true);

    /// <summary>
    /// 直接添加原始字符串，不进行任何处理（不考虑缩进，也不处理换行符）。<br/>
    /// 请注意，在调用本方法之前，如果当前行存在文本，则会立即换行，再添加原始字符串。
    /// </summary>
    /// <param name="rawText">要写入的文本。</param>
    /// <returns>辅助链式调用。</returns>
    public IndentedStringBuilder AppendRaw(string rawText)
    {
        if (_lineBuffer.Length > 0)
        {
            // 在添加原始字符串之前，立即把行缓冲区的内容写入。
            AppendLine(_lineBuffer.ToString());
        }

        // 直接添加原始字符串，不进行任何处理。
        _builder.Append(rawText);
        return this;
    }

    /// <summary>
    /// 直接添加原始字符串，不进行任何处理（不考虑缩进，也不处理换行符）。<br/>
    /// 请注意，在调用本方法之前，如果当前行存在文本，则会立即换行，再添加原始字符串后再换行。
    /// </summary>
    /// <param name="rawText">要写入的文本。</param>
    /// <returns>辅助链式调用。</returns>
    public IndentedStringBuilder AppendRawLine(string rawText)
    {
        AppendRaw(rawText);
        _builder.Append(NewLine);
        return this;
    }

    private IndentedStringBuilder AppendCore(ReadOnlySpan<char> text, bool newLine)
    {
        return (_lineBuffer.Length is 0, newLine) switch
        {
            // 正准备写入完整的一行。
            (true, true) => AppendNewLine(text),
            // 正在写入一行的开头。
            (true, false) => AppendLineStart(text),
            // 在行前有内容的情况下，写入本行剩余部分。
            (false, true) => AppendLineEnd(text),
            // 写入一行的中间部分。
            (false, false) => AppendLineMiddle(text),
        };
    }

    private IndentedStringBuilder AppendNewLine(ReadOnlySpan<char> text)
    {
        var leftPart = text;
        while (leftPart.Length > 0)
        {
            var newLineIndex = leftPart.IndexOf('\n');
            if (newLineIndex < 0)
            {
                // 剩余部分已经没有换行符了，直接写入。
                AppendInline(leftPart);
                break;
            }

            // 提取当前行，提取后续部分继续循环处理。
            var line = leftPart[..newLineIndex].TrimEnd('\r');
            leftPart = leftPart[(newLineIndex + 1)..];

            AppendInline(line);
            _builder.Append(NewLine);
        }
        // 最后一个换行符。
        _builder.Append(NewLine);
        return this;
    }

    private IndentedStringBuilder AppendLineStart(ReadOnlySpan<char> text)
    {
        _lineBuffer.Append(text);
        return this;
    }

    private IndentedStringBuilder AppendLineEnd(ReadOnlySpan<char> text)
    {
        _lineBuffer.Append(text);
        var line = _lineBuffer.ToString();
        _lineBuffer.Clear();
        AppendNewLine(line.AsSpan());
        return this;
    }

    private IndentedStringBuilder AppendLineMiddle(ReadOnlySpan<char> text)
    {
        _lineBuffer.Append(text.ToString());
        return this;
    }

    /// <summary>
    /// 视参数 <paramref name="line"/> 为单行文本（不包含换行符），并将其写入。考虑缩进，但不添加换行符。
    /// </summary>
    /// <param name="line">单行文本。</param>
    /// <returns>辅助链式调用。</returns>
    private IndentedStringBuilder AppendInline(ReadOnlySpan<char> line)
    {
        var result = ProcessLine(line);
        if (result.Type is IndentLineType.NormalLine)
        {
            // 是普通文本行，应用缩进。
            for (var i = 0; i < IndentLevel; i++)
            {
                _builder.Append(Indentation);
            }
            _builder.Append(line.Slice(result.StartIndex, result.Length));
        }
        else if (result.Type is IndentLineType.NoIndentLine)
        {
            // 是无缩进行，直接写入。
            _builder.Append(line.Slice(result.StartIndex, result.Length));
        }
        else
        {
            throw new InvalidOperationException($"未知的行处理结果：{result}");
        }
        return this;
    }

    /// <summary>
    /// 处理一行文本，应用行处理器（如果有的话）。
    /// </summary>
    /// <param name="line">当前行文本（不包含换行符）。</param>
    /// <returns>处理结果，指示此行文本是否进行缩进。</returns>
    private IndentLineProcessedResult ProcessLine(ReadOnlySpan<char> line)
    {
        var processor = _tempProcessor ?? LineProcessor;
        return processor is not null
            ? processor(line)
            : new IndentLineProcessedResult(IndentLineType.NormalLine, 0, line.Length);
    }

    /// <summary>
    /// 移除字符串末尾的所有空白字符。
    /// </summary>
    /// <returns>辅助链式调用。</returns>
    public IndentedStringBuilder TrimEnd()
    {
        // 如果 _lineBuffer 中存在内容，去除其尾随空白。
        if (_lineBuffer.Length > 0)
        {
            _lineBuffer.Length = GetTrimmedLength(_lineBuffer);
            return this;
        }

        // 如果 _builder 为空，无需处理。
        if (_builder.Length == 0)
        {
            return this;
        }

        // 去除 _builder 末尾的所有空白字符后的长度。
        var trimmedLength = GetTrimmedLength(_builder);

        // 在去除空白后的内容中，从后向前查找最后一个换行符。
        var lastNewLineIndex = FindLastNewLineIndex(_builder, trimmedLength, NewLine);

        // 计算最后一行的起始位置（换行符之后，或者从 0 开始）。
        var lastLineStart = lastNewLineIndex >= 0 ? lastNewLineIndex + NewLine.Length : 0;

        // 跳过行首的缩进空格，找到实际内容的起始位置。
        var contentStart = SkipLeadingSpaces(_builder, lastLineStart, trimmedLength);

        // 将去除缩进后的内容移到 _lineBuffer。
        MoveRangeToTarget(_builder, _lineBuffer, contentStart, trimmedLength);

        // _builder 保留到换行符（包含换行符），如果没有换行符则清空。
        _builder.Length = lastNewLineIndex >= 0 ? lastNewLineIndex + NewLine.Length : 0;

        return this;

        static int GetTrimmedLength(StringBuilder builder)
        {
            var length = builder.Length;
            for (var i = builder.Length - 1; i >= 0; i--)
            {
                if (char.IsWhiteSpace(builder[i]))
                {
                    length--;
                }
                else
                {
                    break;
                }
            }
            return length;
        }

        static int FindLastNewLineIndex(StringBuilder builder, int endIndex, string newLine)
        {
            var newLineLength = newLine.Length;
            for (var i = endIndex - newLineLength; i >= 0; i--)
            {
                var isMatch = true;
                for (var j = 0; j < newLineLength; j++)
                {
                    if (builder[i + j] != newLine[j])
                    {
                        isMatch = false;
                        break;
                    }
                }
                if (isMatch)
                {
                    return i;
                }
            }
            return -1;
        }

        static int SkipLeadingSpaces(StringBuilder builder, int start, int end)
        {
            while (start < end && builder[start] == ' ')
            {
                start++;
            }
            return start;
        }

        static void MoveRangeToTarget(StringBuilder source, StringBuilder target, int start, int end)
        {
            for (var i = start; i < end; i++)
            {
                target.Append(source[i]);
            }
        }
    }

    /// <summary>
    /// 返回当前构建的字符串。
    /// </summary>
    /// <returns>当前构建的字符串。</returns>
    public override string ToString()
    {
        // 如果 _lineBuffer 中有内容，需要先将其写入 _builder。
        if (_lineBuffer.Length > 0)
        {
            // 临时将 _lineBuffer 的内容写入，但不清空 _lineBuffer。
            var tempBuilder = new StringBuilder(_builder.Length + Indentation.Length * IndentLevel + _lineBuffer.Length);
            tempBuilder.Append(_builder);

            // 应用缩进并写入 _lineBuffer 的内容。
            for (var i = 0; i < IndentLevel; i++)
            {
                tempBuilder.Append(Indentation);
            }
            tempBuilder.Append(_lineBuffer);

            return tempBuilder.ToString();
        }

        return _builder.ToString();
    }

    /// <summary>
    /// 用于管理缩进级别的作用域。当作用域结束时，缩进级别将恢复到创建作用域时的级别。
    /// </summary>
    public readonly ref struct IndentScope : IDisposable
    {
        private readonly IndentedStringBuilder _builder;
        private readonly int _indentLevel;

        internal IndentScope(IndentedStringBuilder builder, int levels)
        {
            _builder = builder;
            _indentLevel = builder.IndentLevel;
            builder.IndentLevel += levels;
        }

        /// <summary>
        /// 恢复到创建作用域时的缩进级别。
        /// </summary>
        public void Dispose()
        {
            _builder.IndentLevel = _indentLevel;
        }
    }
}

/// <summary>
/// 提供一些常用的缩进行处理器。
/// </summary>
public static class IndentLineProcessors
{
    /// <summary>
    /// 默认行处理器，所有行均视为普通文本行，进行缩进处理。
    /// </summary>
    public static IndentLineProcessorHandler Default { get; } = DefaultLineProcessor;

    /// <summary>
    /// C# 代码行处理器。<br/>
    /// 此处理器会：
    /// <list type="bullet">
    /// <item>识别预处理指令行（以 # 开头的行），并确保这些行不进行缩进。</item>
    /// </list>
    /// 识别预处理指令行（以 # 开头的行），并确保这些行不进行缩进。
    /// </summary>
    public static IndentLineProcessorHandler CSharp { get; } = ProcessCSharpCodeLine;

    /// <summary>
    /// 默认行处理器，所有行均视为普通文本行，进行缩进处理。
    /// </summary>
    /// <param name="line">当前行文本（不包含换行符）。</param>
    /// <returns>处理结果，指示此行文本是否进行缩进。</returns>
    private static IndentLineProcessedResult DefaultLineProcessor(ReadOnlySpan<char> line)
    {
        return new IndentLineProcessedResult(IndentLineType.NormalLine, 0, line.Length);
    }

    private static IndentLineProcessedResult ProcessCSharpCodeLine(ReadOnlySpan<char> line)
    {
        // 查看当前行是否满足：行首是空白字符，随后紧跟着预处理命令 # 字符。
        var index = line.GetTrimmedIndex();
        return index < line.Length && line[index] == '#'
            // 是预处理命令行，直接写入，不考虑缩进。
            ? new IndentLineProcessedResult(IndentLineType.NoIndentLine, index, line.Length - index)
            // 不是预处理命令行，视为普通代码行，考虑缩进。
            : new IndentLineProcessedResult(IndentLineType.NormalLine, 0, line.Length);
    }

    private static int GetTrimmedIndex(this ReadOnlySpan<char> span)
    {
        var startingIndex = 0;
        while (startingIndex < span.Length && char.IsWhiteSpace(span[startingIndex]))
        {
            startingIndex++;
        }
        return startingIndex;
    }
}

/// <summary>
/// 支持自定义处理一行文本的缩进写入。
/// </summary>
/// <param name="line">当前行文本（不包含换行符）。</param>
/// <returns>处理结果，指示此行文本是否进行缩进，以及是否进行裁剪。</returns>
public delegate IndentLineProcessedResult IndentLineProcessorHandler(ReadOnlySpan<char> line);

/// <summary>
/// 表示对一行文本进行缩进处理后的结果。
/// </summary>
public readonly record struct IndentLineProcessedResult(IndentLineType Type, int StartIndex = 0, int Length = 0);

/// <summary>
/// 指示行处理的类型。
/// </summary>
public enum IndentLineType
{
    /// <summary>
    /// 普通文本行。
    /// </summary>
    NormalLine,

    /// <summary>
    /// 此行为「无缩进行」，即使当前处于缩进级别中，此行也不进行缩进。
    /// </summary>
    NoIndentLine,
}

#if !NETCOREAPP3_1_OR_GREATER
file static class Extensions
{
    public static StringBuilder Append(this StringBuilder builder, ReadOnlySpan<char> value)
    {
        foreach (var c in value)
        {
            builder.Append(c);
        }
        return builder;
    }
}
#endif
