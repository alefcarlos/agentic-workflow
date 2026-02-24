#!/usr/bin/env dotnet

#:sdk Microsoft.NET.Sdk.Web
#:property TargetFramework=net10.0
#:property CopyOutputSymbolsToPublishDirectory=false

using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateSlimBuilder(args);

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
        return TypedResults.Ok(SkillsIndex.Empty());
    }

    static string Sha256(byte[] data)
    {
        var hash = SHA256.HashData(data);
        return $"sha256:{Convert.ToHexString(hash).ToLowerInvariant()}";
    }

    static async Task<SkillMetadata> GetSkillMetadata(string path)
    {
        using var stream = new StreamReader(path);
        string? name = null;
        string? description = null;
        bool startFrontmatter = false;

        while (await stream.ReadLineAsync() is string line && (name is null || description is null))
        {
            if (line == "---" && !startFrontmatter)
            {
                startFrontmatter = true;
                continue;
            }

            if (line == "---" && startFrontmatter)
            {
                break;
            }

            if (line.StartsWith("name:"))
            {
                name = line.Split(':', StringSplitOptions.RemoveEmptyEntries)[1];
            }

            if (line.StartsWith("description:"))
            {
                description = line.Split(':', StringSplitOptions.RemoveEmptyEntries)[1];
            }

            if (name is not null && description is not null) break;
        }

        return new(name ?? string.Empty, description ?? string.Empty);
    }

    static async IAsyncEnumerable<FileEntry> GetFiles(string dir)
    {
        foreach (var f in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
        {
            var bytes = await File.ReadAllBytesAsync(f);
            var digest = Sha256(bytes);

            var relative = Path
                .GetRelativePath(dir, f)
                .Replace('\\', '/');

            yield return new FileEntry(relative, digest);
        }
    }

    static async IAsyncEnumerable<Skill> GetSkills(IWebHostEnvironment environment)
    {
        var skillsPath = Path.Combine(environment.ContentRootPath, "skills");

        foreach (var skillDir in Directory.GetDirectories(skillsPath))
        {
            var skillName = Path.GetFileName(skillDir);
            var skillMdPath = Path.Combine(skillDir, "SKILL.md");

            var metadata = await GetSkillMetadata(skillMdPath);

            if (metadata.Name == string.Empty || metadata.Description == string.Empty)
                continue;

            var allFiles = await GetFiles(skillDir).ToArrayAsync();

            var skillMd = allFiles.First(x => x.Path == "SKILL.md");
            var rest = allFiles.Where(x => x.Path != "SKILL.md").OrderBy(x => x.Path, StringComparer.Ordinal);
            FileEntry[] files = [skillMd, .. rest];

            yield return new Skill(metadata.Name, metadata.Description, files.ComputeSkillDigest(), files);
        }
    }

    var skills = await GetSkills(environment).ToArrayAsync();

    return TypedResults.Ok(new SkillsIndex("0.2.0", skills));
});

await app.RunAsync();

public record SkillMetadata(string Name, string Description);
public record FileEntry(string Path, string Digest);
public record Skill(string Name, string Description, string Digest, FileEntry[] Files);
public record SkillsIndex(string Version, Skill[] Skills);

public static class SkillsIndexExtensions
{
    extension(SkillsIndex source)
    {
        public static SkillsIndex Empty() => new("0.2.0", []);
    }
}

public static class FileEntriesExtensions
{
    extension(FileEntry[] source)
    {
        public string ComputeSkillDigest()
        {
            var sorted = source.OrderBy(f => f.Path, StringComparer.Ordinal);

            var builder = new StringBuilder();

            foreach (var f in sorted)
            {
                var hex = f.Digest.StartsWith("sha256:", StringComparison.Ordinal)
                    ? f.Digest["sha256:".Length..]
                    : f.Digest;

                builder.Append(f.Path);
                builder.Append('\0');       // null byte
                builder.Append(hex);
                builder.Append('\n');       // LF (0x0A)
            }

            var manifestBytes = Encoding.UTF8.GetBytes(builder.ToString());

            var hash = SHA256.HashData(manifestBytes);
            var finalHex = Convert.ToHexString(hash).ToLowerInvariant();

            return $"sha256:{finalHex}";
        }
    }
}

[JsonSerializable(typeof(SkillsIndex))]
[JsonSerializable(typeof(Skill))]
[JsonSerializable(typeof(SkillMetadata))]
[JsonSerializable(typeof(FileEntry))]
public partial class AppJsonSerializerContext : JsonSerializerContext
{
}
