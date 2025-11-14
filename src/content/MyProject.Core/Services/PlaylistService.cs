using System;
using System.IO;
using System.Linq;
using System.Text;

namespace MyProject.Core;

public static class MyProjectService
{
    public static void Generate(string directory, string? outputFile = null, string? extensions = null, bool recursive = true)
    {
        if (string.IsNullOrWhiteSpace(directory))
            throw new ArgumentException("Path is required", nameof(directory));

        if (!Directory.Exists(directory))
            throw new DirectoryNotFoundException(directory);

        var exts = !string.IsNullOrWhiteSpace(extensions) ? extensions.Split(",") : [".mp4", ".mkv", ".mp3", ".m4a", ".flac", ".avi"];
        var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var files = Directory.EnumerateFiles(directory, "*.*", option)
            .Where(f => exts.Any(e => f.EndsWith(e, StringComparison.OrdinalIgnoreCase)));

        var sb = new StringBuilder();
        sb.AppendLine("#EXTM3U");
        foreach (var f in files)
            sb.AppendLine(f);

        string outPath = outputFile ?? Path.Combine(directory, "playlist.m3u");
        File.WriteAllText(outPath, sb.ToString(), Encoding.UTF8);
        Console.WriteLine($"Playlist generated: {outPath}");
    }
}
