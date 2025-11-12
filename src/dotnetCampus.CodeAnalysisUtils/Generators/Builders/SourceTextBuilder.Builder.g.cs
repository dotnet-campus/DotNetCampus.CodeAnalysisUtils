#nullable enable
using System;
using System.Collections.Generic;

namespace DotNetCampus.CodeAnalysis.Utils.Generators.Builders;

/// <summary>
/// 每个源代码构建器都实现此接口。
/// </summary>
public interface ISourceTextBuilder
{
    /// <summary>
    /// 根 <see cref="SourceTextBuilder"/> 实例。
    /// </summary>
    SourceTextBuilder Root { get; }

    /// <summary>
    /// 添加原始字符串作为源代码文本的一部分。
    /// </summary>
    /// <param name="rawText">要添加的原始字符串。</param>
    void AddRawText(string rawText);
}

/// <summary>
/// 指示该源代码构建器支持在内部放入嵌套的源代码。
/// </summary>
public interface IAllowNestedSourceTextBuilder : ISourceTextBuilder
{
    /// <summary>
    /// 添加一个新的相当于「成员」的源代码（命名空间声明、类型声明、成员声明、语句等，取决于具体的构建器类型）。
    /// </summary>
    /// <param name="memberBuilder">要添加的成员构建器。</param>
    void AddNestedSourceCode(IndentSourceTextBuilder memberBuilder);
}

/// <summary>
/// 指示该源代码构建器支持在内部放入「块命名空间」。
/// </summary>
public interface IAllowScopedNamespace : IAllowNestedSourceTextBuilder;

/// <summary>
/// 指示该源代码构建器支持在内部放入「类型声明」。
/// </summary>
public interface IAllowTypeDeclaration : IAllowNestedSourceTextBuilder;

/// <summary>
/// 指示该源代码构建器支持在内部放入「成员声明」。
/// </summary>
public interface IAllowMemberDeclaration : IAllowMethodDeclaration;

/// <summary>
/// 指示该源代码构建器支持在内部放入「方法声明」。
/// </summary>
public interface IAllowMethodDeclaration : IAllowNestedSourceTextBuilder;

/// <summary>
/// 指示该源代码构建器支持在内部放入「语句」。
/// </summary>
public interface IAllowStatement : IAllowNestedSourceTextBuilder;

/// <summary>
/// 指示该源代码构建器支持在内部放入「XML 文档注释」。
/// </summary>
public interface IAllowDocumentationComment
{
    /// <summary>
    /// 文档注释构建器。
    /// </summary>
    DocumentationCommentSourceTextBuilder DocumentationCommentBuilder { get; }
}

/// <summary>
/// 指示该源代码构建器支持在内部放入「特性」。
/// </summary>
public interface IAllowAttributes
{
    /// <summary>
    /// 特性列表构建器。
    /// </summary>
    AttributeListSourceTextBuilder AttributeListBuilder { get; }
}

/// <summary>
/// 指示该源代码构建器支持在内部放入「泛型约束」。
/// </summary>
public interface IAllowTypeConstraints
{
    /// <summary>
    /// 泛型约束构建器。
    /// </summary>
    TypeConstraintsSourceTextBuilder TypeConstraintsBuilder { get; }
}

/// <summary>
/// 提供 <see cref="SourceTextBuilder"/> 的扩展方法。
/// </summary>
public static class SourceTextBuilderBaseExtensions
{
    /// <summary>
    /// 当在某个源代码构建器的参数中不需要做任何事情时，调用此方法以保持链式调用的完整性。
    /// </summary>
    /// <param name="builder">辅助链式调用。</param>
    /// <returns>辅助链式调用。</returns>
    public static TBuilder Ignore<TBuilder>(this TBuilder builder)
        where TBuilder : ISourceTextBuilder
    {
        return builder;
    }
}

/// <summary>
/// 提供 <see cref="IAllowScopedNamespace"/> 的扩展方法。
/// </summary>
public static class AllowScopedNamespaceExtensions
{
    /// <summary>
    /// 添加命名空间声明。
    /// </summary>
    /// <param name="builder">辅助链式调用。</param>
    /// <param name="namespace">命名空间。</param>
    /// <param name="namespaceDeclarationBuilder">命名空间声明构建器。</param>
    /// <returns>辅助链式调用。</returns>
    public static TBuilder AddNamespaceDeclaration<TBuilder>(this TBuilder builder,
        string @namespace,
        Action<NamespaceDeclarationSourceTextBuilder> namespaceDeclarationBuilder)
        where TBuilder : IAllowScopedNamespace
    {
        var namespaceDeclaration = new NamespaceDeclarationSourceTextBuilder(builder.Root, @namespace);
        namespaceDeclarationBuilder(namespaceDeclaration);
        builder.AddNestedSourceCode(namespaceDeclaration);
        return builder;
    }

