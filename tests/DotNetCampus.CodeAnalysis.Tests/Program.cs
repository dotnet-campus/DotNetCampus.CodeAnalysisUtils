using DotNetCampus.CodeAnalysis.Utils.Generators.Builders;

var builder = new SourceTextBuilder()
    .AddFoo(3);
var sourceText = builder.ToString();
Console.WriteLine(sourceText);

public static class Extensions
{
    public static TBuilder AddFoo<TBuilder>(this TBuilder builder, int level)
        where TBuilder : IAllowStatement
    {
        if (level is 0)
        {
            return builder.AddRawStatement("new List<int>()");
        }
        return builder.AddBracketScope(" => ", s => s
            .AddStatement("var a = ", ";", t => t
                .AddFoo(level - 1))
        );
    }
}
