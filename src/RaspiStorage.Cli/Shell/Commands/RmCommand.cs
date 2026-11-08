using RaspiStorage.Cli.Api;
using RaspiStorage.Cli.Exceptions;

namespace RaspiStorage.Cli.Shell.Commands;

public class RmCommand : ICommand
{
    public string Description { get; } = "Remove file";
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

        var (parentPath, fileName) = PathResolver.Split(args[0], "rm");

        var parent = await PathResolver.ResolveAsync(parentPath, ctx, ct, _foldersApi, "rm");

        var files = await _filesApi.GetByFolderIdAsync(parent.LastOrDefault()?.Id, ct);
        var file = files.FirstOrDefault(f => f.Name == fileName)
            ?? throw new CliException("rm", $"File '{fileName}' not found.");

        await _filesApi.DeleteAsync(file.Id, ct);
    }
}