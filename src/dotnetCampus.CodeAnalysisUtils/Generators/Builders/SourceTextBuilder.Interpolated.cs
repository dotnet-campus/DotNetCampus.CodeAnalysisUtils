namespace DotNetCampus.CodeAnalysis.Utils.Generators.Builders;

/// <summary>
/// 在构建内插字符串式源代码时，可通过此接口内的方法，在内插字符串中添加特殊标记，以获得特殊的处理。
/// </summary>
public interface IInterpolatedTemplateSourceTextBuilder
{

}

internal sealed class InterpolatedTemplateSourceTextBuilder : IInterpolatedTemplateSourceTextBuilder
{
}
