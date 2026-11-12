using RaspiStorage.Cli.Api;
using RaspiStorage.Cli.DTOs;
using RaspiStorage.Cli.Exceptions;
using Spectre.Console;

namespace RaspiStorage.Cli.Shell.Commands;

public class MvCommand : ICommand
{
    public string Usage => "mv <path> <target-dir>";
    public string Description => "Move file or folder to another folder";
    private readonly FoldersApi _foldersApi;
    private readonly FilesApi _filesApi;

    public MvCommand(FoldersApi foldersApi, FilesApi filesApi)
    {
        _foldersApi = foldersApi;
        _filesApi = filesApi;
    }

    public async Task ExecuteAsync(string[] args, Context ctx, CancellationToken ct)
    {
        if (args.Length != 2)
            throw new CliException("mv", $"Usage: {Usage}");

        var (sourceParentPath, name) = PathResolver.Split(args[0], "mv");

        var target = await PathResolver.ResolveAsync(args[1], ctx, ct, _foldersApi, "mv");
        var targetId = target.LastOrDefault()?.Id;

        var sourceParent = await PathResolver.ResolveAsync(sourceParentPath, ctx, ct, _foldersApi, "mv");
        var sourceParentId = sourceParent.LastOrDefault()?.Id;

        var folders = await _foldersApi.GetByParentIdAsync(sourceParentId, ct);
        if (folders.FirstOrDefault(f => f.Name == name) is { } folder)
        {
            if (ctx.Folders.Any(f => f.Id == folder.Id))
                throw new CliException("mv", "Can't move the current folder or its parent.");

            await _foldersApi.MoveAsync(folder.Id, targetId, ct);
            PrintMoved(name + "/", target);
            return;
        }

        var files = await _filesApi.GetByFolderIdAsync(sourceParentId, ct);
        var file = files.FirstOrDefault(f => f.Name == name)
                   ?? throw new CliException("mv", $"'{name}' not found.");

        await _filesApi.MoveAsync(file.Id, targetId, ct);
        PrintMoved(name, target);
    }

    private static void PrintMoved(string name, IEnumerable<FolderDto> target) =>
        Output.Success($"Moved {Markup.Escape(name)} [grey]→[/] [bold blue]{Markup.Escape(PathResolver.ToPath(target))}[/]");
}
