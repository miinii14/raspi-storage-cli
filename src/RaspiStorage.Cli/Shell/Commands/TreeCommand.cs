using RaspiStorage.Cli.Api;
using RaspiStorage.Cli.DTOs;
using RaspiStorage.Cli.Exceptions;
using Spectre.Console;

namespace RaspiStorage.Cli.Shell.Commands;

public class TreeCommand : ICommand
{
    public string Usage => "tree [path]";
    public string Description => "Show folder structure recursively";
    private readonly FoldersApi _foldersApi;
    private readonly FilesApi _filesApi;
    private int _foldersCount;
    private int _filesCount;
    private long _totalBytes;

    public TreeCommand(FoldersApi foldersApi, FilesApi filesApi)
    {
        _foldersApi = foldersApi;
        _filesApi = filesApi;
    }

    private async Task BuildAsync(FolderDto? root, IHasTreeNodes node, StatusContext status, CancellationToken ct)
    {
        status.Status($"Scanning [bold]{Markup.Escape(root?.Name ?? "/")}[/] [grey]· {Plural(_foldersCount, "folder")}, {Plural(_filesCount, "file")}[/]");

        var folders = await _foldersApi.GetByParentIdAsync(root?.Id, ct);
        var files = await _filesApi.GetByFolderIdAsync(root?.Id, ct);

        foreach (var folder in folders.OrderBy(f => f.Name))
        {
            _foldersCount++;
            var newNode = node.AddNode($"[bold blue]{Markup.Escape(folder.Name)}/[/]");
            await BuildAsync(folder, newNode, status, ct);
        }

        foreach (var file in files.OrderBy(f => f.Name))
        {
            _filesCount++;
            _totalBytes += file.SizeBytes;
            node.AddNode($"{Markup.Escape(file.Name)} [grey]{Format.Size(file.SizeBytes)}[/]");
        }
    }
    
    public async Task ExecuteAsync(string[] args, Context ctx, CancellationToken ct)
    {
        if (args.Length > 1)
            throw new CliException("tree", $"Usage: {Usage}");

        _filesCount = 0;
        _foldersCount = 0;
        _totalBytes = 0;

        var parentFolder = args is []
            ? ctx.CurrentFolder
            : (await PathResolver.ResolveAsync(args[0], ctx, ct, _foldersApi, "tree")).LastOrDefault();
        
        var tree = new Tree($"[bold steelblue1]{Markup.Escape(parentFolder?.Name ?? "/")}[/]")
        {
            Guide = TreeGuide.Line,
            Style = Style.Parse("grey")
        };
        
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("steelblue1"))
            .StartAsync("Scanning...", async status =>
            {
                await BuildAsync(parentFolder, tree, status, ct);
            });
        
        AnsiConsole.Write(tree);
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[grey]{Plural(_foldersCount, "folder")}, {Plural(_filesCount, "file")} · {Format.Size(_totalBytes)}[/]");
    }

    private static string Plural(int count, string word) => $"{count} {word}{(count == 1 ? "" : "s")}";
}