    /// <summary>
    /// 添加命名空间声明。
    /// </summary>
    /// <param name="builder">辅助链式调用。</param>
    /// <param name="namespaceConverter">命名空间。</param>
    /// <param name="namespaceDeclarationBuilder">命名空间声明构建器。</param>
    /// <param name="items"></param>
    /// <returns>辅助链式调用。</returns>
    public static TBuilder AddNamespaceDeclarations<TBuilder, TItem>(this TBuilder builder, IEnumerable<TItem> items,
        Func<TItem, string> namespaceConverter,
        Action<NamespaceDeclarationSourceTextBuilder, TItem> namespaceDeclarationBuilder)
        where TBuilder : IAllowScopedNamespace
    {
        foreach (var item in items)
        {
            var @namespace = namespaceConverter(item);
            var namespaceDeclaration = new NamespaceDeclarationSourceTextBuilder(builder.Root, @namespace);
            namespaceDeclarationBuilder(namespaceDeclaration, item);
            builder.AddNestedSourceCode(namespaceDeclaration);
        }
        return builder;
    }
}

/// <summary>
/// 提供 <see cref="IAllowTypeDeclaration"/> 的扩展方法。
/// </summary>
public static class AllowTypeDeclarationExtensions
{
    /// <summary>
    /// 添加类型声明。
    /// </summary>
    /// <param name="builder">辅助链式调用。</param>
    /// <param name="declarationLine">类型声明行（如 "public class MyClass"）。</param>
    /// <param name="typeDeclarationBuilder">类型声明构建器。</param>
    /// <returns>辅助链式调用。</returns>
    public static TBuilder AddTypeDeclaration<TBuilder>(this TBuilder builder,
        string declarationLine,
        Action<TypeDeclarationSourceTextBuilder> typeDeclarationBuilder)
        where TBuilder : IAllowTypeDeclaration
    {
        var typeDeclaration = new TypeDeclarationSourceTextBuilder(builder.Root, declarationLine);
        typeDeclarationBuilder(typeDeclaration);
        builder.AddNestedSourceCode(typeDeclaration);
        return builder;
    }

    /// <summary>
    /// 添加类型声明。
    /// </summary>
    /// <param name="builder">辅助链式调用。</param>
    /// <param name="items">用于生成多个类型声明的数据源。</param>
    /// <param name="declarationLineConverter">类型声明行（如 "public class MyClass"）。</param>
    /// <param name="typeDeclarationBuilder">类型声明构建器。</param>
    /// <returns>辅助链式调用。</returns>
    public static TBuilder AddTypeDeclarations<TBuilder, TItem>(this TBuilder builder,
        IEnumerable<TItem> items,
        Func<TItem, string> declarationLineConverter,
        Action<TypeDeclarationSourceTextBuilder, TItem> typeDeclarationBuilder)
        where TBuilder : IAllowTypeDeclaration
    {
        foreach (var item in items)
        {
            var declarationLine = declarationLineConverter(item);
            var typeDeclaration = new TypeDeclarationSourceTextBuilder(builder.Root, declarationLine);
            typeDeclarationBuilder(typeDeclaration, item);
            builder.AddNestedSourceCode(typeDeclaration);
        }
        return builder;
    }
}

/// <summary>
/// 提供 <see cref="IAllowMemberDeclaration"/> 的扩展方法。
/// </summary>
public static class AllowMemberDeclarationExtensions
{
    /// <summary>
    /// 为此类型声明添加方法成员。
    /// </summary>
    /// <param name="builder">辅助链式调用。</param>
    /// <param name="signature">方法签名行（如 "public void MyMethod()"）。</param>
    /// <param name="methodDeclarationBuilder">方法声明构建器。</param>
    /// <returns>辅助链式调用。</returns>
    public static TBuilder AddMethodDeclaration<TBuilder>(this TBuilder builder, string signature,
        Action<MethodDeclarationSourceTextBuilder> methodDeclarationBuilder)
        where TBuilder : IAllowMemberDeclaration
    {
        var methodDeclaration = new MethodDeclarationSourceTextBuilder(builder.Root, signature);
        methodDeclarationBuilder(methodDeclaration);
        builder.AddNestedSourceCode(methodDeclaration);
        return builder;
    }

