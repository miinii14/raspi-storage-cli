using RaspiStorage.Cli.Api;
using RaspiStorage.Cli.Exceptions;

namespace RaspiStorage.Cli.Shell.Commands;

public class CdCommand : ICommand
{
    public string Usage => "cd <path>";
    public string Description => "Change current folder";

    private readonly FoldersApi _foldersApi;

    public CdCommand(FoldersApi foldersApi)
    {
        _foldersApi = foldersApi;
    }

    public async Task ExecuteAsync(string[] args, Context ctx, CancellationToken ct)
    {
        if (args.Length != 1)
            throw new CliException("cd", $"Usage: {Usage}");

        ctx.Folders = await PathResolver.ResolveAsync(args[0], ctx, ct, _foldersApi, "cd");
    }
}