using RaspiStorage.Cli.Api;
using RaspiStorage.Cli.Exceptions;
using Spectre.Console;

namespace RaspiStorage.Cli.Shell.Commands;

public class RenameCommand : ICommand
{
    public string Usage => "rename <path> <new-name>";
    public string Description => "Rename file or folder";
    private readonly FoldersApi _foldersApi;
    private readonly FilesApi _filesApi;

    public RenameCommand(FoldersApi foldersApi, FilesApi filesApi)
    {
        _foldersApi = foldersApi;
        _filesApi = filesApi;
    }

    public async Task ExecuteAsync(string[] args, Context ctx, CancellationToken ct)
    {
        if (args.Length != 2)
            throw new CliException("rename", $"Usage: {Usage}");

        var (parentPath, oldName) = PathResolver.Split(args[0], "rename");
        var newName = args[1];

        var parent = await PathResolver.ResolveAsync(parentPath, ctx, ct, _foldersApi, "rename");
        var parentId = parent.LastOrDefault()?.Id;

        var folders = await _foldersApi.GetByParentIdAsync(parentId, ct);
        if (folders.FirstOrDefault(f => f.Name == oldName) is { } folder)
        {
            var renamed = await _foldersApi.RenameAsync(folder.Id, newName, ct);

            var index = ctx.Folders.FindIndex(f => f.Id == renamed.Id);
            if (index >= 0)
                ctx.Folders[index] = renamed;

            PrintRenamed(oldName + "/", renamed.Name + "/");
            return;
        }

        var files = await _filesApi.GetByFolderIdAsync(parentId, ct);
        var file = files.FirstOrDefault(f => f.Name == oldName)
            ?? throw new CliException("rename", $"'{oldName}' not found.");

        var renamedFile = await _filesApi.RenameAsync(file.Id, newName, ct);
        PrintRenamed(oldName, renamedFile.Name);
    }

    private static void PrintRenamed(string oldName, string newName) =>
        Output.Success($"Renamed {Markup.Escape(oldName)} [grey]→[/] [bold]{Markup.Escape(newName)}[/]");
}