    /// <summary>
    /// 为此类型声明添加方法成员。
    /// </summary>
    /// <param name="builder">辅助链式调用。</param>
    /// <param name="useExpressionBody">是否使用表达式主体。</param>
    /// <param name="signature">方法签名行（如 "public void MyMethod()"）。</param>
    /// <param name="methodDeclarationAndExpressionBodyBuilder">方法声明和表达式主体的构建器。</param>
    /// <returns>辅助链式调用。</returns>
    public static TBuilder AddMethodDeclaration<TBuilder>(this TBuilder builder, bool useExpressionBody, string signature,
        Action<MethodDeclarationSourceTextBuilder> methodDeclarationAndExpressionBodyBuilder)
        where TBuilder : IAllowMemberDeclaration
    {
        var methodDeclaration = new MethodDeclarationSourceTextBuilder(builder.Root, signature)
        {
            UseExpressionBody = useExpressionBody,
        };
        methodDeclarationAndExpressionBodyBuilder(methodDeclaration);
        builder.AddNestedSourceCode(methodDeclaration);
        return builder;
    }

    /// <summary>
    /// 为此类型声明添加方法成员。
    /// </summary>
    /// <param name="builder">辅助链式调用。</param>
    /// <param name="items">用于生成多个方法声明的数据源。</param>
    /// <param name="signatureConverter">方法签名行（如 "public void MyMethod()"）。</param>
    /// <param name="methodDeclarationBuilder">方法声明构建器。</param>
    /// <returns>辅助链式调用。</returns>
    public static TBuilder AddMethodDeclarations<TBuilder, TItem>(this TBuilder builder, IEnumerable<TItem> items,
        Func<TItem, string> signatureConverter,
        Action<MethodDeclarationSourceTextBuilder, TItem> methodDeclarationBuilder)
        where TBuilder : IAllowMemberDeclaration
    {
        foreach (var item in items)
        {
            var signature = signatureConverter(item);
            var methodDeclaration = new MethodDeclarationSourceTextBuilder(builder.Root, signature);
            methodDeclarationBuilder(methodDeclaration, item);
            builder.AddNestedSourceCode(methodDeclaration);
        }
        return builder;
    }

    /// <summary>
    /// 为此类型声明批量添加原始成员文本（此文本块与其他成员是平级的）。
    /// </summary>
    /// <param name="builder">辅助链式调用。</param>
    /// <param name="rawTexts">要批量添加的原始成员文本。</param>
    /// <returns>辅助链式调用。</returns>
    public static TBuilder AddRawMembers<TBuilder>(this TBuilder builder, params ReadOnlySpan<string> rawTexts)
        where TBuilder : IAllowMemberDeclaration
    {
        foreach (var rawText in rawTexts)
        {
            var memberDeclaration = new RawSourceTextBuilder(builder.Root)
            {
                RawText = rawText,
            };
            builder.AddNestedSourceCode(memberDeclaration);
        }
        return builder;
    }

    /// <summary>
    /// 为此类型声明批量添加原始成员文本（此文本块与其他成员是平级的）。
    /// </summary>
    /// <param name="builder">辅助链式调用。</param>
    /// <param name="rawTexts">要批量添加的原始成员文本。</param>
    /// <returns>辅助链式调用。</returns>
    public static TBuilder AddRawMembers<TBuilder>(this TBuilder builder, IEnumerable<string> rawTexts)
        where TBuilder : IAllowMemberDeclaration
    {
        foreach (var rawText in rawTexts)
        {
            var memberDeclaration = new RawSourceTextBuilder(builder.Root)
            {
                RawText = rawText,
            };
            builder.AddNestedSourceCode(memberDeclaration);
        }
        return builder;
    }
}

/// <summary>
/// 提供 <see cref="IAllowStatement"/> 的扩展方法。
/// </summary>
public static class AllowStatementExtensions
{
    /// <summary>
    /// 添加一个空行以分隔上下代码块。
    /// </summary>
    /// <param name="builder">辅助链式调用。</param>
    /// <param name="force">
    /// 是否强制添加空行，即使这是第一个或最后一个语句。默认为 <see langword="false"/>。
    /// </param>
    /// <returns>辅助链式调用。</returns>
    public static TBuilder AddLineSeparator<TBuilder>(this TBuilder builder, bool force = false)
        where TBuilder : IAllowStatement
    {
        builder.AddNestedSourceCode(new CodeBlockSourceTextBuilder(builder.Root)
        {
            IsLineSeparator = true,
        });
        return builder;
    }

