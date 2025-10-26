
using System;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.IO;
using PlaylistTools.Core;

namespace PlaylistTool.Cli.Commands;

public static class GenerateCommand
{
    public static Command Build()
    {
        var cmd = new Command("generate", "Generate a playlist from media files");
        var pathArg = new Argument<string>("path");
        var recursiveOpt = new Option<bool>("Scan subdirectories", ["--recursive", "-r"]);
        var extensionsOpt = new Option<string>("Comma-separated file extensions", ["--extensions", "-e"]) { DefaultValueFactory = _ => ".mp4,.mkv,.avi" };
        var outputOpt = new Option<string>("Output file", ["--output", "-o"]) { DefaultValueFactory = _ => "playlist.m3u" };

        cmd.Arguments.Add(pathArg);
        cmd.Options.Add(recursiveOpt);
        cmd.Options.Add(extensionsOpt);
        cmd.Options.Add(outputOpt);

        cmd.SetAction(async (parseResult, token) =>
        {
            if (parseResult.Errors.Count == 0)
            {
                PlaylistService.Generate(parseResult.GetValue(pathArg), parseResult.GetValue(outputOpt), parseResult.GetValue(extensionsOpt), parseResult.GetValue(recursiveOpt));
                return 0;
            }
            foreach (ParseError parseError in parseResult.Errors)
            {
                Console.Error.WriteLine(parseError.Message);
            }
            return 1;
        });

        // cmd.SetAction((DirectoryInfo path, bool recursive, string extensions, FileInfo output) =>
        // {
        //     var gen = new PlaylistGenerator();
        //     var exts = extensions.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        //     gen.Generate(path.FullName, recursive, exts, output.FullName);
        // }, pathArg, recursiveOpt, extensionsOpt, outputOpt);

        return cmd;
    }
}