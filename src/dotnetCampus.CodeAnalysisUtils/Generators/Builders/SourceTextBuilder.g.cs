#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace DotNetCampus.CodeAnalysis.Utils.Generators.Builders;

/// <summary>
/// 辅助链式生成源代码文本的构建器。
/// </summary>
public class SourceTextBuilder : IDisposable
{
    private readonly HashSet<string> _systemUsings = [];
    private readonly HashSet<string> _otherUsings = [];
    private readonly HashSet<string> _staticUsings = [];
    private readonly HashSet<string> _aliasUsings = [];
    private readonly List<BracketSourceTextBuilder> _topLevelCode = [];
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

    /// <summary>
    /// 添加原始文本块（此文本块与类型声明是平级的）。
    /// </summary>
    /// <param name="rawText">要添加的原始文本块。</param>
    /// <returns>辅助链式调用。</returns>
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
    /// 添加命名空间声明。
    /// </summary>
    /// <param name="namespace">命名空间。</param>
    /// <param name="namespaceDeclarationBuilder">命名空间声明构建器。</param>
    /// <returns>辅助链式调用。</returns>
    public SourceTextBuilder AddNamespaceDeclaration(string @namespace,
        Action<NamespaceDeclarationSourceTextBuilder> namespaceDeclarationBuilder)
    {
        var namespaceDeclaration = new NamespaceDeclarationSourceTextBuilder(Root, @namespace);
        namespaceDeclarationBuilder(namespaceDeclaration);
        _topLevelCode.Add(namespaceDeclaration);
        return this;
    }

    /// <summary>
    /// 添加类型声明。
    /// </summary>
    /// <param name="declarationLine">类型声明行（如 "public class MyClass"）。</param>
    /// <param name="typeDeclarationBuilder">类型声明构建器。</param>
    /// <returns>辅助链式调用。</returns>
    public SourceTextBuilder AddTypeDeclaration(string declarationLine,
        Action<TypeDeclarationSourceTextBuilder> typeDeclarationBuilder)
    {
        var typeDeclaration = new TypeDeclarationSourceTextBuilder(Root, declarationLine);
        typeDeclarationBuilder(typeDeclaration);
        _topLevelCode.Add(typeDeclaration);
        return this;
    }