    /// <summary>
    /// 为此方法声明添加一个语句块。
    /// </summary>
    /// <param name="builder">辅助链式调用。</param>
    /// <param name="codeBlockBuilder">语句块构建器。</param>
    /// <returns>辅助链式调用。</returns>
    public static TBuilder AddStatement<TBuilder>(this TBuilder builder,
        Action<CodeBlockSourceTextBuilder> codeBlockBuilder)
        where TBuilder : IAllowStatement
    {
        var codeBlock = new CodeBlockSourceTextBuilder(builder.Root)
        {
        };
        codeBlockBuilder(codeBlock);
        builder.AddNestedSourceCode(codeBlock);
        return builder;
    }

    /// <summary>
    /// 为此方法声明添加一个语句块，语句块由前缀、表达式主体、后缀组成。
    /// </summary>
    /// <param name="builder">辅助链式调用。</param>
    /// <param name="prefix">表达式主体前缀，例如 return 等。</param>
    /// <param name="suffix">表达式主体后缀，例如 ; 等。</param>
    /// <param name="expressionBuilder">表达式主体构建器。</param>
    /// <returns>辅助链式调用。</returns>
    public static TBuilder AddStatement<TBuilder>(this TBuilder builder,
        string? prefix, string? suffix,
        Action<CodeBlockSourceTextBuilder> expressionBuilder)
        where TBuilder : IAllowStatement
    {
        var codeBlock = new CodeBlockSourceTextBuilder(builder.Root)
        {
            IsPartExpression = true,
            Header = prefix,
            Footer = suffix,
        };
        expressionBuilder(codeBlock);
        builder.AddNestedSourceCode(codeBlock);
        return builder;
    }

    /// <summary>
    /// 为此方法声明添加多个语句块。
    /// </summary>
    /// <param name="builder">辅助链式调用。</param>
    /// <param name="items">用于生成多个方法声明的数据源。</param>
    /// <param name="codeBlockBuilder">语句块构建器。</param>
    /// <returns>辅助链式调用。</returns>
    public static TBuilder AddStatements<TBuilder, TItem>(this TBuilder builder, IEnumerable<TItem> items,
        Action<CodeBlockSourceTextBuilder, TItem> codeBlockBuilder)
        where TBuilder : IAllowStatement
    {
        foreach (var item in items)
        {
            var codeBlock = new CodeBlockSourceTextBuilder(builder.Root)
            {
            };
            codeBlockBuilder(codeBlock, item);
            builder.AddNestedSourceCode(codeBlock);
        }
        return builder;
    }

    /// <summary>
    /// 为此方法声明添加一个原始语句。
    /// </summary>
    /// <param name="builder">辅助链式调用。</param>
    /// <param name="rawText">要添加的原始语句。</param>
    /// <returns>辅助链式调用。</returns>
    public static TBuilder AddRawStatement<TBuilder>(this TBuilder builder,
        string rawText)
        where TBuilder : IAllowStatement
    {
        var statement = new RawSourceTextBuilder(builder.Root)
        {
            RawText = rawText,
        };
        builder.AddNestedSourceCode(statement);
        return builder;
    }

    /// <summary>
    /// 为此方法声明添加一组原始语句。
    /// </summary>
    /// <param name="builder">辅助链式调用。</param>
    /// <param name="rawTexts">要添加的原始语句。</param>
    /// <returns>辅助链式调用。</returns>
    public static TBuilder AddRawStatements<TBuilder>(this TBuilder builder,
        params ReadOnlySpan<string> rawTexts)
        where TBuilder : IAllowStatement
    {
        foreach (var rawText in rawTexts)
        {
            var statement = new RawSourceTextBuilder(builder.Root)
            {
                RawText = rawText,
            };
            builder.AddNestedSourceCode(statement);
        }
        return builder;
    }

    /// <summary>
    /// 为此方法声明批量添加一组原始语句。
    /// </summary>
    /// <param name="builder">辅助链式调用。</param>
    /// <param name="rawTexts">要批量添加的原始语句。</param>
    /// <returns>辅助链式调用。</returns>
    public static TBuilder AddRawStatements<TBuilder>(this TBuilder builder,
        IEnumerable<string> rawTexts)
        where TBuilder : IAllowStatement
    {
        foreach (var rawText in rawTexts)
        {
            var statement = new RawSourceTextBuilder(builder.Root)
            {
                RawText = rawText,
            };
            builder.AddNestedSourceCode(statement);
        }
        return builder;
    }

