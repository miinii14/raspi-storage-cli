using RaspiStorage.Cli.Api;
using RaspiStorage.Cli.DTOs;
using RaspiStorage.Cli.Exceptions;

namespace RaspiStorage.Cli.Shell;

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
        if (args is [])
            throw new CliException("rm", "No path provided.");

        if(args.Length > 1)
            throw new CliException("rm", "Too many arguments.");

        var slash = args[0].LastIndexOf('/');
        var folders  = args[0][..(slash + 1)];
        var fileName = args[0][(slash + 1)..];

        if(fileName == "")
            throw new CliException("rm" ,"No file name provided.");

        var foldersSequence = await PathResolver.ResolveAsync(folders, ctx, ct, _foldersApi, "rm");
        
        var files = await _filesApi.GetByFolderIdAsync(foldersSequence.LastOrDefault()?.Id, ct);

        var match = files.FirstOrDefault(f => f.Name == fileName) ?? throw new CliException("rm", $"File '{fileName}' not found.");

        await _filesApi.DeleteAsync(match.Id, ct);
    }
}