    /// <summary>
    /// 添加类型声明。
    /// </summary>
    /// <param name="items">用于生成多个类型声明的数据源。</param>
    /// <param name="declarationLineBuilder">类型声明行构建器。</param>
    /// <param name="typeDeclarationBuilder">类型声明构建器。</param>
    /// <returns>辅助链式调用。</returns>
    public SourceTextBuilder AddTypeDeclarations<T>(IEnumerable<T> items,
        Func<T, string> declarationLineBuilder,
        Action<TypeDeclarationSourceTextBuilder, T> typeDeclarationBuilder)
    {
        foreach (var item in items)
        {
            var declarationLine = declarationLineBuilder(item);
            var typeDeclaration = new TypeDeclarationSourceTextBuilder(Root, declarationLine);
            typeDeclarationBuilder(typeDeclaration, item);
            _topLevelCode.Add(typeDeclaration);
        }
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

    /// <summary>
    /// 将生成的源代码文本写入指定的 <see cref="StringBuilder"/> 实例中。
    /// </summary>
    /// <param name="builder">源代码文本将被写入到此实例中。</param>
    /// <param name="indentLevel">缩进级别。</param>
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
/// 带有大括号的源代码文本构建器基类。
/// </summary>
/// <param name="root">根 <see cref="SourceTextBuilder"/> 实例。</param>
public abstract class BracketSourceTextBuilder(SourceTextBuilder root)
{
    /// <summary>
    /// 根 <see cref="SourceTextBuilder"/> 实例。
    /// </summary>
    public SourceTextBuilder Root => root;

    /// <summary>
    /// 获取缩进字符串。
    /// </summary>
    public string Indent => root.Indent;

    /// <summary>
    /// 将生成的源代码文本写入指定的 <see cref="StringBuilder"/> 实例中。
    /// </summary>
    /// <param name="builder">源代码文本将被写入到此实例中。</param>
    /// <param name="indentLevel">缩进级别。</param>
    public abstract void BuildInto(StringBuilder builder, int indentLevel);

    /// <summary>
    /// 将成员列表生成到指定的 <see cref="StringBuilder"/> 实例中。
    /// </summary>
    /// <param name="builder">源代码文本将被写入到此实例中。</param>
    /// <param name="indentLevel">缩进级别。</param>
    /// <param name="members">要生成的成员列表。</param>
    protected void BuildMembersInto(StringBuilder builder, int indentLevel, List<BracketSourceTextBuilder> members)
    {
        for (var i = 0; i < members.Count; i++)
        {
            if (i > 0)
            {
                builder.AppendLine();
            }

            members[i].BuildInto(builder, indentLevel);
        }
    }
}

/// <summary>
/// 命名空间声明源代码文本构建器。使用这种方式创建的命名空间只能是传统的大括号包裹的命名空间。
/// </summary>
/// <param name="root">根 <see cref="SourceTextBuilder"/> 实例。</param>
/// <param name="namespace">命名空间。</param>
public class NamespaceDeclarationSourceTextBuilder(SourceTextBuilder root, string @namespace) : BracketSourceTextBuilder(root)
{
    private readonly string _namespace = @namespace;
    private readonly List<BracketSourceTextBuilder> _typeDeclarations = [];

    /// <summary>
    /// 添加原始文本块（此文本块与类型声明是平级的）。
    /// </summary>
    /// <param name="rawText">要添加的原始文本块。</param>
    /// <returns>辅助链式调用。</returns>
    public NamespaceDeclarationSourceTextBuilder AddRawText(string rawText)
    {
        var rawDeclaration = new RawSourceTextBuilder(Root)
        {
            RawText = rawText,
        };
        _typeDeclarations.Add(rawDeclaration);
        return this;
    }

    /// <summary>
    /// 添加类型声明。
    /// </summary>
    /// <param name="declarationLine">类型声明行（如 "public class MyClass"）。</param>
    /// <param name="typeDeclarationBuilder">类型声明构建器。</param>
    /// <returns>辅助链式调用。</returns>
    public NamespaceDeclarationSourceTextBuilder AddTypeDeclaration(string declarationLine,
        Action<TypeDeclarationSourceTextBuilder> typeDeclarationBuilder)
    {
        var typeDeclaration = new TypeDeclarationSourceTextBuilder(Root, declarationLine);
        typeDeclarationBuilder(typeDeclaration);
        _typeDeclarations.Add(typeDeclaration);
        return this;
    }

    /// <summary>
    /// 添加类型声明。
    /// </summary>
    /// <param name="items">用于生成多个类型声明的数据源。</param>
    /// <param name="declarationLineBuilder">类型声明行构建器。</param>
    /// <param name="typeDeclarationBuilder">类型声明构建器。</param>
    /// <returns>辅助链式调用。</returns>
    public NamespaceDeclarationSourceTextBuilder AddTypeDeclarations<T>(IEnumerable<T> items,
        Func<T, string> declarationLineBuilder,
        Action<TypeDeclarationSourceTextBuilder, T> typeDeclarationBuilder)
    {
        foreach (var item in items)
        {
            var declarationLine = declarationLineBuilder(item);
            var typeDeclaration = new TypeDeclarationSourceTextBuilder(Root, declarationLine);
            typeDeclarationBuilder(typeDeclaration, item);
            _typeDeclarations.Add(typeDeclaration);
        }
        return this;
    }

    /// <summary>
    /// 将生成的源代码文本写入指定的 <see cref="StringBuilder"/> 实例中。
    /// </summary>
    /// <param name="builder">源代码文本将被写入到此实例中。</param>
    /// <param name="indentLevel">缩进级别。</param>
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
public class TypeDeclarationSourceTextBuilder(SourceTextBuilder root, string declarationLine) : BracketSourceTextBuilder(root)
{
    private readonly List<string> _attributes = [];
    private readonly List<string> _baseTypes = [];
    private readonly List<string> _typeConstraints = [];
    private readonly List<BracketSourceTextBuilder> _members = [];
    private DocumentationCommentSourceTextBuilder? _documentationCommentBuilder;

    /// <summary>
    /// 类型声明行（如 "public class MyClass"）。
    /// </summary>
    public string DeclarationLine { get; } = declarationLine;

    /// <summary>
    /// 为此类型声明添加文档注释。
    /// </summary>
    /// <param name="documentationCommentBuilder">文档注释构建器。</param>
    /// <returns>辅助链式调用。</returns>
    public TypeDeclarationSourceTextBuilder WithDocumentationComment(Action<DocumentationCommentSourceTextBuilder> documentationCommentBuilder)
    {
        _documentationCommentBuilder = new DocumentationCommentSourceTextBuilder(Root);
        documentationCommentBuilder(_documentationCommentBuilder);
        return this;
    }

    /// <summary>
    /// 为此类型声明添加文档注释（原始字符串，需自行包含 /// 等符号）。
    /// </summary>
    /// <param name="documentationComment">要添加的文档注释。</param>
    /// <returns>辅助链式调用。</returns>
    public TypeDeclarationSourceTextBuilder WithRawDocumentationComment(string documentationComment)
    {
        _documentationCommentBuilder = new DocumentationCommentSourceTextBuilder(Root);
        _documentationCommentBuilder.AddRawText(documentationComment);
        return this;
    }

    /// <summary>
    /// 为此类型声明添加 summary 文档注释。
    /// </summary>
    /// <param name="summary">要添加的 summary 文档注释。</param>
    /// <returns>辅助链式调用。</returns>
    public TypeDeclarationSourceTextBuilder WithSummaryComment(string summary)
    {
        _documentationCommentBuilder = new DocumentationCommentSourceTextBuilder(Root);
        _documentationCommentBuilder.AddSummaryComment(summary);
        return this;
    }

    /// <summary>
    /// 为此类型声明添加特性（如 [GeneratedCode(...)]）。
    /// </summary>
    /// <param name="attribute">要添加的特性行。</param>
    /// <returns>辅助链式调用。</returns>
    public TypeDeclarationSourceTextBuilder AddAttribute(string attribute)
    {
        _attributes.Add(attribute);
        return this;
    }

    /// <summary>
    /// 为此类型声明添加特性（如 [GeneratedCode(...)]）。
    /// </summary>
    /// <param name="attributes">要添加的特性行。</param>
    /// <returns>辅助链式调用。</returns>
    public TypeDeclarationSourceTextBuilder AddAttributes(params ReadOnlySpan<string> attributes)
    {
        foreach (var attribute in attributes)
        {
            _attributes.Add(attribute);
        }
        return this;
    }

    /// <summary>
    /// 为此类型声明添加特性（如 [GeneratedCode(...)]）。
    /// </summary>
    /// <param name="attributes">要添加的特性行。</param>
    /// <returns>辅助链式调用。</returns>
    public TypeDeclarationSourceTextBuilder AddAttributes(IEnumerable<string> attributes)
    {
        foreach (var attribute in attributes)
        {
            _attributes.Add(attribute);
        }
        return this;
    }

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

    /// <summary>
    /// 为此类型声明添加类型约束。
    /// </summary>
    /// <param name="typeConstraints">要添加的类型约束。</param>
    /// <returns>辅助链式调用。</returns>
    public TypeDeclarationSourceTextBuilder AddTypeConstraints(params ReadOnlySpan<string> typeConstraints)
    {
        foreach (var typeConstraint in typeConstraints)
        {
            _typeConstraints.Add(typeConstraint);
        }
        return this;
    }

    /// <summary>
    /// 添加类型声明。
    /// </summary>
    /// <param name="declarationLine">类型声明行（如 "public class MyClass"）。</param>
    /// <param name="typeDeclarationBuilder">类型声明构建器。</param>
    /// <returns>辅助链式调用。</returns>
    public TypeDeclarationSourceTextBuilder AddTypeDeclaration(string declarationLine,
        Action<TypeDeclarationSourceTextBuilder> typeDeclarationBuilder)
    {
        var typeDeclaration = new TypeDeclarationSourceTextBuilder(Root, declarationLine);
        typeDeclarationBuilder(typeDeclaration);
        _members.Add(typeDeclaration);
        return this;
    }

    /// <summary>
    /// 添加类型声明。
    /// </summary>
    /// <param name="items">用于生成多个类型声明的数据源。</param>
    /// <param name="declarationLineBuilder">类型声明行构建器。</param>
    /// <param name="typeDeclarationBuilder">类型声明构建器。</param>
    /// <returns>辅助链式调用。</returns>
    public TypeDeclarationSourceTextBuilder AddTypeDeclarations<T>(IEnumerable<T> items,
        Func<T, string> declarationLineBuilder,
        Action<TypeDeclarationSourceTextBuilder, T> typeDeclarationBuilder)
    {
        foreach (var item in items)
        {
            var declarationLine = declarationLineBuilder(item);
            var typeDeclaration = new TypeDeclarationSourceTextBuilder(Root, declarationLine);
            typeDeclarationBuilder(typeDeclaration, item);
            _members.Add(typeDeclaration);
        }
        return this;
    }

    /// <summary>
    /// 为此类型声明添加方法成员。
    /// </summary>
    /// <param name="signature">方法签名行（如 "public void MyMethod()"）。</param>
    /// <param name="methodDeclarationBuilder">方法声明构建器。</param>
    /// <returns>辅助链式调用。</returns>
    public TypeDeclarationSourceTextBuilder AddMethodDeclaration(string signature,
        Action<MethodDeclarationSourceTextBuilder> methodDeclarationBuilder)
    {
        var methodDeclaration = new MethodDeclarationSourceTextBuilder(Root, signature);
        methodDeclarationBuilder(methodDeclaration);
        _members.Add(methodDeclaration);
        return this;
    }

    /// <summary>
    /// 为此类型声明添加方法成员。
    /// </summary>
    /// <param name="items">用于生成多个方法声明的数据源。</param>
    /// <param name="signatureBuilder">方法签名行构建器。</param>
    /// <param name="methodDeclarationBuilder">方法声明构建器。</param>
    /// <returns>辅助链式调用。</returns>
    public TypeDeclarationSourceTextBuilder AddMethodDeclarations<T>(IEnumerable<T> items,
        Func<T, string> signatureBuilder,
        Action<MethodDeclarationSourceTextBuilder> methodDeclarationBuilder)
    {
        foreach (var item in items)
        {
            var signature = signatureBuilder(item);
            var methodDeclaration = new MethodDeclarationSourceTextBuilder(Root, signature);
            methodDeclarationBuilder(methodDeclaration);
            _members.Add(methodDeclaration);
        }
        return this;
    }

    /// <summary>
    /// 为此类型声明添加原始成员文本（此文本块与其他成员是平级的）。
    /// </summary>
    /// <param name="rawTexts">要添加的原始成员文本。</param>
    /// <returns>辅助链式调用。</returns>
    public TypeDeclarationSourceTextBuilder AddRawMembers(params ReadOnlySpan<string> rawTexts)
    {
        foreach (var rawText in rawTexts)
        {
            var memberDeclaration = new RawSourceTextBuilder(Root)
            {
                RawText = rawText,
            };
            _members.Add(memberDeclaration);
        }
        return this;
    }

    /// <summary>
    /// 为此类型声明批量添加原始成员文本（此文本块与其他成员是平级的）。
    /// </summary>
    /// <param name="rawTexts">要批量添加的原始成员文本。</param>
    /// <returns>辅助链式调用。</returns>
    public TypeDeclarationSourceTextBuilder AddRawMembers(IEnumerable<string> rawTexts)
    {
        foreach (var rawText in rawTexts)
        {
            var memberDeclaration = new RawSourceTextBuilder(Root)
            {
                RawText = rawText,
            };
            _members.Add(memberDeclaration);
        }
        return this;
    }

    /// <summary>
    /// 添加原始文本块（此文本块与成员声明是平级的）。
    /// </summary>
    /// <param name="rawText">要添加的原始文本块。</param>
    /// <returns>辅助链式调用。</returns>
    public TypeDeclarationSourceTextBuilder AddRawText(string rawText)
    {
        var rawDeclaration = new RawSourceTextBuilder(Root)
        {
            RawText = rawText,
        };
        _members.Add(rawDeclaration);
        return this;
    }

    /// <summary>
    /// 将生成的类型声明源代码文本写入指定的 <see cref="StringBuilder"/> 实例中。
    /// </summary>
    /// <param name="builder">源代码文本将被写入到此实例中。</param>
    /// <param name="indentLevel">缩进级别。</param>
    public override void BuildInto(StringBuilder builder, int indentLevel)
    {
        if (_documentationCommentBuilder is { } documentationCommentBuilder)
        {
            documentationCommentBuilder.BuildInto(builder, indentLevel);
        }
        foreach (var attribute in _attributes)
        {
            builder.AppendLineWithIndent(attribute, Indent, indentLevel);
        }
        builder.AppendWithIndent(DeclarationLine, Indent, indentLevel);
        if (_typeConstraints.Count > 0)
        {
            for (var i = 0; i < _typeConstraints.Count; i++)
            {
                var constraint = _typeConstraints[i];
                builder.AppendLineWithIndent(
                    i == _typeConstraints.Count - 1 ? $"{constraint}" : $"{constraint},",
                    Indent, indentLevel + 1);
            }
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
/// 代码块源代码文本构建器。
/// </summary>
/// <param name="root">根 <see cref="SourceTextBuilder"/> 实例。</param>
public class CodeBlockSourceTextBuilder(SourceTextBuilder root) : BracketSourceTextBuilder(root)
{
    private readonly List<List<BracketSourceTextBuilderInfo>> _statementGroups = [];

    /// <summary>
    /// 开始一个大括号作用域代码块。
    /// </summary>
    /// <param name="header">作用域头部代码，例如 if (condition) 等。</param>
    /// <param name="codeBlockBuilder">代码块构建器。</param>
    /// <param name="footer">作用域尾部代码，例如 else 等。</param>
    /// <returns></returns>
    public CodeBlockSourceTextBuilder BeginBracketScope(
        string? header,
        Action<CodeBlockSourceTextBuilder> codeBlockBuilder,
        string? footer = null)
    {
        var list = new List<BracketSourceTextBuilderInfo>();
        if (header is not null)
        {
            list.Add(new RawSourceTextBuilder(Root)
            {
                RawText = header,
            });
        }
        var codeBlock = new CodeBlockSourceTextBuilder(Root);
        codeBlockBuilder(codeBlock);
        list.Add(new(codeBlock, true));
        if (footer is not null)
        {
            list.Add(new RawSourceTextBuilder(Root)
            {
                RawText = footer,
            });
        }
        _statementGroups.Add(list);
        return this;
    }

    /// <summary>
    /// 为此代码块声明添加一组原始语句。<br/>
    /// 这些语句之间没有空行分隔，如果需要空行分隔，请调用多次此方法。
    /// </summary>
    /// <param name="rawTexts">要添加的原始语句。</param>
    /// <returns>辅助链式调用。</returns>
    public CodeBlockSourceTextBuilder AddRawStatements(params ReadOnlySpan<string> rawTexts)
    {
        var list = new List<BracketSourceTextBuilderInfo>(rawTexts.Length);
        for (var i = 0; i < rawTexts.Length; i++)
        {
            list.Add(new RawSourceTextBuilder(Root)
            {
                RawText = rawTexts[i],
            });
        }
        _statementGroups.Add(list);
        return this;
    }

    /// <summary>
    /// 为此代码块声明批量添加一组原始语句。<br/>
    /// 这些语句之间没有空行分隔。
    /// </summary>
    /// <param name="rawTexts">要批量添加的原始语句。</param>
    /// <returns>辅助链式调用。</returns>
    public CodeBlockSourceTextBuilder AddRawStatements(IEnumerable<string> rawTexts)
    {
        _statementGroups.Add(
        [
            ..rawTexts.Select(x => new RawSourceTextBuilder(Root)
            {
                RawText = x,
            }),
        ]);
        return this;
    }

    /// <summary>
    /// 将生成的代码块源代码文本写入指定的 <see cref="StringBuilder"/> 实例中。
    /// </summary>
    /// <param name="builder">源代码文本将被写入到此实例中。</param>
    /// <param name="indentLevel">缩进级别。</param>
    public override void BuildInto(StringBuilder builder, int indentLevel)
    {
        for (var groupIndex = 0; groupIndex < _statementGroups.Count; groupIndex++)
        {
            if (groupIndex > 0)
            {
                builder.AppendLine();
            }

            for (var index = 0; index < _statementGroups[groupIndex].Count; index++)
            {
                var group = _statementGroups[groupIndex][index];
                using var _ = group.ExtraIndent
                    ? BracketScope.Begin(builder, Indent, indentLevel)
                    : EmptyScope.Begin();
                group.Builder.BuildInto(builder, indentLevel + (group.ExtraIndent ? 1 : 0));
            }
        }
    }
}

/// <summary>
/// 方法声明源代码文本构建器。
/// </summary>
/// <param name="root">根 <see cref="SourceTextBuilder"/> 实例。</param>
/// <param name="signature">方法签名行（如 "public void MyMethod()"）。</param>
public class MethodDeclarationSourceTextBuilder(SourceTextBuilder root, string signature) : CodeBlockSourceTextBuilder(root)
{
    private DocumentationCommentSourceTextBuilder? _documentationCommentBuilder;
    private readonly List<string> _attributes = [];
    private readonly List<string> _typeConstraints = [];
    private readonly List<List<BracketSourceTextBuilder>> _statementGroups = [];

    /// <summary>
    /// 为此方法声明添加文档注释。
    /// </summary>
    /// <param name="documentationCommentBuilder">文档注释构建器。</param>
    /// <returns>辅助链式调用。</returns>
    public MethodDeclarationSourceTextBuilder WithDocumentationComment(Action<DocumentationCommentSourceTextBuilder> documentationCommentBuilder)
    {
        _documentationCommentBuilder = new DocumentationCommentSourceTextBuilder(Root);
        documentationCommentBuilder(_documentationCommentBuilder);
        return this;
    }

    /// <summary>
    /// 为此方法声明添加文档注释（原始字符串，需自行包含 /// 等符号）。
    /// </summary>
    /// <param name="documentationComment">要添加的文档注释。</param>
    /// <returns>辅助链式调用。</returns>
    public MethodDeclarationSourceTextBuilder WithRawDocumentationComment(string documentationComment)
    {
        _documentationCommentBuilder = new DocumentationCommentSourceTextBuilder(Root);
        _documentationCommentBuilder.AddRawText(documentationComment);
        return this;
    }

    /// <summary>
    /// 为此方法声明添加 summary 文档注释。
    /// </summary>
    /// <param name="summary">要添加的 summary 文档注释。</param>
    /// <returns>辅助链式调用。</returns>
    public MethodDeclarationSourceTextBuilder WithSummaryComment(string summary)
    {
        _documentationCommentBuilder = new DocumentationCommentSourceTextBuilder(Root);
        _documentationCommentBuilder.AddSummaryComment(summary);
        return this;
    }

    /// <summary>
    /// 方法签名行（如 "public void MyMethod()"）。
    /// </summary>
    public string Signature { get; } = signature;

    /// <summary>
    /// 为此方法声明添加特性（如 [GeneratedCode(...)]）。
    /// </summary>
    /// <param name="attribute">要添加的特性行。</param>
    /// <returns>辅助链式调用。</returns>
    public MethodDeclarationSourceTextBuilder AddAttribute(string attribute)
    {
        _attributes.Add(attribute);
        return this;
    }

    /// <summary>
    /// 为此方法声明添加特性（如 [GeneratedCode(...)]）。
    /// </summary>
    /// <param name="attributes">要添加的特性行。</param>
    /// <returns>辅助链式调用。</returns>
    public MethodDeclarationSourceTextBuilder AddAttributes(params ReadOnlySpan<string> attributes)
    {
        foreach (var attribute in attributes)
        {
            _attributes.Add(attribute);
        }
        return this;
    }

    /// <summary>
    /// 为此方法声明添加特性（如 [GeneratedCode(...)]）。
    /// </summary>
    /// <param name="attributes">要添加的特性行。</param>
    /// <returns>辅助链式调用。</returns>
    public MethodDeclarationSourceTextBuilder AddAttributes(IEnumerable<string> attributes)
    {
        foreach (var attribute in attributes)
        {
            _attributes.Add(attribute);
        }
        return this;
    }

    /// <summary>
    /// 为此方法声明添加类型约束。
    /// </summary>
    /// <param name="typeConstraints">要添加的类型约束。</param>
    /// <returns>辅助链式调用。</returns>
    public MethodDeclarationSourceTextBuilder AddTypeConstraints(params ReadOnlySpan<string> typeConstraints)
    {
        foreach (var typeConstraint in typeConstraints)
        {
            _typeConstraints.Add(typeConstraint);
        }
        return this;
    }

    /// <summary>
    /// 开始一个大括号作用域代码块。
    /// </summary>
    /// <param name="header">作用域头部代码，例如 if (condition) 等。</param>
    /// <param name="codeBlockBuilder">代码块构建器。</param>
    /// <param name="footer">作用域尾部代码，例如 else 等。</param>
    /// <returns></returns>
    public new MethodDeclarationSourceTextBuilder BeginBracketScope(
        string? header,
        Action<CodeBlockSourceTextBuilder> codeBlockBuilder,
        string? footer = null)
    {
        base.BeginBracketScope(header, codeBlockBuilder, footer);
        return this;
    }

    /// <summary>
    /// 为此方法声明添加一组原始语句。<br/>
    /// 这些语句之间没有空行分隔，如果需要空行分隔，请调用多次此方法。
    /// </summary>
    /// <param name="rawTexts">要添加的原始语句。</param>
    /// <returns>辅助链式调用。</returns>
    public new MethodDeclarationSourceTextBuilder AddRawStatements(params ReadOnlySpan<string> rawTexts)
    {
        base.AddRawStatements(rawTexts);
        return this;
    }

    /// <summary>
    /// 为此方法声明批量添加一组原始语句。<br/>
    /// 这些语句之间没有空行分隔。
    /// </summary>
    /// <param name="rawTexts">要批量添加的原始语句。</param>
    /// <returns>辅助链式调用。</returns>
    public new MethodDeclarationSourceTextBuilder AddRawStatements(IEnumerable<string> rawTexts)
    {
        base.AddRawStatements(rawTexts);
        return this;
    }

    /// <summary>
    /// 将生成的方法声明源代码文本写入指定的 <see cref="StringBuilder"/> 实例中。
    /// </summary>
    /// <param name="builder">源代码文本将被写入到此实例中。</param>
    /// <param name="indentLevel">缩进级别。</param>
    public override void BuildInto(StringBuilder builder, int indentLevel)
    {
        if (_documentationCommentBuilder is { } documentationCommentBuilder)
        {
            documentationCommentBuilder.BuildInto(builder, indentLevel);
        }
        foreach (var attribute in _attributes)
        {
            builder.AppendLineWithIndent(attribute, Indent, indentLevel);
        }
        builder.AppendLineWithIndent(Signature, Indent, indentLevel);
        if (_typeConstraints.Count > 0)
        {
            for (var i = 0; i < _typeConstraints.Count; i++)
            {
                var constraint = _typeConstraints[i];
                builder.AppendLineWithIndent(
                    i == _typeConstraints.Count - 1 ? $"{constraint}" : $"{constraint},",
                    Indent, indentLevel + 1);
            }
        }
        using var _ = BracketScope.Begin(builder, Indent, indentLevel);
        base.BuildInto(builder, indentLevel + 1);
    }
}

/// <summary>
/// 原始文本块源代码文本构建器。
/// </summary>
/// <param name="root">根 <see cref="SourceTextBuilder"/> 实例。</param>
public class RawSourceTextBuilder(SourceTextBuilder root) : BracketSourceTextBuilder(root)
{
    /// <summary>
    /// 要添加的原始文本块。
    /// </summary>
    public required string RawText { get; init; }

    /// <summary>
    /// 将生成的原始文本块源代码文本写入指定的 <see cref="StringBuilder"/> 实例中。
    /// </summary>
    /// <param name="builder">源代码文本将被写入到此实例中。</param>
    /// <param name="indentLevel">缩进级别。</param>
    public override void BuildInto(StringBuilder builder, int indentLevel)
    {
        builder.AppendLineWithIndent(RawText, Indent, indentLevel);
    }
}

/// <summary>
/// 文档注释源代码文本构建器。
/// </summary>
/// <param name="root">根 <see cref="SourceTextBuilder"/> 实例。</param>
public class DocumentationCommentSourceTextBuilder(SourceTextBuilder root) : BracketSourceTextBuilder(root)
{
    private readonly List<string> _lines = [];

    /// <summary>
    /// 为此文档注释添加 summary 文档注释。
    /// </summary>
    /// <param name="summary">要添加的 summary 文档注释。</param>
    /// <returns>辅助链式调用。</returns>
    public DocumentationCommentSourceTextBuilder AddSummaryComment(string summary)
    {
        _lines.Add("/// <summary>");
        foreach (var line in summary.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
        {
            _lines.Add($"/// {line}");
        }
        _lines.Add("/// </summary>");
        return this;
    }

    /// <summary>
    /// 为此文档注释添加 remarks 文档注释。
    /// </summary>
    /// <param name="remarks">要添加的 remarks 文档注释。</param>
    /// <returns>辅助链式调用。</returns>
    public DocumentationCommentSourceTextBuilder AddRemarksComment(string remarks)
    {
        _lines.Add("/// <remarks>");
        foreach (var line in remarks.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
        {
            _lines.Add($"/// {line}");
        }
        _lines.Add("/// </remarks>");
        return this;
    }

    /// <summary>
    /// 为此文档注释添加原始文档注释（原始字符串，需自行包含 /// 等符号）。
    /// </summary>
    /// <param name="rawText">要添加的原始文档注释。</param>
    /// <returns>辅助链式调用。</returns>
    public DocumentationCommentSourceTextBuilder AddRawText(string rawText)
    {
        _lines.Add(rawText);
        return this;
    }

    /// <summary>
    /// 将生成的文档注释源代码文本写入指定的 <see cref="StringBuilder"/> 实例中。
    /// </summary>
    /// <param name="builder">源代码文本将被写入到此实例中。</param>
    /// <param name="indentLevel">缩进级别。</param>
    public override void BuildInto(StringBuilder builder, int indentLevel)
    {
        for (var i = 0; i < _lines.Count; i++)
        {
            builder.AppendLineWithIndent(_lines[i], Indent, indentLevel);
        }
    }
}

/// <summary>
/// 包含 <see cref="BracketSourceTextBuilder"/> 实例及其是否需要额外缩进的信息。
/// </summary>
/// <param name="Builder">带有大括号的源代码文本构建器实例。</param>
/// <param name="ExtraIndent">需要额外缩进。</param>
internal readonly record struct BracketSourceTextBuilderInfo(BracketSourceTextBuilder Builder, bool ExtraIndent)
{
    public static implicit operator BracketSourceTextBuilderInfo(BracketSourceTextBuilder builder) => new(builder, false);
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
        var currentLineStart = 0;
        for (var index = 0; index < text.Length; index++)
        {
            if (text[index] != '\n' && index != text.Length - 1)
            {
                continue;
            }

            var isPreprocessorDirective = text[currentLineStart] == '#';
            if (!isPreprocessorDirective)
            {
                builder.AppendIndent(indent, indentLevel);
            }
            builder.Append(text, currentLineStart, index - currentLineStart + 1);
            currentLineStart = index + 1;
        }
        return builder;
    }

    internal static StringBuilder AppendLineWithIndent(this StringBuilder builder, string text, string indent, int indentLevel)
    {
        return AppendWithIndent(builder, text, indent, indentLevel).AppendLine();
    }
}
