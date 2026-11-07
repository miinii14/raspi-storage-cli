using RaspiStorage.Cli.Api;
using RaspiStorage.Cli.DTOs;
using RaspiStorage.Cli.Exceptions;

namespace RaspiStorage.Cli.Shell;

public static class PathResolver
{
    public static async Task<List<FolderDto>> ResolveAsync(string path, Context ctx, CancellationToken ct, FoldersApi foldersApi, string commandName)
    {
        var folders = path.Split("/", StringSplitOptions.RemoveEmptyEntries);

        List<FolderDto> foldersSequence = [];
        
        if (!path.StartsWith('/'))
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

            if(folder == ".")
                continue;

            var children = await foldersApi.GetByParentIdAsync(foldersSequence.LastOrDefault()?.Id, ct);

            var targetFolder = children.FirstOrDefault(f => f.Name == folder) ?? throw new CliException(commandName, $"Folder '{folder}' not found.");

            foldersSequence.Add(targetFolder);
        }

        return foldersSequence;       
    }
}