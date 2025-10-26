using System.CommandLine;
using PlaylistTool.Cli.Commands;

var root = new RootCommand("PlaylistTool - versatile playlist generator")
{
    GenerateCommand.Build()
};

await root.Parse(args).InvokeAsync();