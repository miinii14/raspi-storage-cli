using RaspiStorage.Cli.Api;
using RaspiStorage.Cli.Exceptions;
using Spectre.Console;

namespace RaspiStorage.Cli.Shell.Commands;

public class RmCommand : ICommand
{
    public string Description { get; } = "Remove file or folder";
    private readonly FoldersApi _foldersApi;
    private readonly FilesApi _filesApi;
    
    public RmCommand(FoldersApi foldersApi, FilesApi filesApi)
    {
        _foldersApi = foldersApi;
        _filesApi = filesApi;
    }

    public async Task ExecuteAsync(string[] args, Context ctx, CancellationToken ct)
    {
        if (args.Length != 1)
            throw new CliException("rm", "Usage: rm <path>");

        var (parentPath, name) = PathResolver.Split(args[0], "rm");

        var parent = await PathResolver.ResolveAsync(parentPath, ctx, ct, _foldersApi, "rm");
        
        var folders = await _foldersApi.GetByParentIdAsync(parent.LastOrDefault()?.Id, ct);
        if (folders.FirstOrDefault(f => f.Name == name) is { } folder)
        {
            if (ctx.Folders.Any(f => f.Id == folder.Id))
                throw new CliException("rm", "Can't remove the current folder or its parent");
            
            if (!AnsiConsole.Confirm($"Delete folder '{Markup.Escape(folder.Name)}' and everything inside?", defaultValue: false))
                return;
            
            await _foldersApi.DeleteAsync(folder.Id, ct);
            return;
        }

        var files = await _filesApi.GetByFolderIdAsync(parent.LastOrDefault()?.Id, ct);
        var file = files.FirstOrDefault(f => f.Name == name)
            ?? throw new CliException("rm", $"'{name}' not found.");

        await _filesApi.DeleteAsync(file.Id, ct);
    }
}