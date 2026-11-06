using RaspiStorage.Cli.Api;
using RaspiStorage.Cli.DTOs;
using RaspiStorage.Cli.Exceptions;

namespace RaspiStorage.Cli.Shell;

public class CdCommand : ICommand
{
    public string Description => "Move to folder's localizaton";

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

        if(args[0] == "/")
        {
            ctx.Folders.Clear();
            return;
        }

        var folders = args[0].Split("/", StringSplitOptions.RemoveEmptyEntries);

        if(folders is [])
            throw new CliException("cd", "Bad argument.");

        List<FolderDto> foldersSequence = [];
        
        if (!args[0].StartsWith('/'))
        {
            foldersSequence = [.. ctx.Folders];
        }

        foreach(var folder in folders)
        {
            if(folder == "..")
            {
                if (foldersSequence.Count > 0)
                    foldersSequence.RemoveAt(foldersSequence.Count - 1);
                
                continue;
            }

            var children = await _foldersApi.GetByParentIdAsync(foldersSequence.LastOrDefault()?.Id, ct);

            var targetFolder = children.FirstOrDefault(f => f.Name == folder) ?? throw new CliException("cd", $"Folder '{folder}' not found.");

            foldersSequence.Add(targetFolder);
        }

        ctx.Folders = foldersSequence;
    }
}