    /// <summary>
    /// 添加一个大括号作用域代码块。
    /// </summary>
    /// <param name="builder">辅助链式调用。</param>
    /// <param name="header">作用域头部代码，例如 if (condition) 等。</param>
    /// <param name="codeBlockBuilder">代码块构建器。</param>
    /// <returns>辅助链式调用。</returns>
    public static TBuilder AddBracketScope<TBuilder>(this TBuilder builder,
        string? header,
        Action<CodeBlockSourceTextBuilder> codeBlockBuilder)
        where TBuilder : IAllowStatement
    {
        builder.AddBracketScope(header, "{", "}", codeBlockBuilder);
        return builder;
    }

    /// <summary>
    /// 添加一个大括号作用域代码块。
    /// </summary>
    /// <param name="builder">辅助链式调用。</param>
    /// <param name="header">作用域头部代码，例如 if (condition) 等。</param>
    /// <param name="startBracket">自定义起始括号。适用于有多层括号时的场景。</param>
    /// <param name="endBracket">自定义结束括号。适用于有多层括号时的场景。</param>
    /// <param name="codeBlockBuilder">代码块构建器。</param>
    /// <returns>辅助链式调用。</returns>
    public static TBuilder AddBracketScope<TBuilder>(this TBuilder builder,
        string? header, string startBracket, string endBracket,
        Action<CodeBlockSourceTextBuilder> codeBlockBuilder)
        where TBuilder : IAllowStatement
    {
        var codeBlock = new CodeBlockSourceTextBuilder(builder.Root)
        {
            IsBracketBlock = true,
            Header = header,
            StartBracket = startBracket,
            EndBracket = endBracket,
        };
        codeBlockBuilder(codeBlock);
        builder.AddNestedSourceCode(codeBlock);
        return builder;
    }

    /// <summary>
    /// 添加一个大括号作用域代码块。
    /// </summary>
    /// <param name="builder">辅助链式调用。</param>
    /// <param name="header">作用域头部代码，例如 if (condition) 等。</param>
    /// <param name="startBracket">自定义起始括号。适用于有多层括号时的场景。</param>
    /// <param name="endBracket">自定义结束括号。适用于有多层括号时的场景。</param>
    /// <param name="isExpressionBody">整个代码块是否作为表达式主体使用。</param>
    /// <param name="codeBlockBuilder">代码块构建器。</param>
    /// <returns>辅助链式调用。</returns>
    public static TBuilder AddBracketScope<TBuilder>(this TBuilder builder,
        string? header, string startBracket, string endBracket, bool isExpressionBody,
        Action<CodeBlockSourceTextBuilder> codeBlockBuilder)
        where TBuilder : IAllowStatement
    {
        var codeBlock = new CodeBlockSourceTextBuilder(builder.Root)
        {
            IsBracketBlock = true,
            IsExpression = true,
            Header = header,
            StartBracket = startBracket,
            EndBracket = endBracket,
        };
        codeBlockBuilder(codeBlock);
        builder.AddNestedSourceCode(codeBlock);
        return builder;
    }
}

/// <summary>
/// 提供 <see cref="IAllowDocumentationComment"/> 的扩展方法。
/// </summary>
public static class AllowDocumentationCommentExtensions
{
    /// <summary>
    /// 添加文档注释。
    /// </summary>
    /// <param name="builder">辅助链式调用。</param>
    /// <param name="documentationCommentBuilder">文档注释构建器。</param>
    /// <returns>辅助链式调用。</returns>
    public static TBuilder WithDocumentationComment<TBuilder>(this TBuilder builder,
        Action<DocumentationCommentSourceTextBuilder> documentationCommentBuilder)
        where TBuilder : IAllowDocumentationComment
    {
        documentationCommentBuilder(builder.DocumentationCommentBuilder);
        return builder;
    }

    /// <summary>
    /// 添加原始文档注释字符串。可带 /// 前缀，也可不带（将自动添加）。
    /// </summary>
    /// <param name="builder">辅助链式调用。</param>
    /// <param name="documentationComment">要添加的文档注释。</param>
    /// <returns>辅助链式调用。</returns>
    public static TBuilder WithRawDocumentationComment<TBuilder>(this TBuilder builder,
        string documentationComment)
        where TBuilder : IAllowDocumentationComment
    {
        builder.DocumentationCommentBuilder.AddRawText(documentationComment);
        return builder;
    }

