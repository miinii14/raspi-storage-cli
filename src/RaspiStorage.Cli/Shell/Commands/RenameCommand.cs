using RaspiStorage.Cli.Api;
using RaspiStorage.Cli.Exceptions;

namespace RaspiStorage.Cli.Shell.Commands;

public class RenameCommand : ICommand
{
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
            throw new CliException("rename", "Usage: rename <path> <new-name>");

        var (parentPath, oldName) = PathResolver.Split(args[0], "rename");
        var newName = args[1];

        var parent = await PathResolver.ResolveAsync(parentPath, ctx, ct, _foldersApi, "rename");
        var parentId = parent.LastOrDefault()?.Id;

        var folders = await _foldersApi.GetByParentIdAsync(parentId, ct);
        if (folders.FirstOrDefault(f => f.Name == oldName) is { } folder)
        {
            await _foldersApi.RenameAsync(folder.Id, newName, ct);
            return;
        }

        var files = await _filesApi.GetByFolderIdAsync(parentId, ct);
        var file = files.FirstOrDefault(f => f.Name == oldName)
            ?? throw new CliException("rename", $"'{oldName}' not found.");

        await _filesApi.RenameAsync(file.Id, newName, ct);
    }
}
