using System.Collections.Generic;
using Nuke.Common;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Utilities;
using Serilog;

using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class Build : NukeBuild
{

    public static int Main() => Execute<Build>(x => x.Pack);

    // --- GitVersion & GitRepository 自动注入 ---

    [GitVersion(NoFetch = true)]
    readonly GitVersion GitVersion; // 仅作为字段/属性定义

    // --- 核心配置属性 ---

    AbsolutePath ArtifactsDirectory => RootDirectory / "package";
    AbsolutePath TemplateProjectFile => RootDirectory / "src" / "Tools.Template.csproj";
    Configuration Configuration => Configuration.Release;

    // --- 参数和密钥 ---

    [Parameter("NuGet API Key for the package source.")]
    [Secret]
    readonly string NuGetApiKey;

    [Parameter("NuGet URL for the package source.")]
    readonly string NuGetSource = "https://api.nuget.org/v3/index.json";

    // --- 目标定义 ---

    Target Clean => _ => _
        .Description("Cleans the artifacts directory.")
        .Executes(() => ArtifactsDirectory.CreateOrCleanDirectory());
    Target Pack => _ => _
        .Description("Packs the dotnet template project into a .nupkg file.")
        .DependsOn(Clean)
        // 自动注入机制已确保 GitVersion 在此可用
        .Produces(ArtifactsDirectory / "*.nupkg")
        .Executes(() =>
        {
            Log.Information($"Packing project with version {GitVersion.SemVer}");

            DotNetPack(s => s
                .SetProject(TemplateProjectFile)
                .EnableNoBuild()

                .SetOutputDirectory(ArtifactsDirectory)
                .SetConfiguration(Configuration)
                .SetVersion(GitVersion.SemVer)
            );
        });

    Target Push => _ => _
        .DependsOn(Pack)
        .Requires(() => !NuGetApiKey.IsNullOrEmpty())
        .Executes(() =>
        {
            // 1. 查找所有 .nupkg 文件
            IReadOnlyCollection<AbsolutePath> packages = ArtifactsDirectory.GlobFiles("*.nupkg");

            // 2. 在 DotNetNuGetPush 方法调用上使用 CombineWith
            DotNetNuGetPush(_ => _
                // 这是配置公共设置的部分 (Source, ApiKey)
                .SetSource(NuGetSource)
                .SetApiKey(NuGetApiKey)
                .SetSkipDuplicate(true)

                // 3. 在这里使用 CombineWith。它会为 packages 列表中的每个元素，
                //    生成一个新的 'dotnet nuget push' 命令。
                .CombineWith(packages, (settings, packagePath) => settings
                    // 这是配置每个单独命令的设置 (TargetPath)
                    .SetTargetPath(packagePath)));
        });
}