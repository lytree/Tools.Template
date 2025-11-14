using System.CommandLine;
using MyProject.Cli.Commands;

var root = new RootCommand("PlaylistTool - versatile playlist generator")
{
    GenerateCommand.Build()
};

await root.Parse(args).InvokeAsync();