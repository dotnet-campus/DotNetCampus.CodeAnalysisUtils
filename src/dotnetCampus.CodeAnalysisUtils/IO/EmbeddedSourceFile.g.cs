#nullable enable
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace DotNetCampus.CodeAnalysis.Utils.IO;

/// <summary>
/// 嵌入的文本资源的数据。
/// </summary>
/// <param name="FileName">文件的名称（含扩展名）。</param>
/// <param name="FileRelativePath">文件的相对路径（含扩展名）。</param>
/// <param name="TypeName">文件的名称（不含扩展名），或者也很可能是类型名称。</param>
/// <param name="Namespace">文件的命名空间。</param>
/// <param name="Content">文件的文本内容。</param>
internal readonly record struct EmbeddedSourceFile(
    string FileName,
    string FileRelativePath,
    string TypeName,
    string Namespace,
    string Content)
{
    /// <summary>
    /// 寻找 <paramref name="relativePath"/> 路径下的源代码名称和内容。
    /// </summary>
    /// <param name="relativePath">资源文件的相对路径。请以“/”或“\”分隔文件夹。</param>
    /// <returns>嵌入的资源文件。</returns>
    internal static EmbeddedSourceFile Get(string relativePath)
    {
        var directory = Path.GetDirectoryName(relativePath)!;
        var fileName = Path.GetFileName(relativePath);
        var file = EmbeddedSourceFiles.Enumerate(directory)
            .FirstOrDefault(x => x.FileName == fileName);
        if (file == default)
        {
            throw new FileNotFoundException($"未找到嵌入的资源文件：{relativePath}");
        }
        return file;
    }

    /// <summary>
    /// 寻找原本 <typeparamref name="TReferenceType"/> 类型的源代码被嵌入后的名称和内容。
    /// </summary>
    /// <typeparam name="TReferenceType">用于定位嵌入资源的类型。</typeparam>
    /// <returns>嵌入的资源文件。</returns>
    internal static EmbeddedSourceFile Get<TReferenceType>()
    {
        var templateTypeName = typeof(TReferenceType).Name;
        var templateNamespace = typeof(TReferenceType).Namespace!;
        var templatesFolder = templateNamespace.Substring(GeneratorInfo.RootNamespace.Length + 1);
        var embeddedFile = EmbeddedSourceFiles.Enumerate(templatesFolder)
            .Single(x => x.TypeName == templateTypeName);
        return embeddedFile;
    }
}
