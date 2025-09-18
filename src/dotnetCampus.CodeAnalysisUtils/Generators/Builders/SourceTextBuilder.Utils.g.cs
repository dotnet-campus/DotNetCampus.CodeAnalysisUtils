#nullable enable
using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetCampus.CodeAnalysis.Utils.Generators.Builders;

/// <summary>
/// 带有缩进的源代码文本构建器基类。
/// </summary>
/// <param name="root">根 <see cref="SourceTextBuilder"/> 实例。</param>
public abstract class IndentSourceTextBuilder(SourceTextBuilder root)
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
    protected void BuildMembersInto(StringBuilder builder, int indentLevel, IReadOnlyList<IndentSourceTextBuilder> members)
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
/// 辅助 <see cref="FlowSourceTextBuilderExtensions.Condition"/> 编写带有条件判断的链式调用代码。
/// </summary>
/// <typeparam name="TBuilder">原始的源代码构建器。</typeparam>
public sealed class ConditionSourceTextBuilder<TBuilder>(TBuilder builder)
{
    /// <summary>
    /// 根据 <paramref name="condition"/> 的值决定是否执行 <paramref name="action"/> 中的代码。
    /// </summary>
    /// <param name="condition">条件。</param>
    /// <param name="action">链式调用源代码构建器。</param>
    /// <typeparam name="TBuilder">原始的源代码构建器。</typeparam>
    /// <returns>带有条件判断的源代码构建器。</returns>
    public ConditionSourceTextBuilder<TBuilder> Condition(bool condition, Action<TBuilder> action)
    {
        if (condition)
        {
            action(builder);
        }
        return this;
    }

    /// <summary>
    /// 当之前的 <see cref="Condition(bool, Action{TBuilder})"/> 判断为 false 时，执行 <paramref name="action"/> 中的代码。
    /// </summary>
    /// <param name="action">链式调用源代码构建器。</param>
    /// <returns>带有条件判断的源代码构建器。</returns>
    public ConditionSourceTextBuilder<TBuilder> Otherwise(Action<TBuilder> action)
    {
        action(builder);
        return this;
    }

    /// <summary>
    /// 解除条件判断，返回原始的源代码构建器。
    /// </summary>
    /// <returns>原始的源代码构建器。</returns>
    public TBuilder EndCondition()
    {
        return builder;
    }
}

/// <summary>
/// 为 <see cref="SourceTextBuilder"/> 的链式调用提供控制流支持。
/// </summary>
public static partial class FlowSourceTextBuilderExtensions
{
    /// <summary>
    /// 根据 <paramref name="condition"/> 的值决定是否执行 <paramref name="action"/> 中的代码。
    /// </summary>
    /// <param name="originalBuilder">辅助链式调用。</param>
    /// <param name="condition">条件。</param>
    /// <param name="action">链式调用源代码构建器。</param>
    /// <typeparam name="TBuilder">原始的源代码构建器。</typeparam>
    /// <returns>带有条件判断的源代码构建器。</returns>
    public static ConditionSourceTextBuilder<TBuilder> Condition<TBuilder>(this TBuilder originalBuilder,
        bool condition, Action<TBuilder> action)
    {
        var builder = new ConditionSourceTextBuilder<TBuilder>(originalBuilder);
        return builder.Condition(condition, action);
    }
}
