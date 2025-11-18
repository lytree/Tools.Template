using System.Threading.Tasks;
using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Core;
using Cake.Core.Diagnostics;
using Cake.Frosting;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Clean;
using Cake.Common.Tools.DotNet.Build;
using Cake.Common.Tools.DotNet.Pack;
using Cake.Common.Tools.GitVersion;
using Cake.Common.Tools.DotNet.NuGet.Push;
public static class Program
{
    public static int Main(string[] args)
    {
        return new CakeHost()
            .UseContext<BuildContext>()
            .Run(args);
    }
}
// 定义构建中使用的常量
public static class BuildParameters
{
    public const string SolutionPath = "../Tools.Template.slnx"; // 确保路径正确
    public const string Configuration = "Release";
    public const string PackageOutputDirectory = "../packages";
    public const string ProjectPath = "../src/Tools.Template.csproj";
    public const string NuGetApiKey = "NUGET_API_KEY";
    public const string Version = "Version";
    public const string NugetSource = "https://api.nuget.org/v3/index.json";
}
public class BuildContext : FrostingContext
{
    public bool HasVersion { get; set; }
    public string NuGetApiKey { get; set; }
    public string NugetSource { get; set; }

    public BuildContext(ICakeContext context)
        : base(context)
    {
        HasVersion = context.Arguments.HasArgument("Version");
        NuGetApiKey = context.Environment.GetEnvironmentVariable(BuildParameters.NuGetApiKey);
        NugetSource = context.Arguments.HasArgument(nameof(BuildParameters.NugetSource)) ? BuildParameters.NugetSource : context.Arguments.GetArgument(nameof(BuildParameters.NugetSource));

    }
}
[TaskName("Clean")]
public sealed class CleanTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        // 使用 DotNetCoreClean 清理 Solution
        context.DotNetClean(BuildParameters.ProjectPath, new DotNetCleanSettings
        {
            Configuration = BuildParameters.Configuration
        });

        // 可选：清理 NuGet 包输出目录
        context.CleanDirectory(BuildParameters.PackageOutputDirectory);
    }
}

[TaskName("Restore")]
[IsDependentOn(typeof(CleanTask))]
public sealed class RestoreTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.DotNetRestore(BuildParameters.ProjectPath);
    }
}

// [TaskName("Compile")]
// [IsDependentOn(typeof(RestoreTask))]
// public class CompileTask : FrostingTask<BuildContext>
// {
//     public override void Run(BuildContext context)
//     {
//         context.DotNetBuild(BuildParameters.ProjectPath, new DotNetBuildSettings
//         {
//             Configuration = BuildParameters.Configuration,
//             NoRestore = true // 已经在 Restore Task 中完成

//         });
//     }
// }


[TaskName("Pack")]
[IsDependentOn(typeof(RestoreTask))]
public class PackTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        // 1. 调用 GitVersion
        // GitVersionSettings 可以用来设置工作目录等参数
        var gitVersionSettings = new GitVersionSettings
        {
            // 如果你的 Git 仓库不在当前 Cake 脚本的根目录，需要设置工作目录
            WorkingDirectory = "../"
        };

        // 运行 GitVersion 并获取结果对象
        var version = context.GitVersion(gitVersionSettings);

        var projectFiles = context.GetFiles(BuildParameters.ProjectPath);

        context.Information($"Found {projectFiles.Count} project(s) matching 'Tools.Template.csproj':");
        foreach (var projectPath in projectFiles)
        {

            context.DotNetPack(projectPath.FullPath, new DotNetPackSettings
            {
                Configuration = BuildParameters.Configuration,
                OutputDirectory = BuildParameters.PackageOutputDirectory,
                NoBuild = true, // 已经在 Compile Task 中完成
                NoRestore = true,
                ArgumentCustomization = args => args
                .Append($"-property:PackageVersion={(context.HasVersion ? context.Arguments.GetArgument(BuildParameters.Version) : version.FullSemVer)}")
            });
        }
    }
}

[TaskName("Push")]
[IsDependentOn(typeof(PackTask))]
public class PushTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        // 1. 定义 NuGet 源 URL
        // 使用 NuGet.org 作为示例

        var nugetSource = context.NugetSource;
        // 2. 检查 API Key
        if (string.IsNullOrWhiteSpace(context.NuGetApiKey))
        {
            context.Log.Error("NuGet API Key is missing. Skipping package publish.");
            // 如果缺少 Key，可以选择直接退出任务
            return;
        }

        // 3. 查找所有 .nupkg 文件
        var packages = context.GetFiles(BuildParameters.PackageOutputDirectory + "/*.nupkg");

        context.Information($"Found {packages.Count} NuGet package(s) to publish.");

        // 4. 循环并推送每个包
        foreach (var package in packages)
        {
            context.Information($"Publishing package: {package.GetFilename()}");

            context.DotNetNuGetPush(package.FullPath, new DotNetNuGetPushSettings
            {
                Source = nugetSource,
                ApiKey = context.NuGetApiKey,
                // 确保 Cake 使用了你定义的 API Key
                SkipDuplicate = true // 如果包已存在，跳过上传
            });
        }

        context.Information("NuGet packages published successfully.");
    }
}



// ---------------------- 5. Default/Main Task ----------------------

[TaskName("Default")]
[IsDependentOn(typeof(PackTask))] // 确保所有任务都完成
public sealed class DefaultTask : FrostingTask<BuildContext>
{
}