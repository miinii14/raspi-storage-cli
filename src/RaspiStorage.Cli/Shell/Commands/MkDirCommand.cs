using RaspiStorage.Cli.Api;
using RaspiStorage.Cli.Exceptions;

namespace RaspiStorage.Cli.Shell.Commands;

public class MkDirCommand : ICommand
{
    public string Description => "Create folder";
    private readonly FoldersApi _foldersApi;

    public MkDirCommand(FoldersApi foldersApi)
    {
        _foldersApi = foldersApi;
    }

    public async Task ExecuteAsync(string[] args, Context ctx, CancellationToken ct)
    {
        if (args.Length != 1)
            throw new CliException("mkdir", "Usage: mkdir <path>");

        var (parentPath, folderName) = PathResolver.Split(args[0], "mkdir");

        var parent = await PathResolver.ResolveAsync(parentPath, ctx, ct, _foldersApi, "mkdir");

        await _foldersApi.CreateAsync(folderName, parent.LastOrDefault()?.Id, ct);
    }
}