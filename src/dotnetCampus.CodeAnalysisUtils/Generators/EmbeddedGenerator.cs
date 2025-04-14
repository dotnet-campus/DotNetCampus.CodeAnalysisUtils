using System.Text;
using DotNetCampus.CodeAnalysis.Utils.CodeAnalysis;
using DotNetCampus.CodeAnalysis.Utils.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace DotNetCampus.CodeAnalysis.Utils.Generators;

/// <summary>
/// 将模板中的源代码生成到目标项目中。
/// </summary>
[Generator]
public class EmbeddedGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterSourceOutput(context.AnalyzerConfigOptionsProvider, Execute);
    }

    private void Execute(SourceProductionContext context, AnalyzerConfigOptionsProvider analyzerConfigOptionsProvider)
    {
        if (analyzerConfigOptionsProvider.GlobalOptions.TryGetValueWithFallback("RootNamespace", "MSBuildProjectName") is not { } rootNamespace)
        {
            return;
        }

        foreach (var source in EmbeddedSourceFiles.Enumerate(null))
        {
            context.AddSource(source.FileRelativePath.Replace("/", "."), SourceText.From(source.Content, Encoding.UTF8));
        }
    }
}
