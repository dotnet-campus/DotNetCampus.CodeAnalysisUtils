#nullable enable
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