    /// <summary>
    /// 添加 summary 文档注释。
    /// </summary>
    /// <param name="builder">辅助链式调用。</param>
    /// <param name="summary">要添加的 summary 文档注释。</param>
    /// <returns>辅助链式调用。</returns>
    public static TBuilder WithSummaryComment<TBuilder>(this TBuilder builder,
        string summary)
        where TBuilder : IAllowDocumentationComment
    {
        builder.DocumentationCommentBuilder.Summary(summary);
        return builder;
    }
}

/// <summary>
/// 提供 <see cref="IAllowAttributes"/> 的扩展方法。
/// </summary>
public static class AllowAttributesExtensions
{
    /// <summary>
    /// 为此类型声明添加特性（如 [GeneratedCode(...)]）。
    /// </summary>
    /// <param name="builder">辅助链式调用。</param>
    /// <param name="attribute">要添加的特性行。</param>
    /// <returns>辅助链式调用。</returns>
    public static TBuilder AddAttribute<TBuilder>(this TBuilder builder,
        string attribute)
        where TBuilder : IAllowAttributes
    {
        builder.AttributeListBuilder.AddAttribute(attribute);
        return builder;
    }

    /// <summary>
    /// 为此类型声明添加特性（如 [GeneratedCode(...)]）。
    /// </summary>
    /// <param name="builder">辅助链式调用。</param>
    /// <param name="attributes">要添加的特性行。</param>
    /// <returns>辅助链式调用。</returns>
    public static TBuilder AddAttributes<TBuilder>(this TBuilder builder,
        params ReadOnlySpan<string> attributes)
        where TBuilder : IAllowAttributes
    {
        foreach (var attribute in attributes)
        {
            builder.AttributeListBuilder.AddAttribute(attribute);
        }
        return builder;
    }

    /// <summary>
    /// 为此类型声明添加特性（如 [GeneratedCode(...)]）。
    /// </summary>
    /// <param name="builder">辅助链式调用。</param>
    /// <param name="attributes">要添加的特性行。</param>
    /// <returns>辅助链式调用。</returns>
    public static TBuilder AddAttributes<TBuilder>(this TBuilder builder,
        IEnumerable<string> attributes)
        where TBuilder : IAllowAttributes
    {
        foreach (var attribute in attributes)
        {
            builder.AttributeListBuilder.AddAttribute(attribute);
        }
        return builder;
    }
}

/// <summary>
/// 提供 <see cref="IAllowTypeConstraints"/> 的扩展方法。
/// </summary>
public static class AllowTypeConstraintsExtensions
{
    /// <summary>
    /// 为此类型声明泛型约束。
    /// </summary>
    /// <param name="builder">辅助链式调用。</param>
    /// <param name="typeConstraint">要添加的泛型约束。</param>
    /// <returns>辅助链式调用。</returns>
    public static TBuilder AddTypeConstraint<TBuilder>(this TBuilder builder,
        string typeConstraint)
        where TBuilder : IAllowTypeConstraints
    {
        builder.TypeConstraintsBuilder.AddTypeConstraint(typeConstraint);
        return builder;
    }

    /// <summary>
    /// 为此类型声明泛型约束。
    /// </summary>
    /// <param name="builder">辅助链式调用。</param>
    /// <param name="typeConstraints">要添加的泛型约束。</param>
    /// <returns>辅助链式调用。</returns>
    public static TBuilder AddTypeConstraints<TBuilder>(this TBuilder builder,
        params ReadOnlySpan<string> typeConstraints)
        where TBuilder : IAllowTypeConstraints
    {
        foreach (var typeConstraint in typeConstraints)
        {
            builder.TypeConstraintsBuilder.AddTypeConstraint(typeConstraint);
        }
        return builder;
    }

    /// <summary>
    /// 为此类型声明泛型约束。
    /// </summary>
    /// <param name="builder">辅助链式调用。</param>
    /// <param name="typeConstraints">要添加的泛型约束。</param>
    /// <returns>辅助链式调用。</returns>
    public static TBuilder AddTypeConstraints<TBuilder>(this TBuilder builder,
        IEnumerable<string> typeConstraints)
        where TBuilder : IAllowTypeConstraints
    {
        foreach (var typeConstraint in typeConstraints)
        {
            builder.TypeConstraintsBuilder.AddTypeConstraint(typeConstraint);
        }
        return builder;
    }
}
