#!/usr/bin/env dotnet

#:sdk Microsoft.NET.Sdk.Web
#:property TargetFramework=net10.0
#:property PublishAot=false
#:package YamlDotNet@16.3.0
#:package Vecc.YamlDotNet.Analyzers.StaticGenerator@16.3.0

using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.FileProviders;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

builder.Services.AddOutputCache(options => options.AddBasePolicy(builder => builder.Expire(TimeSpan.FromHours(1))));

var app = builder.Build();

app.UseOutputCache();

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(builder.Environment.ContentRootPath, "skills")),
    RequestPath = "/.well-known/skills",
    ServeUnknownFileTypes = true
});

app.MapGet("/.well-known/skills/index.json", async (IWebHostEnvironment environment) =>
{
    var skillsPath = Path.Combine(builder.Environment.ContentRootPath, "skills");

    if (!Directory.Exists(skillsPath))
    {
        TypedResults.Ok(SkillsIndex.Empty());
    }

    static SkillMetadata ParseSkillFrontmatter(string text, string defaultName)
    {
        var frontMatterPattern = @"^---\s*[\r\n]+(?<content>.*?)\r?\n---";
        var match = Regex.Match(text, frontMatterPattern, RegexOptions.Singleline);

        if (!match.Success)
            return new() { name = defaultName, description = string.Empty };

        var content = match.Groups["content"].Value;

        var deserializer = 
            new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            // new StaticDeserializerBuilder(new AppYamlContext())
            .Build();

        var metadata = deserializer.Deserialize<SkillMetadata>(content);

        return metadata;
    }

    static async IAsyncEnumerable<SkillEntry> GetSkills(IWebHostEnvironment environment)
    {
        var skillsPath = Path.Combine(environment.ContentRootPath, "skills");

        foreach (var skillDir in Directory.GetDirectories(skillsPath))
        {
            var skillName = Path.GetFileName(skillDir);
            var skillMdPath = Path.Combine(skillDir, "SKILL.md");

            if (!File.Exists(skillMdPath))
                continue;

            var skillContent = await File.ReadAllTextAsync(skillMdPath);
            var metadata = ParseSkillFrontmatter(skillContent, skillName);

            var files = Directory.GetFiles(skillDir, "*", SearchOption.AllDirectories)
                .Select(f => Path.GetRelativePath(skillDir, f).Replace('\\', '/'))
                .ToArray();

            yield return new SkillEntry(metadata.name, metadata.description, files);
        }
    }

    var skills = await GetSkills(environment).ToArrayAsync();

    return TypedResults.Ok(new SkillsIndex(skills));
});

await app.RunAsync();

public class SkillMetadata()
{
    public string description { get; set; } = default!;
    public string name { get; set; } = default!;
}


public record SkillEntry(string Name, string Description, string[] Files);
public record SkillsIndex(SkillEntry[] Skills);

public static class SkillsIndexExtensions
{
    extension(SkillsIndex source)
    {
        public static SkillsIndex Empty() => new([]);
    }
}

[JsonSerializable(typeof(SkillsIndex))]
[JsonSerializable(typeof(SkillEntry))]
public partial class AppJsonSerializerContext : JsonSerializerContext
{
}

//https://github.com/aaubry/YamlDotNet/issues/1031
// [YamlStaticContext]
// [YamlSerializable(typeof(SkillMetadata))]
// public partial class AppYamlContext : StaticContext
// {
// }