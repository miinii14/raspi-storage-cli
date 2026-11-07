using RaspiStorage.Cli.Api;
using RaspiStorage.Cli.DTOs;
using RaspiStorage.Cli.Exceptions;

namespace RaspiStorage.Cli.Shell;

public class CdCommand : ICommand
{
    public string Description => "Move to folder's localization";

    private readonly FoldersApi _foldersApi;

    public CdCommand(FoldersApi foldersApi)
    {
        _foldersApi = foldersApi;
    }

    public async Task ExecuteAsync(string[] args, Context ctx, CancellationToken ct)
    {
        if (args is [])
            throw new CliException("cd", "No path provided.");

        if(args.Length > 1)
            throw new CliException("cd", "Too many arguments.");

        ctx.Folders = await PathResolver.ResolveAsync(args[0], ctx, ct, _foldersApi, "cd");
    }
}