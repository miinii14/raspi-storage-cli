using RaspiStorage.Cli.Api;
using RaspiStorage.Cli.Exceptions;

namespace RaspiStorage.Cli.Shell.Commands;

public class MvCommand : ICommand
{
    public string Description => "Move file or folder to another location";
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
            throw new CliException("mv", "Usage: mv <path> <path>");

        var (oldPath, name) = PathResolver.Split(args[0], "mv");
        
        var newParent = await PathResolver.ResolveAsync(args[1], ctx, ct, _foldersApi, "mv");
        var newParentId = newParent.LastOrDefault()?.Id ;
        
        var oldParent = await PathResolver.ResolveAsync(oldPath, ctx, ct, _foldersApi, "mv");
        var oldParentId = oldParent.LastOrDefault()?.Id;
        
        var folders = await _foldersApi.GetByParentIdAsync(oldParentId, ct);
        if (folders.FirstOrDefault(f => f.Name == name) is { } folder)
        {
            await _foldersApi.MoveAsync(folder.Id, newParentId, ct);
            return;
        }

        var files = await _filesApi.GetByFolderIdAsync(oldParentId, ct);
        var file = files.FirstOrDefault(f => f.Name == name)
                   ?? throw new CliException("mv", $"'{name}' not found.");
        
        await _filesApi.MoveAsync(file.Id, newParentId, ct);
    }
}