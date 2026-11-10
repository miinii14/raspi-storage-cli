using RaspiStorage.Cli.Api;
using RaspiStorage.Cli.DTOs;
using RaspiStorage.Cli.Exceptions;
using Spectre.Console;

namespace RaspiStorage.Cli.Shell.Commands;

public class LsCommand : ICommand
{
    public string Description => "List files and folders in current folder";
    private readonly FilesApi _filesApi;
    private readonly FoldersApi _foldersApi;

    public LsCommand(FilesApi filesApi, FoldersApi foldersApi)
    {
        _filesApi = filesApi;
        _foldersApi = foldersApi;
    }

    public async Task ExecuteAsync(string[] args, Context ctx, CancellationToken ct){
        if (args.Length > 1)
            throw new CliException("ls", "Usage: ls or ls <path>");

        Guid? parentFolderId;
        
        if (args is not [])
        {
            var path = await PathResolver.ResolveAsync(args[0], ctx, ct, _foldersApi, "ls");
            parentFolderId = path.LastOrDefault()?.Id;
        }
        else
        {
            parentFolderId = ctx.CurrentFolder?.Id;
        }
        
        var files = await _filesApi.GetByFolderIdAsync(parentFolderId, ct);
        var folders = await _foldersApi.GetByParentIdAsync(parentFolderId, ct);
    
        if (!files.Any() && !folders.Any())
        {
            AnsiConsole.MarkupLine("[grey]Folder is empty[/]");
            return;
        }

        PrintTable(files, folders);
    }

    private void PrintTable(IEnumerable<FileDto> files, IEnumerable<FolderDto> folders)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .AddColumn("[grey]Name[/]")
            .AddColumn("[grey]Size[/]")
            .AddColumn("[grey]Modified[/]");

        foreach (var folder in folders.OrderBy(f => f.Name))
            table.AddRow(
                new Markup($"[bold blue]{Markup.Escape(folder.Name)}/[/]"),
                new Markup("[grey]—[/]").RightJustified(),
                new Markup($"[grey]{folder.CreatedAt.ToLocalTime():yyyy-MM-dd HH:mm}[/]"));

        foreach (var file in files.OrderBy(f => f.Name))
            table.AddRow(
                new Markup(Markup.Escape(file.Name)),
                new Markup(Format.Size(file.SizeBytes)).RightJustified(),
                new Markup($"[grey]{file.UpdatedAt.ToLocalTime():yyyy-MM-dd HH:mm}[/]"));

        AnsiConsole.Write(table);
    }
}
