#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DotNetCampus.CodeAnalysis.Utils.Generators.Builders;

/// <summary>
/// 辅助链式生成源代码文本的构建器。
/// </summary>
public class SourceTextBuilder : IDisposable,
    IAllowScopedNamespace, IAllowTypeDeclaration, IAllowStatements
{
    private readonly HashSet<string> _systemUsings = [];
    private readonly HashSet<string> _otherUsings = [];
    private readonly HashSet<string> _staticUsings = [];
    private readonly HashSet<string> _aliasUsings = [];
    private readonly List<IndentSourceTextBuilder> _topLevelCode = [];
    private readonly IDisposable _scope;

    /// <summary>
    /// 创建具有指定命名空间的 <see cref="SourceTextBuilder"/> 实例。<br/>
    /// 请务必使用 <see langword="using"/> 语句来确保调用 <see cref="Dispose"/> 方法。
    /// </summary>
    /// <param name="namespace">命名空间。如果传 <see langword="null"/>，表示使用全局命名空间。</param>
    public SourceTextBuilder(string? @namespace = null)
    {
        Namespace = @namespace;
        _scope = SourceTextBuilderExtensions.BeginBuild(this);
    }

    /// <summary>
    /// 根 <see cref="SourceTextBuilder"/> 实例。
    /// </summary>
    public SourceTextBuilder Root => this;

    /// <summary>
    /// 是否使用文件作用域的命名空间。
    /// </summary>
    public bool UseFileScopedNamespace { get; init; } = true;

    /// <summary>
    /// 此源代码文件的命名空间。（目前一个源代码文件只支持一个命名空间。）
    /// </summary>
    public string? Namespace { get; }

    /// <summary>
    /// 缩进字符串。默认为四个空格（"    "）。
    /// </summary>
    public string Indent { get; init; } = "    ";

    /// <summary>
    /// 源代码中每一行的换行符。默认为换行符（"\n"）。
    /// </summary>
    public string NewLine { get; init; } = "\n";

    /// <summary>
    /// 是否在生成的源代码文本末尾添加一个换行符。默认为 <see langword="true"/>。
    /// </summary>
    public bool AppendNewLineAtEnd { get; init; } = true;

    /// <summary>
    /// 是否允许通过 using 引用命名空间，从而简化类型名称。<br/>
    /// 如果允许，则在生成类型名称时，会尝试将类型的命名空间添加到 using 列表中，并返回简化后的类型名称字符串。<br/>
    /// 如果不允许，则生成类型名称时，直接返回完整类型名称字符串。
    /// </summary>
    public bool SimplifyTypeNamesByUsingNamespace { get; init; }

    /// <summary>
    /// 是否给所有涉及到命名空间的代码添加 global:: 前缀。<br/>
    /// 在 <see cref="SimplifyTypeNamesByUsingNamespace"/> 为 <see langword="true"/> 时，此选项无效。
    /// </summary>
    public bool ShouldPrependGlobal { get; init; } = true;

    /// <summary>
    /// 添加 using 引用的命名空间。
    /// </summary>
    /// <param name="usingNamespace">要引用的命名空间。</param>
    /// <returns>辅助链式调用。</returns>
    public SourceTextBuilder Using(string usingNamespace)
    {
        var ns = usingNamespace.PrependGlobal(ShouldPrependGlobal);
        var systemNamespacePrefix = ShouldPrependGlobal ? "global::System" : "System";
        var isSystemNamespace = ns.Equals(systemNamespacePrefix, StringComparison.Ordinal)
                                || ns.StartsWith($"{systemNamespacePrefix}.", StringComparison.Ordinal);
        if (isSystemNamespace)
        {
            _systemUsings.Add(ns);
        }
        else
        {
            _otherUsings.Add(ns);
        }
        return this;
    }

    /// <summary>
    /// 添加 using static 引用的类型。
    /// </summary>
    /// <param name="usingStatic">要引用的类型。</param>
    /// <returns>辅助链式调用。</returns>
    public SourceTextBuilder UsingStatic(string usingStatic)
    {
        _staticUsings.Add(usingStatic.PrependGlobal(ShouldPrependGlobal));
        return this;
    }

    /// <summary>
    /// 添加类型别名。
    /// </summary>
    /// <param name="alias">别名。</param>
    /// <param name="fullTypeName">完整类型名称。</param>
    /// <returns>辅助链式调用。</returns>
    public SourceTextBuilder UsingTypeAlias(string alias, string fullTypeName)
    {
        _aliasUsings.Add($"{alias} = {fullTypeName.PrependGlobal(ShouldPrependGlobal)}");
        return this;
    }

    void ISourceTextBuilder.AddRawText(string rawText) => AddRawText(rawText);

    void IAllowNestedSourceTextBuilder.AddNestedSourceCode(IndentSourceTextBuilder memberBuilder) => _topLevelCode.Add(memberBuilder);

    /// <inheritdoc cref="ISourceTextBuilder.AddRawText" />
    public SourceTextBuilder AddRawText(string rawText)
    {
        var rawDeclaration = new RawSourceTextBuilder(Root)
        {
            RawText = rawText,
        };
        _topLevelCode.Add(rawDeclaration);
        return this;
    }

    /// <summary>
    /// 本源代码生成器包含一些指示如何生成源代码的选项，在此期间创建的文本会使用这些选项。<br/>
    /// 在处置完成后，那些选项将不再起作用。
    /// </summary>
    public void Dispose()
    {
        _scope.Dispose();
    }

    /// <summary>
    /// 生成源代码文本。
    /// </summary>
    /// <returns>生成的源代码文本。</returns>
    public override string ToString()
    {
        var builder = new StringBuilder();
        BuildInto(builder, 0);
        return builder.ToString();
    }

    /// <inheritdoc cref="IndentSourceTextBuilder.BuildInto(StringBuilder, int)" />
    public void BuildInto(StringBuilder builder, int indentLevel)
    {
        builder.AppendLine("#nullable enable");

        // usings
        foreach (var line in _systemUsings.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
        {
            builder.AppendLine($"using {line};");
        }
        foreach (var line in _otherUsings.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
        {
            builder.AppendLine($"using {line};");
        }
        foreach (var line in _staticUsings.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
        {
            builder.AppendLine($"using static {line};");
        }
        foreach (var line in _aliasUsings.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
        {
            builder.AppendLine($"using {line};");
        }
        if (_systemUsings.Count > 0 || _otherUsings.Count > 0 || _staticUsings.Count > 0 || _aliasUsings.Count > 0)
        {
            builder.AppendLine();
        }

        // namespace
        if (Namespace is { } @namespace)
        {
            if (UseFileScopedNamespace)
            {
                builder.AppendLine($"namespace {Namespace};").AppendLine();
            }
            else
            {
                builder.AppendLine($"namespace {Namespace}");
            }
        }
        using var _ = UseFileScopedNamespace || Namespace is null
            ? EmptyScope.Begin()
            : BracketScope.Begin(builder, Indent, indentLevel);
        var typeIndentLevel = UseFileScopedNamespace ? 0 : indentLevel + 1;

        // types
        for (var i = 0; i < _topLevelCode.Count; i++)
        {
            if (i > 0)
            {
                builder.AppendLine();
            }

            _topLevelCode[i].BuildInto(builder, typeIndentLevel);
        }

        // 统一化换行符
        builder.Replace("\r", "");

        // 确保最后有且仅有一个换行符
        while (builder.Length > 0 && char.IsWhiteSpace(builder[^1]))
        {
            builder.Length--;
        }
        if (AppendNewLineAtEnd)
        {
            builder.AppendLine();
        }
    }
}

/// <summary>
/// 命名空间声明源代码文本构建器。使用这种方式创建的命名空间只能是传统的大括号包裹的命名空间。
/// </summary>
/// <param name="root">根 <see cref="SourceTextBuilder"/> 实例。</param>
/// <param name="namespace">命名空间。</param>
public class NamespaceDeclarationSourceTextBuilder(SourceTextBuilder root, string @namespace) : IndentSourceTextBuilder(root),
    IAllowTypeDeclaration
{
    private readonly string _namespace = @namespace;
    private readonly List<IndentSourceTextBuilder> _typeDeclarations = [];

    void ISourceTextBuilder.AddRawText(string rawText) => AddRawText(rawText);

    void IAllowNestedSourceTextBuilder.AddNestedSourceCode(IndentSourceTextBuilder memberBuilder) => _typeDeclarations.Add(memberBuilder);

    /// <inheritdoc cref="ISourceTextBuilder.AddRawText" />
    public NamespaceDeclarationSourceTextBuilder AddRawText(string rawText)
    {
        var rawDeclaration = new RawSourceTextBuilder(Root)
        {
            RawText = rawText,
        };
        _typeDeclarations.Add(rawDeclaration);
        return this;
    }

    /// <inheritdoc />
    public override void BuildInto(StringBuilder builder, int indentLevel)
    {
        builder.Append("namespace ").AppendLine(_namespace);
        using var _ = BracketScope.Begin(builder, Indent, indentLevel);
        BuildMembersInto(builder, indentLevel + 1, _typeDeclarations);
    }
}

/// <summary>
/// 类型声明源代码文本构建器。
/// </summary>
/// <param name="root">根 <see cref="SourceTextBuilder"/> 实例。</param>
/// <param name="declarationLine">类型声明行（如 "public class MyClass"）。</param>
public class TypeDeclarationSourceTextBuilder(SourceTextBuilder root, string declarationLine) : IndentSourceTextBuilder(root),
    IAllowTypeDeclaration, IAllowMemberDeclaration, IAllowDocumentationComment, IAllowAttributes, IAllowTypeConstraints
{
    private DocumentationCommentSourceTextBuilder? _documentationCommentBuilder;
    private AttributeListSourceTextBuilder? _attributeListBuilder;
    private TypeConstraintsSourceTextBuilder? _typeConstraintBuilder;
    private readonly List<string> _baseTypes = [];
    private readonly List<IndentSourceTextBuilder> _members = [];

    /// <summary>
    /// 类型声明行（如 "public class MyClass"）。
    /// </summary>
    public string DeclarationLine { get; } = declarationLine;

    DocumentationCommentSourceTextBuilder IAllowDocumentationComment.DocumentationCommentBuilder =>
        _documentationCommentBuilder ??= new DocumentationCommentSourceTextBuilder(Root);

    AttributeListSourceTextBuilder IAllowAttributes.AttributeListBuilder =>
        _attributeListBuilder ??= new AttributeListSourceTextBuilder(Root);

    TypeConstraintsSourceTextBuilder IAllowTypeConstraints.TypeConstraintsBuilder =>
        _typeConstraintBuilder ??= new TypeConstraintsSourceTextBuilder(Root);

    /// <summary>
    /// 为此类型声明添加基类或接口。
    /// </summary>
    /// <param name="baseTypes">要添加的基类或接口名称。</param>
    /// <returns>辅助链式调用。</returns>
    public TypeDeclarationSourceTextBuilder AddBaseTypes(params ReadOnlySpan<string> baseTypes)
    {
        foreach (var baseType in baseTypes)
        {
            _baseTypes.Add(baseType);
        }
        return this;
    }

    void ISourceTextBuilder.AddRawText(string rawText) => AddRawText(rawText);

    void IAllowNestedSourceTextBuilder.AddNestedSourceCode(IndentSourceTextBuilder memberBuilder) => _members.Add(memberBuilder);

    /// <inheritdoc cref="ISourceTextBuilder.AddRawText" />
    public TypeDeclarationSourceTextBuilder AddRawText(string rawText)
    {
        var rawDeclaration = new RawSourceTextBuilder(Root)
        {
            RawText = rawText,
        };
        _members.Add(rawDeclaration);
        return this;
    }

    /// <inheritdoc />
    public override void BuildInto(StringBuilder builder, int indentLevel)
    {
        if (_documentationCommentBuilder is { } documentationCommentBuilder)
        {
            documentationCommentBuilder.BuildInto(builder, indentLevel);
        }
        if (_attributeListBuilder is { } attributeListBuilder)
        {
            attributeListBuilder.BuildInto(builder, indentLevel);
        }
        builder.AppendWithIndent(DeclarationLine, Indent, indentLevel);
        if (_typeConstraintBuilder is { } typeConstraintBuilder)
        {
            typeConstraintBuilder.BuildInto(builder, indentLevel + 1);
        }
        if (_baseTypes.Count > 0)
        {
            for (var i = 0; i < _baseTypes.Count; i++)
            {
                builder.Append(i is 0 ? " : " : ", ");
                builder.Append(_baseTypes[i]);
            }
        }
        builder.AppendLine();

        using var _ = BracketScope.Begin(builder, Indent, indentLevel);
        BuildMembersInto(builder, indentLevel + 1, _members);
    }
}

/// <summary>
/// 方法声明源代码文本构建器。
/// </summary>
/// <param name="root">根 <see cref="SourceTextBuilder"/> 实例。</param>
/// <param name="signature">方法签名行（如 "public void MyMethod()"）。</param>
public class MethodDeclarationSourceTextBuilder(SourceTextBuilder root, string signature) : IndentSourceTextBuilder(root),
    IAllowStatements, IAllowMethodDeclaration, IAllowDocumentationComment, IAllowAttributes, IAllowTypeConstraints
{
    private DocumentationCommentSourceTextBuilder? _documentationCommentBuilder;
    private AttributeListSourceTextBuilder? _attributeListBuilder;
    private TypeConstraintsSourceTextBuilder? _typeConstraintBuilder;
    private readonly List<IndentSourceTextBuilder> _statements = [];

    DocumentationCommentSourceTextBuilder IAllowDocumentationComment.DocumentationCommentBuilder =>
        _documentationCommentBuilder ??= new DocumentationCommentSourceTextBuilder(Root);

    AttributeListSourceTextBuilder IAllowAttributes.AttributeListBuilder =>
        _attributeListBuilder ??= new AttributeListSourceTextBuilder(Root);

    TypeConstraintsSourceTextBuilder IAllowTypeConstraints.TypeConstraintsBuilder =>
        _typeConstraintBuilder ??= new TypeConstraintsSourceTextBuilder(Root);

    /// <summary>
    /// 方法签名行（如 "public void MyMethod()"）。
    /// </summary>
    public string Signature { get; } = signature;

    void ISourceTextBuilder.AddRawText(string rawText) => AddRawText(rawText);

    void IAllowNestedSourceTextBuilder.AddNestedSourceCode(IndentSourceTextBuilder memberBuilder) => _statements.Add(memberBuilder);

    /// <inheritdoc cref="ISourceTextBuilder.AddRawText" />
    MethodDeclarationSourceTextBuilder AddRawText(string rawText)
    {
        var statement = new RawSourceTextBuilder(Root)
        {
            RawText = rawText,
        };
        _statements.Add(statement);
        return this;
    }

    /// <inheritdoc />
    public override void BuildInto(StringBuilder builder, int indentLevel)
    {
        if (_documentationCommentBuilder is { } documentationCommentBuilder)
        {
            documentationCommentBuilder.BuildInto(builder, indentLevel);
        }
        if (_attributeListBuilder is { } attributeListBuilder)
        {
            attributeListBuilder.BuildInto(builder, indentLevel);
        }
        builder.AppendLineWithIndent(Signature, Indent, indentLevel);
        if (_typeConstraintBuilder is { } typeConstraintBuilder)
        {
            typeConstraintBuilder.BuildInto(builder, indentLevel + 1);
        }
        using var _ = BracketScope.Begin(builder, Indent, indentLevel);
        var methodBodyIndentLevel = indentLevel + 1;
        foreach (var statement in _statements)
        {
            if (statement is CodeBlockSourceTextBuilder { WrapWithBracket: true } codeBlock)
            {
                using var c = BracketScope.Begin(builder, Indent, methodBodyIndentLevel);
                codeBlock.BuildInto(builder, methodBodyIndentLevel + 1);
            }
            else
            {
                statement.BuildInto(builder, methodBodyIndentLevel);
            }
        }
    }
}

/// <summary>
/// 代码块源代码文本构建器。
/// </summary>
/// <param name="root">根 <see cref="SourceTextBuilder"/> 实例。</param>
/// <param name="wrapWithBracket">是否使用大括号包裹代码块。</param>
public class CodeBlockSourceTextBuilder(SourceTextBuilder root, bool wrapWithBracket) : IndentSourceTextBuilder(root),
    IAllowStatements
{
    private readonly List<IndentSourceTextBuilder> _statements = [];

    /// <summary>
    /// 是否使用大括号包裹代码块。
    /// </summary>
    internal bool WrapWithBracket => wrapWithBracket;

    void ISourceTextBuilder.AddRawText(string rawText) => AddRawText(rawText);

    void IAllowNestedSourceTextBuilder.AddNestedSourceCode(IndentSourceTextBuilder memberBuilder) => _statements.Add(memberBuilder);

    /// <inheritdoc cref="ISourceTextBuilder.AddRawText" />
    CodeBlockSourceTextBuilder AddRawText(string rawText)
    {
        var statement = new RawSourceTextBuilder(Root)
        {
            RawText = rawText,
        };
        _statements.Add(statement);
        return this;
    }

    /// <inheritdoc />
    public override void BuildInto(StringBuilder builder, int indentLevel)
    {
        foreach (var statement in _statements)
        {
            if (statement is CodeBlockSourceTextBuilder { WrapWithBracket: true } codeBlock)
            {
                using var _ = BracketScope.Begin(builder, Indent, indentLevel);
                codeBlock.BuildInto(builder, indentLevel + 1);
            }
            else
            {
                statement.BuildInto(builder, indentLevel);
            }
        }
    }
}

/// <summary>
/// 原始文本块源代码文本构建器。
/// </summary>
/// <param name="root">根 <see cref="SourceTextBuilder"/> 实例。</param>
public class RawSourceTextBuilder(SourceTextBuilder root) : IndentSourceTextBuilder(root)
{
    /// <summary>
    /// 要添加的原始文本块。
    /// </summary>
    public required string RawText { get; set; }

    /// <inheritdoc />
    public override void BuildInto(StringBuilder builder, int indentLevel)
    {
        builder.AppendLineWithIndent(RawText, Indent, indentLevel);
    }
}

/// <summary>
/// 文档注释源代码文本构建器。
/// </summary>
/// <param name="root">根 <see cref="SourceTextBuilder"/> 实例。</param>
public class DocumentationCommentSourceTextBuilder(SourceTextBuilder root) : IndentSourceTextBuilder(root)
{
    private string? _summary;
    private readonly List<string> _params = [];
    private string? _returns;
    private string? _remarks;
    private string? _footerRawText;

    /// <summary>
    /// 为此类型声明添加 summary 文档注释。
    /// </summary>
    /// <param name="summary">要添加的 summary 文档注释。</param>
    /// <returns>辅助链式调用。</returns>
    public DocumentationCommentSourceTextBuilder Summary(string summary)
    {
        _summary = $"""
            <summary>
            {summary}
            </summary>
            """;
        return this;
    }

    /// <summary>
    /// 为此类型声明添加 param 文档注释。
    /// </summary>
    /// <param name="paramName">参数名称。</param>
    /// <param name="paramDescription">参数描述。</param>
    /// <returns>辅助链式调用。</returns>
    public DocumentationCommentSourceTextBuilder AddParam(string paramName, string paramDescription)
    {
        var param = paramDescription.IndexOf('\n') >= 0
            ? $"""
                <param name="{paramName}">
                {paramDescription}
                </param>
                """
            : $"""<param name="{paramName}">{paramDescription}</param>""";
        _params.Add(param);
        return this;
    }

    /// <summary>
    /// 为此类型声明添加 returns 文档注释。
    /// </summary>
    /// <param name="returns">要添加的 returns 文档注释。</param>
    /// <returns>辅助链式调用。</returns>
    public DocumentationCommentSourceTextBuilder Returns(string returns)
    {
        _returns = returns.IndexOf('\n') >= 0
            ? $"""
                <returns>
                {returns}
                </returns>
                """
            : $"<returns>{returns}</remarks>";
        return this;
    }

    /// <summary>
    /// 为此类型声明添加 remarks 文档注释。
    /// </summary>
    /// <param name="remarks">要添加的 remarks 文档注释。</param>
    /// <returns>辅助链式调用。</returns>
    public DocumentationCommentSourceTextBuilder Remarks(string remarks)
    {
        _remarks = $"""
            <remarks>
            {remarks}
            </remarks>
            """;
        return this;
    }

    /// <summary>
    /// 在此文档注释的末尾添加原始文档注释字符串。可带 /// 前缀，也可不带（将自动添加）。
    /// </summary>
    /// <param name="rawText">要添加的原始文档注释。</param>
    /// <returns>辅助链式调用。</returns>
    public DocumentationCommentSourceTextBuilder AddRawText(string rawText)
    {
        var builder = new StringBuilder();
        var lines = rawText.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
        foreach (var line in lines)
        {
            var trimmedLine = line.StartsWith("///") ? line.AsSpan()[3..].Trim() : line.AsSpan().Trim();
            builder.AppendLine(trimmedLine.ToString());
        }
        _footerRawText = builder.ToString();
        return this;
    }

    /// <inheritdoc />
    public override void BuildInto(StringBuilder builder, int indentLevel)
    {
        if (_summary is { } summary)
        {
            builder.AppendWithIndentAndPrefix(summary, "/// ", Indent, indentLevel).AppendLine();
        }
        foreach (var param in _params)
        {
            builder.AppendWithIndentAndPrefix(param, "/// ", Indent, indentLevel).AppendLine();
        }
        if (_returns is { } returns)
        {
            builder.AppendWithIndentAndPrefix(returns, "/// ", Indent, indentLevel).AppendLine();
        }
        if (_remarks is { } remarks)
        {
            builder.AppendWithIndentAndPrefix(remarks, "/// ", Indent, indentLevel).AppendLine();
        }
        if (_footerRawText is { } footerRawText)
        {
            builder.AppendWithIndentAndPrefix(footerRawText, "/// ", Indent, indentLevel).AppendLine();
        }
    }
}

/// <summary>
/// 特性列表源代码文本构建器。
/// </summary>
/// <param name="root">根 <see cref="SourceTextBuilder"/> 实例。</param>
public class AttributeListSourceTextBuilder(SourceTextBuilder root) : IndentSourceTextBuilder(root)
{
    private readonly List<string> _attributes = [];

    /// <inheritdoc cref="ISourceTextBuilder.AddRawText" />
    public AttributeListSourceTextBuilder AddAttribute(string rawText)
    {
        _attributes.Add(rawText);
        return this;
    }

    /// <inheritdoc />
    public override void BuildInto(StringBuilder builder, int indentLevel)
    {
        foreach (var attribute in _attributes)
        {
            builder.AppendLineWithIndent($"{attribute}", Indent, indentLevel);
        }
    }
}

/// <summary>
/// 泛型约束源代码文本构建器。
/// </summary>
/// <param name="root">根 <see cref="SourceTextBuilder"/> 实例。</param>
public class TypeConstraintsSourceTextBuilder(SourceTextBuilder root) : IndentSourceTextBuilder(root)
{
    private readonly List<string> _typeConstraints = [];

    /// <summary>
    /// 为此类型声明添加泛型约束。
    /// </summary>
    /// <param name="typeConstraint">要添加的泛型约束。</param>
    /// <returns>辅助链式调用。</returns>
    public TypeConstraintsSourceTextBuilder AddTypeConstraint(string typeConstraint)
    {
        _typeConstraints.Add(typeConstraint);
        return this;
    }

    /// <inheritdoc />
    public override void BuildInto(StringBuilder builder, int indentLevel)
    {
        for (var i = 0; i < _typeConstraints.Count; i++)
        {
            var constraint = _typeConstraints[i];
            builder.AppendLineWithIndent(
                i == _typeConstraints.Count - 1 ? $"{constraint}" : $"{constraint},",
                Indent, indentLevel + 1);
        }
    }
}

/// <summary>
/// 可自动添加大括号作用域的辅助类。
/// </summary>
file class BracketScope : IDisposable
{
    private readonly StringBuilder _builder;
    private readonly string _indent;
    private readonly int _indentLevel;

    public BracketScope(StringBuilder builder, string indent, int indentLevel)
    {
        _builder = builder;
        _indent = indent;
        _indentLevel = indentLevel;
        for (var i = 0; i < _indentLevel; i++)
        {
            _builder.Append(_indent);
        }
        _builder.AppendLine("{");
    }

    public void Dispose()
    {
        for (var i = 0; i < _indentLevel; i++)
        {
            _builder.Append(_indent);
        }
        _builder.AppendLine("}");
    }

    public static IDisposable Begin(StringBuilder builder, string indent, int indentLevel)
    {
        return new BracketScope(builder, indent, indentLevel);
    }
}

/// <summary>
/// 空作用域辅助类（什么都不做）。这是为了能与 <see cref="BracketScope"/> 统一使用方式。
/// </summary>
file class EmptyScope : IDisposable
{
    public void Dispose()
    {
    }

    public static IDisposable Begin()
    {
        return new EmptyScope();
    }
}

file static class Extensions
{
    internal static string PrependGlobal(this string name, bool? shouldPrependGlobal = true) => shouldPrependGlobal switch
    {
        // 一定加上 global:: 前缀
        true => name.StartsWith("global::", StringComparison.Ordinal) ? name : $"global::{name}",
        // 一定去掉 global:: 前缀
        false => name.StartsWith("global::", StringComparison.Ordinal) ? name[8..] : name,
        // 保持不变
        null => name,
    };

    internal static StringBuilder AppendIndent(this StringBuilder builder, string indent, int indentLevel)
    {
        for (var indentIndex = 0; indentIndex < indentLevel; indentIndex++)
        {
            builder.Append(indent);
        }
        return builder;
    }

    internal static StringBuilder AppendWithIndent(this StringBuilder builder, string text, string indent, int indentLevel)
    {
        return AppendWithIndentAndPrefix(builder, text, "", indent, indentLevel);
    }

    internal static StringBuilder AppendLineWithIndent(this StringBuilder builder, string text, string indent, int indentLevel)
    {
        return AppendWithIndent(builder, text, indent, indentLevel).AppendLine();
    }

    internal static StringBuilder AppendWithIndentAndPrefix(this StringBuilder builder, string text, string prefix, string indent, int indentLevel)
    {
        var currentLineStart = 0;
        for (var index = 0; index < text.Length; index++)
        {
            if (text[index] != '\n' && index != text.Length - 1)
            {
                continue;
            }

            var isPreprocessorDirective = text[currentLineStart] == '#';
            if (string.IsNullOrEmpty(prefix))
            {
                if (!isPreprocessorDirective)
                {
                    builder.AppendIndent(indent, indentLevel);
                }
            }
            else
            {
                builder.AppendIndent(indent, indentLevel).Append(prefix);
            }
            builder.Append(text, currentLineStart, index - currentLineStart + 1);
            currentLineStart = index + 1;
        }
        return builder;
    }
}
