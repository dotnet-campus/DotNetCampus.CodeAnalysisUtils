#nullable enable
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DotNetCampus.CodeAnalysis.Utils.CodeAnalysis;

/// <summary>
/// 辅助源生成器获取 MSBuild 属性值。
/// </summary>
internal static class AnalyzerConfigOptionsExtensions
{
    /// <summary>
    /// 获取 build_property.{key} 的值。
    /// </summary>
    /// <param name="options">源生成器配置选项。</param>
    /// <param name="key">要获取的 MSBuild 属性名。</param>
    /// <param name="value">属性值。</param>
    /// <typeparam name="T">属性类型。（只支持 <see langword="string"/> 和 <see langword="bool"/>。）</typeparam>
    /// <returns>获取到的值的结果。可放到 <see langword="if"/> 条件中。</returns>
    public static AnalyzerConfigOptionResult TryGetValue<T>(
        this AnalyzerConfigOptions options,
        string key,
        out T value)
        where T : notnull
    {
        if (options.TryGetValue($"build_property.{key}", out var stringValue) && !string.IsNullOrWhiteSpace(stringValue))
        {
            value = ConvertFromString<T>(stringValue);
            return new AnalyzerConfigOptionResult(options, true)
            {
                UnsetPropertyNames = ImmutableList<string>.Empty,
            };
        }

        value = default!;
        return new AnalyzerConfigOptionResult(options, false)
        {
            UnsetPropertyNames = ImmutableList.Create<string>(key),
        };
    }

    /// <summary>
    /// 连续获取 build_property.{key} 的值。
    /// </summary>
    /// <param name="builder">使用链式方式获取多个 MSBuild 属性的值。</param>
    /// <param name="key">要获取的 MSBuild 属性名。</param>
    /// <param name="value">属性值。</param>
    /// <typeparam name="T">属性类型。（只支持 <see langword="string"/> 和 <see langword="bool"/>。）</typeparam>
    /// <returns>获取到的值的结果。可放到 <see langword="if"/> 条件中，只有能连续获取到的所有属性值时，才满足条件。</returns>
    public static AnalyzerConfigOptionResult TryGetValue<T>(
        this AnalyzerConfigOptionResult builder,
        string key,
        out T value)
        where T : notnull
    {
        var options = builder.Options;

        if (options.TryGetValue($"build_property.{key}", out var stringValue) && !string.IsNullOrWhiteSpace(stringValue))
        {
            value = ConvertFromString<T>(stringValue);
            return builder.Link(true, key);
        }

        value = default!;
        return builder.Link(false, key);
    }

    /// <summary>
    /// 连续获取 build_property.{key} 的值，在未获取到的时候获取另外由 <paramref name="fallbackKeys"/> 指定的属性值。
    /// </summary>
    /// <param name="options">源生成器配置选项。</param>
    /// <param name="key">要获取的 MSBuild 属性名。</param>
    /// <param name="fallbackKeys">当获取不到前一个属性名的值时，依次往后获取属性名的值。</param>
    /// <returns>第一个能获取到的属性的值。如果全获取不到，返回 <see langword="null"/>。</returns>
    public static string? TryGetValueWithFallback(this AnalyzerConfigOptions options, string key, params string[] fallbackKeys)
    {
        if (options.TryGetValue($"build_property.{key}", out var stringValue) && !string.IsNullOrWhiteSpace(stringValue))
        {
            return stringValue;
        }

        foreach (var fallbackKey in fallbackKeys)
        {
            if (options.TryGetValue($"build_property.{fallbackKey}", out stringValue) && !string.IsNullOrWhiteSpace(stringValue))
            {
                return stringValue;
            }
        }

        return null;
    }

    private static T ConvertFromString<T>(string value)
    {
        if (typeof(T) == typeof(string))
        {
            return (T)(object)value;
        }
        if (typeof(T) == typeof(bool))
        {
            return (T)(object)value.Equals("true", StringComparison.OrdinalIgnoreCase);
        }
        return default!;
    }

    public static string EnsureGetValueWithFallback(this AnalyzerConfigOptions options, string key, params string[] fallbackKeys)
    {
        return TryGetValueWithFallback(options, key, fallbackKeys)
               ?? throw new InvalidOperationException($"No value found for key '{key}' or any of the fallback keys.");
    }
}

/// <summary>
/// 用于连续获取 MSBuild 属性值的结果。
/// </summary>
/// <param name="Options">源生成器配置选项。</param>
/// <param name="GotValue">在此之前如果获取到了所有的属性值，则为 <see langword="true"/>。</param>
public readonly record struct AnalyzerConfigOptionResult(AnalyzerConfigOptions Options, bool GotValue)
{
    /// <summary>
    /// 未能获取到值的属性名列表。
    /// </summary>
    public required ImmutableList<string> UnsetPropertyNames { get; init; }

    /// <summary>
    /// 将当前结果与新的获取结果链接起来。只有所有的值都获取到了，才返回 <see langword="true"/>；否则返回 <see langword="false"/> 并记录未获取到值的属性名。
    /// </summary>
    /// <param name="result">新的获取结果。</param>
    /// <param name="propertyName">新的属性名。</param>
    /// <returns>链接后的结果。</returns>
    public AnalyzerConfigOptionResult Link(bool result, string propertyName)
    {
        if (result)
        {
            return this;
        }

        if (propertyName is null)
        {
            throw new ArgumentNullException(nameof(propertyName), @"The property name must be specified if the result is false.");
        }

        return this with
        {
            GotValue = false,
            UnsetPropertyNames = UnsetPropertyNames.Add(propertyName),
        };
    }

    /// <summary>
    /// 允许将结果直接用在 <see langword="if"/> 条件中。<see langword="true"/> 表示所有属性都成功获取到了。
    /// </summary>
    /// <param name="result">获取结果。</param>
    /// <returns>如果所有属性都成功获取到了，返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
    public static implicit operator bool(AnalyzerConfigOptionResult result) => result.GotValue;
}
